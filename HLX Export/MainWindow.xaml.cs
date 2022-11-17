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
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HLXExport
{
    // These values should all be constants and should be checked before every release version of the software
    public static class ApplicationConstants
    {
        public const string SOFTWARE_VERSION = "v0.1.1";
        public const string SOFTWARE_NAME = "HLX Export";
        public const string FORMATTED_TITLE = SOFTWARE_NAME + " " + SOFTWARE_VERSION;
        public const string SOFTWARE_SUPPORT_LINK = "https://github.com/angusbarnes/HelixExportTool/issues";
        public const string TEMP_DATA_PATH = ".\\temp\\";
#if DEBUG
        public const bool DEBUG_MODE_ENABLED = true;
#endif
#if !DEBUG 
        public const bool DEBUG_MODE_ENABLED = true;
#endif
    }

    public partial class MainWindow : Window
    {
        private ConsoleWindow consoleWindow = new();
        private readonly MainWindowListener mainWindowListener = new();

        private ZippedFileCollection _zip;

        private string SELECTED_FILE;

        private ObservableCollection<HeaderData> headerData;
        
        public struct HeaderData {
            public string SourceName { get; set; }
            public bool Include { get; set; }

        }

        public MainWindow() {
            InitializeComponent();
            Title = ApplicationConstants.FORMATTED_TITLE;
            Trace.Listeners.Add(mainWindowListener);

            consoleWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            if (ApplicationConstants.DEBUG_MODE_ENABLED)
                consoleWindow.Show();

            if (Directory.Exists(ApplicationConstants.TEMP_DATA_PATH) == false)
                Directory.CreateDirectory(ApplicationConstants.TEMP_DATA_PATH);

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

            if(File.Exists(openFileDialog.FileName)) {

                if (_zip != null)
                    _zip.DestroyCollection();

                _zip = ZippedFileCollection.Open(openFileDialog.FileName, ApplicationConstants.TEMP_DATA_PATH);

                foreach (string filename in _zip.GetFiles(".csv")) {
                    ListDisplay.Items.Add(filename.Split('\\').Last());
                    Debug.Log("Found File in cluster: " + filename);

                    List<string> potentialFiles = new List<string>();
                    if (filename.Contains("header", StringComparison.OrdinalIgnoreCase) || filename.Contains("collar", StringComparison.OrdinalIgnoreCase)) {
                        potentialFiles.Add(filename);
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
            headerData = new ObservableCollection<HeaderData>();
            MainGrid.DataContext = headerData;
            ProjectAreaList.Items.Clear();

            if (e.AddedItems.Count > 1) {
                Trace.Write("MainWindow: Multiple Objects were provided: ");
                foreach (string file in e.AddedItems) {
                    Trace.WriteLine("   -->" + file);
                }

            } else if (e.AddedItems.Count == 1)  {
                string? file = e.AddedItems[0].ToString();
                Trace.WriteLine($"MainWindow: Single File Selection Made: {file}");
                SELECTED_FILE = _zip.GetFilePath(file);

                using CSVReader reader = new CSVReader(SELECTED_FILE);
                
                string[] fields = reader.FieldNames;
                Debug.Notify("MainWindow: Selected Headers: " + fields.FlattenToString());

                foreach (string field in fields) {
                    headerData.Add(new HeaderData() {
                        SourceName = field,
                        Include = false
                    });
                }
                

                List<string> areas = new();

                reader.Reset();

                Debug.Status("Collar ANALYSE ON");

                foreach (string value in reader.GetField("\"ProjectArea\"")) {
                    if (areas.Contains(value)) {
                        continue;
                    }

                    CheckBox checkBox = new() {
                        Content = value.Trim('"')
                    };

                    //checkBox.Checked += UpdateHoleSelectionList;
                    ProjectAreaList.Items.Add(checkBox);

                    areas.Add(value);
                }

                Trace.WriteLine("MainWindow: Attempting To Process File");
            }
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
            Debug.Status("MainWindow: Set Console Display ON");
        }

        // I think this method should work for default browsers on Windows
        private void Button_ReportIssue(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", ApplicationConstants.SOFTWARE_SUPPORT_LINK);
        }

        private void Button_ClearDataCache(object sender, RoutedEventArgs e) {
            Directory.Delete(ApplicationConstants.TEMP_DATA_PATH, true);
            Directory.CreateDirectory(ApplicationConstants.TEMP_DATA_PATH);
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
            if (_zip != null)
                _zip.DestroyCollection();
            base.OnClosed(e);
            consoleWindow.Close();
        }
    }
}
