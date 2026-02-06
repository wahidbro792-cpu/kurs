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
using System.IO;

namespace AtlasServiceCenter.Windows
{
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponent();
            Loaded += SplashScreenWindow_Loaded;
        }

        private void SplashScreenWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Папка, где лежит exe
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                // Media\intro.mp4 рядом с exe
                string videoPath = Path.Combine(baseDir, "Media", "intro.mp4");

                if (File.Exists(videoPath))
                {
                    IntroMedia.Source = new Uri(videoPath, UriKind.Absolute);
                    IntroMedia.Play();
                }
                else
                {
                    // Видео не нашли – сразу идём на логин
                    OpenLoginAndClose();
                }
            }
            catch
            {
                // На любой ошибке – тоже сразу логин
                OpenLoginAndClose();
            }
        }

        private void IntroMedia_MediaEnded(object sender, RoutedEventArgs e)
        {
            OpenLoginAndClose();
        }

        private void IntroMedia_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            // Если видео не проигралось – просто продолжаем запуск
            OpenLoginAndClose();
        }

        private void OpenLoginAndClose()
        {
            var login = new LoginWindow();
            Application.Current.MainWindow = login;
            login.Show();

            this.Close();
        }
    }
}
