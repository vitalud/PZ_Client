using Client.Service.Abstract;
using DynamicData;
using Newtonsoft.Json;
using ProjectZeroLib;
using ProjectZeroLib.Enums;
using ProjectZeroLib.Signal;
using ProjectZeroLib.Utils;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace Client.Service.Sub
{
    public partial class Subscription : ReactiveObject
    {
        PlaceOrders? placeOrders;
        CloseOrders? closeOrders;
        GetTickerPrice? getTickerPrice;
        UpdateOrderPrice? updateOrderPrice;
        ClosePositions? closePositions;
        CheckBalance? checkBalance;

        private readonly List<Stock> _stocks = [];

        private readonly SourceList<Order> _trades = new();
        private readonly SourceList<Order> _orders = new();
        private readonly SourceList<Position> _positions = new();

        private readonly ReadOnlyObservableCollection<Order> _tradesTable;
        private readonly ReadOnlyObservableCollection<Order> _ordersTable;
        private readonly ReadOnlyObservableCollection<Position> _positionsTable;

        private decimal _tradeLimit = 0;
        private decimal _activeLimit = 0;
        private int _position = 10;
        private bool _isActive = false;
        private int _leverage = 1;
        private int _updateTime = 1;
        private decimal _profit = 0;
        private bool _holding = false;

        private Side _lastTradeSide;
        private Timer? _timer;

        public string DateFormat { get; } = "dd/MM HH:mm:ss";

        [JsonProperty(nameof(Name))]
        public BurseName Name { get; set; }

        [JsonProperty(nameof(Code))]
        public string Code { get; set; } = string.Empty;
        public string ClientOrderId { get; set; } = string.Empty;
        public string Types { get; set; } = string.Empty;
        public string Instruments { get; set; } = string.Empty;

        [JsonProperty("ClientLimit")]
        public decimal TradeLimit
        {
            get => _tradeLimit;
            set => this.RaiseAndSetIfChanged(ref _tradeLimit, value);
        }
        public decimal ActiveLimit
        {
            get => _activeLimit;
            set => this.RaiseAndSetIfChanged(ref _activeLimit, value);
        }
        public int Position
        {
            get => _position;
            set => this.RaiseAndSetIfChanged(ref _position, value);
        }
        public bool IsActive
        {
            get => _isActive;
            set => this.RaiseAndSetIfChanged(ref _isActive, value);
        }

        [JsonProperty(nameof(Leverage))]
        public int Leverage
        {
            get => _leverage;
            set => this.RaiseAndSetIfChanged(ref _leverage, value);
        }
        public int UpdateTime
        {
            get => _updateTime;
            set => this.RaiseAndSetIfChanged(ref _updateTime, value);
        }
        public decimal Profit
        {
            get => _profit;
            set => this.RaiseAndSetIfChanged(ref _profit, value);
        }
        public bool Holding
        {
            get => _holding;
            set => this.RaiseAndSetIfChanged(ref _holding, value);
        }

        [JsonProperty(nameof(Stocks))]
        public List<Stock> Stocks => _stocks;

        public SourceList<Order> Trades => _trades;
        public SourceList<Order> Orders => _orders;
        public SourceList<Position> Positions => _positions;

        public ReadOnlyObservableCollection<Order> TradesTable => _tradesTable;
        public ReadOnlyObservableCollection<Order> OrdersTable => _ordersTable;
        public ReadOnlyObservableCollection<Position> PositionsTable => _positionsTable;

        public Subscription()
        {
            _orders.Connect()
                    .Bind(out _ordersTable)
                    .Subscribe();
            _trades.Connect()
                    .Bind(out _tradesTable)
                    .Subscribe();
            _positions.Connect()
                    .Bind(out _positionsTable)
                    .Subscribe();

            InitializeSubscription();
        }

        /// <summary>
        /// Создает подписки на изменения данных.
        /// </summary>
        private void InitializeSubscription()
        {
            Orders.Connect()
                .Subscribe(OnOrdersListChanged);
            Trades.Connect()
                .Subscribe(OnTradesListChanged);
            Positions.Connect()
                .Subscribe(OnPositionsListChanged);

            this.WhenAnyValue(x => x.IsActive)
                .Skip(1)
                .Subscribe(value =>
                {
                    if (checkBalance != null && value)
                        IsActive = checkBalance.Invoke(TradeLimit);
                });

            this.WhenAnyValue(x => x.UpdateTime)
                .Skip(1)
                .Subscribe(value => ConfigService.SetInterval(Code, value));

            StartOrdersUpdating();
        }

        /// <summary>
        /// Обрабатывает изменение в коллекции ордеров.
        /// </summary>
        /// <param name="set">Изменения.</param>
        private void OnOrdersListChanged(IChangeSet<Order> set)
        {
            if (set == null) return;

            foreach (var change in set)
            {
                if (change.Reason == ListChangeReason.Add)
                    OnOrderAdded(change);
            }
        }

        /// <summary>
        /// Создает подписку на изменения данных, необходимых для
        /// рассчета ордеров, а также для заполнения таблицы трейдов
        /// при их исполнении.
        /// </summary>
        /// <param name="change">Изменения.</param>
        private void OnOrderAdded(Change<Order> change)
        {
            var item = change.Item.Current;

            item.WhenAnyValue(x => x.Status)
                .Skip(1)
                .Subscribe(status => OnOrderStatusChanged(item, status));
        }

        /// <summary>
        /// При исполнении ордера перемещает его в таблицу сделок 
        /// и формирует позицию.
        /// </summary>
        /// <param name="order">Ордер.</param>
        /// <param name="status">Статус ордера.</param>
        private void OnOrderStatusChanged(Order order, string status)
        {
            if (status == "Filled")
            {
                Trades.Add(order);
                Orders.Remove(order);

                CreatePosition(order);
            }
        }

        private void CreatePosition(Order order)
        {
            var clientOrderId = ClientOrderId + GetClientOrderId(32 - ClientOrderId.Length);

            var position = new Position(order.Id, order.Type, order.Burse, order.Size, order.Side, clientOrderId)
            {
                Price = order.Price
            };

            Positions.Add(position);
        }

        /// <summary>
        /// Обрабатывает изменение в коллекции трейдов.
        /// При добавлении трейда в коллекцию (исполнение ордера)
        /// пишется лог сделки.
        /// TODO: включить лог.
        /// </summary>
        /// <param name="set">Изменения.</param>
        private void OnTradesListChanged(IChangeSet<Order> set)
        {
            if (set == null) return;

            foreach (var change in set)
            {
                if (change.Reason.Equals(ListChangeReason.Add))
                {
                    var item = change.Item.Current;
                    //Logger.Log($"{item.Date}: {item.InstrumentId} {item.Side} {item.Size} {item.Price}", $"\\Trades\\", $"{item.InstrumentId}.txt");
                }
            }
        }

        /// <summary>
        /// Обрабатывает изменение в коллекции позиций.
        /// При удалении позиции из коллекции вычисляется профит 
        /// и добавляется в результат работы подписки.
        /// </summary>
        /// <param name="set">Изменения.</param>
        private void OnPositionsListChanged(IChangeSet<Position> set)
        {
            if (set == null) return;

            foreach (var change in set)
            {
                if (change.Reason.Equals(ListChangeReason.Remove))
                    OnPositionRemoved(change);
            }
        }

        /// <summary>
        /// Рассчитывает итоговый профит при закрытии позиции.
        /// TODO: добавить расчет коммиссии.
        /// </summary>
        /// <param name="change"></param>
        private void OnPositionRemoved(Change<Position> change)
        {
            var item = change.Item.Current;
            Profit += item.Profit;
        }

        /// <summary>
        /// Запускает с новой минуты задачу на обновление ордеров.
        /// </summary>
        private void StartOrdersUpdating()
        {
            _timer = new Timer(UpdateOrders, null, TimeSpan.Zero, TimeSpan.FromSeconds(UpdateTime));
        }

        /// <summary>
        /// Обновляет ордер по интервалу <see cref="UpdateTime" />.
        /// </summary>
        /// <param name="state"></param>
        private async void UpdateOrders(object? state)
        {
            if (!IsActive) return;

            if (getTickerPrice == null || updateOrderPrice == null) return;

            var tasks = Orders.Items.Select(async order =>
            {
                var price = await getTickerPrice.Invoke(order, Position);
                
                if (order.Price != price)
                {
                    SetPositionalPrice(order, price);
                    await updateOrderPrice.Invoke(order);
                }
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Формирует ордера в зависимости от текущего состояния подписки и нового сигнала.
        /// </summary>
        /// <param name="data">Новый сигнал.</param>
        /// <returns></returns>
        public async Task SignalHandler(SignalData data)
        {
            if (data.Stocks.Count != Stocks.Count || !data.Stocks.Any()) return;

            if (placeOrders == null || checkBalance == null) return;

            if (!OrderRule(data)) return;

            await CloseOrders();
            await ClosePositions();

            var tasks = data.Stocks.Select(GetOrder);

            await Task.WhenAll(tasks);

            await CalcOrderSize();

            if (checkBalance.Invoke(TradeLimit))
                await placeOrders.Invoke(Orders);
        }

        /// <summary>
        /// Обрабатывает входящие сигналы в соответствии с типом стратегии.
        /// TODO: Пока всего один тип, нет условной конструкции.
        /// </summary>
        /// <returns>True - сигнал подлежит обработке.</returns>
        private bool OrderRule(SignalData data)
        {
            if (data.Stocks.Count == 0) return false;

            var signal = data.Stocks.First();

            if (signal.Side != _lastTradeSide)
            {
                _lastTradeSide = data.Stocks[0].Side;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Закрывает неисполненные заявки.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private async Task CloseOrders()
        {
            if (closeOrders == null) 
                throw new ArgumentNullException(nameof(closeOrders));

            await closeOrders.Invoke(Orders);

            await UiInvoker.UiInvoke(Orders.Clear);
        }

        /// <summary>
        /// Закрывает позиции.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private async Task ClosePositions()
        {
            if (closePositions == null)
                throw new ArgumentNullException(nameof(closePositions));

            await closePositions.Invoke(Positions);

            await UiInvoker.UiInvoke(Positions.Clear);
        }

        /// <summary>
        /// Формирует ордер из сигнала и добавляет его в таблицу.
        /// </summary>
        /// <param name="stock"></param>
        /// <returns></returns>
        private async Task GetOrder(StockSignal stock)
        {
            await UiInvoker.UiInvoke(() =>
            {
                var temp = Stocks.FirstOrDefault(x =>
                x.Id == stock.Id &&
                x.Type == stock.Type &&
                x.Burse == stock.Burse);

                if (temp != null)
                {
                    var priceStep = temp.PriceStep;
                    var clientOrderId = ClientOrderId + GetClientOrderId(32 - ClientOrderId.Length);

                    var order = new Order(
                        stock.Id,
                        stock.Type,
                        stock.Burse,
                        clientOrderId,
                        stock.Side,
                        priceStep)
                    {
                        Date = DateTime.UtcNow.ToString(DateFormat),
                        Status = "Waiting"
                    };

                    Orders.Add(order);
                }
            });
        }

        /// <summary>
        /// Рассчитывает <see cref="Order.Size" /> для всех ордеров.
        /// Не учитывает Leverage.
        /// </summary>
        /// <returns></returns>
        private async Task CalcOrderSize()
        {
            if (Orders.Items.Count == 0) return;

            await GetPrices();

            var oneOrderLimit = TradeLimit / Orders.Items.Count;

            foreach (var order in Orders.Items)
            {
                var stock = Stocks.FirstOrDefault(x => 
                x.Id == order.Id &&
                x.Type == order.Type &&
                x.Burse == order.Burse);

                if (stock != null)
                {
                    var size = oneOrderLimit / (order.Price * stock.Equivalent);
                    order.Size = RoundDown(size, stock.Round);
                }
            }
        }

        /// <summary>
        /// Получает цены всех инструментов для формирования ордеров.
        /// </summary>
        /// <returns></returns>
        private async Task GetPrices()
        {
            if (getTickerPrice == null) return;

            var tasks = Orders.Items.Select(async order =>
            {
                var price = await getTickerPrice.Invoke(order, Position);
                SetPositionalPrice(order, price);
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Устанавливает цену ордера по позиции.
        /// TODO: добавить обработку Hold.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="price"></param>
        private void SetPositionalPrice(Order order, decimal price)
        {
            var offset = order.PriceStep * Position;

            if (order.Side == Side.Sell)
            {
                order.Price = price + offset;
            }
            else if (order.Side == Side.Buy)
            {
                order.Price = price - offset;
            }
        }

        /// <summary>
        /// TODO: вынести в отдельный класс
        /// </summary>
        /// <param name="value"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        public static decimal RoundDown(decimal value, int round)
        {
            var multiplier = (decimal)Math.Pow(10, round);

            return Math.Floor(value * multiplier) / multiplier;
        }
        /// <summary>
        /// TODO: вынести в отдельный класс
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static string GetClientOrderId(int x)
        {
            var pass = string.Empty;

            var rand = new Random();
            while (pass.Length < x)
            {
                char ch = (char)rand.Next(33, 125);
                if (char.IsLetterOrDigit(ch))
                    pass += ch;
            }
            return pass;
        }

        public void RegisterPlaceOrdersHandler(PlaceOrders del) => placeOrders = del;
        public void RegisterCloseOrdersHandler(CloseOrders del) => closeOrders = del;
        public void RegisterGetPositionalPriceHandler(GetTickerPrice del) => getTickerPrice = del;
        public void RegisterUpdateOrderPriceHandler(UpdateOrderPrice del) => updateOrderPrice = del;
        public void RegisterClosePositionsHandler(ClosePositions del) => closePositions = del;
        public void RegisterCheckBalanceHandler(CheckBalance del) => checkBalance = del;
    }
}
