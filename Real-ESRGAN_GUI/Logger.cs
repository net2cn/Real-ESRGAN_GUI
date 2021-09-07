using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace Real_ESRGAN_GUI
{
    public class Logger : INotifyPropertyChanged
    {
        public ObservableCollection<string> Buffer { get; set; }
        public int Progress {
            get => _progress; 
            set {
                this._progress = value;
                NotifyPropertyChanged(nameof(Progress));
            } 
        }

        private int maxLimit = 500;
        private int _progress = 0;

        private static TimeSpan duration = TimeSpan.FromSeconds(2);
        private static readonly Lazy<Logger> lazy = new Lazy<Logger>(() => new Logger());

        public event PropertyChangedEventHandler PropertyChanged;
        internal void NotifyPropertyChanged(String propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public static Logger Instance { get { return lazy.Value; } }

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
