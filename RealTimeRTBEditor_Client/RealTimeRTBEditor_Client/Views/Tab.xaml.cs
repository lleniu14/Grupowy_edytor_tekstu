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

namespace RealTimeRTBEditor_Client
{
    public partial class Tab : Window
    {
        TextEditor _editor;
        List<Items> items = new List<Items>();
        public Tab(TextEditor editor)
        {
            InitializeComponent();
            _editor = editor;
            Visibility = Visibility.Visible;
        }
        private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox textBox_kolumny = sender as TextBox;
            if (textBox_kolumny.Text == "Liczba kolumn")
                textBox_kolumny.Text = string.Empty;
            if (textBox_wiersze.Text == "Liczba wierszy")
                textBox_wiersze.Text = string.Empty;
        }

        private void btnAddDoc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int columns = int.Parse(textBox_kolumny.Text);
                int rows = int.Parse(textBox_wiersze.Text);
                _editor.CreateTable(columns, rows);
                Close();
            }
            catch
            {
                MessageBox.Show("Wpisane wartości nie są liczbami");
            }
        }

    }

    
}
