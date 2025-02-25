using ReactiveUI;

namespace Client.Service.Abstract
{
    /// <summary>
    /// Абстрактный класс, описывающий методы обмена данными с сервером.
    /// </summary>
    public abstract class Connector(SubscriptionsRepository subscriptions) : ReactiveObject
    {
        protected readonly SubscriptionsRepository _subscriptions = subscriptions;

        protected readonly string address = ConfigService.GetIp();
        protected readonly int dataPort = 49107;
        protected readonly int authPort = 29019;
        protected readonly Guid sessionId = Guid.NewGuid();

        /// <summary>
        /// Отсылает запрос на аутентификацию на сервере.
        /// </summary>
        /// <returns></returns>
        public abstract Task<bool> Authentication();

        /// <summary>
        /// Закрывает обмен данными с сервером.
        /// TODO: проверить необходимость.
        /// </summary>
        /// <returns></returns>
        public abstract Task Close();

        /// <summary>
        /// Запускает обмен данными после удачной аутентификации.
        /// </summary>
        /// <returns></returns>
        protected abstract Task DataExchange();

        /// <summary>
        /// Обрабатывает входящие сообщения от сервера.
        /// </summary>
        /// <returns></returns>
        protected abstract Task ReceiveData();

        /// <summary>
        /// Отправляет сообщение на сервер.
        /// </summary>
        /// <param name="message">Сообщение от сервера.</param>
        /// <returns></returns>
        protected abstract Task SendMessage(string message);

        /// <summary>
        /// Определяет тип сообщения от сервера и выполняет связанное с ним действие.
        /// </summary>
        /// <param name="message">Сообщение от сервера.</param>
        protected abstract Task MessageHandler(string message);

        /// <summary>
        /// Отправляет сообщение серверу об изменении статуса подписки.
        /// </summary>
        /// <param name="code">Код подписки.</param>
        /// <param name="status">Статус подписки.</param>
        protected abstract void OnSubscriptionStatusChanged(string code, bool status);
    }
}
