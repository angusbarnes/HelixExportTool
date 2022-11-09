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
using LazyCSV;

namespace HLXExport {
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window {
        public ProgressWindow() {
            InitializeComponent();
        }

        public double Progress { set { ProgressBar.Value = value; } }

        public void JobDescription(string Title, string Description) {
            this.Title = Title;
            JobDescriptionText.Content = Description;
        }
    }
}
