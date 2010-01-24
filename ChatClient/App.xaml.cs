using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Net;

namespace ChatClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static char Delimiter = (char)3;

        public static bool IsValidIP(string addr)
        {
            IPAddress ip;
            bool valid = false;
            if (string.IsNullOrEmpty(addr))
            {
                valid = false;
            }
            else
            {
                valid = IPAddress.TryParse(addr, out ip);
            }
            return valid;
        }
    }
}
