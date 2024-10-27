using Client.Service.Sub;
using DynamicData;
using ProjectZeroLib.Enums;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace Client.Service.Abstract
{
    public class BurseViewModel : ReactiveObject
    {
        protected readonly BurseModel _burseModel;

        private readonly ReadOnlyObservableCollection<Subscription> _subscriptions;
        public ReadOnlyObservableCollection<Subscription> Subscriptions => _subscriptions;

        private Subscription _selectedSub;
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

        public ReadOnlyObservableCollection<Order> SelectedOrders => SelectedSub?.OrdersTable;
        public ReadOnlyObservableCollection<Order> SelectedTrades => SelectedSub?.TradesTable;
        public ReadOnlyObservableCollection<Position> SelectedPositions => SelectedSub?.PositionsTable;


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

        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
        public ReactiveCommand<Unit, Unit> TestCommand { get; }

        public BurseViewModel(BurseModel burseModel)
        {
            _burseModel = burseModel;
            _burseModel.Subscriptions.Connect()
                .Bind(out _subscriptions)
                .Subscribe();

            Balance = _burseModel.Balance;
            _burseModel.WhenAnyValue(m => m.Balance)
                    .Subscribe(value => Balance = value);
            Name = _burseModel.Name;
            _burseModel.WhenAnyValue(m => m.Name)
                    .Subscribe(value => Name = value);
            IsConnected = _burseModel.IsConnected;
            _burseModel.WhenAnyValue(m => m.IsConnected)
                    .Subscribe(value => IsConnected = value);

            ConnectCommand = ReactiveCommand.Create(_burseModel.Connect);
            TestCommand = ReactiveCommand.Create(_burseModel.Test);
        }
    }
}
