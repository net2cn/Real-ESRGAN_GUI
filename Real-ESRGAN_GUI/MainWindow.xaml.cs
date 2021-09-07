using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using Ookii.Dialogs.Wpf;

namespace Real_ESRGAN_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string modelPath="./models";
        private string[] supportedOutputFormats = { "png", "jpg", "gif" };
        public Logger Logger { get; set; } = Logger.Instance;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            // Add supported output format.
            foreach (var format in supportedOutputFormats)
            {
                OutputFormatComboBox.Items.Add(format);
            }
            OutputFormatComboBox.SelectedIndex = 0;

            // Search models.
            string[] models = Utils.SearchDirectory(modelPath, "*.onnx");
            foreach (var model in models)
            {
                ModelSelectionComboBox.Items.Add(model.Split(".")[0]);
            }
            ModelSelectionComboBox.SelectedIndex = 0;
        }

        private async void StartButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            Logger.Progress = 0;
            string inputPath = InputPathTextBox.Text;
            string outputPath = OutputPathTextBox.Text;

            string selectedModelPath = $"{modelPath}/{ModelSelectionComboBox.SelectedItem}.onnx";

            // Pre-check if parameters are all set.
            if(!File.Exists(inputPath) && !Directory.Exists(inputPath))
            {
                MessageBox.Show("Input path not exists!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Path.GetFullPath(outputPath);
            }
            catch
            {
                MessageBox.Show("Output path not valid!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(selectedModelPath))
            {
                MessageBox.Show("Selected model not exists!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Regex validator = new Regex(@"^[A-Za-z0-9:]+$");
            if (!validator.IsMatch(InputFormatTextBox.Text))
            {
                MessageBox.Show("Input format is invalid!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create output path. No need to check if path exists.
            Directory.CreateDirectory(outputPath);
            Logger.Progress = 10;

            StartButton.IsEnabled = false;

            Logger.Log($"Loading model {selectedModelPath}...");
            Model model = new Model();
            await model.LoadModel(modelPath, ModelSelectionComboBox.SelectedItem.ToString());
            Logger.Progress = 30;
            await model.Scale(inputPath, outputPath, OutputFormatComboBox.SelectedItem.ToString(), 1);
            Logger.Progress = 100;
            Logger.Log("Done!");
            model.Dispose();
            StartButton.IsEnabled = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Progress += 10;
        }

        private void InputPathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog dialog = new VistaOpenFileDialog();
            // Construct filter string for open file dialog.
            var filter = "";
            foreach(var ext in InputFormatTextBox.Text.Split(":"))
            {
                filter += $"{ext} (*.{ext})|*.{ext}|";
            }
            dialog.Filter = filter+"All files (*.*)|*.*";

            if ((bool)dialog.ShowDialog(this))
            {
                InputPathTextBox.Text = dialog.FileName;
                OutputPathTextBox.Text = Path.Combine(Path.GetDirectoryName(dialog.FileName), " ").TrimEnd();
            }
        }

        private void OutputPathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Please select output folder.";
            dialog.UseDescriptionForTitle = true; // This applies to the Vista style dialog only, not the old dialog.
            if ((bool)dialog.ShowDialog(this))
            {
                OutputPathTextBox.Text = dialog.SelectedPath;
            }
        }
    }

    public class ProgressBarSmoother
    {
        public static readonly DependencyProperty SmoothValueProperty =
               DependencyProperty.RegisterAttached("SmoothValue", typeof(double), typeof(ProgressBarSmoother), new PropertyMetadata(0.0, changing));

        public static double GetSmoothValue(DependencyObject obj)
        {
            return (double)obj.GetValue(SmoothValueProperty);
        }

        public static void SetSmoothValue(DependencyObject obj, double value)
        {
            obj.SetValue(SmoothValueProperty, value);
        }

        private static void changing(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var anim = new DoubleAnimation((double)e.OldValue, (double)e.NewValue, new TimeSpan(0, 0, 0, 0, 250));
            (d as ProgressBar).BeginAnimation(ProgressBar.ValueProperty, anim, HandoffBehavior.Compose);
        }
    }
}
