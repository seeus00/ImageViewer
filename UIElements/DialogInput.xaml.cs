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

namespace ImageViewer.UIElements
{
    public partial class DialogInput : Window
    {
        public string HostText
        {
            get { return HostTextBox.Text; }
            set { HostTextBox.Text = value; }
        }
        public string PortText
        {
            get { return PortTextBox.Text; }
            set { PortTextBox.Text = value; }
        }

        public string UsernameText
        {
            get { return UsernameTextBox.Text; }
            set { UsernameTextBox.Text = value; }
        }
        public string PasswordText
        {
            get { return PasswordTextBox.Text; }
            set { PasswordTextBox.Text = value; }
        }

        public DialogInput()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
