using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HLXExport {
    /// <summary>
    /// Interaction logic for CollarFileSelection.xaml
    /// </summary>
    public partial class CollarFileSelection : Window {
        public CollarFileSelection() {
            InitializeComponent();
        }

        public CollarSelectionResult SelectionOutcome;
        public CollarFileSelection(List<string> files) {
            InitializeComponent();

            MessageText.Text = $"Found {files.Count} potential header files. Please select one from the list or manually locate it.";

            FileOptions.SelectionMode = SelectionMode.Single;

            foreach (string file in files) {
                FileOptions.Items.Add(file.Split('\\').Last());
            }
        }

        public string? SelectedFile;

        private void Button_Locate(object sender, RoutedEventArgs e) {
            SelectionOutcome = CollarSelectionResult.ManualSelectionRequested;
            DialogResult = true;
            this.Close();
        }

        private void Button_Select(object sender, RoutedEventArgs e) {
            if (string.IsNullOrEmpty(SelectedFile))
                return;


            SelectionOutcome = CollarSelectionResult.SelectionMade;
            DialogResult = true;
            this.Close();
        }

        private void FileOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedFile = e.AddedItems[0].ToString();
            Debug.Log("CollarFileSelection:" + SelectedFile);
        }
    }

    public enum CollarSelectionResult { SelectionMade, ManualSelectionRequested}
}
