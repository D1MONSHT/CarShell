using System.Windows;
using System.Windows.Controls;
using CarShell.Services;
namespace CarShell.Pages
{
    public partial class SettingsPage : UserControl
    {
        private readonly MainWindow main;

        public SettingsPage(MainWindow mainWindow)
        {
            InitializeComponent();
            main = mainWindow;
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var update = await UpdateService.CheckAsync();

                if (!update.HasUpdate)
                {
                    MessageBox.Show(
                        $"Установлена последняя версия: {VersionInfo.Version}",
                        "Обновление");
                    return;
                }

                MessageBox.Show(
                    $"Доступна новая версия: {update.Version}\n\n{update.Notes}",
                    "Обновление найдено");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка проверки обновлений:\n" + ex.Message,
                    "Ошибка");
            }
        }
    }
}