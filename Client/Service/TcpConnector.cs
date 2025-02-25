using Client.Service.Abstract;
using ProjectZeroLib.Utils;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;

namespace Client.Service
{
    /// <summary>
    /// Класс, реализующий обмен данными с сервером через tcp соединение.
    /// </summary>
    /// <param name="subscriptions"></param>
    public partial class TcpConnector(SubscriptionsRepository subscriptions) : Connector(subscriptions)
    {
        private TcpClient? _auth;
        private TcpClient? _data;
        private NetworkStream? _stream;

        public override async Task<bool> Authentication()
        {
            _auth = new TcpClient(address, authPort);

            if (!_auth.Connected || !_auth.GetStream().CanRead) 
                return false;

            _stream = _auth.GetStream();
            var data = $"{ConfigService.GetLogin()}_{ConfigService.GetPassword()}";
            await SendMessage(data);

            var message = await TcpService.ReadMessageAsync(_stream);
            if (message == null) 
                return false;

            var received = message.Split('_');
            if (received.Length < 2 || !received[0].Equals("auth") || !received[1].Equals("success"))
                return false;

            await DataExchange();
            return true;
        }

        public override async Task Close()
        {
            if (_stream != null)
                await SendMessage("disconnect");
        }

        protected override async Task DataExchange()
        {
            _auth?.Dispose();
            _stream?.Dispose();

            _data = new TcpClient(address, dataPort);

            if (_data.Connected && _data.GetStream().CanRead)
            {
                _stream = _data.GetStream();

                _ = Task.Run(ReceiveData);
                await SendMessage("strategy");
            }
        }

        protected override async Task ReceiveData()
        {
            while (true)
            {
                if (_stream != null)
                {
                    var message = await TcpService.ReadMessageAsync(_stream);
                    if (message != null)
                        await MessageHandler(message);
                }
            }
        }

        protected override async Task SendMessage(string message)
        {
            if (_stream != null)
            {
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

            if (type == "strategy")
                await _subscriptions.GetSubscription(data, OnSubscriptionStatusChanged);
            else if (type == "signal")
                await _subscriptions.GetSignal(data);
        }

        /// <summary>
        /// TODO: Проверить, как считывает сообщение
        /// </summary>
        /// <param name="code"></param>
        /// <param name="status"></param>
        protected override async void OnSubscriptionStatusChanged(string code, bool status)
        {
            var message = $"status_{code}_{status}";
            await SendMessage(message);
        }
    }
}
