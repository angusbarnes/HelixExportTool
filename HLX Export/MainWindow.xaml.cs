using Microsoft.Win32;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;
using System.Diagnostics;
using System.Windows.Data;
using System;
using static HLXExport.Utilities;
using System.IO;
using LazyCSV;

namespace HLXExport
{
    // These values should all be constants and should be checked before every release version of the software
    public static class ApplicationConstants
    {
        public const string SOFTWARE_VERSION = "v0.1.1";
        public const string SOFTWARE_NAME = "HLX Export";
        public const string FORMATTED_TITLE = SOFTWARE_NAME + " " + SOFTWARE_VERSION;
        public const string SOFTWARE_SUPPORT_LINK = "https://www.google.com.au/";
#if DEBUG
        public const bool DEBUG_MODE_ENABLED = true;
#endif
#if !DEBUG 
        public const bool DEBUG_MODE_ENABLED = true;
#endif
    }

    public partial class MainWindow : Window
    {
        private readonly DataProcessor processor = new();
        private ConsoleWindow consoleWindow = new();
        private readonly MainWindowListener mainWindowListener = new();
        
        public MainWindow() {
            InitializeComponent();
            Title = ApplicationConstants.FORMATTED_TITLE;
            Trace.Listeners.Add(mainWindowListener);

            if (ApplicationConstants.DEBUG_MODE_ENABLED)
                consoleWindow.Show();
        }

        private void Button_LoadFromSource(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            

            if (openFileDialog.ShowDialog() == true) {

                DataSourceLocationPath.Text = openFileDialog.FileName;

            } else {
                
                MessageBox.Show("Please Select a File", "Loading Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if(processor.LoadData(openFileDialog.FileName)) {

                ZippedFileCollection files = Utilities.OpenZipFile(openFileDialog.FileName, ".\\temp\\");

                foreach (string filename in files.GetFiles(".csv")) {
                    ListDisplay.Items.Add(filename);

                    using (CSVReader reader = new CSVReader(filename)) {
                        Debug.Log($"MainWindow: Found CSV Headers In File {filename}: " + reader.GetFieldNames.FlattenToString());
                    }
                }

            } else {
                Debug.Warn("MainWindow: User selected a bad file. Not correct format");
                MessageBox.Show("The File select either does not exist or is not a ZIP file", "Load File Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private void Button_SaveToDestination(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new();
            if (dialog.ShowDialog() == true)
                DataDestinationLocationPath.Text = dialog.SelectedPath;
        }

        private void UI_CollarFileSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (e.AddedItems.Count > 1) {
                Trace.Write("MainWindow: Multiple Objects were provided: ");
                foreach (string file in e.AddedItems) {
                    Trace.WriteLine("   -->" + file);
                }

            } else if (e.AddedItems.Count == 1)  {
                string? file = e.AddedItems[0].ToString();
                Trace.WriteLine($"MainWindow: Single File Selection Made: {file}");
                processor.SelectFile(file);

                using (CSVReader reader = new CSVReader(file)) {
                    Debug.Warn("MainWindow: Selected Headers: " + reader.GetFieldNames.FlattenToString());
                }
            }
        }

        private void Button_AnalyseCollar(object sender, RoutedEventArgs e) {
            if (processor.GetSelectedFile() == null) {
                return;
            }

            List<string> areas = new();

            foreach (string value in processor.GetFieldValuesFromSelectedFile("ProjectArea")) {

                if (areas.Contains(value)) {
                    continue;
                }
                
                CheckBox checkBox = new() {
                    Content = value
                };

                //checkBox.Checked += UpdateHoleSelectionList;
                ProjectAreaList.Items.Add(checkBox);

                areas.Add(value);
            }

            Trace.WriteLine("MainWindow: Attempting To Process File");
        }

        private void Button_ShowCSVTools(object sender, RoutedEventArgs e)
        {
            CSVCleaner cleaner = new();
            cleaner.Show();
        }

        private void Button_ShowDebugWindow(object sender, RoutedEventArgs e)
        {
            if (consoleWindow.IsClosed)
            {
                consoleWindow = new ConsoleWindow();
            }
            
            consoleWindow.Show();
            Debug.Log("MainWindow: Set Console Display ON");
        }

        // I think this method should work for default browsers on Windows
        private void Button_ReportIssue(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", ApplicationConstants.SOFTWARE_SUPPORT_LINK);
        }

        // Simple Trace Listener
        public class MainWindowListener : TraceListener
        {

            public override void Write(string? message)
            {
                Debug.Log(message);
            }

            public override void WriteLine(string? message)
            {
                Debug.Log(message);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            consoleWindow.Close();
        }
    }
}
