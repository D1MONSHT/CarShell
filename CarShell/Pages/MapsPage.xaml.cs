using System;
using System.Windows;
using System.Windows.Controls;
using CarShell.Services;

namespace CarShell.Pages
{
    public partial class MapsPage : UserControl
    {
        private readonly MainWindow mainWindow;
        private readonly LocalMapServer mapServer = new();

        public MapsPage(MainWindow mainWindow)
        {
            InitializeComponent();

            this.mainWindow = mainWindow;

            Loaded += MapsPage_Loaded;
        }

        private async void MapsPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                mapServer.Start();

                await MapWeb.EnsureCoreWebView2Async();

               // MapWeb.CoreWebView2.OpenDevToolsWindow();

                MapWeb.Source = new Uri(mapServer.Url + "map.html");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка запуска карты");
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.ShowHome();
        }
    }
}