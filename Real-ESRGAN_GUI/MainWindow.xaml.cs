using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using Ookii.Dialogs.Wpf;
using Microsoft.ML.OnnxRuntime;
using System.Collections.Generic;

namespace Real_ESRGAN_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string modelPath="./models";
        private string[] supportedOutputFormats = { "png", "jpg", "gif" };
        private CancellationTokenSource cancellationTokenSource;
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
            // Todo: Walk through directory recursively to find matching files if input path is a directory.
            if(!File.Exists(inputPath) && !Directory.Exists(inputPath))
            {
                MessageBox.Show("Input path does not exists!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Path.GetFullPath(outputPath);
            }
            catch
            {
                MessageBox.Show("Output path is not valid!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(selectedModelPath))
            {
                MessageBox.Show("Selected model does not exists!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Regex validator = new Regex(@"^[A-Za-z0-9:]+$");
            if (!validator.IsMatch(InputFormatTextBox.Text))
            {
                MessageBox.Show("Input format is invalid!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check whether input path is a directory.
            List<string> files = new List<string>();
            if (Directory.Exists(inputPath))
            {
                // inputPath is a directory.
                var filters = @"\." + String.Join(@"$|\.", InputFormatTextBox.Text.Split(":")) + "$";
                foreach (string file in Directory.EnumerateFiles(inputPath, "*.*", SearchOption.AllDirectories))
                {
                    if (Regex.IsMatch(file, filters))
                    {
                        files.Add(file);
                    }
                }
            }
            else
            {
                // inputPath is a file.
                files.Add(inputPath);
            }
            Logger.Progress = 10;

            // Update UI element and prepare cancellation token source.
            StartButton.IsEnabled = false;
            ModelSelectionComboBox.IsEnabled = false;

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Dispose();
            }
            cancellationTokenSource = new CancellationTokenSource();

            Model model = new Model();
            try
            {
                Logger.Log($"Loading model {selectedModelPath}...");
                
                Logger.Progress = 30;

                if (await model.LoadModel(modelPath, ModelSelectionComboBox.SelectedItem.ToString(), Convert.ToInt32(DeviceIdTextBox.Text), cancellationTokenSource.Token).WaitOrCancel(cancellationTokenSource.Token))
                {
                    CancelButton.IsEnabled = false;
                    await model.Scale(inputPath, files, outputPath, OutputFormatComboBox.SelectedItem.ToString());
                    Logger.Progress = 100;
                    Logger.Log("Done!");
                }
                else
                {
                    Logger.Log("Failed to load model!");
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Log("Operation was cancelled by user.");
            }
            catch
            {
                throw;
            }
            finally
            {
                model.Dispose();
                GC.Collect();
                ModelSelectionComboBox.IsEnabled = true;
                StartButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
            else
            {
                Logger.Log("Nothing to cancel.");
            }
        }

        private void InputPathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog dialog = new VistaOpenFileDialog();
            
            // Construct filter string for open file dialog.
            var filter = "";
            var count = 1;
            foreach(var ext in InputFormatTextBox.Text.Split(":"))
            {
                filter += $"{ext} (*.{ext})|*.{ext}|";
                count++;
            }
            dialog.Filter = filter+"All files (*.*)|*.*";
            dialog.FilterIndex = count;

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
                OutputPathTextBox.Text = Path.Combine(dialog.SelectedPath, " ").TrimEnd();
            }
        }

        private void InputPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (File.Exists(InputPathTextBox.Text))
            {
                OutputPathTextBox.Text = Path.GetDirectoryName(InputPathTextBox.Text)+"\\";
            }
            else if (Directory.Exists(InputPathTextBox.Text))
            {
                OutputPathTextBox.Text = InputPathTextBox.Text.TrimEnd('\\') + "_Upscaled\\";
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
