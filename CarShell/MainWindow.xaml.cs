using System.Windows;
using CarShell.Pages;

namespace CarShell
{
    public partial class MainWindow : Window
    {
        private HomePage homePage;
        private MusicPage musicPage;
        private NavigationPage navigationPage;
        private CameraPage cameraPage;
        private SettingsPage settingsPage;
        private BoardPage boardPage;
        private ErrorPage errorPage;
        private MapsPage mapsPage;
        private DiagnosticsPage diagnosticsPage;

        public MainWindow()
        {
            App.WriteBootLog("MainWindow constructor START");

            InitializeComponent();
            App.WriteBootLog("InitializeComponent finished");

            homePage = new HomePage(this);
            App.WriteBootLog("HomePage created");

           musicPage = new MusicPage(this);
            App.WriteBootLog("MusicPage created");

            navigationPage = new NavigationPage(this);
            App.WriteBootLog("NavigationPage created");
/*
            cameraPage = new CameraPage(this);
            App.WriteBootLog("CameraPage created");*/

            settingsPage = new SettingsPage(this);
            App.WriteBootLog("SettingsPage created");

            boardPage = new BoardPage(this);
            App.WriteBootLog("BoardPage created");

            errorPage = new ErrorPage(this);
            App.WriteBootLog("ErrorPage created");

            diagnosticsPage = new DiagnosticsPage(this);
            App.WriteBootLog("DiagnosticsPage created");

           /*sPage = new MapsPage(this);
            App.WriteBootLog("MapsPage created");*/

            ShowHome();

            Loaded += (_, _) =>
                App.WriteBootLog("MainWindow LOADED");

            ContentRendered += (_, _) =>
                App.WriteBootLog("MainWindow CONTENT RENDERED");

            App.WriteBootLog("MainWindow constructor END");
        }
       /*lic void ShowMaps()
        {
            MainContent.Content = mapsPage;
        }*/
        public void ShowHome()
        {
            MainContent.Content = homePage;
        }

        public void ShowMusic()
        {
            MainContent.Content = musicPage;
        }

        public void ShowNavigation()
        {
            MainContent.Content = navigationPage;
        }/*
       
        public void ShowCamera()
        {
            MainContent.Content = cameraPage;
        }
       */
        public void ShowSettings()
        {
            MainContent.Content = settingsPage;
        }
        public void ShowBoard()
        {
            MainContent.Content = boardPage;
        }
        public void ShowError()
        {
            MainContent.Content = errorPage;
        }

        public void ShowDiagnostics()
        {
            MainContent.Content = diagnosticsPage;
        }
       //rivate void Maps_Click(object sender, RoutedEventArgs e) => ShowMaps();
        private void Home_Click(object sender, RoutedEventArgs e) => ShowHome();
       private void Navigation_Click(object sender, RoutedEventArgs e) => ShowNavigation();
        private void Music_Click(object sender, RoutedEventArgs e) => ShowMusic();
        //pivate void Camera_Click(object sender, RoutedEventArgs e) => ShowCamera();
        private void Settings_Click(object sender, RoutedEventArgs e) => ShowSettings();
        private void Board_Click(object sender, RoutedEventArgs e) => ShowBoard();
        private void Error_Click(object sender, RoutedEventArgs e) => ShowError();
        private void Diagnostics_Click(object sender, RoutedEventArgs e) => ShowDiagnostics();
        private void CheckEngineButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDiagnostics();
        }

        private async void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            await musicPage.PlayPause();
        }

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            await musicPage.NextTrack();
        }

        private async void Prev_Click(object sender, RoutedEventArgs e)
        {
            await musicPage.PrevTrack();
        }
    }
}