using Microsoft.Win32;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System;
using static HLXExport.Utilities;
using System.IO;
using LazyCSV;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows.Documents;

namespace HLXExport
{
    public partial class MainWindow : Window
    {
        private ConsoleWindow consoleWindow = new();
        private readonly MainWindowListener mainWindowListener = new();

        private ZippedFileCollection zip;

        private string SELECTED_FILE;

        private ObservableCollection<CSVTableHeaderData> temporaryGridData;

        private Dictionary<string, CSVTableHeaderData[]> fileSpecificExportSettings = new Dictionary<string, CSVTableHeaderData[]>();
        
        public class CSVTableHeaderData : INotifyPropertyChanged {
            public string SourceName { get; set; }
            public bool Include { get; set; }

            public string NameMapping { get; set; }

            public string DesiredUnit { get; set; }
            public string DetectedUnit { get; set; }

            public string BaseElementInfo { get; set; }

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

            FileList.SelectionChanged += FileList_SelectionChanged;
        }

        private void SaveDataTable(string tableKey, IEnumerable<CSVTableHeaderData> tableHeaderSettings)
        {
            if (fileSpecificExportSettings.ContainsKey(tableKey))
            {
                fileSpecificExportSettings[tableKey] = tableHeaderSettings.ToArray();
                return;
            }

            fileSpecificExportSettings.Add(tableKey, tableHeaderSettings.ToArray());
        }

        private void PopulateTable(IEnumerable<CSVTableHeaderData> tableData)
        {
            Debug.Status("MainWindow: Attempting To Populate DataGrid");
            ObservableCollection<CSVTableHeaderData> list = new ObservableCollection<CSVTableHeaderData>();
            tableData.ToList().ForEach(list.Add);
            temporaryGridData = list;
            MainGrid.DataContext = list;
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0)
                return;

            if(temporaryGridData != null)
                SaveDataTable(Path.GetFileName(SELECTED_FILE), temporaryGridData);

            SELECTED_FILE = zip.GetFilePath(e.AddedItems[0].ToString());

            string fileListSelection = e.AddedItems[0].ToString();

            if (fileSpecificExportSettings.ContainsKey(fileListSelection))
            {
                Debug.Status("MainWindow: Found File Specific Settings for file: " + fileListSelection);
                PopulateTable(fileSpecificExportSettings[fileListSelection]);
                return;
            }

            temporaryGridData = new ObservableCollection<CSVTableHeaderData>();
            MainGrid.DataContext = temporaryGridData;

            using CSVReader reader = new CSVReader(SELECTED_FILE);

            string[] fields = reader.FieldNames;
            Debug.Notify("MainWindow: Previously Unknown File Loaded with " + fields.Length + " headers in file @ " + SELECTED_FILE);

            MinStatusBar.Value = 0;
            foreach (string field in fields)
            {
                CSVTableHeaderData data = new CSVTableHeaderData()
                {
                    SourceName = field.Trim('"'),
                    Include = false
                };

                var commonName = Elements.MatchCommonName(data.SourceName[..2]);
                var sourceName = data.SourceName;
                
                if (commonName.IsMatch) {
                    data.BaseElementInfo = commonName.CommonName;

                    if (sourceName.Contains("ppm")) {
                        data.DetectedUnit = "PPM";
                    } 
                    else if (sourceName.Contains("ppb")) {
                        data.DetectedUnit = "PPB";
                    } 
                    else if (sourceName.Contains("%")) {
                        data.DetectedUnit = "PERCENT";
                    }

                    data.BaseElementInfo = $"{commonName.CommonName}_{data.DetectedUnit}";
                }

      
                temporaryGridData.Add(data);

                MinStatusBar.Value += 5;
            }

            MinStatusBar.Value = 0;
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

                FileList.Items.Clear();

                if(openFileDialog.FileName.EndsWith(".zip") == false)
                {
                    MessageBox.Show("Please Select a valid ZIP file", "Loading Error", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (File.Exists(".\\settings.json"))
                {
                    string jsonString = File.ReadAllText(".\\settings.json");
                    fileSpecificExportSettings = JsonSerializer.Deserialize<Dictionary<string, CSVTableHeaderData[]>>(jsonString);
                }

                zip = ZippedFileCollection.Open(openFileDialog.FileName, ApplicationConstants.TEMP_DATA_PATH);

                List<string> potentialFiles = new List<string>();
                foreach (string filename in zip.GetFiles(".csv")) {

                    FileList.Items.Add(Path.GetFileName(filename));

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

                    FileList.SelectedValue = Path.GetFileName(SELECTED_FILE);

                    ProjectAreaList.Items.Clear();

                    using CSVReader reader = new CSVReader(SELECTED_FILE);

                    List<string> areas = new();

                    reader.Reset();

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
                    Debug.Callout("Latched Collar File, found " + reader.RowCount + " holes");
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

        private void ExportDataToFile(string FilePath, bool OpenFileLocation)
        {
            IEnumerable<string> files = zip.Filter(".csv");

            DataProcessor.DoWorkWithModal(progress => {

                foreach(string file in files)
                {
                    string fileName = Path.GetFileName(file);

                    if (fileSpecificExportSettings.ContainsKey(fileName) == false)
                        continue;
                    
                    List<string> includedFields = new List<string>();
                    List<string> includedFieldNames = new List<string>();
                    
                    CSVTableHeaderData[] csvHeaders = fileSpecificExportSettings[fileName];
                    
                    foreach (CSVTableHeaderData row in csvHeaders)
                    {
                        if (row.Include)
                        {
                            includedFields.Add(row.SourceName);

                            bool nameRemapped = string.IsNullOrEmpty(row.NameMapping);
                            includedFieldNames.Add(nameRemapped ? row.SourceName : row.NameMapping);
                        }
                    }

                    using StreamWriter writer = new StreamWriter(FilePath + "\\" + fileName);
                    using CSVReader reader = new CSVReader(file);

                    writer.WriteLine(includedFieldNames.FlattenToString());
                    
                    while (reader.IsEOF == false)
                    {
                        CSVRow row = reader.ReadRow();

                        string[] values = new string[includedFields.Count];
                        
                        for (int i = 0; i < values.Length; i++)
                        {
                            values[i] = row.GetField('"' + includedFields[i] + '"');
                        }
                        
                        if (SelectedProjects.Contains(row.GetField("\"ProjectArea\"").Trim('"')))
                        {
                            writer.WriteLine(values.FlattenToString());
                        }

                        progress.Report(reader.ReadPercentage * 100d);
                    }
                }
                
                string p = FilePath;
                string args = string.Format("/e, /select, \"{0}\"", p);

                if (OpenFileLocation)
                {
                    ProcessStartInfo info = new();
                    info.FileName = "explorer";
                    info.Arguments = args;
                    Process.Start(info);
                }
            });
        }

        private void Button_SaveToDestination(object sender, RoutedEventArgs e)
        {
            // Configure save file dialog box
            //Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog()
            //{
            //    FileName = "Exported_Collar",
            //    DefaultExt = ".csv",
            //    Filter = "Comma Seperated Values |*.csv;*.txt;*.tsv"
            //};

            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            // Show save file dialog box
            bool? result = dialog.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                //string exportLocation = dialog.FileName;
                string exportLocation = dialog.SelectedPath;
                DataDestinationLocationPath.Text = exportLocation;
                bool openFileLocation = (bool)OpenFileLocation.IsChecked;

                Debug.Log(" Selected an export path: " + exportLocation);

                ExportDataToFile(exportLocation, openFileLocation);
                
            }
            SaveDataTable(Path.GetFileName(SELECTED_FILE), temporaryGridData);
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(fileSpecificExportSettings, options);
            File.WriteAllText(".\\settings.json", jsonString);
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

            if (zip != null) {
                zip.DestroyCollection();
                zip = null;
            }
 
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

        private void Button_SelectAllProjects(object sender, RoutedEventArgs e)
        {
            foreach(CheckBox box in ProjectAreaList.Items) {
                box.IsChecked = !box.IsChecked;
            }
        }
    }
}
