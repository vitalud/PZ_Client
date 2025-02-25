using Client.Service.Sub;

namespace Client.Service
{
    /// <summary>
    /// Класс представляет собой методы для получения/сохранения
    /// данных пользователя в конфиг.
    /// </summary>
    public static class ConfigService
    {
        /// <summary>
        /// Получает логин.
        /// </summary>
        /// <returns></returns>
        public static string GetLogin()
        {
            return Properties.Settings.Default.Login;
        }

        /// <summary>
        /// Сохраняет логин.
        /// </summary>
        /// <param name="login">Логин.</param>
        public static void SetLogin(string login)
        {
            Properties.Settings.Default.Login = login;
            Save();
        }

        /// <summary>
        /// Получает пароль.
        /// </summary>
        /// <returns></returns>
        public static string GetPassword()
        {
            return Properties.Settings.Default.Password;
        }

        /// <summary>
        /// Сохраняет пароль.
        /// </summary>
        /// <param name="login">Пароль.</param>
        public static void SetPassword(string password)
        {
            Properties.Settings.Default.Password = password;
            Save();
        }

        /// <summary>
        /// Получает ip сервера.
        /// </summary>
        /// <returns></returns>
        public static string GetIp()
        {
            return Properties.Settings.Default.ServerIp;
        }


        /// <summary>
        /// Получает интервал обновления для подписки в случае,
        /// если она была ранее запущена.
        /// </summary>
        /// <param name="sub">Подписка.</param>
        public static void GetInterval(Subscription sub)
        {
            var intervals = Properties.Settings.Default.Intervals;
            foreach (var interval in intervals)
            {
                if (interval != null)
                {
                    if (interval.Contains(sub.Code))
                    {
                        var value = interval.Split('~');
                        sub.UpdateTime = int.Parse(value[1]);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Устанавливает интервал обновления для подписки.
        /// </summary>
        /// <param name="code">Код подписки.</param>
        /// <param name="interval">Интервал обновления.</param>
        public static void SetInterval(string code, int interval)
        {
            if (interval < 1) return;

            var intervals = Properties.Settings.Default.Intervals;
            if (intervals == null) return;

            for (int i = 0; i < intervals.Count; i++)
            {
                if (intervals[i] == null) continue;

                if (intervals[i].Contains(code))
                {
                    intervals.RemoveAt(i);
                    break;
                }
            }
            intervals.Add(code + "~" + interval);
            Save();
        }

        /// <summary>
        /// Получает ключ для API криптобиржи.
        /// </summary>
        /// <param name="burse">Криптобиржа.</param>
        /// <param name="name">Имя ключа.</param>
        /// <returns></returns>
        public static string GetKey(string burse, string name)
        {
            var keys = Properties.Settings.Default.Keys;
            foreach (var key in keys)
            {
                if (key != null)
                {
                    var value = key.Split('~');
                    if (value[0].Equals(burse) && value[1].Equals(name))
                    {
                        return value[2];
                    }
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Устанавливает ключ для API криптобиржи.
        /// </summary>
        /// <param name="burse">Криптобиржа.</param>
        /// <param name="name">Имя ключа.</param>
        /// <param name="key">Ключ.</param>
        public static void SetKey(string burse, string name, string key)
        {
            var keys = Properties.Settings.Default.Keys;
            if (keys == null) return;

            var temp = burse + '~' + name;
            var keyToRemove = keys.Cast<string>().FirstOrDefault(item => item != null && item.StartsWith(temp));

            if (keyToRemove != null)
            {
                keys.Remove(keyToRemove);
            }

            keys.Add(temp + "~" + key);
            Save();
        }

        /// <summary>
        /// Сохраняет данные в конфиге.
        /// </summary>
        public static void Save()
        {
            Properties.Settings.Default.Save();
        }
    }
}
