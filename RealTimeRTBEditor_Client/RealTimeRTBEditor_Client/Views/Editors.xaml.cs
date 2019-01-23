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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Editors : Window
    {
        TextEditor _editor;
        public List<string> lb = new List<string>();
        public List<User> users = new List<User>();
        public Editors(TextEditor editor, string list)
        {
            _editor = editor;
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
                    users.Add(new User { Name = name, UserId = id });
                    lb.Add(name);
                    idx = i + 1;
                    id = -1;
                    name = "";
                }
            }
            listView.ItemsSource = lb;
            Visibility = Visibility.Visible;
        }

        private void btnDodaj_Click(object sender, RoutedEventArgs e)
        {
            int idx = listView.SelectedIndex;
            int userid = users[idx].UserId;
            _editor.client.SendChooseUser(userid);
            Close();
        }
    }

    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; }
    }
}
