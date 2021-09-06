using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        public Logger Logger { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Logger = new Logger();
            DataContext = this;

            // Add supported output format.
            foreach (var format in supportedOutputFormats)
            {
                OutputFormatComboBox.Items.Add(format);
            }
            OutputFormatComboBox.SelectedIndex = 0;

            // Search models.
            string[] models = Utils.SearchDirectory(modelPath, "*.param");
            foreach (var model in models)
            {
                ModelSelectionComboBox.Items.Add(model.Split(".")[0]);
            }
            ModelSelectionComboBox.SelectedIndex = 0;
        }

        private async void StartButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            string inputPath = InputPathTextBox.Text;
            string outputPath = OutputPathTextBox.Text;

            string selectedModelPath = $"{modelPath}/{ModelSelectionComboBox.SelectedItem}.bin";

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

            Model model = new Model();
            Logger.Log($"Loading model {selectedModelPath}.");
            model.LoadModel(modelPath, ModelSelectionComboBox.SelectedItem.ToString());
            await model.Scale(inputPath, outputPath, OutputFormatComboBox.SelectedItem.ToString(), 1);
            model.Dispose();
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
}
