using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AtlasServiceCenter.Windows;

namespace AtlasServiceCenter
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            // Сначала показываем окно заставки
            var splash = new SplashScreenWindow();
            this.MainWindow = splash;
            splash.Show();
        }
    }
}
