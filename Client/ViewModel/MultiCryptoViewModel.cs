using Client.Model;
using Client.Service.Abstract;
using Client.Service.Sub;
using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;

namespace Client.ViewModel
{
    public class MultiCryptoViewModel : ReactiveObject
    {
        private readonly MultiCryptoModel _multiCryptoModel;

        private readonly ReadOnlyObservableCollection<Subscription> _subscriptions;
        private Subscription _selectedSub = new();

        public ReadOnlyObservableCollection<Subscription> Subscriptions => _subscriptions;
        public Subscription SelectedSub
        {
            get => _selectedSub;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedSub, value);
                this.RaisePropertyChanged(nameof(SelectedOrders));
                this.RaisePropertyChanged(nameof(SelectedTrades));
                this.RaisePropertyChanged(nameof(SelectedPositions));
            }
        }
        public ReadOnlyObservableCollection<Order> SelectedOrders => SelectedSub.OrdersTable;
        public ReadOnlyObservableCollection<Order> SelectedTrades => SelectedSub.TradesTable;
        public ReadOnlyObservableCollection<Position> SelectedPositions => SelectedSub.PositionsTable;

        public bool BinanceStatus => _multiCryptoModel.BinanceStatus;
        public bool BybitStatus => _multiCryptoModel.BybitStatus;
        public bool OkxStatus => _multiCryptoModel.OkxStatus;

        public ReactiveCommand<Unit, Unit> TestCommand { get; }

        public MultiCryptoViewModel(MultiCryptoModel multiCrypto) 
        {
            _multiCryptoModel = multiCrypto;

            _multiCryptoModel.Subscriptions.Connect()
                .Bind(out _subscriptions)
                .Subscribe();

            TestCommand = ReactiveCommand.Create(_multiCryptoModel.Test);
        }
    }
}
