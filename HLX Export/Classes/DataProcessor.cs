using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using CsvHelper; 
using System.Diagnostics;
using System.IO.Compression;
using System.Data;
using System.ComponentModel;

namespace HLXExport
{
    public class DataProcessor
    {
        private string pathToDataFile;
        private string? selectedFile= null;

        public List<string> FilesLoaded = new();

        public Dictionary<string, List<string>> SelectedFileFieldValues = new();
        public Dictionary<string, DataTable> CSVTables = new();

        public bool LoadData(string PathToDataFile)
        {
            if (File.Exists(PathToDataFile) == false)
                return false;

            if (PathToDataFile.EndsWith(".zip") == false)
                return false;

            pathToDataFile = PathToDataFile;

            return true;
        }

        public void ReadData(bool RemovePrevious = false)
        {
            if (RemovePrevious)
                UnloadData();

            DoWorkWithModal(progress => {
                using (ZipArchive archive = new(new StreamReader(pathToDataFile).BaseStream)) {
                    double count = archive.Entries.Count;
                    for(int i = 0; i < count; i++) {
                        ZipArchiveEntry entry = archive.Entries[i];
                       
                        if (entry.Name.EndsWith(".csv") == false)
                            continue;

                        FilesLoaded.Add(entry.Name);

                        var stream = entry.Open();
                        using var reader = new StreamReader(stream);
                        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                        using (var dr = new CsvDataReader(csv)) {
                            var dt = new DataTable();
                            dt.TableName = entry.Name;
                            dt.Load(dr);

                            CSVTables.Add(entry.Name, dt);
                        }

                        progress.Report((i / count) * 100d);
                    }
                }
            });
        }

        public static void DoWorkWithModal(Action<IProgress<double>> work) {
            ProgressWindow progressDialogue = new ProgressWindow();
            progressDialogue.JobDescription("Loading Data", "Importing Data Files");
            progressDialogue.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            progressDialogue.Loaded += (_, args) =>
            {
                BackgroundWorker worker = new BackgroundWorker();

                Progress<double> progress = new Progress<double>(
                    data => progressDialogue.Progress = data);

                worker.DoWork += (s, workerArgs) => work(progress);

                worker.RunWorkerCompleted +=
                    (s, workerArgs) => progressDialogue.Close();

                worker.RunWorkerAsync();
            };

            progressDialogue.ShowDialog();
        }
        public List<string> GetFieldValuesFromSelectedFile(string FieldID) {
            if (SelectedFileFieldValues.ContainsKey(FieldID))
                return SelectedFileFieldValues[FieldID];

            Debug.Error("DataProcessor: The Requested Field ID Does not exist");
            return null;
        }

        public void UnloadData()
        {
            FilesLoaded.Clear();
            selectedFile = null;
            SelectedFileFieldValues.Clear();
            CSVTables.Clear();
        }

        public void SelectFile(string Filename) {
            selectedFile = Filename;

            if (CSVTables.ContainsKey(Filename)) {
                DataTable dt = CSVTables[Filename];


                for (int i = 0; i < dt.Columns.Count; i++) {
                    string FieldName = dt.Columns[i].ColumnName;
                    List<string> fieldValues = new();
                    Trace.WriteLine("DataProcessor: " + FieldName);
                    for (int j = 0; j < dt.Rows.Count; j++) {
                        fieldValues.Add(dt.Rows[j][i].ToString());
                    }

                    SelectedFileFieldValues.Add(FieldName, fieldValues);

                }              
            }
        }

        public string? GetSelectedFile() {
            return selectedFile;
        }
    }
}
