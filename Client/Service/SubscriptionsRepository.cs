using Client.Service.Sub;
using DynamicData;
using Newtonsoft.Json;
using ProjectZeroLib.Signal;
using ProjectZeroLib.Utils;
using ReactiveUI;
using System.Reactive.Linq;

namespace Client.Service
{
    /// <summary>
    /// Хранилище подписок.
    /// </summary>
    public class SubscriptionsRepository
    {
        private readonly SourceList<Subscription> _subscriptions = new();
        public SourceList<Subscription> Subscriptions => _subscriptions;

        /// <summary>
        /// Добавляет полученную от сервера подписку в хранилище.
        /// </summary>
        /// <param name="data">Данные подписки.</param>
        /// <param name="onStatusChanged">Метод обработки статуса подписки.</param>
        public async Task GetSubscription(string data, Action<string, bool> onStatusChanged)
        {
            var sub = JsonConvert.DeserializeObject<Subscription>(data);

            if (sub == null) return;

            ConfigService.GetInterval(sub);

            sub.ClientOrderId = $"st{sub.Code}";

            foreach (var stock in sub.Stocks)
            {
                sub.Instruments += stock.Id + '/';
                sub.Types += stock.Type + '/';
            }

            sub.Instruments = sub.Instruments.TrimEnd('/');
            sub.Types = sub.Types.TrimEnd('/');

            sub.WhenAnyValue(x => x.IsActive)
                .Skip(1)
                .Subscribe(_ => onStatusChanged(sub.Code, sub.IsActive));

            await UiInvoker.UiInvoke(() => Subscriptions.Add(sub));
        }

        /// <summary>
        /// Добавляет полученный от сервера сигнал в подписку.
        /// </summary>
        /// <param name="data">Данные сигнала.</param>
        public async Task GetSignal(string data)
        {
            var signal = JsonConvert.DeserializeObject<SignalData>(data);

            if (signal == null || signal.Signal == "ping") return;

            var sub = Subscriptions.Items.FirstOrDefault(x => x.Code == signal.Code);

            if (sub != null && sub.IsActive)
            {
                await sub.SignalHandler(signal);
            }
        }
    }
}
