using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RealTimeRTBEditor_Client
{
    /// <summary>
    /// Logika interakcji dla klasy App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Start(object sender, StartupEventArgs e)
        {
            StartupUri = new Uri("Views/LoginScreen.xaml", UriKind.Relative);
        }
    }
}
