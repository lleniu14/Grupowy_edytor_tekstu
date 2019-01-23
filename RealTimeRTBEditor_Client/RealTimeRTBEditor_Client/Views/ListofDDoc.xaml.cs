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

namespace RealTimeRTBEditor_Client
{
    /// <summary>
    /// Logika interakcji dla klasy ListofDDoc.xaml
    /// </summary>
    public partial class ListofDDoc : Window
    {
        TextEditor _editor;
        public List<Items> items = new List<Items>();
        public List<string> l = new List<string>();
        public ListofDDoc(string list, TextEditor textEditor)
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
            listBox_Local.ItemsSource = l;
            Visibility = Visibility.Visible;
        }
        private void btnChooseDoc_Click(object sender, RoutedEventArgs e)
        {
            int idx = listBox_Local.SelectedIndex;
            int id = items[idx].DocId;
            _editor.client.SendChooseDoc(id);
            Close();
        }
    }
}
