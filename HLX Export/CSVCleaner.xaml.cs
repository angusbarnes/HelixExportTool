using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace HLXExport
{
    /// <summary>
    /// Interaction logic for CSVCleaner.xaml
    /// </summary>
    public partial class CSVCleaner : Window
    {
        public CSVCleaner()
        {
            InitializeComponent();
        }

        private void LoadFromFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();

            string selectedFile = "";
            if (openFileDialog.ShowDialog() == true)
            {

                LoadPath.Text = openFileDialog.FileName;
                selectedFile = openFileDialog.FileName;

            }
            else
            {
                MessageBox.Show("Please Select a File", "Loading Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

    }
}
