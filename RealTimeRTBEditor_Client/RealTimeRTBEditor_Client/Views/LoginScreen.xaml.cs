using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
    public partial class LoginScreen : Window
    {
        Client client;
        public LoginScreen()
        {
            client = new Client();
            client.LogIn += LogIn;
            client.Register += Register;
            InitializeComponent();           
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (txtUsername.Text == "" || txtPassword.Password == "") MessageBox.Show("Login i hasło nie mogą być puste");
            else
            {
                AuthorizationData authorizationData = new AuthorizationData
                {
                    username = txtUsername.Text,
                    password = txtPassword.Password
                };
                client.SendAuthorizationData(authorizationData, "01");
            }
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            if (txtUsername.Text == "" || txtPassword.Password == "") MessageBox.Show("Login i hasło nie mogą być puste");
            else
            {
                AuthorizationData authorizationData = new AuthorizationData
                {
                    username = txtUsername.Text,
                    password = txtPassword.Password
                };
                client.SendAuthorizationData(authorizationData, "02");
            }
        }

        private void LogIn(object sender, EditorData editorData)
        {
            if(editorData.Authorized)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate {
                    TextEditor textEditor = new TextEditor(editorData, client);
                    Close();
                });
            }
            else
            {
                MessageBox.Show("Niepoprawne dane");
            }
        }

        private void Register(object sender, EditorData editorData)
        {
            if (editorData.Authorized)
            {
                MessageBox.Show("Konto zostało utworzone");
            }
            else
            {
                MessageBox.Show("Login już istnieje");
            }
        }
    }

    public class AuthorizationData
    {
        public string username { get; set; }
        public string password { get; set; }
    }
}
