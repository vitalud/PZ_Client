using Client.Service;
using Client.Service.Abstract;
using ReactiveUI;
using System.Text.RegularExpressions;

namespace Client.Model
{
    /// <summary>
    /// Класс, представляющий собой авторизацию клиента.
    /// </summary>
    /// <remarks>
    /// Получает логин и пароль из конфига.
    /// </remarks>
    /// <param name="сonnector"></param>
    public partial class AuthModel(Connector сonnector) : ReactiveObject
    {
        private readonly Connector _сonnector = сonnector;

        public static string LoadLogin()
        {
            return ConfigService.GetLogin();
        }

        public static string LoadPassword()
        {
            return ConfigService.GetPassword();
        }

        /// <summary>
        /// Отправляет запрос на аутентификацию на сервер.
        /// В случае успеха сохраняет логин и пароль в конфиг
        /// при <see cref="RememberMe"/> = true.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Authenticate(string login, string password, bool rememberMe)
        {
            if (!CheckAuthData(login, password)) return false;

            bool result = await _сonnector.Authentication(login, password);
            if (result && rememberMe)
            {
                ConfigService.SetLogin(login);
                ConfigService.SetPassword(password);
                ConfigService.Save();
            }

            return result;
        }

        /// <summary>
        /// Проверяет через регулярное выражение введенные логин и пароль.
        /// </summary>
        /// <returns></returns>
        private static bool CheckAuthData(string login, string password)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password)) return false;

            var regex = new Regex(@"(\W)");

            if (!regex.IsMatch(login + password))
            {
                return true;
            }

            return false;
        }
    }
}
