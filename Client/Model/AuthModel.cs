using Client.Service;
using Client.Service.Abstract;
using ReactiveUI;
using System.Text.RegularExpressions;

namespace Client.Model
{
    /// <summary>
    /// Класс, представляющий собой авторизацию клиента.
    /// </summary>
    public partial class AuthModel : ReactiveObject
    {
        private readonly Connector _сonnector;

        private string _login = string.Empty;
        private string _password = string.Empty;
        private bool _rememberMe;

        /// <summary>
        /// Логин клиента.
        /// </summary>
        public string Login
        {
            get => _login;
            set => this.RaiseAndSetIfChanged(ref _login, value);
        }

        /// <summary>
        /// Пароль клиента.
        /// </summary>
        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        /// <summary>
        /// Запомнить данные клиента.
        /// </summary>
        public bool RememberMe
        {
            get => _rememberMe;
            set => this.RaiseAndSetIfChanged(ref _rememberMe, value);
        }

        /// <summary>
        /// Получает логин и пароль из конфига.
        /// </summary>
        /// <param name="сonnector"></param>
        public AuthModel(Connector сonnector)
        {
            _сonnector = сonnector;

            Login = ConfigService.GetLogin();
            Password = ConfigService.GetPassword();
        }

        /// <summary>
        /// Отправляет запрос на аутентификацию на сервер.
        /// В случае успеха сохраняет логин и пароль в конфиг
        /// при <see cref="RememberMe"/> = true.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Authentication()
        {
            if (!CheckAuthData()) return false;

            bool result = await _сonnector.Authentication();
            if (result && RememberMe)
                ConfigService.Save();

            return result;
        }

        /// <summary>
        /// Проверяет через регулярное выражение введенные логин и пароль.
        /// </summary>
        /// <returns></returns>
        private bool CheckAuthData()
        {
            bool result = false;

            if (!string.IsNullOrEmpty(Login) && !string.IsNullOrEmpty(Password))
            {
                var regex = new Regex(@"(\W)");
                if (!regex.IsMatch(Login + Password))
                {
                    result = true;
                    ConfigService.SetLogin(Login);
                    ConfigService.SetPassword(Password);
                }
            }

            return result;
        }
    }
}
