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
        private List<DiagnosticError> activeErrors = new();
        private List<DiagnosticError> errorHistory = new();

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

        public void UpdateDiagnostics(DiagnosticStatus? status = null)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateDiagnostics(status));
                return;
            }

            status ??= new DiagnosticStatus();

            EngineStatusText.Text = status.EngineStatus;
            EngineStatusText.Foreground = GetStatusBrush(status.EngineStatus);

            TransmissionStatusText.Text = status.TransmissionStatus;
            TransmissionStatusText.Foreground = GetStatusBrush(status.TransmissionStatus);

            EmissionStatusText.Text = status.EmissionStatus;
            EmissionStatusText.Foreground = GetStatusBrush(status.EmissionStatus);

            BrakeStatusText.Text = status.BrakeStatus;
            BrakeStatusText.Foreground = GetStatusBrush(status.BrakeStatus);

            activeErrors = status.ActiveErrors;
            errorHistory = status.ErrorHistory;

            UpdateErrorsDisplay();
            UpdateWarningsDisplay();
            UpdateHistoryDisplay();
            UpdateStatusIndicator();

            ClockText.Text = DateTime.Now.ToString("HH:mm");
        }

        private void UpdateErrorsDisplay()
        {
            ErrorsPanel.Children.Clear();

            if (activeErrors.Count == 0)
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
                ErrorsPanel.Children.Add(border);
                return;
            }

            foreach (var error in activeErrors)
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

                ErrorsPanel.Children.Add(border);
            }
        }

        private void UpdateWarningsDisplay()
        {
            WarningsPanel.Children.Clear();

            var warnings = activeErrors.Where(e => e.Severity == "WARNING").ToList();

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
                WarningsPanel.Children.Add(border);
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

                WarningsPanel.Children.Add(border);
            }
        }

        private void UpdateHistoryDisplay()
        {
            HistoryPanel.Children.Clear();

            if (errorHistory.Count == 0)
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
                                Text = "История пуста",
                                Foreground = new SolidColorBrush(Color.FromRgb(146, 155, 165)),
                                FontSize = 12
                            }
                        }
                    }
                };
                HistoryPanel.Children.Add(border);
                return;
            }

            foreach (var entry in errorHistory.Take(5))
            {
                var codeBlock = new TextBlock
                {
                    Text = $"{entry.Code} ({entry.Timestamp:HH:mm:ss})",
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 185, 192)),
                    FontSize = 11,
                    FontFamily = new FontFamily("Consolas")
                };

                var descBlock = new TextBlock
                {
                    Text = entry.Description,
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 167, 175)),
                    FontSize = 10,
                    Margin = new Thickness(0, 2, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };

                var panel = new StackPanel { Children = { codeBlock, descBlock } };

                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(11, 15, 20)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(37, 43, 51)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 6),
                    Child = panel
                };

                HistoryPanel.Children.Add(border);
            }
        }

        private void UpdateStatusIndicator()
        {
            if (activeErrors.Count == 0)
            {
                StatusIndicatorText.Text = "ВСЕ СИСТЕМЫ OK";
                StatusIndicatorText.Foreground = new SolidColorBrush(Color.FromRgb(105, 214, 109));
            }
            else
            {
                int errorCount = activeErrors.Count(e => e.Severity == "ERROR");
                int warningCount = activeErrors.Count(e => e.Severity == "WARNING");

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
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public sealed class DiagnosticStatus
    {
        public string EngineStatus { get; set; } = "НОРМА";
        public string TransmissionStatus { get; set; } = "НОРМА";
        public string EmissionStatus { get; set; } = "НОРМА";
        public string BrakeStatus { get; set; } = "НОРМА";
        public List<DiagnosticError> ActiveErrors { get; set; } = new();
        public List<DiagnosticError> ErrorHistory { get; set; } = new();
    }
}
