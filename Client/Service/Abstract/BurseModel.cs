using Client.Service.Sub;
using DynamicData;
using ProjectZeroLib;
using ProjectZeroLib.Enums;
using ReactiveUI;
using System.Reactive.Linq;

namespace Client.Service.Abstract
{
    public delegate Task PlaceOrder(SourceList<Order> orders, decimal limit);
    public delegate void OrderUpdate(Order order, StockInfo stock);
    public delegate Task ClosePosition(Position position);
    public delegate bool CheckBalance(decimal limit);
    public abstract class BurseModel : ReactiveObject
    {
        private readonly SubscriptionsService _subs;

        protected object _lock = new();
        protected readonly string dateFormat = "dd/MM HH:mm:ss";
        protected string[] keys;

        private decimal _balance;
        public decimal Balance
        {
            get => _balance;
            set => this.RaiseAndSetIfChanged(ref _balance, value);
        }

        private BurseName _name;
        public BurseName Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => this.RaiseAndSetIfChanged(ref _isConnected, value);
        }

        private readonly IObservableList<Subscription> _subscriptions;
        public IObservableList<Subscription> Subscriptions => _subscriptions;

        protected BurseModel(SubscriptionsService subscriptions, BurseName name)
        {
            _subs = subscriptions;
            _name = name;

            _subscriptions = _subs.Subscriptions.Connect()
                .Filter(x => x.Name.Equals(name))
                .AsObservableList();

            Subscriptions.Connect()
                .Subscribe(OnSubsCountChanged);
        }

        private void OnSubsCountChanged(IChangeSet<Subscription> changes)
        {
            lock (_lock)
            {
                foreach (var change in changes)
                {
                    if (change.Reason.Equals(ListChangeReason.Add))
                    {
                        var sub = change.Item.Current;
                        RegisterStrategy(sub);
                    }
                    else if (change.Reason.Equals(ListChangeReason.AddRange))
                    {
                        foreach (var sub in change.Range)
                        {
                            RegisterStrategy(sub);
                        }
                    }
                }
            }
        }
        private void RegisterStrategy(Subscription sub)
        {
            ConfigService.GetInterval(sub);
            sub.ClientOrderId = ConfigService.GetLogin() + sub.Code;
            var index = 0;
            foreach (var stock in sub.Stocks)
            {
                sub.InstrumentIds += stock.InstrumentId + "\t";
                sub.InstrumentTypes += stock.InstrumentType + "\t";
                stock.ClientOrderId = sub.ClientOrderId + index;
                index++;
            }
            sub.RegisterPlaceOrderHandler(PlaceOrder);
            sub.RegisterOrderUpdateHandler(UpdateOrderByTime);
            sub.RegisterClosePositionHandler(ClosePosition);
            sub.RegisterCheckBalanceHandler(CheckBalanceOnStart);

            sub.WhenAnyValue(x => x.Signal)
                .Skip(1)
                .Subscribe(signal => GetOrdersFromSignal(sub, signal));
        }
        protected void GetOrdersFromSignal(Subscription sub, Signal signal)
        {
            lock (sub)
            {
                if (sub.Orders.Items.Count > 0 && sub.Orders.Items.Count < signal.Stocks.Count)
                {
                    //не все ордера исполнены
                }
                else if (signal.Stocks.Count > 0)
                {
                    for (int i = 0; i < signal.Stocks.Count; i++)
                    {
                        var order = signal.Stocks[i];
                        var _ = sub.Orders.Items.FirstOrDefault(x => (x.InstrumentId, x.InstrumentType).Equals((order.InstrumentId, order.InstrumentType)));
                        if (_ != null)
                        {
                            Logger.UiInvoke(() =>
                            {
                                _.Date = DateTime.UtcNow.ToString(dateFormat);
                                _.Side = order.Signal;
                                _.Price = order.OpenPrice;
                            });
                        }
                        else
                        {
                            Logger.UiInvoke(() =>
                            {
                                sub.Orders.Add(new()
                                {
                                    Date = DateTime.UtcNow.ToString(dateFormat),
                                    InstrumentId = order.InstrumentId,
                                    InstrumentType = order.InstrumentType,
                                    ClientOrderId = sub.ClientOrderId + i,
                                    Price = order.OpenPrice,
                                    Side = order.Signal,
                                    Status = "Waiting"
                                });
                            });
                        }
                    }
                }
            }
        }

        public async void Connect()
        {
            IsConnected = await GetConnection();
            if (IsConnected)
            {
                await SetupSocket();
            }
        }

        protected abstract Task PlaceOrder(SourceList<Order> orders, decimal limit);
        protected abstract void UpdateOrderByTime(Order order, StockInfo stock);
        protected abstract Task ClosePosition(Position position);
        protected abstract bool CheckBalanceOnStart(decimal limit);
        protected abstract Task SetupSocket();
        protected abstract Task<bool> GetConnection();
        protected abstract Task GetOrders();
        public abstract void Test();
    }
}
