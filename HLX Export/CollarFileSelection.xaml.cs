using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HLXExport {
    /// <summary>
    /// Interaction logic for CollarFileSelection.xaml
    /// </summary>
    public partial class CollarFileSelection : Window {
        public CollarFileSelection() {
            InitializeComponent();
        }

        public CollarFileSelection(List<string> files) {
            InitializeComponent();

            foreach (string file in files) {
                FileOptions.Items.Add(file);
            }
        }

        public string SelectedFile;

        private void Button_Locate(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new();

            if(openFileDialog.ShowDialog() == true) {

            }
        }

        private void Button_Select(object sender, RoutedEventArgs e) {
            if (string.IsNullOrEmpty(SelectedFile))
                return;

            Close();
        }
    }
}
