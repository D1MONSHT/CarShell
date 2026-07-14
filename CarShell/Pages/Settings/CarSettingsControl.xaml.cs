using System.Windows.Controls;
using System.Windows.Input;

namespace CarShell.Pages.Settings
{
    public partial class CarSettingsControl : UserControl
    {
        public CarSettingsControl()
        {
            InitializeComponent();
        }

        private void ScrollViewer_ManipulationBoundaryFeedback(
            object sender,
            ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }
    }
}
