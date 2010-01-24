using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace ChatClient
{
    public class StatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            String status = ((String)value).ToLower();
            String file;
            switch (status)
            {
                case "online":
                    file = "leaf.png";
                    break;
                case "away":
                    file = "clock.png";
                    break;
                case "busy":
                    file = "minus-circle.png";
                    break;
                case "offline":
                    file = "user-thief.png";
                    break;
                default:
                    file = "notebook.png";
                    break;
            }
            return new Uri("Icons/" + file, UriKind.Relative);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
