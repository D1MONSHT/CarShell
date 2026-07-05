using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CarShell.Services;

namespace CarShell.Pages
{
    public partial class UpdatePage : UserControl
    {
        private readonly MainWindow mainWindow;
        private UpdateInfo? latestUpdate;
        private string? downloadedZipPath;

        public UpdatePage(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;

            CurrentVersionText.Text = $"Текущая версия: {VersionInfo.Version}";
            LatestVersionText.Text = "Последняя версия: неизвестно";
            StatusText.Text = "Нажми «Проверить»";
            NotesText.Text = "";

            DownloadButton.IsEnabled = false;
            InstallButton.IsEnabled = false;
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Проверка обновлений...";
                CheckButton.IsEnabled = false;
                DownloadButton.IsEnabled = false;
                InstallButton.IsEnabled = false;

                latestUpdate = await UpdateService.CheckAsync();
                downloadedZipPath = null;

                LatestVersionText.Text = $"Последняя версия: {latestUpdate.Version}";
                NotesText.Text = string.IsNullOrWhiteSpace(latestUpdate.Notes)
                    ? "Описание отсутствует."
                    : latestUpdate.Notes;

                if (latestUpdate.HasUpdate)
                {
                    StatusText.Text = "🟡 Доступно обновление";
                    DownloadButton.IsEnabled = true;
                }
                else
                {
                    StatusText.Text = "🟢 Установлена последняя версия";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "🔴 Ошибка проверки";
                NotesText.Text = ex.Message;
            }
            finally
            {
                CheckButton.IsEnabled = true;
            }
        }

        private async void DownloadUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (latestUpdate == null)
                    return;

                StatusText.Text = "Скачивание обновления...";
                DownloadButton.IsEnabled = false;
                InstallButton.IsEnabled = false;

                downloadedZipPath = await UpdateService.DownloadAsync(latestUpdate.DownloadUrl);

                StatusText.Text = "🟢 Обновление скачано";
                InstallButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                StatusText.Text = "🔴 Ошибка скачивания";
                NotesText.Text = ex.Message;
                DownloadButton.IsEnabled = true;
            }
        }

        private void InstallUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(downloadedZipPath) || !File.Exists(downloadedZipPath))
                {
                    StatusText.Text = "🔴 Файл обновления не найден";
                    return;
                }

                string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                string updaterPath = Path.Combine(appDir, "Updater.exe");

                if (!File.Exists(updaterPath))
                {
                    StatusText.Text = "🔴 Не найден Updater.exe";
                    return;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    WorkingDirectory = appDir,
                    UseShellExecute = true
                };

                psi.ArgumentList.Add(appDir);
                psi.ArgumentList.Add(downloadedZipPath);

                Process.Start(psi);

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                StatusText.Text = "🔴 Ошибка установки";
                NotesText.Text = ex.Message;
            }
        }
    }
}