using System.Windows;
using System.Windows.Controls;

namespace CarShell.Pages
{
    public partial class FilesPage : UserControl
    {
        private readonly MainWindow main;

        public FilesPage(MainWindow mainWindow)
        {
            InitializeComponent();
            main = mainWindow;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            main.ShowHome();
        }
    }
}