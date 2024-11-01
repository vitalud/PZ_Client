namespace Client.Service
{
    public static class ConfigService
    {
        public static string GetLogin()
        {
            return Properties.Settings.Default.Login;
        }

        public static void SetLogin(string login)
        {
            Properties.Settings.Default.Login = login;
            Save();
        }

        public static string GetPassword()
        {
            return Properties.Settings.Default.Password;
        }

        public static void SetPassword(string password)
        {
            Properties.Settings.Default.Password = password;
            Save();
        }

        public static string GetIp()
        {
            return Properties.Settings.Default.ServerIp;
        }

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
        public static void SetInterval(string code, int interval)
        {
            if (interval < 1) return;

            var intervals = Properties.Settings.Default.Intervals;
            for (int i = 0; i < intervals.Count; i++)
            {
                if (intervals[i].Contains(code))
                {
                    intervals.RemoveAt(i);
                    break;
                }
            }
            intervals.Add(code + "~" + interval);
            Save();
        }

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
        public static void SetKey(string burse, string name, string key)
        {
            var keys = Properties.Settings.Default.Keys;
            var temp = burse + '~' + name;
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i].Contains(temp))
                {
                    keys.RemoveAt(i);
                    break;
                }
            }
            keys.Add(temp + "~" + key);
            Save();
        }

        public static void Save()
        {
            Properties.Settings.Default.Save();
        }
    }
}
