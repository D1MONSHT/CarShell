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
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.ShowUpdate();
        }
    }
}