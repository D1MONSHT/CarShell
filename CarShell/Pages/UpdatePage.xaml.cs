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

                var update = await UpdateService.CheckAsync();
                latestUpdate = update;
                downloadedZipPath = null;

                LatestVersionText.Text = $"Последняя версия: {update.Version}";
                NotesText.Text = string.IsNullOrWhiteSpace(update.Notes)
                    ? "Описание отсутствует."
                    : update.Notes;

                if (update.HasUpdate)
                {
                    StatusText.Text = "🟡 Доступно обновление";
                    DownloadButton.IsEnabled = true;
                }
                else
                {
                    StatusText.Text = "🟢 Система обновлена";
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

                string path = await UpdateService.DownloadAsync(latestUpdate.DownloadUrl);
                downloadedZipPath = path;

                StatusText.Text = $"🟢 Обновление скачано:\n{path}";
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

                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string updaterPath = Path.Combine(appDir, "Updater.exe");

                if (!File.Exists(updaterPath))
                {
                    StatusText.Text = "🔴 Updater.exe не найден";
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = $"\"{downloadedZipPath}\" \"{appDir}\" \"CarShell.exe\"",
                    UseShellExecute = true
                });

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