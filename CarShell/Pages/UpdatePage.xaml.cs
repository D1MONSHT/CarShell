using System;
using System.Windows;
using System.Windows.Controls;
using CarShell.Services;

namespace CarShell.Pages
{
    public partial class UpdatePage : UserControl
    {
        private readonly MainWindow mainWindow;
        private UpdateInfo? latestUpdate;

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
    }
}