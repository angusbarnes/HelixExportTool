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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace HLXExport
{
    /// <summary>
    /// Interaction logic for DisplayGridRow.xaml
    /// </summary>
    public partial class DisplayGridRow : UserControl
    {
        public DisplayGridRow()
        {
            InitializeComponent();
            AddButtonField("Test Button");
            AddLabel("Test Label");
            AddTextBox("Hmm");
        }

        public void AddButtonField(string ButtonText)
        {
            Row.Children.Add(new Button() { Content = ButtonText, Margin = new Thickness(0, 0, 15, 0), Height = 20 });
        }

        public void AddLabel(string LabelText)
        {
            Row.Children.Add(new Label() { Content = LabelText, Margin = new Thickness(0, 0, 15, 0), Height = 23 });
        }

        public void AddTextBox(string PlaceholderText)
        {
            Row.Children.Add(new TextBox() { Text = PlaceholderText, MinWidth = 120, Margin = new Thickness(0, 0, 15, 0), Height = 20 });
        }
    }
}
