using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace CarShell.Pages
{
    public partial class SettingsPage : UserControl
    {
        private readonly MainWindow mainWindow;

        public SettingsPage(MainWindow mainWindow)
        {
            InitializeComponent();

            this.mainWindow = mainWindow;

            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadVersionInformation();
            UpdateDateTimeText();
            ShowPanel(NetworkPanel, NetworkNavigationButton);
        }

        private void LoadVersionInformation()
        {
            try
            {
                string version = VersionInfo.Version;

                CurrentVersionText.Text = version;
                SidebarVersionText.Text = $"Версия {version}";
                AboutVersionText.Text = $"Версия {version}";
            }
            catch
            {
                CurrentVersionText.Text = "Неизвестно";
                SidebarVersionText.Text = "Версия неизвестна";
                AboutVersionText.Text = "Версия неизвестна";
            }
        }

        private void UpdateDateTimeText()
        {
            DateTimeStatusText.Text =
                $"{DateTime.Now:dd.MM.yyyy, HH:mm}";
        }

        private void HideAllPanels()
        {
            NetworkPanel.Visibility = Visibility.Collapsed;
            SystemPanel.Visibility = Visibility.Collapsed;
            UpdatePanel.Visibility = Visibility.Collapsed;
            CarPanel.Visibility = Visibility.Collapsed;
            AboutPanel.Visibility = Visibility.Collapsed;

            ResetNavigationButtons();
        }

        private void ResetNavigationButtons()
        {
            NetworkNavigationButton.Background =
                System.Windows.Media.Brushes.Transparent;

            SystemNavigationButton.Background =
                System.Windows.Media.Brushes.Transparent;

            UpdateNavigationButton.Background =
                System.Windows.Media.Brushes.Transparent;

            CarNavigationButton.Background =
                System.Windows.Media.Brushes.Transparent;

            AboutNavigationButton.Background =
                System.Windows.Media.Brushes.Transparent;
        }

        private void ShowPanel(
            FrameworkElement panel,
            Button selectedButton)
        {
            HideAllPanels();

            panel.Visibility = Visibility.Visible;

            selectedButton.Background =
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(
                        24,
                        63,
                        91));
        }

        private void BackButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            mainWindow.ShowHome();
        }

        private void NetworkNavigationButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPanel(
                NetworkPanel,
                NetworkNavigationButton);
        }

        private void SystemNavigationButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            UpdateDateTimeText();

            ShowPanel(
                SystemPanel,
                SystemNavigationButton);
        }

        private void UpdateNavigationButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPanel(
                UpdatePanel,
                UpdateNavigationButton);
        }

        private void CarNavigationButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPanel(
                CarPanel,
                CarNavigationButton);
        }

        private void AboutNavigationButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPanel(
                AboutPanel,
                AboutNavigationButton);
        }

        private void WifiToggle_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Реальное включение и отключение Wi-Fi
            // подключим через RadioService следующим этапом.

            WifiStatusText.Text =
                WifiToggle.IsChecked == true
                    ? "Wi-Fi включён"
                    : "Wi-Fi выключен";
        }

        private void BluetoothToggle_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Реальное включение и отключение Bluetooth
            // подключим через RadioService следующим этапом.

            BluetoothStatusText.Text =
                BluetoothToggle.IsChecked == true
                    ? "Bluetooth включён"
                    : "Bluetooth выключен";
        }

        private void OpenWifiButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Следующим шагом здесь будет список доступных Wi-Fi сетей.",
                "CarShell",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void OpenBluetoothButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Следующим шагом здесь будет список Bluetooth-устройств.",
                "CarShell",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void CheckUpdatesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            UpdateStatusText.Text =
                "Переход к проверке обновлений...";

            /*
             * Если старая UpdatePage пока сохранена,
             * здесь можно вызвать:
             *
             * mainWindow.ShowUpdate();
             */
        }

        private void VersionHistoryButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Здесь будет список GitHub Releases с возможностью выбрать версию.",
                "История версий",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BrightnessButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenWindowsSettings("ms-settings:display");
        }

        private void VolumeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenWindowsSettings("ms-settings:sound");
        }

        private void DateTimeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenWindowsSettings("ms-settings:dateandtime");
        }

        private static void OpenWindowsSettings(string address)
        {
            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = address,
                        UseShellExecute = true
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось открыть настройки Windows.\n\n{ex.Message}",
                    "CarShell",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}