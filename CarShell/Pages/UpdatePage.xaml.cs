using System;
using System.Windows;
using System.Windows.Controls;
using CarShell.Services;

namespace CarShell.Pages
{
    public partial class UpdatePage : UserControl
    {
        private readonly MainWindow mainWindow;

        public UpdatePage(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;

            CurrentVersionText.Text = $"Текущая версия: {VersionInfo.Version}";
            LatestVersionText.Text = "Последняя версия: неизвестно";
            StatusText.Text = "Нажми «Проверить»";
            NotesText.Text = "";
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Проверка обновлений...";

                var update = await UpdateService.CheckAsync();

                LatestVersionText.Text = $"Последняя версия: {update.Version}";
                NotesText.Text = string.IsNullOrWhiteSpace(update.Notes)
                    ? "Описание отсутствует."
                    : update.Notes;

                StatusText.Text = update.HasUpdate
                    ? "🟡 Доступно обновление"
                    : "🟢 Система обновлена";
            }
            catch (Exception ex)
            {
                StatusText.Text = "🔴 Ошибка проверки";
                NotesText.Text = ex.Message;
            }
        }
    }
}