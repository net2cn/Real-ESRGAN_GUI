using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Real_ESRGAN_GUI
{
    public class Logger
    {
        public ObservableCollection<string> Buffer { get; set; }
        private int maxLimit = 500;

        public Logger()
        {
            Buffer = new ObservableCollection<string>();
        }
        public void Log(string content)
        {
            if (Buffer.Count < maxLimit)
            {
                Buffer.Add(content);
            }
            else
            {
                Buffer.RemoveAt(0);
                Buffer.Add(content);
            }
        }
    }

    public class ListToStringConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is IEnumerable)
            {
                return string.Join(Environment.NewLine, ((IEnumerable)values[0]).OfType<string>().ToArray());
            }
            return "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
