using Client.Service.Sub;
using DynamicData;
using ProjectZeroLib.Enums;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace Client.Service.Abstract
{
    public partial class BurseViewModel : ReactiveObject
    {
        protected readonly BurseModel _burseModel;

        private readonly ReadOnlyObservableCollection<Subscription> _subscriptions;
        private Subscription _selectedSub = new();

        private decimal _balance;
        private bool _isConnected;

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

        public BurseName Name
        {
            get => _burseModel.Name;
            set => _burseModel.Name = value;
        }
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

        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
        public ReactiveCommand<Unit, Unit> TestCommand { get; }

        public BurseViewModel(BurseModel burseModel)
        {
            _burseModel = burseModel;

            _burseModel.Subscriptions.Connect()
                .Bind(out _subscriptions)
                .Subscribe();

            Balance = _burseModel.Balance;
            _burseModel.WhenAnyValue(x => x.Balance)
                .Subscribe(value => Balance = value);

            IsConnected = _burseModel.IsConnected;
            _burseModel.WhenAnyValue(x => x.IsConnected)
                .Subscribe(value => IsConnected = value);

            ConnectCommand = ReactiveCommand.Create(_burseModel.Connect);
            TestCommand = ReactiveCommand.Create(_burseModel.Test);
        }
    }
}
