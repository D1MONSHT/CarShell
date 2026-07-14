using System.Windows.Controls;
using System.Windows.Input;

namespace CarShell.Pages.Settings
{
    public partial class AboutSettingsControl : UserControl
    {
        public AboutSettingsControl()
        {
            InitializeComponent();
            LoadVersion();
        }

        private void LoadVersion()
        {
            try
            {
                AboutVersionText.Text =
                    $"Версия {VersionInfo.Version}";
            }
            catch
            {
                AboutVersionText.Text =
                    "Версия неизвестна";
            }
        }

        private void ScrollViewer_ManipulationBoundaryFeedback(
            object sender,
            ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }
    }
}
