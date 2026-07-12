using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CarShell.Pages.Settings;
using CarShell.Services;

namespace CarShell.Pages
{
    public partial class SettingsPage : UserControl
    {
        private readonly MainWindow mainWindow;
        private readonly UpdateSettingsControl updateSettingsControl;
        private readonly DispatcherTimer clockTimer;

        private bool systemControlsLoading;
        private CancellationTokenSource? brightnessCancellationTokenSource;

        public SettingsPage(MainWindow mainWindow)
        {
            InitializeComponent();

            this.mainWindow = mainWindow;

            updateSettingsControl =
                new UpdateSettingsControl();

            clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            clockTimer.Tick += ClockTimer_Tick;

            Loaded += SettingsPage_Loaded;
            Unloaded += SettingsPage_Unloaded;
        }

        private void SettingsPage_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            LoadVersionInformation();
            InitializeDateTimeControls();
            LoadSystemControls();
            UpdateDateTimeText();

            clockTimer.Start();

            ShowPanel(
                NetworkPanel,
                NetworkNavigationButton);
        }

        private void SettingsPage_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            clockTimer.Stop();
        }

        private void ClockTimer_Tick(
            object? sender,
            EventArgs e)
        {
            UpdateDateTimeText();
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

        private void InitializeDateTimeControls()
        {
            selectedDateTime = DateTime.Now;
            UpdateDateTimeEditor();
        }
        private void DayMinusButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            selectedDateTime =
                selectedDateTime.AddDays(-1);

            UpdateDateTimeEditor();
        }

        private void DayPlusButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            selectedDateTime =
                selectedDateTime.AddDays(1);

            UpdateDateTimeEditor();
        }

        private void MonthMinusButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            selectedDateTime =
                selectedDateTime.AddMonths(-1);

            UpdateDateTimeEditor();
        }

        private void MonthPlusButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            selectedDateTime =
                selectedDateTime.AddMonths(1);

            UpdateDateTimeEditor();
        }

        private void YearMinusButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            selectedDateTime =
                selectedDateTime.AddYears(-1);

            UpdateDateTimeEditor();
        }

        private void YearPlusButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            selectedDateTime =
                selectedDateTime.AddYears(1);

            UpdateDateTimeEditor();
        }
        private void HourMinusButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            selectedDateTime =
                selectedDateTime.AddHours(-1);

            UpdateDateTimeEditor();
        }

        private void HourPlusButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            selectedDateTime =
                selectedDateTime.AddHours(1);

            UpdateDateTimeEditor();
        }

        private void MinuteMinusButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            selectedDateTime =
                selectedDateTime.AddMinutes(-1);

            UpdateDateTimeEditor();
        }

        private void MinutePlusButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            selectedDateTime =
                selectedDateTime.AddMinutes(1);

            UpdateDateTimeEditor();
        }
        private void UpdateDateTimeEditor()
        {
            DayValueText.Text =
                selectedDateTime.Day.ToString("00");

            MonthValueText.Text =
                monthNames[selectedDateTime.Month - 1];

            YearValueText.Text =
                selectedDateTime.Year.ToString();

            HourValueText.Text =
                selectedDateTime.Hour.ToString("00");

            MinuteValueText.Text =
                selectedDateTime.Minute.ToString("00");

            SelectedDateText.Text =
                $"{selectedDateTime.Day} " +
                $"{monthNames[selectedDateTime.Month - 1].ToLower()} " +
                $"{selectedDateTime.Year}";

            SelectedTimeText.Text =
                selectedDateTime.ToString("HH:mm");
        }

        private void LoadSystemControls()
        {
            systemControlsLoading = true;

            try
            {
                int brightness =
                    BrightnessService.GetBrightness();

                if (brightness >= 0)
                {
                    BrightnessSlider.IsEnabled = true;
                    BrightnessSlider.Value = brightness;

                    BrightnessValueText.Text =
                        $"{brightness}%";

                    BrightnessErrorText.Visibility =
                        Visibility.Collapsed;
                }
                else
                {
                    BrightnessSlider.IsEnabled = false;

                    BrightnessValueText.Text =
                        "Недоступно";

                    BrightnessErrorText.Visibility =
                        Visibility.Visible;
                }

                int volume =
                    AudioService.GetVolume();

                if (volume >= 0)
                {
                    VolumeSlider.IsEnabled = true;
                    VolumeSlider.Value = volume;

                    VolumeValueText.Text =
                        $"{volume}%";
                }
                else
                {
                    VolumeSlider.IsEnabled = false;

                    VolumeValueText.Text =
                        "Недоступно";
                }

                bool muted =
                    AudioService.IsMuted();

                MuteToggle.IsChecked = muted;

                MuteStatusText.Text =
                    muted
                        ? "Звук выключен"
                        : "Звук включён";
            }
            finally
            {
                systemControlsLoading = false;
            }
        }

        private async void BrightnessSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            int brightness =
                (int)Math.Round(e.NewValue);

            BrightnessValueText.Text =
                $"{brightness}%";

            if (systemControlsLoading)
            {
                return;
            }

            brightnessCancellationTokenSource?.Cancel();
            brightnessCancellationTokenSource?.Dispose();

            brightnessCancellationTokenSource =
                new CancellationTokenSource();

            CancellationToken token =
                brightnessCancellationTokenSource.Token;

            try
            {
                // Небольшая задержка, чтобы не отправлять WMI-команду
                // на каждый пиксель движения ползунка.
                await Task.Delay(100, token);

                bool result = await Task.Run(
                    () => BrightnessService.SetBrightness(
                        brightness),
                    token);

                if (!result)
                {
                    BrightnessErrorText.Visibility =
                        Visibility.Visible;

                    BrightnessErrorText.Text =
                        "Не удалось изменить яркость";
                }
                else
                {
                    BrightnessErrorText.Visibility =
                        Visibility.Collapsed;
                }
            }
            catch (OperationCanceledException)
            {
                // Пользователь продолжил двигать ползунок.
            }
        }

        private void VolumeSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            int volume =
                (int)Math.Round(e.NewValue);

            VolumeValueText.Text =
                $"{volume}%";

            if (systemControlsLoading)
            {
                return;
            }

            AudioService.SetVolume(volume);

            if (volume > 0 &&
                MuteToggle.IsChecked == true)
            {
                systemControlsLoading = true;

                MuteToggle.IsChecked = false;
                MuteStatusText.Text = "Звук включён";

                AudioService.SetMuted(false);

                systemControlsLoading = false;
            }
        }

        private void MuteToggle_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (systemControlsLoading)
            {
                return;
            }

            bool muted =
                MuteToggle.IsChecked == true;

            if (AudioService.SetMuted(muted))
            {
                MuteStatusText.Text =
                    muted
                        ? "Звук выключен"
                        : "Звук включён";
            }
            else
            {
                systemControlsLoading = true;

                MuteToggle.IsChecked = !muted;

                systemControlsLoading = false;

                MessageBox.Show(
                    "Не удалось изменить состояние звука.",
                    "CarShell",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UpdateDateTimeText()
        {
            DateTime now = DateTime.Now;

            CurrentTimeText.Text =
                now.ToString("HH:mm");

            CurrentDateText.Text =
                now.ToString(
                    "d MMMM yyyy 'г.'",
                    new System.Globalization.CultureInfo("ru-RU"));

            string dayOfWeek =
                now.ToString(
                    "dddd",
                    new System.Globalization.CultureInfo("ru-RU"));

            CurrentDayOfWeekText.Text =
                char.ToUpper(dayOfWeek[0]) +
                dayOfWeek.Substring(1);
        }

        private void SynchronizeTimeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                SystemDateTimeService.SynchronizeTime();

                DateTimeResultText.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(57, 185, 128));

                DateTimeResultText.Text =
                    "Команда синхронизации отправлена.";
            }
            catch (Exception ex)
            {
                DateTimeResultText.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(229, 115, 115));

                DateTimeResultText.Text =
                    $"Ошибка синхронизации: {ex.Message}";
            }
        }

        private void ApplyDateTimeButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            bool result =
                SystemDateTimeService.SetDateTime(
                    selectedDateTime);

            if (result)
            {
                DateTimeResultText.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(57, 185, 128));

                DateTimeResultText.Text =
                    "Дата и время успешно изменены.";

                UpdateDateTimeText();
            }
            else
            {
                DateTimeResultText.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(229, 115, 115));

                DateTimeResultText.Text =
                    "Не удалось изменить системное время. Недостаточно прав.";
            }
        }
            
        private DateTime selectedDateTime = DateTime.Now;

        private readonly string[] monthNames =
        {
    "Январь",
    "Февраль",
    "Март",
    "Апрель",
    "Май",
    "Июнь",
    "Июль",
    "Август",
    "Сентябрь",
    "Октябрь",
    "Ноябрь",
    "Декабрь"
};
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
            LoadSystemControls();
            UpdateDateTimeText();

            ShowPanel(
                SystemPanel,
                SystemNavigationButton);
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
        private void UpdateNavigationButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPanel(
                UpdatePanel,
                UpdateNavigationButton);

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
            WifiStatusText.Text =
                WifiToggle.IsChecked == true
                    ? "Wi-Fi включён"
                    : "Wi-Fi выключен";
        }

        private void BluetoothToggle_Click(
            object sender,
            RoutedEventArgs e)
        {
            BluetoothStatusText.Text =
                BluetoothToggle.IsChecked == true
                    ? "Bluetooth включён"
                    : "Bluetooth выключен";
        }

        private void OpenExplorer_Click(
            object sender,
            RoutedEventArgs e)
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
    }
}