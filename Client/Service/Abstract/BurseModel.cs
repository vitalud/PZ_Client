using Client.Service.Sub;
using DynamicData;
using ProjectZeroLib.Enums;
using ReactiveUI;
using System.Reactive.Linq;

namespace Client.Service.Abstract
{
    public delegate Task PlaceOrders(SourceList<Order> orders);
    public delegate Task CloseOrders(SourceList<Order> orders);
    public delegate Task<decimal> GetTickerPrice(Order order, int pos);
    public delegate Task UpdateOrderPrice(Order order);
    public delegate Task ClosePositions(SourceList<Position> positions);
    public delegate bool CheckBalance(decimal limit);

    public abstract class BurseModel : ReactiveObject
    {
        private readonly SubscriptionsRepository _subs;

        protected object _lock = new();
        protected string[] keys = [];

        private readonly IObservableList<Subscription> _subscriptions;
        private decimal _balance;
        private BurseName _name;
        private bool _isConnected;

        public IObservableList<Subscription> Subscriptions => _subscriptions;
        public decimal Balance
        {
            get => _balance;
            set => this.RaiseAndSetIfChanged(ref _balance, value);
        }
        public BurseName Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }
        public bool IsConnected
        {
            get => _isConnected;
            set => this.RaiseAndSetIfChanged(ref _isConnected, value);
        }

        protected BurseModel(SubscriptionsRepository subscriptions, BurseName name)
        {
            _subs = subscriptions;
            _name = name;

            _subscriptions = _subs.Subscriptions.Connect()
                .Filter(x => x.Name == name)
                .AsObservableList();

            InitializeData();
        }

        /// <summary>
        /// Устанавливает подписки на изменения данных.
        /// </summary>
        private void InitializeData()
        {
            Subscriptions.Connect()
                .Subscribe(OnSubscriptionsCountChanged);

            this.WhenAnyValue(x => x.Balance)
                .Skip(1)
                .Subscribe(value => Balance = Math.Round(value, 3));
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
            sub.RegisterPlaceOrdersHandler(PlaceOrders);
            sub.RegisterCloseOrdersHandler(CloseOrders);
            sub.RegisterGetPositionalPriceHandler(GetTickerPrice);
            sub.RegisterUpdateOrderPriceHandler(UpdateOrderPrice);
            sub.RegisterClosePositionsHandler(ClosePositions);
            sub.RegisterCheckBalanceHandler(CheckBalance);
        }

        /// <summary>
        /// Устанавливает соединение с биржей.
        /// </summary>
        public async void Connect()
        {
            if (IsConnected) return;

            IsConnected = await GetConnection();
            if (IsConnected)
                await SetupSocket();
        }

        /// <summary>
        /// Устанавливает соединение с биржей.
        /// </summary>
        /// <returns>True - соединение установлено.</returns>
        protected abstract Task<bool> GetConnection();

        /// <summary>
        /// Создает подписки на потоки данных по ордерам и балансу.
        /// </summary>
        /// <returns></returns>
        protected abstract Task SetupSocket();

        /// <summary>
        /// Выставляет сформированные ордеры из подписки.
        /// </summary>
        /// <param name="orders">Ордеры из подписки.</param>
        /// <param name="limit">Общий торговый лимит.</param>
        /// <returns></returns>
        protected abstract Task PlaceOrders(SourceList<Order> orders);

        /// <summary>
        /// Закрывает нереализованные ордеры в подписке.
        /// </summary>
        /// <param name="orders">Ордеры из подписки.</param>
        /// <param name="limit">Общий торговый лимит.</param>
        /// <returns></returns>
        protected abstract Task CloseOrders(SourceList<Order> orders);

        /// <summary>
        /// Получает цену ордера на определенной позиции.
        /// </summary>
        /// <param name="order">Ордер.</param>
        /// <param name="stock">Данные по инструменту.</param>
        /// <param name="position">Позиция для расчета цены.</param>
        protected abstract Task<decimal> GetTickerPrice(Order order, int pos);

        /// <summary>
        /// Обновляет цену ордера по интервалу подписки.
        /// </summary>
        /// <param name="order">Ордер.</param>
        /// <param name="stock">Данные по инструменту.</param>
        /// <param name="position">Позиция для расчета цены.</param>
        protected abstract Task UpdateOrderPrice(Order order);

        /// <summary>
        /// Закрывает позицию.
        /// </summary>
        /// <param name="position">Позиция.</param>
        /// <param name="clientOrderId">Уникальный id ордера.</param>
        /// <returns></returns>
        protected abstract Task ClosePositions(SourceList<Position> positions);

        /// <summary>
        /// TODO: проверить функционал
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        private bool CheckBalance(decimal limit)
        {
            return Balance >= limit;
        }

        /// <summary>
        /// Рассчитывает профит при закрытии позиции на основе комиссии биржи.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="price"></param>
        protected abstract void CalcProfit(Position position, decimal price);

        public abstract void Test();
    }
}
