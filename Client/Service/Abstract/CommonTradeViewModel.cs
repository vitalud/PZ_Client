using Client.Service.Interface;
using Client.Service.Sub;
using DynamicData;
using ProjectZeroLib.Enums;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace Client.Service.Abstract
{
    public partial class CommonTradeViewModel : ReactiveObject
    {
        protected readonly IExchange _cryptoModel;

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

        public BurseName Name => _cryptoModel.Name;
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

        public CommonTradeViewModel(IExchange burseModel)
        {
            _cryptoModel = burseModel;

            _cryptoModel.Subscriptions.Connect()
                .Bind(out _subscriptions)
                .Subscribe();

            Balance = _cryptoModel.Balance;
            _cryptoModel.WhenAnyValue(x => x.Balance)
                .Subscribe(value => Balance = value);

            IsConnected = _cryptoModel.IsConnected;
            _cryptoModel.WhenAnyValue(x => x.IsConnected)
                .Subscribe(value => IsConnected = value);

            ConnectCommand = ReactiveCommand.Create(_cryptoModel.Connect);
            TestCommand = ReactiveCommand.Create(_cryptoModel.Test);
        }
    }
}
