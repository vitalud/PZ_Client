using Client.Model.Crypto;
using Client.Service;
using Client.Service.Abstract;
using Client.Service.Sub;
using DynamicData;
using ProjectZeroLib.Enums;
using ReactiveUI;
using Serilog;

namespace Client.Model
{
    /// <summary>
    /// TODO: подключить реализации бирж как сервисы и обращаться к public методам бирж.
    /// Подумать над обновлением ордеров.
    /// </summary>
    public class MultiCryptoModel : ReactiveObject
    {
        private readonly CryptoModel _binance;
        private readonly CryptoModel _bybit;
        private readonly CryptoModel _okx;
        private readonly SubscriptionsRepository _subs;
        private readonly ILogger _logger;

        private readonly BurseName _name;
        private readonly IObservableList<Subscription> _subscriptions;

        public BurseName Name => _name;
        public IObservableList<Subscription> Subscriptions => _subscriptions;
        public bool BinanceStatus => _binance.IsConnected;
        public bool BybitStatus => _bybit.IsConnected;
        public bool OkxStatus => _okx.IsConnected;
        

        public MultiCryptoModel(BinanceModel binanceModel, BybitModel bybitModel, OkxModel okxModel, SubscriptionsRepository subscriptions, ILogger logger, BurseName name)
        {
            _binance = binanceModel;
            _bybit = bybitModel;
            _okx = okxModel;
            _subs = subscriptions;
            _logger = logger;
            _name = name;

            _subscriptions = _subs.Subscriptions.Connect()
                .Filter(x => x.Name == name)
                .AsObservableList();



            //Subscriptions.Connect()
            //    .Subscribe(OnSubscriptionsCountChanged);
        }

        public void Test()
        {

        }
    }
}
