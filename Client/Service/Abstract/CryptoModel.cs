using Client.Service.Interface;
using Client.Service.Sub;
using DynamicData;
using ProjectZeroLib.Enums;
using ProjectZeroLib.Utils;
using ReactiveUI;
using Serilog;
using System.Reactive.Linq;

namespace Client.Service.Abstract
{
    public delegate Task PlaceOrder(Order order);
    public delegate Task CloseOrder(Order order);
    public delegate Task<decimal> GetTickerPrice(Order order, int pos);
    public delegate Task UpdateOrderPrice(Order order);
    public delegate Task ClosePosition(Position position);
    public delegate bool CheckBalance(decimal limit);

    public abstract class CryptoModel : ReactiveObject, IExchange
    {
        private readonly SubscriptionsRepository _subs;
        protected readonly ILogger _logger;

        protected string[] keys = [];

        private readonly BurseName _name;

        private readonly IObservableList<Subscription> _subscriptions;
        private decimal _balance;
        private bool _isConnected;

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

        protected CryptoModel(SubscriptionsRepository subscriptions, ILogger logger, BurseName name)
        {
            _subs = subscriptions;
            _logger = logger;
            _name = name;

            _subscriptions = _subs.Subscriptions.Connect()
                .Filter(x => x.Name == name)
                .AsObservableList();

            Subscriptions.Connect()
                .Subscribe(OnSubscriptionsCountChanged);
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
        /// TODO: для мульти своя реализация.
        /// </summary>
        /// <param name="sub">Подписка.</param>
        private void InitializeSubscription(Subscription sub)
        {
            if (sub.Name == BurseName.Multi) return;

            sub.RegisterPlaceOrderHandler(PlaceOrder);
            sub.RegisterCloseOrderHandler(CloseOrder);
            sub.RegisterGetPositionalPriceHandler(GetTickerPrice);
            sub.RegisterUpdateOrderPriceHandler(UpdateOrderPrice);
            sub.RegisterClosePositionsHandler(ClosePosition);
            sub.RegisterCheckBalanceHandler(CheckBalance);

            _logger.Information($"[{Name}] Зарегистрирована подписка {sub.Code}");
        }

        /// <summary>
        /// Устанавливает соединение с биржей.
        /// </summary>
        public async void Connect()
        {
            if (IsConnected) return;

            _logger.Information($"[{Name}] Подключение к бирже");
            IsConnected = await SubscribeToUpdates();

            if (IsConnected) _logger.Information($"[{Name}] Созданы подписки на обновления");
            else _logger.Information($"[{Name}] Ошибка подключения");
        }

        /// <summary>
        /// Обрабатывает ответ от биржи при изменении ордера.
        /// </summary>
        /// <param name="response">Ответ от биржи.</param>
        /// <returns></returns>
        protected async Task OnOrderUpdated(OrderUpdateResponse response)
        {
            var type = response.ClientOrderId[..2];
            if (type != "st") return;

            var code = response.ClientOrderId.Substring(2, 4);

            var sub = Subscriptions.Items.FirstOrDefault(x => x.Code == code);
            if (sub == null) return;

            _logger.Information($"[{Name}] Получено обновление по ордеру {response.ClientOrderId}");

            var order = sub.Orders.Items.FirstOrDefault(x => x.ClientOrderId == response.ClientOrderId);
            if (order != null)
            {
                await UiInvoker.UiInvoke(() => ChangeOrderStatus(order, sub, response));
                return;
            }

            var position = sub.Positions.Items.FirstOrDefault(x => x.ClientOrderId == response.ClientOrderId);
            if (position != null)
            {
                //CalcProfit(position, response);
                await UiInvoker.UiInvoke(() => sub.Positions.Remove(position));
            }
        }

        /// <summary>
        /// Изменяет статус ордера в зависимости от ответа биржи.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="sub"></param>
        /// <param name="response"></param>
        private static void ChangeOrderStatus(Order order, Subscription sub, OrderUpdateResponse response)
        {
            if (response.State == OrderState.Live)
            {
                order.Date = response.UpdateTime.ToString(sub.DateFormat);

                if (order.Status == "Live")
                    order.Price = response.Price;
                else
                    order.Status = "Live";
            }
            else if (response.State == OrderState.PartiallyFilled)
            {

            }
            else if (response.State == OrderState.Filled)
            {

            }
        }

        /// <summary>
        /// Обрабатывает ответ от биржи при изменении данных аккаунта.
        /// </summary>
        /// <param name="balance"></param>
        protected void OnAccountUpdated(decimal balance)
        {
            Balance = Math.Round(balance, 3);
        }

        /// <summary>
        /// Подписывается на необходимые обновления сервера.
        /// </summary>
        /// <returns>True - все подписки успешны.</returns>
        protected abstract Task<bool> SubscribeToUpdates();

        /// <summary>
        /// Выставляет сформированный ордер из подписки.
        /// </summary>
        /// <param name="order">Ордер из подписки.</param>
        /// <returns></returns>
        public abstract Task PlaceOrder(Order order);

        /// <summary>
        /// Закрывает нереализованный ордер в подписке.
        /// </summary>
        /// <param name="order">Ордер из подписки.</param>
        /// <returns></returns>
        public abstract Task CloseOrder(Order order);

        /// <summary>
        /// Получает цену ордера на определенной позиции.
        /// </summary>
        /// <param name="order">Ордер.</param>
        /// <param name="pos">Позиция для расчета цены.</param>
        public abstract Task<decimal> GetTickerPrice(Order order, int pos);

        /// <summary>
        /// Обновляет цену ордера по интервалу подписки.
        /// </summary>
        /// <param name="order">Ордер.</param>
        public abstract Task UpdateOrderPrice(Order order);

        /// <summary>
        /// Закрывает позицию.
        /// </summary>
        /// <param name="position">Позиция.</param>
        /// <returns></returns>
        public abstract Task ClosePosition(Position position);

        /// <summary>
        /// Устанавливает торговое плечо.
        /// </summary>
        /// <param name="id">Инструмент.</param>
        /// <param name="leverage">Плечо.</param>
        /// <returns></returns>
        public abstract Task SetLeverage(string id, int leverage);

        /// <summary>
        /// TODO: проверить функционал
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        private bool CheckBalance(decimal limit)
        {
            return Balance >= limit;
        }

        public abstract void Test();
    }
}
