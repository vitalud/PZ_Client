using Client.Service;
using ReactiveUI;

namespace Client.Model
{
    /// <summary>
    /// Класс, представляющий собой настройки приложения.
    /// Реализованы только хранения ключей.
    /// </summary>
    public partial class SettingsModel : ReactiveObject
    {
        private string _okxApi = string.Empty;
        private string _okxSecret = string.Empty;
        private string _okxWord = string.Empty;
        private string _binanceApi = string.Empty;
        private string _binanceSecret = string.Empty;
        private string _bybitApi = string.Empty;
        private string _bybitSecret = string.Empty;

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

        /// <summary>
        /// Считывает ключи из конфига.
        /// </summary>
        public SettingsModel()
        {
            OkxApi = ConfigService.GetKey("Okx", "Api");
            OkxSecret = ConfigService.GetKey("Okx", "Secret");
            OkxWord = ConfigService.GetKey("Okx", "Word");
            BinanceApi = ConfigService.GetKey("Binance", "Api");
            BinanceSecret = ConfigService.GetKey("Binance", "Secret");
            BybitApi = ConfigService.GetKey("Bybit", "Api");
            BybitSecret = ConfigService.GetKey("Bybit", "Secret");
        }

        /// <summary>
        /// Сохраняет ключи в конфиг.
        /// </summary>
        public void SaveKeys()
        {
            ConfigService.SetKey("Okx", "Api", OkxApi);
            ConfigService.SetKey("Okx", "Secret", OkxSecret);
            ConfigService.SetKey("Okx", "Word", OkxWord);
            ConfigService.SetKey("Binance", "Api", BinanceApi);
            ConfigService.SetKey("Binance", "Secret", BinanceSecret);
            ConfigService.SetKey("Bybit", "Api", BybitApi);
            ConfigService.SetKey("Bybit", "Secret", BybitSecret);
            ConfigService.Save();
        }
    }
}
