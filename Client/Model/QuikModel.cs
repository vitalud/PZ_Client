using Client.Service;
using Client.Service.Abstract;
using Client.Service.Interface;
using Client.Service.Sub;
using DynamicData;
using ProjectZeroLib.Enums;
using ProjectZeroLib.Utils;
using ReactiveUI;
using Serilog;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Client.Model
{
    public partial class QuikModel : ReactiveObject, IExchange
    {
        private readonly SubscriptionsRepository _subs;
        private readonly ILogger _logger;

        private readonly BurseName _name;
        private readonly IObservableList<Subscription> _subscriptions;
        private decimal _balance;
        private bool _isConnected;

        private readonly TcpListener _dataListener = new(IPAddress.Parse("127.0.0.2"), 50000);
        private TcpClient? _dataQuik;
        private NetworkStream? _dataStream;
        private readonly TcpListener _orderListener = new(IPAddress.Parse("127.0.0.1"), 50000);
        private TcpClient? _orderQuik;
        private NetworkStream? _orderStream;
        private readonly CancellationTokenSource _cts = new();

        private string _clientCode = string.Empty;
        private string _spbfut = string.Empty;
        private string _tqbr = string.Empty;
        private string _cets = string.Empty;

        #region private static fields
        private static readonly Dictionary<string, int[]> _sessions = new()
        {
            { "TQBR", new[] { 70000, 100000, 100000, 183945, 190500, 234945 } },
            { "SPBFUT", new[] { 90000, 135955, 140500, 184945, 190500, 234945 } },
            { "CETS", new[] { 100000, 185945, 100000, 185945, 100000, 185945 } }
        };

        private static readonly int[] _weekendSession = [100000, 185945, 100000, 185945, 100000, 185945];

        private static readonly HashSet<string> _exceptional_names =
        [
            "USDRUBF", "EURRUBF", "CNYRUBF", "GLDRUBF", "IMOEXF", "SBERF", "GAZPF"
        ];

        private static readonly char[] _common_months =
        [
            'H', 'H', 'H', 'M', 'M', 'M', 'U', 'U', 'U', 'Z', 'Z', 'Z'
        ];

        private static readonly char[] _br_months =
        [
            'G', 'H', 'J', 'K', 'M', 'N', 'Q', 'U', 'V', 'X', 'Z', 'F'
        ];

        private static readonly char[] _ng_months =
        [
            'F', 'G', 'H', 'J', 'K', 'M', 'N', 'Q', 'U', 'V', 'X', 'Z', 
        ];
        #endregion

        public BurseName Name => _name;
        public IObservableList<Subscription> Subscriptions => _subscriptions;
        public decimal Balance
        {
            get => _balance;
            set => this.RaiseAndSetIfChanged(ref _balance, value);
        }
        public bool IsConnected
        {
            get => _isConnected;
            set => this.RaiseAndSetIfChanged(ref _isConnected, value);
        }

        public QuikModel(SubscriptionsRepository subscriptions, ILogger logger, BurseName name)
        {
            _subs = subscriptions;
            _logger = logger;
            _name = name;

            _subscriptions = _subs.Subscriptions.Connect()
                .Filter(x => x.Name == name)
                .AsObservableList();

            Subscriptions.Connect()
                .Subscribe(OnSubscriptionsCountChanged);

            Connect();
        }

        /// <summary>
        /// Обрабатывает изменение количества подписок пользователя.
        /// TODO: добавить обработку удаления стратегии.
        /// </summary>
        /// <param name="changes">Изменения.</param>
        private void OnSubscriptionsCountChanged(IChangeSet<Subscription> changes)
        {
            foreach (var change in changes)
            {
                if (change.Reason.Equals(ListChangeReason.Add))
                {
                    var sub = change.Item.Current;
                    InitializeSubscription(sub);
                }
                else if (change.Reason.Equals(ListChangeReason.AddRange))
                {
                    foreach (var sub in change.Range)
                    {
                        InitializeSubscription(sub);
                    }
                }
            }
        }

        /// <summary>
        /// Регистрирует делегаты в стратегии пользователя.
        /// </summary>
        /// <param name="sub">Подписка.</param>
        private void InitializeSubscription(Subscription sub)
        {
            sub.RegisterPlaceOrderHandler(PlaceOrder);
            sub.RegisterCloseOrderHandler(CloseOrder);
            sub.RegisterGetPositionalPriceHandler(GetTickerPrice);
            sub.RegisterUpdateOrderPriceHandler(UpdateOrderPrice);
            sub.RegisterClosePositionsHandler(ClosePosition);
            //sub.RegisterCheckBalanceHandler(CheckBalance);

            _logger.Information($"[{Name}] Зарегистрирована подписка {sub.Code}");
        }

        /// <summary>
        /// Запускает прослушивание tcp подключений от коннектора Quik.
        /// </summary>
        public void Connect()
        {
            if (!_dataListener.Server.Connected)
            {
                _dataListener.Start();
                _orderListener.Start();
                _logger.Debug($"[{Name}] Ожидание подключения от коннектора Quik");

                _clientCode = ConfigService.GetKey("Quik", "ClientCode");
                _spbfut = ConfigService.GetKey("Quik", "SPBFUT");
                _tqbr = ConfigService.GetKey("Quik", "TQBR");
                _cets = ConfigService.GetKey("Quik", "CETS");

                Task.Run(() => GetQuikAsync(_cts.Token));
            }
        }

        /// <summary>
        /// Запускает цикл на ожидание подключения от коннектора.
        /// </summary>
        /// <returns></returns>
        private async Task GetQuikAsync(CancellationToken cancellationToken)
        {
            while (!IsConnected)
            {
                _dataQuik = await _dataListener.AcceptTcpClientAsync(cancellationToken);
                _orderQuik = await _orderListener.AcceptTcpClientAsync(cancellationToken);

                try
                {
                    _logger.Debug($"[{Name}] Подключен коннектор Quik");
                    await HandleQuikClientAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.Error($"[{Name}] Разрыв соединения с Quik: {ex.Message}");
                }
                await Task.Delay(25, cancellationToken);
            }
        }

        /// <summary>
        /// Обменивается данными с коннектором Quik.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task HandleQuikClientAsync(CancellationToken cancellationToken)
        {
            if (_dataQuik != null && _orderQuik != null)
            {
                _dataStream = _dataQuik.GetStream();
                _orderStream = _orderQuik.GetStream();
                IsConnected = true;
                try
                {
                    while (IsConnected)
                    {
                        var message = await TcpService.ReadMessageAsync(_dataStream, cancellationToken);

                        if (message != null && message != string.Empty)
                        {
                            GetQuikData(message);
                        }
                        else
                        {
                            IsConnected = false;
                        }
                        await Task.Delay(25, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"[{Name}] Ошибка чтения от коннектора Quik: {ex.Message}");
                }
                finally
                {
                    _dataQuik.Dispose();
                    _orderQuik.Dispose();
                    IsConnected = false;
                    _logger.Debug($"[{Name}] Отключен коннектор Quik");
                }
            }
        }





        private void GetQuikData(string message)
        {
            Debug.WriteLine(message);
        }

        private async Task SendMessage(QuikOrderMessage message)
        {
            if (_dataStream != null)
            {
                var data = Converter.CreateJson(message);
                var bytes = Encoding.UTF8.GetBytes(data);
                await _dataStream.WriteAsync(bytes);
            }
        }


        private async Task PlaceOrder(Order order)
        {
            if (!IsConnected) return;

            var account = ParseAccountName(order.Type);
            order.ClientOrderId = RandomNumberGenerator.GetInt32(1, int.MaxValue - 1).ToString();

            var message = new QuikOrderMessage()
            {
                Status = 1,
                TransactionId = order.ClientOrderId,
                ClientCode = _clientCode,
                Account = account,
                Type = order.Type,
                Id = order.Id,
                Action = order.Side.ToString()[..1],
                Price = order.Price,
                Balance = order.Size,
                OrderNum = 0,
            };

            await SendMessage(message);
        }

        private async Task CloseOrder(Order order)
        {
            if (!IsConnected) return;

            var account = ParseAccountName(order.Type);

            var message = new QuikOrderMessage()
            {
                Status = -1,
                TransactionId = order.ClientOrderId,
                ClientCode = _clientCode,
                Account = account,
                Type = order.Type,
                Id = order.Id,
                Action = order.Side.ToString()[..1],
                Price = order.Price,
                Balance = order.Size,
                OrderNum = order.OrderId,
            };

            await SendMessage(message);
        }

        private Task<decimal> GetTickerPrice(Order order, int pos)
        {
            return Task.FromResult(1m);
        }

        private Task UpdateOrderPrice(Order order)
        {
            return Task.CompletedTask;
        }

        private Task ClosePosition(Position position)
        {
            return Task.CompletedTask;
        }

        private Task SetLeverage(string id, int leverage)
        {
            return Task.CompletedTask;
        }


        private string ParseAccountName(string type)
        {
            return type switch
            {
                "SPBFUT" => _spbfut,
                "TQBR" => _tqbr,
                "CETS" => _cets,
                _ => string.Empty,
            };
        }

        /// <summary>
        /// Определяет, соответствует ли текущее время рабочей сессии.
        /// </summary>
        /// <param name="order">Заявка.</param>
        /// <returns>True - соответствует.</returns>
        private static bool IsWorkingSession(Order order)
        {
            int[] currentSession;
            var dateNow = DateTime.Now;

            if ((dateNow.DayOfWeek == DayOfWeek.Sunday || dateNow.DayOfWeek == DayOfWeek.Saturday) && order.Type == "TQBR")
            {
                currentSession = _weekendSession;
            }
            else
            {
                currentSession = _sessions[order.Type];
            }

            var date = dateNow.Hour * 10000 + dateNow.Minute * 100 + dateNow.Second;

            return (date > currentSession[1] && date < currentSession[2]) ||
                   (date > currentSession[3] && date < currentSession[4]) ||
                   (date > currentSession[5] && date < currentSession[6]);
        }

        /// <summary>
        /// Получает дату экспирации для формирования Id (SecCode).
        /// </summary>
        /// <param name="order">Заявка.</param>
        /// <returns> False - день экспирации.</returns>
        private bool GetExpirationDate(Order order)
        {
            var now = DateTime.Now;
            var quart_month = 3 * (int)Math.Ceiling(now.Month / 3.0);
            var lastDay = new DateTime(now.Year, quart_month, 1);

            if (order.Type == "SPBFUT" && !_exceptional_names.Contains(order.Id))
            {
                if (order.Id == "BR")
                {
                    lastDay = new DateTime(now.Year, now.Month + 1, 1);
                    for (int i = 0; i < 2; i++)
                    {
                        lastDay = lastDay.AddDays(-1);

                        while (lastDay.DayOfWeek == DayOfWeek.Saturday || lastDay.DayOfWeek == DayOfWeek.Sunday)
                        {
                            lastDay = lastDay.AddDays(-1);
                        }
                    }
                }
                else if (order.Id == "NG")
                {
                    lastDay = new DateTime(now.Year, now.Month + 1, 1);
                    for (int i = 0; i < 5; i++)
                    {
                        lastDay = lastDay.AddDays(-1);

                        while (lastDay.DayOfWeek == DayOfWeek.Saturday || lastDay.DayOfWeek == DayOfWeek.Sunday)
                        {
                            lastDay = lastDay.AddDays(-1);
                        }
                    }
                }
                else if (order.Id == "SF" || order.Id == "GD" || order.Id == "SV")
                {
                    while (lastDay.DayOfWeek != DayOfWeek.Friday)
                    {
                        lastDay = lastDay.AddDays(1);
                    }
                    lastDay = lastDay.AddDays(12);
                }
                else
                {
                    while (lastDay.DayOfWeek != DayOfWeek.Thursday)
                    {
                        lastDay = lastDay.AddDays(1);
                    }
                    lastDay = lastDay.AddDays(12);
                }

                var current = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                var nextMonth = new DateTime(lastDay.Year, lastDay.Month + 1, lastDay.Day);

                var day = (int)Math.Ceiling((double)current.Ticks / lastDay.Ticks);
                var month = day == 1 ? current.Month : nextMonth.Month;
                var year = day == 1 ? current.Year.ToString()[3] : nextMonth.Year.ToString()[3];

                var monthCode = _common_months[month];
                if (order.Id == "BR") monthCode = _br_months[month];
                else if (order.Id == "NG") monthCode = _ng_months[month];

                order.Id += monthCode + year;

                if (lastDay.ToString("yy:MM:dd") == nextMonth.ToString("yy:MM:dd")) return false;
            }

            return true;
        }

        private void ReadCashLog()
        {

        }

        public static double SumMas(IEnumerable<double> arr, bool mod = false)
        {
            double sum = 0;
            foreach (var item in arr)
            {
                sum += item;
            }

            if (!mod)
            {
                sum = Math.Floor(sum); // целочисленная сумма по умолчанию
            }

            return sum;
        }

        public async void Test()
        {
            var message = new QuikOrderMessage()
            {
                Status = 2,
                TransactionId = "1",
                ClientCode = "404WGDL",
                Account = "L01-00000F00",
                Type = "TQBR",
                Id = "MTLR",
                Action = "B",
                Price = 100,
                Balance = 1,
                OrderNum = 0,
            };

            await SendMessage(message);
        }
    }
}
