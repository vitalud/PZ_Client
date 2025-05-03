using Client.Model;
using ReactiveUI;
using System.Reactive;

namespace Client.ViewModel
{
    public partial class MainViewModel : ReactiveObject
    {
        private readonly MainModel _mainModel;
        private readonly CryptosViewModel _cryptos;
        private readonly MultiCryptoViewModel _multiCrypto;
        private readonly QuikViewModel _quik;
        private readonly SettingsViewModel _settings;

        private bool _applicationClosing;
        public bool ApplicationClosing
        {
            get => _applicationClosing;
            set => this.RaiseAndSetIfChanged(ref _applicationClosing, value);
        }

        public ReactiveCommand<Unit, Unit> CloseCommand { get; }
        public ReactiveCommand<Unit, ReactiveObject> ShowCryptosCommand { get; }
        public ReactiveCommand<Unit, ReactiveObject> ShowMultiCryptoCommand { get; }
        public ReactiveCommand<Unit, ReactiveObject> ShowQuikCommand { get; }
        public ReactiveCommand<Unit, ReactiveObject> ShowSettingsCommand { get; }

        private ReactiveObject _currentViewModel;
        public ReactiveObject CurrentViewModel
        {
            get => _currentViewModel;
            set => this.RaiseAndSetIfChanged(ref _currentViewModel, value);
        }

        public MainViewModel(MainModel mainModel, CryptosViewModel cryptos, MultiCryptoViewModel multiCrypto, QuikViewModel quik, SettingsViewModel settings)
        {
            _mainModel = mainModel;
            _cryptos = cryptos;
            _multiCrypto = multiCrypto;
            _quik = quik;
            _settings = settings;

            _currentViewModel = cryptos;

            CloseCommand = ReactiveCommand.Create(CloseApplication);
            ShowCryptosCommand = ReactiveCommand.Create(() => CurrentViewModel = _cryptos);
            ShowMultiCryptoCommand = ReactiveCommand.Create(() => CurrentViewModel = _multiCrypto);
            ShowQuikCommand = ReactiveCommand.Create(() => CurrentViewModel = _quik);
            ShowSettingsCommand = ReactiveCommand.Create(() => CurrentViewModel = _settings);
        }

        /// <summary>
        /// TODO: проверить функционал
        /// </summary>
        private void CloseApplication()
        {
            _mainModel.CloseApplication();
            ApplicationClosing = true;
        }
    }
}
