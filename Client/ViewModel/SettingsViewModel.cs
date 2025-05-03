using Client.Model;
using ReactiveUI;
using System.Reactive;

namespace Client.ViewModel
{
    public partial class SettingsViewModel : ReactiveObject
    {
        private readonly SettingsModel _settings;

        public string OkxApi
        {
            get => _settings.OkxApi;
            set => _settings.OkxApi = value;
        }
        public string OkxSecret
        {
            get => _settings.OkxSecret;
            set => _settings.OkxSecret = value;
        }
        public string OkxWord
        {
            get => _settings.OkxWord;
            set => _settings.OkxWord = value;
        }
        public string BinanceApi
        {
            get => _settings.BinanceApi;
            set => _settings.BinanceApi = value;
        }
        public string BinanceSecret
        {
            get => _settings.BinanceSecret;
            set => _settings.BinanceSecret = value;
        }
        public string BybitApi
        {
            get => _settings.BybitApi;
            set => _settings.BybitApi = value;
        }
        public string BybitSecret
        {
            get => _settings.BybitSecret;
            set => _settings.BybitSecret = value;
        }
        public string QuikClientCode
        {
            get => _settings.QuikClientCode;
            set => _settings.QuikClientCode = value;
        }
        public string QuikFirst
        {
            get => _settings.QuikFirst;
            set => _settings.QuikFirst = value;
        }
        public string QuikSecond
        {
            get => _settings.QuikSecond;
            set => _settings.QuikSecond = value;
        }
        public string QuikThird
        {
            get => _settings.QuikThird;
            set => _settings.QuikThird = value;
        }

        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        public SettingsViewModel(SettingsModel settings)
        {
            _settings = settings;

            SaveCommand = ReactiveCommand.Create(_settings.SaveKeys);
        }
    }
}
