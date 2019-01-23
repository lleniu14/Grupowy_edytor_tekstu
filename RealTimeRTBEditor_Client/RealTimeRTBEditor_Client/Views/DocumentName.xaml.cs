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
    /// <summary>
    /// Logika interakcji dla klasy DocumentName.xaml
    /// </summary>
    public partial class DocumentName : Window
    {
        TextEditor _editor;
        public DocumentName(TextEditor editor)
        {
            _editor = editor;
            InitializeComponent();
            Visibility = Visibility.Visible;
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (docName.Text == null || docName.Text =="")
            {
                MessageBox.Show("Podaj nazwę");
            }
            else
            {
                _editor.client.SendCreateDocument(docName.Text);
                Close();
            }
        }
    }
}
