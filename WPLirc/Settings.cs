using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO.IsolatedStorage;

namespace Ben.LircSharp.Phone
{
    public class Settings
    {
        public static string Host
        {
            get { return GetSetting("Host", "192.168.1.1"); }
            set { SetSetting("Host", value); }
        }

        public static int Port
        {
            get { return GetSetting("Port", 8765); }
            set { SetSetting("Port", value); }
        }

        public static Uri RemoteLayoutUrl
        {
            get { return GetSetting("RemoteLayoutUrl", (Uri)null); }
            set { SetSetting("RemoteLayoutUrl", value); }
        }

        public static string RemoteLayoutXaml
        {
            get { return GetSetting("RemoteLayoutXaml", string.Empty); }
            set { SetSetting("RemoteLayoutXaml", value); }
        }

        public static TSetting GetSetting<TSetting>(string settingName, TSetting defaultValue)
        {
            TSetting value;
            if(!IsolatedStorageSettings.ApplicationSettings.TryGetValue(settingName, out value))
            {
                IsolatedStorageSettings.ApplicationSettings[settingName] = value = defaultValue;
            }

            return value;
        }

        public static void SetSetting<TSetting>(string settingName, TSetting value)
        {
            IsolatedStorageSettings.ApplicationSettings[settingName] = value;
        }

        public static void Save()
        {
            IsolatedStorageSettings.ApplicationSettings.Save();
        }
    }
}
