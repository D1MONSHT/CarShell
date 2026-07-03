using System.Windows;
using System.Windows.Controls;

namespace CarShell.Pages
{
    public partial class ErrorPage : UserControl
    {
        private readonly MainWindow mainWindow;

        public ErrorPage(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            ShowMessage("Выбери блок");
        }

        private void ShowMessage(string text)
        {
            ErrorList.Items.Clear();
            ErrorList.Items.Add(text);
        }

        private void ShowErrors(string title, params string[] errors)
        {
            ErrorList.Items.Clear();
            ErrorList.Items.Add(title);
            ErrorList.Items.Add("");

            if (errors.Length == 0)
            {
                ErrorList.Items.Add("Ошибок нет");
                return;
            }

            foreach (var error in errors)
                ErrorList.Items.Add(error);
        }

        private void Back_Click(object sender, RoutedEventArgs e) => mainWindow.ShowHome();

        private void Ecu_Click(object sender, RoutedEventArgs e)
        {
            ShowErrors("ECU", "P0234 — Передув турбины", "P0400 — Ошибка потока EGR");
        }

        private void Uec_Click(object sender, RoutedEventArgs e)
        {
            ShowErrors("UEC", "B2575 — Ошибка цепи освещения");
        }

        private void Abs_Click(object sender, RoutedEventArgs e) => ShowErrors("ABS");
        private void Rec_Click(object sender, RoutedEventArgs e) => ShowErrors("REC");
        private void Cim_Click(object sender, RoutedEventArgs e) => ShowErrors("CIM");
        private void Ipc_Click(object sender, RoutedEventArgs e) => ShowErrors("IPC");
        private void Pdm_Click(object sender, RoutedEventArgs e) => ShowErrors("PDM");

        private void Ddm_Click(object sender, RoutedEventArgs e)
        {
            ShowErrors("DDM", "Нет связи с блоком двери водителя");
        }
    }
}