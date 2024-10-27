using Client.Model;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;

namespace Client.ViewModel
{
    public class SettingsViewModel : ReactiveObject
    {
        private readonly SettingsModel _settings;

        private string _okxApi;
        private string _okxSecret;
        private string _okxWord;
        private string _binanceApi;
        private string _binanceSecret;
        private string _bybitApi;
        private string _bybitSecret;
        public string OkxApi
        {
            get => _okxApi;
            set => this.RaiseAndSetIfChanged(ref _okxApi, value);
        }
        public string OkxSecret
        {
            get => _okxSecret;
            set => this.RaiseAndSetIfChanged(ref _okxSecret, value);
        }
        public string OkxWord
        {
            get => _okxWord;
            set => this.RaiseAndSetIfChanged(ref _okxWord, value);
        }
        public string BinanceApi
        {
            get => _binanceApi;
            set => this.RaiseAndSetIfChanged(ref _binanceApi, value);
        }
        public string BinanceSecret
        {
            get => _binanceSecret;
            set => this.RaiseAndSetIfChanged(ref _binanceSecret, value);
        }
        public string BybitApi
        {
            get => _bybitApi;
            set => this.RaiseAndSetIfChanged(ref _bybitApi, value);
        }
        public string BybitSecret
        {
            get => _bybitSecret;
            set => this.RaiseAndSetIfChanged(ref _bybitSecret, value);
        }

        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        public SettingsViewModel(SettingsModel settings)
        {
            _settings = settings;

            SaveCommand = ReactiveCommand.Create(_settings.SaveKeys);

            OkxApi = _settings.OkxApi;
            OkxSecret = _settings.OkxSecret;
            OkxWord = _settings.OkxWord;
            BinanceApi = _settings.BinanceApi;
            BinanceSecret = _settings.BinanceSecret;
            BybitApi = _settings.BybitApi;
            BybitSecret = _settings.BybitSecret;

            this.WhenAnyValue(x => x.OkxApi)
                .Skip(1)
                .Subscribe(value => _settings.OkxApi = value);
            this.WhenAnyValue(x => x.OkxSecret)
                .Skip(1)
                .Subscribe(value => _settings.OkxSecret = value);
            this.WhenAnyValue(x => x.OkxWord)
                .Skip(1)
                .Subscribe(value => _settings.OkxWord = value);
            this.WhenAnyValue(x => x.BinanceApi)
                .Skip(1)
                .Subscribe(value => _settings.BinanceApi = value);
            this.WhenAnyValue(x => x.BinanceSecret)
                .Skip(1)
                .Subscribe(value => _settings.BinanceSecret = value);
            this.WhenAnyValue(x => x.BybitApi)
                .Skip(1)
                .Subscribe(value => _settings.BybitApi = value);
            this.WhenAnyValue(x => x.BybitSecret)
                .Skip(1)
                .Subscribe(value => _settings.BybitSecret = value);
        }
    }
}
