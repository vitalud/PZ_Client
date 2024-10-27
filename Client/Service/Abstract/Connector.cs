using ReactiveUI;
using System.Diagnostics;

namespace Client.Service.Abstract
{
    public abstract class Connector : ReactiveObject
    {
        public event EventHandler<DataReceivedEventArgs> MessageReceived;

        protected readonly SubscriptionsService _subscriptions;

        protected readonly string address = ConfigService.GetIp();
        protected readonly int dataPort = 44444;
        protected readonly int authPort = 22222;
        protected readonly Guid sessionId = Guid.NewGuid();

        public Connector(SubscriptionsService subscriptions)
        {
            _subscriptions = subscriptions;
            MessageReceived += (sender, e) => MessageHandler(e.ReceivedData);
        }

        public abstract Task<bool> Authorization();
        protected abstract Task DataExchange();
        public abstract Task Close();
        protected abstract Task ReceiveData();
        protected abstract void OnStrategyStatusChanged(string code, bool status);
        protected abstract void MessageHandler(string message);
        protected virtual void OnMessageReceived(string data)
        {
            MessageReceived?.Invoke(this, new DataReceivedEventArgs(data));
        }
    }
    public class DataReceivedEventArgs(string data) : EventArgs
    {
        public string ReceivedData { get; } = data;
    }
}
