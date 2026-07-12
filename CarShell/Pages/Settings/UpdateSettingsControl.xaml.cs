using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CarShell.Services;

namespace CarShell.Pages.Settings
{
    public partial class UpdateSettingsControl : Page
    {
        private UpdateInfo? latestUpdate;
        private UpdateInfo? selectedUpdate;

        private string? downloadedZipPath;

        private bool isInitialized;
        private bool isBusy;

        public UpdateSettingsControl()
        {
            InitializeComponent();

            Loaded += UpdateSettingsControl_Loaded;
        }

        private async void UpdateSettingsControl_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;

            CurrentVersionText.Text =
                $"Текущая версия: {VersionInfo.Version}";

            LatestVersionText.Text =
                "Последняя версия: неизвестно";

            SelectedVersionText.Text =
                "Выбранная версия: не выбрана";

            StatusText.Text =
                "Нажмите «Проверить»";

            NotesText.Text =
                "Описание обновления появится здесь.";

            DownloadButton.IsEnabled = false;
            InstallButton.IsEnabled = false;
            RollbackButton.IsEnabled = false;

            await LoadVersionsAsync();
        }

        private async Task LoadVersionsAsync()
        {
            if (isBusy)
            {
                return;
            }

            try
            {
                SetBusyState(true);

                StatusText.Text =
                    "Загрузка списка версий...";

                VersionsComboBox.ItemsSource = null;

                List<UpdateInfo> versions =
                    await UpdateService.GetVersionsAsync();

                VersionsComboBox.ItemsSource =
                    versions;

                VersionsComboBox.DisplayMemberPath =
                    nameof(UpdateInfo.Version);

                if (versions.Count > 0)
                {
                    VersionsComboBox.SelectedIndex = 0;

                    StatusText.Text =
                        $"Загружено версий: {versions.Count}";
                }
                else
                {
                    selectedUpdate = null;

                    SelectedVersionText.Text =
                        "Выбранная версия: не выбрана";

                    StatusText.Text =
                        "Опубликованные версии не найдены";

                    NotesText.Text =
                        "В GitHub Releases не найдены релизы с файлом CarShell-win-x64.zip.";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    "🔴 Не удалось загрузить список версий";

                NotesText.Text =
                    ex.Message;
            }
            finally
            {
                SetBusyState(false);
                UpdateButtonsState();
            }
        }

        private async void CheckUpdate_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (isBusy)
            {
                return;
            }

            try
            {
                SetBusyState(true);

                StatusText.Text =
                    "Проверка обновлений...";

                latestUpdate =
                    await UpdateService.CheckAsync();

                downloadedZipPath = null;

                LatestVersionText.Text =
                    $"Последняя версия: {latestUpdate.Version}";

                NotesText.Text =
                    string.IsNullOrWhiteSpace(latestUpdate.Notes)
                        ? "Описание версии отсутствует."
                        : latestUpdate.Notes;

                if (latestUpdate.HasUpdate)
                {
                    StatusText.Text =
                        $"🟡 Доступна версия {latestUpdate.Version}";
                }
                else
                {
                    StatusText.Text =
                        "🟢 Установлена последняя версия";
                }
            }
            catch (Exception ex)
            {
                latestUpdate = null;

                StatusText.Text =
                    "🔴 Ошибка проверки обновлений";

                NotesText.Text =
                    ex.Message;
            }
            finally
            {
                SetBusyState(false);
                UpdateButtonsState();
            }
        }

        private async void DownloadUpdate_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (isBusy)
            {
                return;
            }

            if (latestUpdate == null)
            {
                StatusText.Text =
                    "Сначала выполните проверку обновлений";

                return;
            }

            await DownloadVersionAsync(
                latestUpdate);
        }

        private async void Rollback_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (isBusy)
            {
                return;
            }

            if (selectedUpdate == null)
            {
                StatusText.Text =
                    "Выберите версию из списка";

                return;
            }

            string selectedVersion =
                selectedUpdate.Version;

            string currentVersion =
                VersionInfo.Version;

            bool isCurrentVersion =
                AreVersionsEqual(
                    selectedVersion,
                    currentVersion);

            string message;

            if (isCurrentVersion)
            {
                message =
                    $"Версия {selectedVersion} уже установлена.\n\n" +
                    "Скачать её повторно?";
            }
            else if (IsVersionNewer(
                         selectedVersion,
                         currentVersion))
            {
                message =
                    $"Скачать обновление {selectedVersion}?\n\n" +
                    $"Текущая версия: {currentVersion}";
            }
            else
            {
                message =
                    $"Выполнить откат до версии {selectedVersion}?\n\n" +
                    $"Текущая версия: {currentVersion}\n\n" +
                    "После скачивания нажмите «Установить».";
            }

            MessageBoxResult result =
                MessageBox.Show(
                    message,
                    "Выбор версии CarShell",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            await DownloadVersionAsync(
                selectedUpdate);
        }

        private async Task DownloadVersionAsync(
            UpdateInfo update)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(
                        update.DownloadUrl))
                {
                    StatusText.Text =
                        "🔴 У выбранного релиза нет ZIP-файла";

                    NotesText.Text =
                        "В GitHub Release должен находиться файл CarShell-win-x64.zip.";

                    return;
                }

                SetBusyState(true);

                downloadedZipPath = null;

                StatusText.Text =
                    $"Скачивание версии {update.Version}...";

                NotesText.Text =
                    string.IsNullOrWhiteSpace(update.Notes)
                        ? "Описание версии отсутствует."
                        : update.Notes;

                string zipPath =
                    await UpdateService.DownloadAsync(
                        update.DownloadUrl);

                if (string.IsNullOrWhiteSpace(zipPath))
                {
                    throw new InvalidOperationException(
                        "Сервис обновления не вернул путь к файлу.");
                }

                if (!File.Exists(zipPath))
                {
                    throw new FileNotFoundException(
                        "Скачанный ZIP-файл не найден.",
                        zipPath);
                }

                downloadedZipPath = zipPath;
                selectedUpdate = update;

                StatusText.Text =
                    $"🟢 Версия {update.Version} скачана. Нажмите «Установить»";
            }
            catch (Exception ex)
            {
                downloadedZipPath = null;

                StatusText.Text =
                    "🔴 Ошибка скачивания версии";

                NotesText.Text =
                    ex.Message;
            }
            finally
            {
                SetBusyState(false);
                UpdateButtonsState();
            }
        }

        private void InstallUpdate_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (isBusy)
            {
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(
                        downloadedZipPath))
                {
                    StatusText.Text =
                        "Сначала скачайте обновление";

                    return;
                }

                if (!File.Exists(downloadedZipPath))
                {
                    StatusText.Text =
                        "🔴 Скачанный ZIP-файл не найден";

                    downloadedZipPath = null;

                    UpdateButtonsState();
                    return;
                }

                string applicationDirectory =
                    AppDomain.CurrentDomain.BaseDirectory
                        .TrimEnd(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar);

                string updaterPath =
                    Path.Combine(
                        applicationDirectory,
                        "Updater.exe");

                if (!File.Exists(updaterPath))
                {
                    StatusText.Text =
                        "🔴 Не найден Updater.exe";

                    NotesText.Text =
                        $"Ожидаемый путь:\n{updaterPath}";

                    return;
                }

                MessageBoxResult result =
                    MessageBox.Show(
                        "CarShell будет закрыт, после чего начнётся установка.\n\nПродолжить?",
                        "Установка обновления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                SetBusyState(true);

                StatusText.Text =
                    "Подготовка к установке...";

                CreateUpdateLock();

                var startInfo =
                    new ProcessStartInfo
                    {
                        FileName = updaterPath,
                        WorkingDirectory =
                            applicationDirectory,
                        UseShellExecute = true
                    };

                startInfo.ArgumentList.Add(
                    applicationDirectory);

                startInfo.ArgumentList.Add(
                    downloadedZipPath);

                try
                {
                    Process? updaterProcess =
                        Process.Start(startInfo);

                    if (updaterProcess == null)
                    {
                        throw new InvalidOperationException(
                            "Не удалось запустить Updater.exe.");
                    }
                }
                catch
                {
                    DeleteUpdateLock();
                    throw;
                }

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                SetBusyState(false);

                StatusText.Text =
                    "🔴 Ошибка запуска установки";

                NotesText.Text =
                    ex.Message;

                UpdateButtonsState();
            }
        }

        private static void CreateUpdateLock()
        {
            Directory.CreateDirectory(
                App.UpdateLockDirectory);

            File.WriteAllText(
                App.UpdateLockPath,
                DateTime.UtcNow.ToString(
                    "O",
                    CultureInfo.InvariantCulture));
        }

        private static void DeleteUpdateLock()
        {
            try
            {
                if (File.Exists(App.UpdateLockPath))
                {
                    File.Delete(App.UpdateLockPath);
                }
            }
            catch
            {
                // Ошибка удаления блокировки здесь
                // не должна скрывать основную ошибку.
            }
        }

        private async void RefreshVersions_Click(
            object sender,
            RoutedEventArgs e)
        {
            await LoadVersionsAsync();
        }

        private void VersionsComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (VersionsComboBox.SelectedItem
                is not UpdateInfo update)
            {
                selectedUpdate = null;

                SelectedVersionText.Text =
                    "Выбранная версия: не выбрана";

                UpdateButtonsState();
                return;
            }

            selectedUpdate = update;
            downloadedZipPath = null;

            SelectedVersionText.Text =
                $"Выбранная версия: {update.Version}";

            NotesText.Text =
                string.IsNullOrWhiteSpace(update.Notes)
                    ? "Описание версии отсутствует."
                    : update.Notes;

            if (AreVersionsEqual(
                    update.Version,
                    VersionInfo.Version))
            {
                StatusText.Text =
                    "Выбрана установленная версия";
            }
            else if (IsVersionNewer(
                         update.Version,
                         VersionInfo.Version))
            {
                StatusText.Text =
                    "Выбрана более новая версия";
            }
            else
            {
                StatusText.Text =
                    "Выбрана предыдущая версия для отката";
            }

            UpdateButtonsState();
        }

        private void SetBusyState(
            bool busy)
        {
            isBusy = busy;

            CheckButton.IsEnabled =
                !busy;

            RefreshVersionsButton.IsEnabled =
                !busy;

            VersionsComboBox.IsEnabled =
                !busy;

            if (busy)
            {
                DownloadButton.IsEnabled = false;
                InstallButton.IsEnabled = false;
                RollbackButton.IsEnabled = false;
            }
        }

        private void UpdateButtonsState()
        {
            if (isBusy)
            {
                DownloadButton.IsEnabled = false;
                InstallButton.IsEnabled = false;
                RollbackButton.IsEnabled = false;
                return;
            }

            CheckButton.IsEnabled = true;
            RefreshVersionsButton.IsEnabled = true;
            VersionsComboBox.IsEnabled = true;

            DownloadButton.IsEnabled =
                latestUpdate != null &&
                latestUpdate.HasUpdate &&
                !string.IsNullOrWhiteSpace(
                    latestUpdate.DownloadUrl);

            InstallButton.IsEnabled =
                !string.IsNullOrWhiteSpace(
                    downloadedZipPath) &&
                File.Exists(downloadedZipPath);

            RollbackButton.IsEnabled =
                selectedUpdate != null &&
                !string.IsNullOrWhiteSpace(
                    selectedUpdate.DownloadUrl);
        }

        private static bool AreVersionsEqual(
            string firstVersion,
            string secondVersion)
        {
            return string.Equals(
                NormalizeVersion(firstVersion),
                NormalizeVersion(secondVersion),
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsVersionNewer(
            string candidateVersion,
            string currentVersion)
        {
            string candidate =
                NormalizeVersion(candidateVersion);

            string current =
                NormalizeVersion(currentVersion);

            if (!Version.TryParse(
                    candidate,
                    out Version? candidateParsed))
            {
                return false;
            }

            if (!Version.TryParse(
                    current,
                    out Version? currentParsed))
            {
                return false;
            }

            return candidateParsed >
                   currentParsed;
        }

        private static string NormalizeVersion(
            string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return "0.0.0";
            }

            string normalized =
                version.Trim();

            if (normalized.StartsWith(
                    "v",
                    StringComparison.OrdinalIgnoreCase))
            {
                normalized =
                    normalized.Substring(1);
            }

            int separatorIndex =
                normalized.IndexOfAny(
                    new[]
                    {
                        '-',
                        '+'
                    });

            if (separatorIndex >= 0)
            {
                normalized =
                    normalized.Substring(
                        0,
                        separatorIndex);
            }

            return normalized;
        }
    }
}