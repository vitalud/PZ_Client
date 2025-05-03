using Client.Service.Abstract;
using ProjectZeroLib.Utils;
using Serilog;
using System.Net.Sockets;
using System.Text;

namespace Client.Service
{
    /// <summary>
    /// Класс, реализующий обмен данными с сервером через tcp соединение.
    /// </summary>
    /// <param name="subscriptions"></param>
    public partial class TcpConnector(SubscriptionsRepository subscriptions, ILogger logger) : Connector(subscriptions, logger)
    {
        private TcpClient? _auth;
        private TcpClient? _data;
        private NetworkStream? _stream;

        private readonly CancellationTokenSource _cts = new();

        public override async Task<bool> Authentication(string login, string password)
        {
            try
            {
                _logger.Information("Подключение к серверу аутентификации");
                _auth = new TcpClient(address, authPort);

                if (!_auth.Connected || !_auth.GetStream().CanRead)
                    return false;

                _stream = _auth.GetStream();
                var data = $"{login}_{password}";
                await SendMessage(data);

                var message = await TcpService.ReadMessageAsync(_stream, _cts.Token);
                if (message == null)
                    return false;

                var received = message.Split('_');
                if (received.Length < 2 || !received[0].Equals("auth") || !received[1].Equals("success"))
                    return false;
                _logger.Information($"Ответ от сервера: {received[1]}");

                await DataExchange(_cts.Token);
                return true;
            }
            catch (Exception ex) {
                _logger.Error(ex, "Ошибка авторизации");
                return false;
            }
        }

        public override async Task Close()
        {
            if (_stream != null)
            {
                _cts?.Cancel();
                await SendMessage("disconnect");
                _cts?.Dispose();
            }
        }

        protected override async Task DataExchange(CancellationToken cancellationToken)
        {
            _auth?.Dispose();
            _stream?.Dispose();

            _logger.Information("Подключение к серверу обмена данных");
            _data = new TcpClient(address, dataPort);

            if (_data.Connected && _data.GetStream().CanRead)
            {
                _stream = _data.GetStream();

                _ = Task.Run(() => ReceiveData(cancellationToken), cancellationToken);
                await SendMessage("strategy");
            }
        }

        protected override async Task ReceiveData(CancellationToken cancellationToken)
        {
            _logger.Information("Начало прослушивания входящих сообщений от сервера");
            while (_stream != null)
            {
                try
                {
                    var message = await TcpService.ReadMessageAsync(_stream, cancellationToken);
                    if (message != null)
                        await MessageHandler(message);
                }
                catch (OperationCanceledException)
                {
                    _logger.Information("Конец прослушивания входящих сообщений от сервера");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Ошибка чтения от сервера: {ex.Message}");
                }

                if (cancellationToken.IsCancellationRequested) break;
            }
        }

        protected override async Task SendMessage(string message)
        {
            if (_stream != null)
            {
                _logger.Debug($"Отправлено сообщение на сервер: {message}");
                var data = Converter.CreateMessage($"{message}_{sessionId}");
                var bytes = Encoding.UTF8.GetBytes(data);
                await _stream.WriteAsync(bytes);
            }
        }

        protected override async Task MessageHandler(string message)
        {
            var index = message.IndexOf('_');

            if (index == -1) return;

            var type = message[..index];
            var data = message[(index + 1)..];

            _logger.Debug($"Получено сообщение от сервера типа: {type}");
            if (type == "strategy")
                await _subscriptions.GetSubscription(data, OnSubscriptionStatusChanged);
            else if (type == "signal")
                await _subscriptions.GetSignal(data);
        }

        protected override async void OnSubscriptionStatusChanged(string code, bool status)
        {
            var message = $"status_{code}_{status}";
            await SendMessage(message);
        }
    }
}
