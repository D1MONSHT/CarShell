using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
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
        private bool wifiStateLoading;
        private bool wifiNetworksLoading;
        private bool bluetoothStateLoading;
        private bool bluetoothDevicesLoading;
        private bool powerEventsSubscribed;

        private CancellationTokenSource? brightnessCancellationTokenSource;
        private CancellationTokenSource? systemSettingsSaveCancellationTokenSource;

        private WifiNetworkInfo? selectedWifiNetwork;
        private DateTime selectedDateTime = DateTime.Now;
        private CarShellUserSettings savedUserSettings = new();

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

        public SettingsPage(MainWindow mainWindow)
        {
            InitializeComponent();

            this.mainWindow = mainWindow;
            updateSettingsControl = new UpdateSettingsControl();

            clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            clockTimer.Tick += ClockTimer_Tick;

            Loaded += SettingsPage_Loaded;
            Unloaded += SettingsPage_Unloaded;

            SubscribePowerEvents();
        }

        // =========================================================
        // ЗАГРУЗКА СТРАНИЦЫ
        // =========================================================

        private async void SettingsPage_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            LoadVersionInformation();
            InitializeDateTimeControls();

            savedUserSettings =
                UserSettingsService.Load();

            await ApplySavedSystemSettingsAsync();

            UpdateDateTimeText();
            clockTimer.Start();

            ShowPanel(
                NetworkPanel,
                NetworkNavigationButton);

            await LoadWifiStateAsync();
            await LoadBluetoothStateAsync();
        }

        private void SettingsPage_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            clockTimer.Stop();

            brightnessCancellationTokenSource?.Cancel();
            brightnessCancellationTokenSource?.Dispose();
            brightnessCancellationTokenSource = null;

            systemSettingsSaveCancellationTokenSource?.Cancel();
            systemSettingsSaveCancellationTokenSource?.Dispose();
            systemSettingsSaveCancellationTokenSource = null;
        }

        private void ClockTimer_Tick(
            object? sender,
            EventArgs e)
        {
            UpdateDateTimeText();
        }

        // =========================================================
        // ПИТАНИЕ / ВОССТАНОВЛЕНИЕ ПОСЛЕ СНА
        // =========================================================

        private void SubscribePowerEvents()
        {
            if (powerEventsSubscribed)
            {
                return;
            }

            SystemEvents.PowerModeChanged +=
                SystemEvents_PowerModeChanged;

            powerEventsSubscribed = true;
        }

        private void SystemEvents_PowerModeChanged(
            object sender,
            PowerModeChangedEventArgs e)
        {
            if (e.Mode != PowerModes.Resume)
            {
                return;
            }

            Dispatcher.BeginInvoke(
                new Action(async () =>
                {
                    await Task.Delay(1800);

                    savedUserSettings =
                        UserSettingsService.Load();

                    await ApplySavedSystemSettingsAsync();
                }));
        }

        // =========================================================
        // СОХРАНЕНИЕ ЯРКОСТИ И ГРОМКОСТИ
        // =========================================================

        private async Task ApplySavedSystemSettingsAsync()
        {
            systemControlsLoading = true;

            try
            {
                int brightness =
                    Math.Clamp(
                        savedUserSettings.Brightness,
                        0,
                        100);

                int volume =
                    Math.Clamp(
                        savedUserSettings.Volume,
                        0,
                        100);

                bool brightnessApplied =
                    await ApplyBrightnessWithRetryAsync(
                        brightness);

                bool volumeApplied =
                    await ApplyVolumeWithRetryAsync(
                        volume,
                        savedUserSettings.IsMuted);

                BrightnessSlider.Value =
                    brightness;

                BrightnessValueText.Text =
                    $"{brightness}%";

                BrightnessSlider.IsEnabled =
                    brightnessApplied;

                BrightnessErrorText.Text =
                    brightnessApplied
                        ? string.Empty
                        : "Не удалось восстановить яркость";

                BrightnessErrorText.Visibility =
                    brightnessApplied
                        ? Visibility.Collapsed
                        : Visibility.Visible;

                VolumeSlider.Value =
                    volume;

                VolumeValueText.Text =
                    $"{volume}%";

                VolumeSlider.IsEnabled =
                    volumeApplied;

                MuteToggle.IsChecked =
                    savedUserSettings.IsMuted;

                MuteStatusText.Text =
                    savedUserSettings.IsMuted
                        ? "Звук выключен"
                        : "Звук включён";
            }
            finally
            {
                systemControlsLoading = false;
            }
        }

        private static async Task<bool>
            ApplyBrightnessWithRetryAsync(
                int brightness)
        {
            for (int attempt = 0;
                 attempt < 5;
                 attempt++)
            {
                bool result =
                    await Task.Run(
                        () =>
                            BrightnessService.SetBrightness(
                                brightness));

                if (result)
                {
                    return true;
                }

                await Task.Delay(700);
            }

            return false;
        }

        private static async Task<bool>
            ApplyVolumeWithRetryAsync(
                int volume,
                bool muted)
        {
            for (int attempt = 0;
                 attempt < 5;
                 attempt++)
            {
                bool volumeResult =
                    AudioService.SetVolume(volume);

                bool muteResult =
                    AudioService.SetMuted(muted);

                if (volumeResult &&
                    muteResult)
                {
                    return true;
                }

                await Task.Delay(700);
            }

            return false;
        }

        private async void ScheduleSystemSettingsSave()
        {
            systemSettingsSaveCancellationTokenSource?.Cancel();
            systemSettingsSaveCancellationTokenSource?.Dispose();

            systemSettingsSaveCancellationTokenSource =
                new CancellationTokenSource();

            CancellationToken token =
                systemSettingsSaveCancellationTokenSource.Token;

            try
            {
                await Task.Delay(
                    400,
                    token);

                UserSettingsService.Save(
                    savedUserSettings);
            }
            catch (OperationCanceledException)
            {
            }
        }

        // =========================================================
        // ВЕРСИЯ
        // =========================================================

        private void LoadVersionInformation()
        {
            try
            {
                string version =
                    VersionInfo.Version;

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

        // =========================================================
        // НАВИГАЦИЯ
        // =========================================================

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

            WifiNetworksPanel.Visibility =
                Visibility.Collapsed;

            BluetoothDevicesPanel.Visibility =
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
            if (WifiPasswordOverlay.Visibility ==
                Visibility.Visible)
            {
                CloseWifiPasswordOverlay();
                return;
            }

            if (WifiNetworksPanel.Visibility ==
                Visibility.Visible)
            {
                CloseWifiNetworksPanel();
                return;
            }

            if (BluetoothDevicesPanel.Visibility ==
                Visibility.Visible)
            {
                CloseBluetoothDevicesPanel();
                return;
            }

            mainWindow.ShowHome();
        }

        private async void NetworkNavigationButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPanel(
                NetworkPanel,
                NetworkNavigationButton);

            await LoadWifiStateAsync();
            await LoadBluetoothStateAsync();
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

        // =========================================================
        // ПРОВОДНИК
        // =========================================================

        private void OpenExplorer_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                Process.Start(
                    new ProcessStartInfo
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

        // =========================================================
        // СИСТЕМА — ЯРКОСТЬ И ГРОМКОСТЬ
        // =========================================================

        private void LoadSystemControls()
        {
            systemControlsLoading = true;

            try
            {
                LoadBrightnessState();
                LoadVolumeState();
            }
            finally
            {
                systemControlsLoading = false;
            }
        }

        private void LoadBrightnessState()
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

                BrightnessErrorText.Text =
                    "Управление яркостью недоступно";

                BrightnessErrorText.Visibility =
                    Visibility.Visible;
            }
        }

        private void LoadVolumeState()
        {
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

            MuteToggle.IsChecked =
                muted;

            MuteStatusText.Text =
                muted
                    ? "Звук выключен"
                    : "Звук включён";
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
                await Task.Delay(
                    100,
                    token);

                bool result =
                    await Task.Run(
                        () =>
                            BrightnessService.SetBrightness(
                                brightness),
                        token);

                if (result)
                {
                    BrightnessErrorText.Visibility =
                        Visibility.Collapsed;

                    savedUserSettings.Brightness =
                        brightness;

                    ScheduleSystemSettingsSave();
                }
                else
                {
                    BrightnessErrorText.Text =
                        "Не удалось изменить яркость";

                    BrightnessErrorText.Visibility =
                        Visibility.Visible;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                BrightnessErrorText.Text =
                    $"Ошибка яркости: {ex.Message}";

                BrightnessErrorText.Visibility =
                    Visibility.Visible;
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

            bool result =
                AudioService.SetVolume(volume);

            if (!result)
            {
                return;
            }

            savedUserSettings.Volume =
                volume;

            ScheduleSystemSettingsSave();

            if (volume > 0 &&
                MuteToggle.IsChecked == true)
            {
                systemControlsLoading = true;

                try
                {
                    AudioService.SetMuted(false);

                    MuteToggle.IsChecked = false;
                    MuteStatusText.Text = "Звук включён";

                    savedUserSettings.IsMuted =
                        false;

                    ScheduleSystemSettingsSave();
                }
                finally
                {
                    systemControlsLoading = false;
                }
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

            bool result =
                AudioService.SetMuted(muted);

            if (result)
            {
                MuteStatusText.Text =
                    muted
                        ? "Звук выключен"
                        : "Звук включён";

                savedUserSettings.IsMuted =
                    muted;

                ScheduleSystemSettingsSave();
                return;
            }

            systemControlsLoading = true;

            try
            {
                MuteToggle.IsChecked =
                    !muted;
            }
            finally
            {
                systemControlsLoading = false;
            }

            MessageBox.Show(
                "Не удалось изменить состояние звука.",
                "CarShell",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        // =========================================================
        // ДАТА И ВРЕМЯ
        // =========================================================

        private void InitializeDateTimeControls()
        {
            selectedDateTime =
                DateTime.Now;

            UpdateDateTimeEditor();
        }

        private void UpdateDateTimeText()
        {
            DateTime now =
                DateTime.Now;

            CurrentTimeText.Text =
                now.ToString("HH:mm");

            CurrentDateText.Text =
                now.ToString(
                    "d MMMM yyyy 'г.'",
                    new System.Globalization.CultureInfo(
                        "ru-RU"));

            string dayOfWeek =
                now.ToString(
                    "dddd",
                    new System.Globalization.CultureInfo(
                        "ru-RU"));

            if (string.IsNullOrWhiteSpace(
                    dayOfWeek))
            {
                CurrentDayOfWeekText.Text =
                    string.Empty;

                return;
            }

            CurrentDayOfWeekText.Text =
                char.ToUpper(dayOfWeek[0]) +
                dayOfWeek.Substring(1);
        }

        private void UpdateDateTimeEditor()
        {
            DayValueText.Text =
                selectedDateTime.Day.ToString("00");

            MonthValueText.Text =
                monthNames[
                    selectedDateTime.Month - 1];

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

        private void ApplyDateTimeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                bool result =
                    SystemDateTimeService.SetDateTime(
                        selectedDateTime);

                if (result)
                {
                    DateTimeResultText.Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                57,
                                185,
                                128));

                    DateTimeResultText.Text =
                        "Дата и время успешно изменены.";

                    UpdateDateTimeText();
                }
                else
                {
                    DateTimeResultText.Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                229,
                                115,
                                115));

                    DateTimeResultText.Text =
                        "Не удалось изменить системное время. Недостаточно прав.";
                }
            }
            catch (Exception ex)
            {
                DateTimeResultText.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(
                            229,
                            115,
                            115));

                DateTimeResultText.Text =
                    $"Ошибка изменения времени: {ex.Message}";
            }
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
                        Color.FromRgb(
                            57,
                            185,
                            128));

                DateTimeResultText.Text =
                    "Команда синхронизации отправлена.";
            }
            catch (Exception ex)
            {
                DateTimeResultText.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(
                            229,
                            115,
                            115));

                DateTimeResultText.Text =
                    $"Ошибка синхронизации: {ex.Message}";
            }
        }

        // =========================================================
        // WI-FI — СОСТОЯНИЕ
        // =========================================================

        private async Task LoadWifiStateAsync()
        {
            if (wifiStateLoading)
            {
                return;
            }

            wifiStateLoading = true;

            try
            {
                WifiStatusText.Text =
                    "Получение состояния Wi-Fi...";

                ConnectedWifiText.Text =
                    string.Empty;

                bool enabled =
                    await WifiService.IsWifiEnabledAsync();

                WifiToggle.IsChecked =
                    enabled;

                if (!enabled)
                {
                    WifiStatusText.Text =
                        "Wi-Fi выключен";

                    return;
                }

                WifiStatusText.Text =
                    "Wi-Fi включён";

                string? connectedNetwork =
                    await WifiService
                        .GetConnectedNetworkAsync();

                ConnectedWifiText.Text =
                    string.IsNullOrWhiteSpace(
                        connectedNetwork)
                        ? "Нет подключения"
                        : $"Подключено: {connectedNetwork}";
            }
            catch (Exception ex)
            {
                WifiStatusText.Text =
                    "Не удалось получить состояние Wi-Fi";

                ConnectedWifiText.Text =
                    ex.Message;
            }
            finally
            {
                wifiStateLoading = false;
            }
        }

        private async void WifiToggle_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (wifiStateLoading)
            {
                return;
            }

            bool enable =
                WifiToggle.IsChecked == true;

            wifiStateLoading = true;
            WifiToggle.IsEnabled = false;

            try
            {
                WifiStatusText.Text =
                    enable
                        ? "Включение Wi-Fi..."
                        : "Выключение Wi-Fi...";

                ConnectedWifiText.Text =
                    string.Empty;

                bool success =
                    await WifiService
                        .SetWifiEnabledAsync(
                            enable);

                if (!success)
                {
                    WifiToggle.IsChecked =
                        !enable;

                    WifiStatusText.Text =
                        "Не удалось изменить состояние Wi-Fi";

                    return;
                }

                await Task.Delay(1200);
            }
            catch (Exception ex)
            {
                WifiToggle.IsChecked =
                    !enable;

                WifiStatusText.Text =
                    "Ошибка управления Wi-Fi";

                ConnectedWifiText.Text =
                    ex.Message;
            }
            finally
            {
                wifiStateLoading = false;
                WifiToggle.IsEnabled = true;
            }

            await LoadWifiStateAsync();
        }

        // =========================================================
        // WI-FI — СПИСОК СЕТЕЙ
        // =========================================================

        private async void OpenWifiButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            bool enabled =
                await WifiService.IsWifiEnabledAsync();

            if (!enabled)
            {
                WifiStatusText.Text =
                    "Сначала включите Wi-Fi";

                return;
            }

            NetworkPanel.Visibility =
                Visibility.Collapsed;

            WifiNetworksPanel.Visibility =
                Visibility.Visible;

            await RefreshWifiNetworksAsync();
        }

        private void CloseWifiNetworksButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            CloseWifiNetworksPanel();
        }

        private void CloseWifiNetworksPanel()
        {
            WifiNetworksPanel.Visibility =
                Visibility.Collapsed;

            NetworkPanel.Visibility =
                Visibility.Visible;

            NetworkNavigationButton.Background =
                new SolidColorBrush(
                    Color.FromRgb(
                        24,
                        63,
                        91));
        }

        private async void RefreshWifiNetworksButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            await RefreshWifiNetworksAsync();
        }

        private async Task RefreshWifiNetworksAsync()
        {
            if (wifiNetworksLoading)
            {
                return;
            }

            wifiNetworksLoading = true;

            WifiNetworksStackPanel.Children.Clear();
            WifiNetworksStatusText.Text =
                "Поиск доступных сетей...";

            try
            {
                IReadOnlyList<WifiNetworkInfo> networks =
                    await WifiService
                        .GetAvailableNetworksAsync();

                if (networks.Count == 0)
                {
                    WifiNetworksStatusText.Text =
                        "Сети не найдены";

                    WifiNetworksStackPanel.Children.Add(
                        new TextBlock
                        {
                            Text =
                                "Поблизости нет доступных Wi-Fi сетей.",

                            Foreground =
                                new SolidColorBrush(
                                    Color.FromRgb(
                                        130,
                                        149,
                                        168)),

                            FontSize = 18,

                            Margin =
                                new Thickness(
                                    0,
                                    30,
                                    0,
                                    0)
                        });

                    return;
                }

                WifiNetworksStatusText.Text =
                    $"Найдено сетей: {networks.Count}";

                foreach (
                    WifiNetworkInfo network
                    in networks)
                {
                    WifiNetworksStackPanel.Children.Add(
                        CreateWifiNetworkCard(
                            network));
                }
            }
            catch (Exception ex)
            {
                WifiNetworksStatusText.Text =
                    "Ошибка поиска сетей";

                WifiNetworksStackPanel.Children.Add(
                    new TextBlock
                    {
                        Text = ex.Message,

                        Foreground =
                            new SolidColorBrush(
                                Color.FromRgb(
                                    229,
                                    115,
                                    115)),

                        FontSize = 16,
                        TextWrapping = TextWrapping.Wrap
                    });
            }
            finally
            {
                wifiNetworksLoading = false;
            }
        }

        private Border CreateWifiNetworkCard(
            WifiNetworkInfo network)
        {
            var card = new Border
            {
                Background =
                    new SolidColorBrush(
                        Color.FromRgb(
                            16,
                            24,
                            32)),

                BorderBrush =
                    new SolidColorBrush(
                        Color.FromRgb(
                            27,
                            42,
                            56)),

                BorderThickness =
                    new Thickness(1),

                CornerRadius =
                    new CornerRadius(18),

                Padding =
                    new Thickness(22),

                Margin =
                    new Thickness(
                        0,
                        0,
                        0,
                        14)
            };

            var grid = new Grid();

            grid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width =
                        new GridLength(78)
                });

            grid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width =
                        new GridLength(
                            1,
                            GridUnitType.Star)
                });

            grid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width =
                        GridLength.Auto
                });

            var signalPanel =
                new StackPanel
                {
                    VerticalAlignment =
                        VerticalAlignment.Center
                };

            signalPanel.Children.Add(
                new TextBlock
                {
                    Text =
                        GetWifiSignalIcon(
                            network.Signal),

                    Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                42,
                                157,
                                255)),

                    FontSize = 20,
                    FontWeight =
                        FontWeights.SemiBold
                });

            signalPanel.Children.Add(
                new TextBlock
                {
                    Text =
                        $"{network.Signal}%",

                    Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                130,
                                149,
                                168)),

                    FontSize = 13,

                    Margin =
                        new Thickness(
                            0,
                            4,
                            0,
                            0)
                });

            Grid.SetColumn(
                signalPanel,
                0);

            grid.Children.Add(
                signalPanel);

            var informationPanel =
                new StackPanel
                {
                    VerticalAlignment =
                        VerticalAlignment.Center
                };

            informationPanel.Children.Add(
                new TextBlock
                {
                    Text =
                        network.Ssid,

                    Foreground =
                        Brushes.White,

                    FontSize = 21,

                    FontWeight =
                        FontWeights.SemiBold,

                    TextTrimming =
                        TextTrimming.CharacterEllipsis
                });

            string description;

            if (network.IsConnected)
            {
                description =
                    $"Подключено · Сигнал {network.Signal}%";
            }
            else if (network.IsSecured)
            {
                description =
                    $"Защищённая сеть · Сигнал {network.Signal}%";
            }
            else
            {
                description =
                    $"Открытая сеть · Сигнал {network.Signal}%";
            }

            informationPanel.Children.Add(
                new TextBlock
                {
                    Text =
                        description,

                    Foreground =
                        network.IsConnected
                            ? new SolidColorBrush(
                                Color.FromRgb(
                                    57,
                                    185,
                                    128))
                            : new SolidColorBrush(
                                Color.FromRgb(
                                    130,
                                    149,
                                    168)),

                    FontSize = 15,

                    Margin =
                        new Thickness(
                            0,
                            5,
                            0,
                            0)
                });

            Grid.SetColumn(
                informationPanel,
                1);

            grid.Children.Add(
                informationPanel);

            var actionButton =
                new Button
                {
                    Content =
                        network.IsConnected
                            ? "Отключить"
                            : "Подключить",

                    Tag =
                        network,

                    MinWidth = 155,
                    Height = 52,

                    Padding =
                        new Thickness(
                            18,
                            0,
                            18,
                            0),

                    Foreground =
                        Brushes.White,

                    Background =
                        network.IsConnected
                            ? new SolidColorBrush(
                                Color.FromRgb(
                                    55,
                                    70,
                                    84))
                            : new SolidColorBrush(
                                Color.FromRgb(
                                    27,
                                    108,
                                    168)),

                    BorderThickness =
                        new Thickness(0),

                    FontSize = 17,

                    FontWeight =
                        FontWeights.SemiBold,

                    Cursor =
                        Cursors.Hand,

                    Margin =
                        new Thickness(
                            20,
                            0,
                            0,
                            0)
                };

            actionButton.Click +=
                WifiNetworkActionButton_Click;

            Grid.SetColumn(
                actionButton,
                2);

            grid.Children.Add(
                actionButton);

            card.Child =
                grid;

            return card;
        }

        private static string GetWifiSignalIcon(
            int signal)
        {
            if (signal >= 75)
            {
                return "▰▰▰";
            }

            if (signal >= 45)
            {
                return "▰▰▱";
            }

            return "▰▱▱";
        }

        private async void WifiNetworkActionButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not WifiNetworkInfo network)
            {
                return;
            }

            button.IsEnabled =
                false;

            try
            {
                if (network.IsConnected)
                {
                    WifiNetworksStatusText.Text =
                        $"Отключение от {network.Ssid}...";

                    await WifiService.DisconnectAsync();

                    await Task.Delay(700);
                    await RefreshWifiNetworksAsync();
                    await LoadWifiStateAsync();

                    return;
                }

                selectedWifiNetwork =
                    network;

                bool hasSavedProfile =
                    await WifiService
                        .HasSavedProfileAsync(
                            network.Ssid);

                if (hasSavedProfile)
                {
                    WifiNetworksStatusText.Text =
                        $"Подключение к {network.Ssid}...";

                    bool connected =
                        await WifiService
                            .ConnectSavedProfileAsync(
                                network.Ssid);

                    if (!connected)
                    {
                        WifiNetworksStatusText.Text =
                            "Не удалось подключиться к сети";

                        return;
                    }

                    await Task.Delay(1500);
                    await RefreshWifiNetworksAsync();
                    await LoadWifiStateAsync();

                    return;
                }

                if (!network.IsSecured)
                {
                    WifiNetworksStatusText.Text =
                        $"Подключение к {network.Ssid}...";

                    bool connected =
                        await WifiService
                            .ConnectOpenNetworkAsync(
                                network.Ssid);

                    if (!connected)
                    {
                        WifiNetworksStatusText.Text =
                            "Не удалось подключиться к открытой сети";

                        return;
                    }

                    await Task.Delay(1500);
                    await RefreshWifiNetworksAsync();
                    await LoadWifiStateAsync();

                    return;
                }

                OpenWifiPasswordOverlay(
                    network);
            }
            catch (Exception ex)
            {
                WifiNetworksStatusText.Text =
                    $"Ошибка подключения: {ex.Message}";
            }
            finally
            {
                button.IsEnabled =
                    true;
            }
        }

        private void OpenWifiPasswordOverlay(
            WifiNetworkInfo network)
        {
            selectedWifiNetwork =
                network;

            WifiPasswordNetworkText.Text =
                network.Ssid;

            WifiPasswordBox.Password =
                string.Empty;

            WifiPasswordErrorText.Text =
                string.Empty;

            WifiPasswordOverlay.Visibility =
                Visibility.Visible;

            WifiPasswordBox.Focus();
        }

        private void CancelWifiPasswordButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            CloseWifiPasswordOverlay();
        }

        private void CloseWifiPasswordOverlay()
        {
            WifiPasswordOverlay.Visibility =
                Visibility.Collapsed;

            WifiPasswordBox.Password =
                string.Empty;

            WifiPasswordErrorText.Text =
                string.Empty;

            selectedWifiNetwork =
                null;
        }

        private async void ConnectWifiPasswordButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (selectedWifiNetwork == null)
            {
                WifiPasswordErrorText.Text =
                    "Сеть не выбрана.";

                return;
            }

            string password =
                WifiPasswordBox.Password;

            if (password.Length < 8)
            {
                WifiPasswordErrorText.Text =
                    "Пароль должен содержать не менее 8 символов.";

                return;
            }

            if (sender is not Button connectButton)
            {
                return;
            }

            connectButton.IsEnabled =
                false;

            WifiPasswordBox.IsEnabled =
                false;

            WifiPasswordErrorText.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        130,
                        149,
                        168));

            WifiPasswordErrorText.Text =
                "Подключение...";

            try
            {
                string ssid =
                    selectedWifiNetwork.Ssid;

                bool connected =
                    await WifiService
                        .ConnectWithPasswordAsync(
                            ssid,
                            password);

                if (!connected)
                {
                    WifiPasswordErrorText.Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                229,
                                115,
                                115));

                    WifiPasswordErrorText.Text =
                        "Не удалось подключиться. Проверьте пароль.";

                    return;
                }

                await Task.Delay(1800);

                string? currentNetwork =
                    await WifiService
                        .GetConnectedNetworkAsync();

                if (!string.Equals(
                        currentNetwork,
                        ssid,
                        StringComparison.OrdinalIgnoreCase))
                {
                    WifiPasswordErrorText.Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                229,
                                115,
                                115));

                    WifiPasswordErrorText.Text =
                        "Подключение не выполнено. Проверьте пароль.";

                    return;
                }

                CloseWifiPasswordOverlay();

                WifiNetworksStatusText.Text =
                    $"Подключено к {ssid}";

                await RefreshWifiNetworksAsync();
                await LoadWifiStateAsync();
            }
            catch (Exception ex)
            {
                WifiPasswordErrorText.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(
                            229,
                            115,
                            115));

                WifiPasswordErrorText.Text =
                    $"Ошибка подключения: {ex.Message}";
            }
            finally
            {
                connectButton.IsEnabled =
                    true;

                WifiPasswordBox.IsEnabled =
                    true;
            }
        }

        // =========================================================
        // BLUETOOTH
        // =========================================================

        private async Task LoadBluetoothStateAsync()
        {
            if (bluetoothStateLoading)
            {
                return;
            }

            bluetoothStateLoading =
                true;

            try
            {
                BluetoothStatusText.Text =
                    "Получение состояния Bluetooth...";

                bool available =
                    await BluetoothService
                        .IsBluetoothAvailableAsync();

                if (!available)
                {
                    BluetoothToggle.IsChecked =
                        false;

                    BluetoothToggle.IsEnabled =
                        false;

                    BluetoothStatusText.Text =
                        "Bluetooth-адаптер не найден";

                    return;
                }

                bool enabled =
                    await BluetoothService
                        .IsBluetoothEnabledAsync();

                BluetoothToggle.IsChecked =
                    enabled;

                BluetoothToggle.IsEnabled =
                    true;

                BluetoothStatusText.Text =
                    enabled
                        ? "Bluetooth включён"
                        : "Bluetooth выключен";
            }
            catch (Exception ex)
            {
                BluetoothToggle.IsEnabled =
                    false;

                BluetoothStatusText.Text =
                    $"Ошибка Bluetooth: {ex.Message}";
            }
            finally
            {
                bluetoothStateLoading =
                    false;
            }
        }

        private async void BluetoothToggle_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (bluetoothStateLoading)
            {
                return;
            }

            bool enabled =
                BluetoothToggle.IsChecked == true;

            bluetoothStateLoading =
                true;

            BluetoothToggle.IsEnabled =
                false;

            try
            {
                BluetoothStatusText.Text =
                    enabled
                        ? "Включение Bluetooth..."
                        : "Выключение Bluetooth...";

                bool result =
                    await BluetoothService
                        .SetBluetoothEnabledAsync(
                            enabled);

                if (!result)
                {
                    BluetoothToggle.IsChecked =
                        !enabled;

                    BluetoothStatusText.Text =
                        "Не удалось изменить состояние Bluetooth";

                    return;
                }

                await Task.Delay(700);
            }
            catch (Exception ex)
            {
                BluetoothToggle.IsChecked =
                    !enabled;

                BluetoothStatusText.Text =
                    $"Ошибка Bluetooth: {ex.Message}";
            }
            finally
            {
                bluetoothStateLoading =
                    false;

                BluetoothToggle.IsEnabled =
                    true;
            }

            await LoadBluetoothStateAsync();
        }

        private async void OpenBluetoothButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            bool enabled =
                await BluetoothService
                    .IsBluetoothEnabledAsync();

            if (!enabled)
            {
                BluetoothStatusText.Text =
                    "Сначала включите Bluetooth";

                return;
            }

            NetworkPanel.Visibility =
                Visibility.Collapsed;

            BluetoothDevicesPanel.Visibility =
                Visibility.Visible;

            await RefreshBluetoothDevicesAsync();
        }

        private void CloseBluetoothDevicesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            CloseBluetoothDevicesPanel();
        }

        private void CloseBluetoothDevicesPanel()
        {
            BluetoothDevicesPanel.Visibility =
                Visibility.Collapsed;

            NetworkPanel.Visibility =
                Visibility.Visible;

            NetworkNavigationButton.Background =
                new SolidColorBrush(
                    Color.FromRgb(
                        24,
                        63,
                        91));
        }

        private async void RefreshBluetoothDevicesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            await RefreshBluetoothDevicesAsync();
        }

        private async Task RefreshBluetoothDevicesAsync()
        {
            if (bluetoothDevicesLoading)
            {
                return;
            }

            bluetoothDevicesLoading =
                true;

            BluetoothPairedDevicesStackPanel
                .Children.Clear();

            BluetoothAvailableDevicesStackPanel
                .Children.Clear();

            BluetoothComputerNameText.Text =
                BluetoothService.GetComputerName();

            BluetoothDevicesStatusText.Text =
                "Поиск Bluetooth-устройств...";

            var foundDevices =
                new Dictionary<string, BluetoothDeviceInfo>(
                    StringComparer.OrdinalIgnoreCase);

            try
            {
                void DeviceFound(
                    BluetoothDeviceInfo device)
                {
                    Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            foundDevices[device.Id] =
                                device;

                            BluetoothDevicesStatusText.Text =
                                $"Поиск устройств... Найдено: {foundDevices.Count}";

                            RenderBluetoothDevices(
                                foundDevices.Values);
                        }));
                }

                IReadOnlyList<BluetoothDeviceInfo> finalDevices =
                    await BluetoothService.ScanDevicesAsync(
                        TimeSpan.FromSeconds(8),
                        DeviceFound);

                foundDevices.Clear();

                foreach (
                    BluetoothDeviceInfo device
                    in finalDevices)
                {
                    foundDevices[device.Id] =
                        device;
                }

                RenderBluetoothDevices(
                    foundDevices.Values);

                BluetoothDevicesStatusText.Text =
                    $"Поиск завершён · Найдено: {foundDevices.Count}";
            }
            catch (Exception ex)
            {
                BluetoothDevicesStatusText.Text =
                    $"Ошибка поиска: {ex.Message}";

                BluetoothAvailableDevicesStackPanel
                    .Children.Add(
                        CreateBluetoothEmptyMessage(
                            "Не удалось получить список устройств."));
            }
            finally
            {
                bluetoothDevicesLoading =
                    false;
            }
        }

        private void RenderBluetoothDevices(
            IEnumerable<BluetoothDeviceInfo> devices)
        {
            BluetoothPairedDevicesStackPanel
                .Children.Clear();

            BluetoothAvailableDevicesStackPanel
                .Children.Clear();

            List<BluetoothDeviceInfo> paired =
                devices
                    .Where(device =>
                        device.IsPaired ||
                        device.IsConnected)
                    .OrderByDescending(device =>
                        device.IsConnected)
                    .ThenBy(device =>
                        device.Name)
                    .ToList();

            List<BluetoothDeviceInfo> available =
                devices
                    .Where(device =>
                        !device.IsPaired &&
                        !device.IsConnected)
                    .OrderBy(device =>
                        device.Name)
                    .ToList();

            if (paired.Count == 0)
            {
                BluetoothPairedDevicesStackPanel
                    .Children.Add(
                        CreateBluetoothEmptyMessage(
                            "Нет сохранённых Bluetooth-устройств."));
            }
            else
            {
                foreach (
                    BluetoothDeviceInfo device
                    in paired)
                {
                    BluetoothPairedDevicesStackPanel
                        .Children.Add(
                            CreateBluetoothDeviceCard(
                                device));
                }
            }

            if (available.Count == 0)
            {
                BluetoothAvailableDevicesStackPanel
                    .Children.Add(
                        CreateBluetoothEmptyMessage(
                            "Поблизости нет доступных устройств."));
            }
            else
            {
                foreach (
                    BluetoothDeviceInfo device
                    in available)
                {
                    BluetoothAvailableDevicesStackPanel
                        .Children.Add(
                            CreateBluetoothDeviceCard(
                                device));
                }
            }
        }

        private static TextBlock CreateBluetoothEmptyMessage(
            string text)
        {
            return new TextBlock
            {
                Text =
                    text,

                Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(
                            112,
                            131,
                            152)),

                FontSize =
                    16,

                Margin =
                    new Thickness(
                        10,
                        12,
                        0,
                        18)
            };
        }

        private Border CreateBluetoothDeviceCard(
            BluetoothDeviceInfo device)
        {
            var card = new Border
            {
                Background =
                    new SolidColorBrush(
                        Color.FromRgb(
                            16,
                            24,
                            32)),

                BorderBrush =
                    new SolidColorBrush(
                        Color.FromRgb(
                            27,
                            42,
                            56)),

                BorderThickness =
                    new Thickness(1),

                CornerRadius =
                    new CornerRadius(18),

                Padding =
                    new Thickness(22),

                Margin =
                    new Thickness(
                        0,
                        0,
                        0,
                        14)
            };

            var grid =
                new Grid();

            grid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width =
                        new GridLength(68)
                });

            grid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width =
                        new GridLength(
                            1,
                            GridUnitType.Star)
                });

            grid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width =
                        GridLength.Auto
                });

            var iconBorder =
                new Border
                {
                    Width = 52,
                    Height = 52,

                    CornerRadius =
                        new CornerRadius(14),

                    Background =
                        new SolidColorBrush(
                            Color.FromRgb(
                                16,
                                46,
                                73)),

                    VerticalAlignment =
                        VerticalAlignment.Center
                };

            iconBorder.Child =
                new TextBlock
                {
                    Text =
                        device.Icon,

                    FontSize =
                        27,

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    VerticalAlignment =
                        VerticalAlignment.Center
                };

            Grid.SetColumn(
                iconBorder,
                0);

            grid.Children.Add(
                iconBorder);

            var information =
                new StackPanel
                {
                    VerticalAlignment =
                        VerticalAlignment.Center
                };

            information.Children.Add(
                new TextBlock
                {
                    Text =
                        device.Name,

                    Foreground =
                        Brushes.White,

                    FontSize =
                        21,

                    FontWeight =
                        FontWeights.SemiBold,

                    TextTrimming =
                        TextTrimming.CharacterEllipsis
                });

            string protocolText =
                device.IsLowEnergy
                    ? "Bluetooth LE"
                    : "Классический Bluetooth";

            information.Children.Add(
                new TextBlock
                {
                    Text =
                        $"{device.DeviceType} · {protocolText} · {device.StatusText}",

                    Foreground =
                        device.IsConnected
                            ? new SolidColorBrush(
                                Color.FromRgb(
                                    57,
                                    185,
                                    128))
                            : new SolidColorBrush(
                                Color.FromRgb(
                                    130,
                                    149,
                                    168)),

                    FontSize =
                        15,

                    Margin =
                        new Thickness(
                            0,
                            5,
                            0,
                            0)
                });

            Grid.SetColumn(
                information,
                1);

            grid.Children.Add(
                information);

            var buttonsPanel =
                new StackPanel
                {
                    Orientation =
                        Orientation.Horizontal,

                    VerticalAlignment =
                        VerticalAlignment.Center,

                    Margin =
                        new Thickness(
                            20,
                            0,
                            0,
                            0)
                };

            if (!device.IsPaired)
            {
                Button pairButton =
                    CreateBluetoothActionButton(
                        "Сопрячь",
                        device,
                        new SolidColorBrush(
                            Color.FromRgb(
                                27,
                                108,
                                168)));

                pairButton.Click +=
                    BluetoothPairButton_Click;

                buttonsPanel.Children.Add(
                    pairButton);
            }
            else
            {
                Button connectionButton =
                    CreateBluetoothActionButton(
                        device.IsConnected
                            ? "Отключить"
                            : "Подключить",
                        device,
                        device.IsConnected
                            ? new SolidColorBrush(
                                Color.FromRgb(
                                    55,
                                    70,
                                    84))
                            : new SolidColorBrush(
                                Color.FromRgb(
                                    27,
                                    108,
                                    168)));

                connectionButton.Click +=
                    BluetoothConnectionButton_Click;

                buttonsPanel.Children.Add(
                    connectionButton);

                Button removeButton =
                    CreateBluetoothActionButton(
                        "Удалить",
                        device,
                        new SolidColorBrush(
                            Color.FromRgb(
                                82,
                                52,
                                60)));

                removeButton.MinWidth =
                    105;

                removeButton.Margin =
                    new Thickness(
                        10,
                        0,
                        0,
                        0);

                removeButton.Click +=
                    BluetoothRemoveButton_Click;

                buttonsPanel.Children.Add(
                    removeButton);
            }

            Grid.SetColumn(
                buttonsPanel,
                2);

            grid.Children.Add(
                buttonsPanel);

            card.Child =
                grid;

            return card;
        }

        private static Button CreateBluetoothActionButton(
            string text,
            BluetoothDeviceInfo device,
            Brush background)
        {
            return new Button
            {
                Content =
                    text,

                Tag =
                    device,

                MinWidth =
                    135,

                Height =
                    52,

                Foreground =
                    Brushes.White,

                Background =
                    background,

                BorderThickness =
                    new Thickness(0),

                FontSize =
                    16,

                FontWeight =
                    FontWeights.SemiBold,

                Cursor =
                    Cursors.Hand,

                Padding =
                    new Thickness(
                        16,
                        0,
                        16,
                        0)
            };
        }

        private async void BluetoothPairButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not BluetoothDeviceInfo device)
            {
                return;
            }

            button.IsEnabled =
                false;

            try
            {
                BluetoothDevicesStatusText.Text =
                    $"Сопряжение с {device.Name}...";

                BluetoothOperationResult result =
                    await BluetoothService.PairAsync(
                        device.Id);

                BluetoothDevicesStatusText.Text =
                    result.Message;

                await Task.Delay(600);
                await RefreshBluetoothDevicesAsync();
            }
            catch (Exception ex)
            {
                BluetoothDevicesStatusText.Text =
                    $"Ошибка сопряжения: {ex.Message}";
            }
            finally
            {
                button.IsEnabled =
                    true;
            }
        }

        private async void BluetoothConnectionButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not BluetoothDeviceInfo device)
            {
                return;
            }

            button.IsEnabled =
                false;

            try
            {
                BluetoothOperationResult result;

                if (device.IsConnected)
                {
                    BluetoothDevicesStatusText.Text =
                        $"Отключение {device.Name}...";

                    result =
                        await BluetoothService
                            .DisconnectAsync(
                                device);
                }
                else
                {
                    BluetoothDevicesStatusText.Text =
                        $"Подключение к {device.Name}...";

                    result =
                        await BluetoothService
                            .ConnectAsync(
                                device);
                }

                BluetoothDevicesStatusText.Text =
                    result.Message;

                await Task.Delay(700);
                await RefreshBluetoothDevicesAsync();
            }
            catch (Exception ex)
            {
                BluetoothDevicesStatusText.Text =
                    $"Ошибка подключения: {ex.Message}";
            }
            finally
            {
                button.IsEnabled =
                    true;
            }
        }

        private async void BluetoothRemoveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not BluetoothDeviceInfo device)
            {
                return;
            }

            button.IsEnabled =
                false;

            try
            {
                BluetoothDevicesStatusText.Text =
                    $"Удаление сопряжения с {device.Name}...";

                BluetoothOperationResult result =
                    await BluetoothService.UnpairAsync(
                        device.Id);

                BluetoothDevicesStatusText.Text =
                    result.Message;

                await Task.Delay(600);
                await RefreshBluetoothDevicesAsync();
            }
            catch (Exception ex)
            {
                BluetoothDevicesStatusText.Text =
                    $"Ошибка удаления: {ex.Message}";
            }
            finally
            {
                button.IsEnabled =
                    true;
            }
        }
    }
}