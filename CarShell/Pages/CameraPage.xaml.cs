using System.Windows;
using System.Windows.Controls;

namespace CarShell.Pages
{
    public partial class CameraPage : UserControl
    {
        private readonly MainWindow main;

        public CameraPage(MainWindow mainWindow)
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