using Client.Service;
using Client.Service.Abstract;
using ReactiveUI;
using System.Text.RegularExpressions;

namespace Client.Model
{
    public class AuthModel : ReactiveObject
    {
        private readonly Connector _сonnector;

        private string _login;
        private string _password;
        private bool _rememberMe;

        public string Login
        {
            get => _login;
            set => this.RaiseAndSetIfChanged(ref _login, value);
        }
        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }
        public bool RememberMe
        {
            get => _rememberMe;
            set => this.RaiseAndSetIfChanged(ref _rememberMe, value);
        }

        public AuthModel(Connector сonnector)
        {
            _сonnector = сonnector;
            Login = ConfigService.GetLogin();
            Password = ConfigService.GetPassword();
        }

        public async Task<bool> Authorization()
        {
            bool result = false;
            if (CheckAuthData())
                result = await _сonnector.Authorization();
            if (result && RememberMe)
                ConfigService.Save();
            return result;
        }

        private bool CheckAuthData()
        {
            bool result = false;
            if (Login != string.Empty && Password != string.Empty)
            {
                var check = Login + Password;
                var regex = new Regex(@"(\W)");
                var matches = regex.Match(check);
                if (matches.Length == 0)
                    result = true;
            }
            ConfigService.SetLogin(Login);
            ConfigService.SetPassword(Password);
            return result;
        }
    }
}
