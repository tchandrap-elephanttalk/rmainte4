using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace rmainte4
{
    // シングルトン
    public class AppSettings : System.Configuration.ApplicationSettingsBase
    {
        // インスタンス化せずに取得するなら、次のようにすればいい。
        // env.Address = AppSettings.GetConfigStringValueOrDefault("Server", "localhost");

        private static readonly AppSettings _instance = new AppSettings();

        // Singleton
        private AppSettings()
        {
        }

        public static AppSettings GetInstance()
        {
            return _instance;
        }

        public static AppSettings Instance
        {
            get { return _instance; }
        }

        public static string GetConfigStringValueOrNull(string key)
        {
            string str = null;
            try
            {
                str = System.Configuration.ConfigurationManager.AppSettings[key];
            }
            catch (Exception)
            {
            }
            return str;
        }

        public static string GetConfigStringValueOrDefault(string key, string defaultValue)
        {
            string str = defaultValue;
            str = GetConfigStringValueOrNull(key);
            if (str == null)
            {
                return defaultValue;
            }
            return str;
        }

        public static bool GetConfigBooleanValueOrDefault(string key, bool defaultValue)
        {
            string str = GetConfigStringValueOrNull(key);
            if (str == null)
            {
                return false;
            }

            bool b = defaultValue;
            try
            {
                b = Boolean.Parse(str);
            }
            catch (Exception)
            {
            }
            return b;
        }

        public static int GetConfigIntegerValueOrDefault(string key, int defaultValue)
        {
            string str = GetConfigStringValueOrNull(key);

            int i = defaultValue;
            try
            {
                i = Int32.Parse(str);
            }
            catch (Exception)
            {
            }
            return i;
        }

        [ApplicationScopedSetting()]
        [DefaultSettingValue("localhost")]
        public string Server
        {
            get { return (string)this["Server"]; }
            set { this["Server"] = value; }
        }

        [ApplicationScopedSetting()]
        [DefaultSettingValue("Http")] // Http or Tcp
        public string ConnectType
        {
            get { return (string)this["ConnectType"]; }
            set { this["ConnectType"] = value; }
        }

        [ApplicationScopedSetting()]
        [DefaultSettingValue("8338")]
        public int TcpPort
        {
            get { return (int)this["TcpPort"]; }
            set { this["TcpPort"] = value; }
        }

        [ApplicationScopedSetting()]
        [DefaultSettingValue("8339")]
        public int HttpPort
        {
            get { return (int)this["HttpPort"]; }
            set { this["HttpPort"] = value; }
        }

        [ApplicationScopedSetting()]
        [DefaultSettingValue("rmainte4")]
        public string ObjectUri
        {
            get { return (string)this["ObjectUri"]; }
            set { this["ObjectUri"] = value; }
        }

        [ApplicationScopedSetting()]
        [DefaultSettingValue("Rmainte4")]
        public string EventSource
        {
            get { return (string)this["EventSource"]; }
            set { this["EventSource"] = value; }
        }

        // ----------------------------

        // new AppSettings().Messageで取れる
        [UserScopedSetting()]
        public string Message
        {
            get
            {
                return (string)this["Message"];
            }
            set
            {
                this["Message"] = value;
            }
        }



    }
}
