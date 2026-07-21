using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CarShell.Pages
{
    public partial class DiagnosticsPage : UserControl
    {
        private readonly DispatcherTimer clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private DiagnosticStatus currentStatus = new();
        private string selectedBlock = "Motor";

        private readonly Dictionary<string, string> blockDisplayNames = new()
        {
            { "Motor", "Motor Elektronik (ME)" },
            { "AGT", "Automatikgetriebe (AGT)" },
            { "ESC", "Elektronische Stabilitätskontrolle (ESC)" },
            { "ABS", "Antiblockier System (ABS)" },
            { "KMB", "Karosserie Management Bus (KMB)" },
            { "EGR", "Abgasreinigung (EGR)" }
        };

        public DiagnosticsPage(MainWindow mainWindow)
        {
            InitializeComponent();
            clockTimer.Tick += (_, _) => ClockText.Text = DateTime.Now.ToString("HH:mm");
            Loaded += DiagnosticsPage_Loaded;
            Unloaded += (_, _) => clockTimer.Stop();
        }

        private void DiagnosticsPage_Loaded(object sender, RoutedEventArgs e)
        {
            clockTimer.Start();
            ClockText.Text = DateTime.Now.ToString("HH:mm");
            UpdateDiagnostics();
        }

        private void OnBlockSelected(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string block)
            {
                selectedBlock = block;
                RefreshBlockDisplay();
                UpdateButtonStyles();
            }
        }

        private void UpdateButtonStyles()
        {
            var buttons = new[] 
            { 
                ("Motor", MotorButton),
                ("AGT", AgtButton),
                ("ESC", EscButton),
                ("ABS", AbsButton),
                ("KMB", KmbButton),
                ("EGR", EgrButton)
            };

            foreach (var (block, button) in buttons)
            {
                button.Style = selectedBlock == block 
                    ? (Style)FindResource("TabButtonActive") 
                    : (Style)FindResource("TabButton");
            }
        }

        private void RefreshBlockDisplay()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(RefreshBlockDisplay);
                return;
            }

            BlockNameText.Text = blockDisplayNames[selectedBlock];

            var blockStatus = selectedBlock switch
            {
                "Motor" => currentStatus.MotorStatus,
                "AGT" => currentStatus.AgtStatus,
                "ESC" => currentStatus.EscStatus,
                "ABS" => currentStatus.AbsStatus,
                "KMB" => currentStatus.KmbStatus,
                "EGR" => currentStatus.EgrStatus,
                _ => "НЕИЗВЕСТНО"
            };

            BlockStatusText.Text = blockStatus;
            BlockStatusText.Foreground = GetStatusBrush(blockStatus);

            var blockErrors = currentStatus.ActiveErrors.Where(e => e.Block == selectedBlock).ToList();
            var blockWarnings = blockErrors.Where(e => e.Severity == "WARNING").ToList();
            var blockCritical = blockErrors.Where(e => e.Severity == "ERROR").ToList();

            UpdateBlockErrorsDisplay(blockCritical);
            UpdateBlockWarningsDisplay(blockWarnings);
        }

        public void UpdateDiagnostics(DiagnosticStatus? status = null)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateDiagnostics(status));
                return;
            }

            currentStatus = status ?? new DiagnosticStatus();

            RefreshBlockDisplay();
            UpdateStatusIndicator();

            ClockText.Text = DateTime.Now.ToString("HH:mm");
        }

        private void UpdateBlockErrorsDisplay(List<DiagnosticError> errors)
        {
            BlockErrorsPanel.Children.Clear();

            if (errors.Count == 0)
            {
                var border = new Border
                {
                    Style = (Style)FindResource("SuccessCard"),
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "✓ Ошибок не обнаружено",
                                Foreground = new SolidColorBrush(Color.FromRgb(105, 214, 109)),
                                FontSize = 12,
                                FontWeight = FontWeights.SemiBold
                            }
                        }
                    }
                };
                BlockErrorsPanel.Children.Add(border);
                return;
            }

            foreach (var error in errors)
            {
                var codeBlock = new TextBlock
                {
                    Text = error.Code,
                    Style = (Style)FindResource("ErrorCode")
                };

                var descBlock = new TextBlock
                {
                    Text = error.Description,
                    Style = (Style)FindResource("ErrorDescription")
                };

                var panel = new StackPanel { Children = { codeBlock, descBlock } };

                var border = new Border
                {
                    Style = (Style)FindResource("ErrorCard"),
                    Child = panel
                };

                BlockErrorsPanel.Children.Add(border);
            }
        }

        private void UpdateBlockWarningsDisplay(List<DiagnosticError> warnings)
        {
            BlockWarningsPanel.Children.Clear();

            if (warnings.Count == 0)
            {
                var border = new Border
                {
                    Style = (Style)FindResource("SuccessCard"),
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "✓ Предупреждений нет",
                                Foreground = new SolidColorBrush(Color.FromRgb(105, 214, 109)),
                                FontSize = 12,
                                FontWeight = FontWeights.SemiBold
                            }
                        }
                    }
                };
                BlockWarningsPanel.Children.Add(border);
                return;
            }

            foreach (var warning in warnings)
            {
                var codeBlock = new TextBlock
                {
                    Text = warning.Code,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 191, 50)),
                    FontSize = 12,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.SemiBold
                };

                var descBlock = new TextBlock
                {
                    Text = warning.Description,
                    Style = (Style)FindResource("ErrorDescription")
                };

                var panel = new StackPanel { Children = { codeBlock, descBlock } };

                var border = new Border
                {
                    Style = (Style)FindResource("WarningCard"),
                    Child = panel
                };

                BlockWarningsPanel.Children.Add(border);
            }
        }

        private void UpdateStatusIndicator()
        {
            var allErrors = currentStatus.ActiveErrors;

            if (allErrors.Count == 0)
            {
                StatusIndicatorText.Text = "ВСЕ СИСТЕМЫ OK";
                StatusIndicatorText.Foreground = new SolidColorBrush(Color.FromRgb(105, 214, 109));
            }
            else
            {
                int errorCount = allErrors.Count(e => e.Severity == "ERROR");
                int warningCount = allErrors.Count(e => e.Severity == "WARNING");

                if (errorCount > 0)
                {
                    StatusIndicatorText.Text = $"⚠ {errorCount} ОШИБОК";
                    StatusIndicatorText.Foreground = new SolidColorBrush(Color.FromRgb(255, 45, 55));
                }
                else if (warningCount > 0)
                {
                    StatusIndicatorText.Text = $"⚠ {warningCount} ПРЕДУПРЕЖДЕНИЙ";
                    StatusIndicatorText.Foreground = new SolidColorBrush(Color.FromRgb(255, 191, 50));
                }
            }
        }

        private SolidColorBrush GetStatusBrush(string status)
        {
            return status switch
            {
                "НОРМА" => new SolidColorBrush(Color.FromRgb(105, 214, 109)),
                "ПРЕДУПРЕЖДЕНИЕ" => new SolidColorBrush(Color.FromRgb(255, 191, 50)),
                "ОШИБКА" => new SolidColorBrush(Color.FromRgb(255, 45, 55)),
                _ => new SolidColorBrush(Color.FromRgb(180, 185, 192))
            };
        }
    }

    public sealed class DiagnosticError
    {
        public string Code { get; set; } = "";
        public string Description { get; set; } = "";
        public string Severity { get; set; } = "ERROR";
        public string Block { get; set; } = "Motor";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public sealed class DiagnosticStatus
    {
        public string MotorStatus { get; set; } = "НОРМА";
        public string AgtStatus { get; set; } = "НОРМА";
        public string EscStatus { get; set; } = "НОРМА";
        public string AbsStatus { get; set; } = "НОРМА";
        public string KmbStatus { get; set; } = "НОРМА";
        public string EgrStatus { get; set; } = "НОРМА";
        public List<DiagnosticError> ActiveErrors { get; set; } = new();
    }
}
