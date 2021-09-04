using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
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

        public MainWindow()
        {
            InitializeComponent();

            // Add supported output format.
            foreach (var format in supportedOutputFormats)
            {
                OutputFormatComboBox.Items.Add(format);
            }
            OutputFormatComboBox.SelectedIndex = 0;

            // Search models.
            string[] models = Utils.SearchDirectory(modelPath, "*.pth");
            foreach (var model in models)
            {
                ModelSelectionComboBox.Items.Add(model.Split(".")[0]);
            }
            ModelSelectionComboBox.SelectedIndex = 0;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            string inputPath = InputPathTextBox.Text;
            string outputPath = OutputPathTextBox.Text;

            string selectedModelPath = $"{modelPath}/{ModelSelectionComboBox.SelectedItem}.bin";

            // Pre-check if parameters are all set.
            if(!Directory.Exists(inputPath))
            {
                MessageBox.Show("Input path not exists!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!Uri.IsWellFormedUriString(outputPath, UriKind.Absolute))
            {
                MessageBox.Show("Input path not exists!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!Directory.Exists(selectedModelPath))
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
        }

        private void InputPathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog dialog = new VistaOpenFileDialog();
            dialog.Filter = "All files (*.*)|*.*";
            if ((bool)dialog.ShowDialog(this))
            {
                InputPathTextBox.Text = dialog.FileName;
            }
        }

        private void OutputPathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Please select output folder.";
            dialog.UseDescriptionForTitle = true; // This applies to the Vista style dialog only, not the old dialog.
            if ((bool)dialog.ShowDialog(this))
            {
                InputPathTextBox.Text = dialog.SelectedPath;
            }
        }
    }
}
