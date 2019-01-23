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
    public partial class ListofDoc : Window
    {
        TextEditor _editor;
        public List<Items> items = new List<Items>();
        public List<string> l = new List<string>();
        public ListofDoc(string list, TextEditor textEditor)
        {
            _editor = textEditor;
            InitializeComponent();
            int idx = 0;
            int id = -1;
            string name = "";
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == '.')
                {
                    id = int.Parse(list.Substring(idx, i - idx));
                    idx = i + 1;
                }
                if (list[i] == ',')
                {
                    name = list.Substring(idx, i - idx);
                    items.Add(new Items { Title = name, DocId = id });
                    l.Add(name);
                    idx = i + 1;
                    id = -1;
                    name = "";
                }
            }
            listBox.ItemsSource = l;
            Visibility = Visibility.Visible;
        }

        private void btnAddDoc_Click(object sender, RoutedEventArgs e)
        {
            DocumentName documentName = new DocumentName(_editor);
            Close();
        }

        private void btnRemoveDoc_Click(object sender, RoutedEventArgs e)
        {
            int idx = listBox.SelectedIndex;
            int id = items[idx].DocId;
            _editor.client.SendRemoveDocument(id);
            items.RemoveAt(idx);
            l.RemoveAt(idx);
            listBox.Items.Refresh();
        }

        private void btnEditDoc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int idx = listBox.SelectedIndex;
                int id = items[idx].DocId;
                _editor.client.SendChooseDoc(id);
                Close();
            }
            catch
            {
                MessageBox.Show("Wybierz dokument");
            }
        }

        private void btnShareDoc_Click(object sender, RoutedEventArgs e)
        {
            int idx = listBox.SelectedIndex;
            int id = items[idx].DocId;
            _editor.client.DocToShareId = id;
            _editor.client.SendRequest("08");
        }
    }

    public class Items
    {
        public string Title { get; set; }
        public int DocId { get; set; }
    }
}
