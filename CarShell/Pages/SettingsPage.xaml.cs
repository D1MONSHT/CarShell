using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CarShell.Pages.Settings;

namespace CarShell.Pages
{
    public partial class SettingsPage : UserControl
    {
        private readonly MainWindow mainWindow;
        private readonly UpdateSettingsControl updateSettingsControl;

        public SettingsPage(MainWindow mainWindow)
        {
            InitializeComponent();

            this.mainWindow = mainWindow;

            // Создаём экран обновлений один раз,
            // чтобы его состояние не терялось при переключении разделов.
            updateSettingsControl = new UpdateSettingsControl();

            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            LoadVersionInformation();
            UpdateDateTimeText();

            ShowPanel(
                NetworkPanel,
                NetworkNavigationButton);
        }

        private void LoadVersionInformation()
        {
            try
            {
                string version = VersionInfo.Version;

                SidebarVersionText.Text =
                    $"Версия {version}";

                AboutVersionText.Text =
                    $"Версия {version}";
            }
            catch
            {
                SidebarVersionText.Text =
                    "Версия неизвестна";

                AboutVersionText.Text =
                    "Версия неизвестна";
            }
        }

        private void UpdateDateTimeText()
        {
            DateTimeStatusText.Text =
                $"{DateTime.Now:dd.MM.yyyy, HH:mm}";
        }

        private void HideAllPanels()
        {
            NetworkPanel.Visibility =
                Visibility.Collapsed;

            SystemPanel.Visibility =
                Visibility.Collapsed;

            UpdatePanel.Visibility =
                Visibility.Collapsed;

            CarPanel.Visibility =
                Visibility.Collapsed;

            AboutPanel.Visibility =
                Visibility.Collapsed;

            ResetNavigationButtons();
        }

        private void ResetNavigationButtons()
        {
            NetworkNavigationButton.Background =
                Brushes.Transparent;

            SystemNavigationButton.Background =
                Brushes.Transparent;

            UpdateNavigationButton.Background =
                Brushes.Transparent;

            CarNavigationButton.Background =
                Brushes.Transparent;

            AboutNavigationButton.Background =
                Brushes.Transparent;
        }

        private void ShowPanel(
            FrameworkElement panel,
            Button selectedButton)
        {
            HideAllPanels();

            panel.Visibility =
                Visibility.Visible;

            selectedButton.Background =
                new SolidColorBrush(
                    Color.FromRgb(
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

            // Загружаем страницу обновлений только при первом открытии.
            if (UpdateSettingsFrame.Content == null)
            {
                UpdateSettingsFrame.Navigate(
                    updateSettingsControl);
            }
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
            // Пока меняем только текст.
            // Реальное управление адаптером добавим через RadioService.

            WifiStatusText.Text =
                WifiToggle.IsChecked == true
                    ? "Wi-Fi включён"
                    : "Wi-Fi выключен";
        }

        private void BluetoothToggle_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Пока меняем только текст.
            // Реальное управление Bluetooth добавим через RadioService.

            BluetoothStatusText.Text =
                BluetoothToggle.IsChecked == true
                    ? "Bluetooth включён"
                    : "Bluetooth выключен";
        }
        private void OpenExplorer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось открыть проводник:\n{ex.Message}",
                    "CarShell",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenWifiButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Здесь будет список доступных Wi-Fi сетей.",
                "CarShell",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void OpenBluetoothButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Здесь будет список Bluetooth-устройств.",
                "CarShell",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BrightnessButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenWindowsSettings(
                "ms-settings:display");
        }

        private void VolumeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenWindowsSettings(
                "ms-settings:sound");
        }

        private void DateTimeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenWindowsSettings(
                "ms-settings:dateandtime");
        }

        private static void OpenWindowsSettings(
            string address)
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