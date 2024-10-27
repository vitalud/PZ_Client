using Client.Service.Abstract;
using Client.Service.Sub;
using DynamicData;
using Newtonsoft.Json;
using ProjectZeroLib;
using ProjectZeroLib.Enums;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace Client.Service
{
    public class Subscription : ReactiveObject
    {
        PlaceOrder? placeOrder;
        OrderUpdate? orderUpdate;
        ClosePosition? closePosition;
        CheckBalance? checkBalance;

        private decimal _tradeLimit = 0;
        private decimal _activeLimit = 0;
        private int _position = 10;
        private bool _isActive = false;
        private int _leverage;
        private int _updateTime = 1;
        private decimal _profit = 0;
        private bool _holding = false;

        [JsonProperty(nameof(Name))]
        public BurseName Name { get; set; }
        [JsonProperty(nameof(Code))]
        public string Code { get; set; }
        public string ClientOrderId { get; set; }
        public string InstrumentTypes { get; set; }
        public string InstrumentIds { get; set; }

        [JsonProperty("TempLimit")]
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

        private Signal _signal;
        public Signal Signal
        {
            get => _signal;
            set => this.RaiseAndSetIfChanged(ref _signal, value);
        }

        private ReadOnlyObservableCollection<Order> _tradesTable;
        private ReadOnlyObservableCollection<Order> _ordersTable;
        private ReadOnlyObservableCollection<Position> _positionsTable;

        public ReadOnlyObservableCollection<Order> TradesTable => _tradesTable;
        public ReadOnlyObservableCollection<Order> OrdersTable => _ordersTable;
        public ReadOnlyObservableCollection<Position> PositionsTable => _positionsTable;

        private SourceList<Order> _trades = new();
        private SourceList<Order> _orders = new();
        private SourceList<Position> _positions = new();

        public SourceList<Order> Trades
        {
            get => _trades;
            set => this.RaiseAndSetIfChanged(ref _trades, value);
        }
        public SourceList<Order> Orders
        {
            get => _orders;
            set => this.RaiseAndSetIfChanged(ref _orders, value);
        }
        public SourceList<Position> Positions
        {
            get => _positions;
            set => this.RaiseAndSetIfChanged(ref _positions, value);
        }



        private ObservableCollection<StockInfo> _stocks = [];

        [JsonProperty("BurseInfo")]
        public ObservableCollection<StockInfo> Stocks
        {
            get => _stocks;
            set => this.RaiseAndSetIfChanged(ref _stocks, value);
        }

        private Timer _timer;

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

            _orders.Connect()
                .Subscribe(OnOrdersListChanged);
            _trades.Connect()
                .Subscribe(OnTradesListChanged);
            _positions.Connect()
                .Subscribe(OnPositionsListChanged);


            this.WhenAnyValue(x => x.IsActive)
                .Skip(1)
                .Subscribe(IsActiveHandler);

            this.WhenAnyValue(x => x.Holding)
                .Skip(1)
                .Subscribe(value =>
                {
                    foreach (var stock in Stocks)
                        stock.Hold = value;
                });
            this.WhenAnyValue(x => x.Position)
                .Skip(1)
                .Subscribe(value =>
                {
                    foreach (var stock in Stocks)
                        stock.Position = value;
                });



            this.WhenAnyValue(x => x.UpdateTime)
                .Skip(1)
                .Subscribe(value => ConfigService.SetInterval(Code, value));

            SetUpdateOrdersTimer();
        }

        private void OnOrdersListChanged(IChangeSet<Order> set)
        {
            if (set != null)
            {
                foreach (var change in set)
                {
                    if (change.Reason.Equals(ListChangeReason.Add))
                        OnOrderAdded(change);
                }
            }
        }
        private void OnOrderAdded(Change<Order> change)
        {
            var item = change.Item.Current;
            item.WhenAnyValue(x => x.Status)
                .Skip(1)
                .Subscribe(status =>
                {
                    if (status.Equals("Filled"))
                    {
                        _trades.Add(item);
                        _orders.Remove(item);
                    }
                });
            item.WhenAnyValue(x => x.Side)
                .Subscribe(side =>
                {
                    OrderRule(item);
                });
        }
        private void OnTradesListChanged(IChangeSet<Order> set)
        {
            if (set != null)
            {
                foreach (var change in set)
                {
                    if (change.Reason.Equals(ListChangeReason.Add))
                    {
                        var item = change.Item.Current;
                        //Logger.Log($"{item.Date}: {item.InstrumentId} {item.Side} {item.Size} {item.Price}", $"\\Trades\\", $"{item.InstrumentId}.txt");
                    }
                }
            }
        }
        private void OnPositionsListChanged(IChangeSet<Position> set)
        {
            if (set != null)
            {
                foreach (var change in set)
                {
                    if (change.Reason.Equals(ListChangeReason.Remove))
                        OnPositionRemoved(change);
                }
            }
        }
        private void OnPositionRemoved(Change<Position> change)
        {
            var item = change.Item.Current;
            Profit += item.Profit;
        }

        private async Task OrderRule(Order order)
        {
            if (Stocks.Count.Equals(Orders.Count))
            {
                var trade = Trades.Items
                .Where(x => x.ClientOrderId.Equals(order.ClientOrderId))
                .OrderByDescending(x => x.Date)
                .FirstOrDefault();

                if (trade != null)
                {
                    if (order.Side.Equals(trade.Side))
                        return;
                    else
                    {
                        var position = Positions.Items.FirstOrDefault(x => x.InstrumentId.Equals(trade.InstrumentId));
                        if (position != null)
                        {
                            await closePosition?.Invoke(position);
                        }
                    }
                }
                await placeOrder?.Invoke(Orders, TradeLimit);
            }
        }
        private async void IsActiveHandler(bool active)
        {
            if (active)
            {
                IsActive = (bool)checkBalance?.Invoke(TradeLimit);
                if (!IsActive)
                    MessageBus.Current.SendMessage(new MessageToShow("Недостаточно средств на счете"));
            }
        }

        private void SetUpdateOrdersTimer()
        {
            var currentTime = DateTime.UtcNow;
            var secondsRemaining = 60 - currentTime.Second;
            var timeToNextMinute = TimeSpan.FromSeconds(secondsRemaining);
            _timer = new Timer(UpdateOrders, null, timeToNextMinute, TimeSpan.FromSeconds(UpdateTime));
        }
        private void UpdateOrders(object? state)
        {
            if (IsActive)
            {
                foreach (var item in Orders.Items)
                {
                    if (item.Status.Equals("Live"))
                    {
                        var stock = Stocks.FirstOrDefault(x => x.InstrumentId.Equals(item.InstrumentId) && x.InstrumentType.Equals(item.InstrumentType));
                        if (stock != null)
                            orderUpdate?.Invoke(item, stock);
                    }
                }
            }
        }

        public void RegisterPlaceOrderHandler(PlaceOrder del) => placeOrder = del;
        public void RegisterOrderUpdateHandler(OrderUpdate del) => orderUpdate = del;
        public void RegisterClosePositionHandler(ClosePosition del) => closePosition = del;
        public void RegisterCheckBalanceHandler(CheckBalance del) => checkBalance = del;
    }
}
