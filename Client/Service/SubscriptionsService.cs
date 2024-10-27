using CryptoExchange.Net.Sockets;
using DynamicData;
using Newtonsoft.Json;
using ProjectZeroLib;
using ProjectZeroLib.Enums;
using ReactiveUI;
using System.Reactive.Linq;

namespace Client.Service
{
    public class SubscriptionsService
    {
        private readonly object _lock = new();

        private readonly SourceList<Subscription> _subscriptions = new();
        public SourceList<Subscription> Subscriptions => _subscriptions;
        public void GetStrategy(Subscription sub)
        {
            Subscriptions.Add(sub);
        }
        public void GetSignal(string data)
        {
            if (data != null)
            {
                var signal = JsonConvert.DeserializeObject<Signal>(data);
                if (signal != null)
                {
                    var sub = Subscriptions.Items.FirstOrDefault(x => x.Code.Equals(signal.Code));
                    if (sub != null)
                    {
                        if (sub.IsActive)
                            sub.Signal = signal;
                    }
                }
            }
        }
    }
}
