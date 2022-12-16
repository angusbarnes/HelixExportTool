using Microsoft.Win32;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;
using System.Diagnostics;
using System;
using static HLXExport.Utilities;
using System.IO;
using LazyCSV;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;

namespace HLXExport
{
    // These values should all be constants and should be checked before every release version of the software
    public static class ApplicationConstants
    {
        public const string SOFTWARE_VERSION = "v0.1.1a";
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

        private ZippedFileCollection zip;

        private string SELECTED_FILE;

        private ObservableCollection<HeaderData> headerData;
        
        public class HeaderData : INotifyPropertyChanged {
            public string SourceName { get; set; }
            public bool Include { get; set; }

            public string NameMapping { get; set; }

            public event PropertyChangedEventHandler? PropertyChanged;

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

                if (zip != null)
                    zip.DestroyCollection();

                zip = ZippedFileCollection.Open(openFileDialog.FileName, ApplicationConstants.TEMP_DATA_PATH);

                List<string> potentialFiles = new List<string>();
                foreach (string filename in zip.GetFiles(".csv")) {
                    Debug.Log("Found File in cluster: " + filename);

                    if (filename.Contains("header", StringComparison.OrdinalIgnoreCase) || filename.Contains("collar", StringComparison.OrdinalIgnoreCase)) {
                        potentialFiles.Add(filename);
                    }
                }

                CollarFileSelection selectionWindow = new CollarFileSelection(potentialFiles);
                selectionWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                if (selectionWindow.ShowDialog() == true) {

                    if (selectionWindow.SelectionOutcome == CollarSelectionResult.SelectionMade) {
                        SELECTED_FILE = zip.GetFilePath(selectionWindow.SelectedFile);
                    } else {
                        OpenFileDialog otherFileDialog = new();
                        otherFileDialog.Multiselect = false;
                        otherFileDialog.InitialDirectory = Path.Join(Directory.GetCurrentDirectory(), zip.CollectionLocation.Trim('.'));
                        Debug.Status("Using manual Selection Fallback @ " + Path.Join(Directory.GetCurrentDirectory(), zip.CollectionLocation));

                        if (otherFileDialog.ShowDialog() == true) {
                            SELECTED_FILE = otherFileDialog.FileName;
                        }
                        else {
                            return;
                        }
                    }

                    headerData = new ObservableCollection<HeaderData>();
                    MainGrid.DataContext = headerData;
                    ProjectAreaList.Items.Clear();

                    using CSVReader reader = new CSVReader(SELECTED_FILE);

                    string[] fields = reader.FieldNames;
                    Debug.Notify("MainWindow: Selected Headers: " + fields.FlattenToString());

                    foreach (string field in fields) {
                        headerData.Add(new HeaderData() {
                            SourceName = field.Trim('"'),
                            Include = false
                        });
                    }

                    List<string> areas = new();

                    reader.Reset();

                    Debug.Status("Collar ANALYSE ON");

                    if (!reader.FieldNames.Contains("\"ProjectArea\""))
                        return;

                    foreach (string value in reader.GetField("\"ProjectArea\"")) {
                        if (areas.Contains(value)) {
                            continue;
                        }

                        CheckBox checkBox = new()
                        {
                            Content = value.Trim('"')
                        };

                        checkBox.Checked += UpdateHoleSelectionList;
                        ProjectAreaList.Items.Add(checkBox);

                        areas.Add(value);
                    }

                    Trace.WriteLine("MainWindow: Attempting To Process File");
                }

            } else {
                Debug.Warn("MainWindow: User selected a bad file. Not correct format");
                MessageBox.Show("The File select either does not exist or is not a ZIP file", "Load File Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<string> SelectedProjects = new();
        private void UpdateHoleSelectionList(object sender, RoutedEventArgs e) {
            SelectedProjects.Clear();
            foreach (CheckBox item in ProjectAreaList.Items) {
                if ((bool)item.IsChecked)
                    SelectedProjects.Add(item.Content.ToString());
            }
            Debug.Status("Selected Projects Updated: " + SelectedProjects.FlattenToString());
        }

        private void Button_SaveToDestination(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new();
            if (dialog.ShowDialog() == true) {
                DataDestinationLocationPath.Text = dialog.SelectedPath;
                Debug.Log(" Selected an export path");

                string exportLocation = dialog.SelectedPath;

                List<string> includedFields = new List<string>();
                foreach (HeaderData row in headerData) {
                    if (row.Include)
                        includedFields.Add(row.SourceName);
                }

                DataProcessor.DoWorkWithModal(progress => {

                    using StreamWriter writer = new StreamWriter(exportLocation + "\\TEST_EXPORT_CollarFile.csv");
                    using CSVReader reader = new CSVReader(SELECTED_FILE);
                    
                    writer.WriteLine(includedFields.FlattenToString());
                    while (reader.IsEOF == false) {
                        CSVRow row = reader.ReadRow();

                        string[] values = new string[includedFields.Count];
                        for (int i = 0; i < values.Length; i++) {
                            values[i] = row.GetField('"' + includedFields[i] + '"');
                        }
                        if (SelectedProjects.Contains(row.GetField("\"ProjectArea\"").Trim('"'))) {
                            writer.WriteLine(values.FlattenToString());
                        }

                        progress.Report(reader.ReadPercentage * 100d);
                    }
                });

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
            zip.DestroyCollection();
            zip = null;
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
            if (zip != null)
                zip.DestroyCollection();
            base.OnClosed(e);
            consoleWindow.Close();
        }
    }
}
