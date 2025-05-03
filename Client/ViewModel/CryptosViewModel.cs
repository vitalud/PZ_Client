using Client.Service.Abstract;
using Client.ViewModel.Crypto;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace Client.ViewModel
{
    public partial class CryptosViewModel : ReactiveObject
    {
        private readonly CommonTradeViewModel _okxViewModel;
        private readonly CommonTradeViewModel _binanceViewModel;
        private readonly CommonTradeViewModel _bybitViewModel;

        private decimal _balance;
        private bool _isConnected;

        public CommonTradeViewModel OkxViewModel => _okxViewModel;
        public CommonTradeViewModel BinanceViewModel => _binanceViewModel;
        public CommonTradeViewModel BybitViewModel => _bybitViewModel;

        public ObservableCollection<CommonTradeViewModel> CryptosViewModels { get; }

        private CommonTradeViewModel _selectedCryptoViewModel;
        public CommonTradeViewModel SelectedCryptoViewModel
        {
            get => _selectedCryptoViewModel;
            set 
            {
                this.RaiseAndSetIfChanged(ref _selectedCryptoViewModel, value);
                this.RaisePropertyChanged(nameof(Balance));
                this.RaisePropertyChanged(nameof(IsConnected));
            }
        }

        public decimal Balance
        {
            get => _selectedCryptoViewModel.Balance;
            set => this.RaiseAndSetIfChanged(ref _balance, value);
        }
        public bool IsConnected
        {
            get => _selectedCryptoViewModel.IsConnected;
            set => this.RaiseAndSetIfChanged(ref _isConnected, value);
        }

        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
        public ReactiveCommand<Unit, Unit> TestCommand { get; }

        public CryptosViewModel(OkxViewModel okxViewModel, BinanceViewModel binanceViewModel, BybitViewModel bybitViewModel)
        {
            _okxViewModel = okxViewModel;
            _binanceViewModel = binanceViewModel;
            _bybitViewModel = bybitViewModel;

            _selectedCryptoViewModel = _okxViewModel;

            CryptosViewModels = [_okxViewModel, _binanceViewModel, _bybitViewModel];

            CreateSubs();

            ConnectCommand = ReactiveCommand.Create(Connect);
            TestCommand = ReactiveCommand.Create(Test);
        }

        private void CreateSubs()
        {
            foreach (var viewModel in CryptosViewModels)
            {
                viewModel.WhenAnyValue(x => x.Balance)
                    .Where(_ => _selectedCryptoViewModel.Name == viewModel.Name)
                    .Subscribe(value => Balance = value);

                viewModel.WhenAnyValue(x => x.IsConnected)
                    .Where(_ => _selectedCryptoViewModel.Name == viewModel.Name)
                    .Subscribe(value => IsConnected = value);
            }
        }

        private void Connect()
        {
            _selectedCryptoViewModel.ConnectCommand.Execute().Subscribe();
        }

        private void Test()
        {
            _selectedCryptoViewModel.TestCommand.Execute().Subscribe();
        }
    }
}
