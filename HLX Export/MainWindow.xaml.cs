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
using System.Drawing;

namespace HLXExport
{
    public partial class MainWindow : Window
    {
        private ConsoleWindow consoleWindow = new();
        private readonly MainWindowListener mainWindowListener = new();

        private ZippedFileCollection zip;

        private string CURRENT_SELECTED_FILE;
        private string SETTINGS_FILE = ".\\settings.json";

        private ObservableCollection<RowDisplayData> temporaryGridData;

        private Dictionary<string, RowDisplayData[]> fileSpecificExportSettings = new Dictionary<string, RowDisplayData[]>();
        
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

            MainGrid.CellEditEnding += MainGrid_CellEditEnding;
        }

        private bool CURRENT_FILE_IS_DIRTY = false;
        private void MainGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
        {
            Debug.Status("Edited Row at index: " + e.Row.GetIndex());
            CURRENT_FILE_IS_DIRTY = true;
        }

        public Dictionary<string, DataModel> fileSettings = new Dictionary<string, DataModel>();
        public ObservableCollection<RowDisplayData> GenerateGridFromSettings(string filename)
        {
            throw new System.NotImplementedException();
        }

        public ObservableCollection<RowDisplayData> GenerateGridFromFile(string filename, DataModel settings)
        {
            ObservableCollection<RowDisplayData> table = new ObservableCollection<RowDisplayData>();

            using CSVReader reader = new CSVReader(filename);

            string[] fields = reader.FieldNames;
            foreach (string field in fields)
            {
                string fieldname = field.Trim('"');
                RowDisplayData data = settings.GetAsRowDisplay(fieldname);

                string ParseElementField(string[] tokens)
                {
                    string TwoLetterCode = tokens[0];
                    string unit = tokens[1];
                    if (Elements.MatchCommonName(TwoLetterCode))
                    {
                        unit = unit == "%" ? "per" : unit;

                        return $"{TwoLetterCode}_{unit}";
                    }

                    return "";
                }

                string[] tokens = data.FieldName.Split();
                string assumedName = "";

                if (tokens.Length == 2)
                    assumedName = ParseElementField(tokens);

                if (string.IsNullOrEmpty(assumedName) == false)
                    data.SuggestedName = assumedName;

                data.IdentifiedElement = Elements.MatchCommonName(tokens[0]).CommonName;
                var sourceName = data.FieldName;

                table.Add(data);
            }

            return table;
        }

        public DataManager dataManager = new DataManager();
        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0)
                return;

            if (CURRENT_FILE_IS_DIRTY)
            {
                dataManager.UpdateDataModelFromDisplay(Path.GetFileName(CURRENT_SELECTED_FILE), temporaryGridData);
                CURRENT_FILE_IS_DIRTY = false;
            }

            CURRENT_SELECTED_FILE = zip.GetFilePath(e.AddedItems[0].ToString());

            DataModel model = dataManager.GetModel(e.AddedItems[0].ToString());
            temporaryGridData = GenerateGridFromFile(CURRENT_SELECTED_FILE, model);
            MainGrid.DataContext = temporaryGridData;
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

                if (File.Exists(Path.Combine(ApplicationConstants.DEFAULT_PROFILE_PATH, "DefaultProfile.json")))
                {
                    SelectNewProfile(Path.Combine(ApplicationConstants.DEFAULT_PROFILE_PATH, "DefaultProfile.json"));
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
                        CURRENT_SELECTED_FILE = zip.GetFilePath(selectionWindow.SelectedFile);
                    } else {
                        OpenFileDialog otherFileDialog = new();
                        otherFileDialog.Multiselect = false;
                        otherFileDialog.InitialDirectory = Path.Join(Directory.GetCurrentDirectory(), zip.CollectionLocation.Trim('.'));
                        Debug.Status("Using manual Selection Fallback @ " + Path.Join(Directory.GetCurrentDirectory(), zip.CollectionLocation));

                        if (otherFileDialog.ShowDialog() == true) {
                            CURRENT_SELECTED_FILE = otherFileDialog.FileName;
                        }
                        else {
                            return;
                        }
                    }

                    FileList.SelectedValue = Path.GetFileName(CURRENT_SELECTED_FILE);

                    ProjectAreaList.Items.Clear();

                    using CSVReader reader = new CSVReader(CURRENT_SELECTED_FILE);

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
                            Content = value.Trim('"'),
                            IsChecked = true
                        };

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

        private void ExportDataToFile(string FilePath, bool OpenFileLocation)
        {
            IEnumerable<string> files = zip.Filter(".csv");

            List<string> SelectedProjects = new List<string>();
            foreach (CheckBox item in ProjectAreaList.Items)
            {
                if ((bool)item.IsChecked)
                    SelectedProjects.Add(item.Content.ToString());
            }

            //DataProcessor.DoWorkWithModal(progress => {

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);

                DataModel data = dataManager.GetModel(fileName);

                List<string> includedFieldNames = new List<string>();
                List<string> includedFields = new List<string>();

                foreach (KeyValuePair<string, FieldSettings> field in data.ExposeDictionary())
                {
                    if(field.Value.IncludeField)
                    {
                        includedFields.Add(field.Key);

                        string name = field.Value.NameMapping;
                        if (string.IsNullOrEmpty(name))
                            name = field.Key;

                        includedFieldNames.Add(name);
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
                        
                    if ( row.ContainsField("\"ProjectArea\"") && SelectedProjects.Contains(row.GetField("\"ProjectArea\"").Trim('"')))
                    {
                        writer.WriteLine(values.FlattenToString());
                    }

                    //progress.Report(reader.ReadPercentage * 100d);
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
            //});
        }

        private void Button_SaveToDestination(object sender, RoutedEventArgs e)
        {
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

            SaveProfileToFile(SETTINGS_FILE);
        }

        private void SaveProfileToFile(string SettingsFileName)
        {
            dataManager.Save(SettingsFileName);
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

        private void Button_LoadFromProfile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                InitialDirectory = ApplicationConstants.DEFAULT_PROFILE_PATH
            };

            if (dialog.ShowDialog() == true)
            {
                SelectNewProfile(dialog.FileName);
            }
        }

        private void Button_SaveNewProfile(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This Tool is in alpha development. Breaking changes may occur to saved export profiles.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);

            if (Directory.Exists(".\\profiles") == false)
                Directory.CreateDirectory(".\\profiles");

            if (CURRENT_FILE_IS_DIRTY)
            {
                dataManager.UpdateDataModelFromDisplay(Path.GetFileName(CURRENT_SELECTED_FILE), temporaryGridData);
                CURRENT_FILE_IS_DIRTY = false;
            }
                
            SaveFileDialog dialog = new SaveFileDialog()
            {
                AddExtension = true,
                DefaultExt = ".json",
                CheckPathExists = true,
                OverwritePrompt = true,
                InitialDirectory = ApplicationConstants.DEFAULT_PROFILE_PATH
            };

            if (dialog.ShowDialog() == true)
            {
                SaveProfileToFile(dialog.FileName);
            }
        }

        private void SelectNewProfile(string ProfilePath)
        {
            if (File.Exists(ProfilePath) == false)
                Debug.Error("Could not find the specified profile: " + ProfilePath);

            dataManager.Load(ProfilePath);
            temporaryGridData = GenerateGridFromFile(CURRENT_SELECTED_FILE, dataManager.GetModel(Path.GetFileName(CURRENT_SELECTED_FILE)));
            MainGrid.DataContext = temporaryGridData;

            CurrentProfileName.Content = Path.GetFileName(ProfilePath);
        }

        private void Button_GenerateNameSuggestions(object sender, RoutedEventArgs e)
        {
            if (temporaryGridData == null)
                return;

            foreach(RowDisplayData row in temporaryGridData)
            {
                if (string.IsNullOrEmpty(row.SuggestedName) == false)
                    row.NameMapping = row.SuggestedName;
            }
            MainGrid.Items.Refresh();
            CURRENT_FILE_IS_DIRTY = true;
            var result = MessageBox.Show("This is an experimental Feature. Please verify all generated names. Would you like to keep these changes?", "Name Generation", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.No)
            {
                foreach (RowDisplayData row in temporaryGridData)
                {
                    if (string.IsNullOrEmpty(row.SuggestedName) == false)
                        row.NameMapping = "";
                }
                MainGrid.Items.Refresh();
            }

        }
    }
}
