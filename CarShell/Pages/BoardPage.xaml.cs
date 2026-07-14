using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CarShell.Pages
{
    public partial class BoardPage : UserControl
    {
        private readonly DispatcherTimer clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };

        public BoardPage(MainWindow mainWindow)
        {
            InitializeComponent();
            clockTimer.Tick += (_, _) => ClockText.Text = DateTime.Now.ToString("HH:mm");
            Loaded += BoardPage_Loaded;
            Unloaded += (_, _) => clockTimer.Stop();
        }

        private void BoardPage_Loaded(object sender, RoutedEventArgs e)
        {
            clockTimer.Start();
            ClockText.Text = DateTime.Now.ToString("HH:mm");

            UpdateTelemetry(new VehicleTelemetry
            {
                SpeedKmh = 82,
                EngineRpm = 2100,
                CoolantTemperatureC = 90,
                OilTemperatureC = 96,
                BoostBar = 1.35,
                BatteryVoltage = 14.2,
                FuelLevelPercent = 42,
                InstantConsumption = 4.2,
                AverageConsumption = 5.6,
                RangeKm = 568,
                MafKgHour = 315,
                AcceleratorPedalPercent = 28,
                MileageKm = 235678,
                Gear = "5",
                FrontLeftTireBar = 2.3,
                FrontRightTireBar = 2.3,
                RearLeftTireBar = 2.4,
                RearRightTireBar = 2.4,
                FrontLeftDoorOpen = true,
                FrontRightDoorOpen = true,
                RearLeftDoorOpen = false,
                RearRightDoorOpen = false,
                TrunkOpen = true,
                LowBeamEnabled = true,
                HandbrakeEnabled = false,
                SeatbeltFastened = true,
                CanConnected = true
            });
        }

        public void UpdateTelemetry(VehicleTelemetry d)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateTelemetry(d));
                return;
            }

            SpeedGauge.AnimateTo(Clamp(d.SpeedKmh, 0, 240));
            RpmGauge.AnimateTo(Clamp(d.EngineRpm, 0, 6000));

            CoolantTemperatureText.Text = $"{Clamp(d.CoolantTemperatureC, -50, 150):0} °C";
            OilTemperatureText.Text = $"{Clamp(d.OilTemperatureC, -50, 180):0} °C";
            BoostText.Text = $"{Clamp(d.BoostBar, 0, 3):0.00} bar";
            BatteryText.Text = $"{Clamp(d.BatteryVoltage, 0, 20):0.0} V";
            TopBatteryText.Text = BatteryText.Text;
            FuelLevelText.Text = $"{Clamp(d.FuelLevelPercent, 0, 100):0} %";
            InstantConsumptionText.Text = $"{Math.Max(0, d.InstantConsumption):0.0} л/100";
            AverageConsumptionText.Text = $"{Math.Max(0, d.AverageConsumption):0.0} л/100";
            RangeText.Text = $"{Math.Max(0, d.RangeKm)} км";
            MafText.Text = $"{Math.Max(0, d.MafKgHour):0} кг/ч";
            PedalText.Text = $"{Clamp(d.AcceleratorPedalPercent, 0, 100):0} %";
            MileageText.Text = $"{Math.Max(0, d.MileageKm):N0} км";

            UpdateGear(d.Gear);
            UpdateDoors(d);
            UpdateTires(d);
            UpdateIndicators(d);
            UpdateCan(d.CanConnected);
        }

        private void UpdateGear(string? gear)
        {
            gear = gear?.Trim().ToUpperInvariant();
            GearText.Text = gear is "R" or "N" or "1" or "2" or "3" or "4" or "5" or "6" ? gear : "—";
            GearText.Foreground = gear == "R"
                ? new SolidColorBrush(Color.FromRgb(255, 47, 55))
                : gear == "N"
                    ? new SolidColorBrush(Color.FromRgb(190, 197, 205))
                    : Brushes.White;
        }

        private void UpdateDoors(VehicleTelemetry d)
        {
            FrontLeftDoorIndicator.Visibility = d.FrontLeftDoorOpen ? Visibility.Visible : Visibility.Hidden;
            FrontRightDoorIndicator.Visibility = d.FrontRightDoorOpen ? Visibility.Visible : Visibility.Hidden;
            RearLeftDoorIndicator.Visibility = d.RearLeftDoorOpen ? Visibility.Visible : Visibility.Hidden;
            RearRightDoorIndicator.Visibility = d.RearRightDoorOpen ? Visibility.Visible : Visibility.Hidden;

            TrunkIndicator.Background = d.TrunkOpen
                ? new SolidColorBrush(Color.FromRgb(84, 16, 20))
                : new SolidColorBrush(Color.FromRgb(65, 70, 75));

            int doors = new[]
            {
                d.FrontLeftDoorOpen, d.FrontRightDoorOpen,
                d.RearLeftDoorOpen, d.RearRightDoorOpen
            }.Count(x => x);

            if (doors == 0 && !d.TrunkOpen)
            {
                VehicleStatusText.Text = "Системы в норме";
                DoorsStatusText.Text = "Всё закрыто";
                DoorsStatusText.Foreground = new SolidColorBrush(Color.FromRgb(146, 155, 165));
            }
            else
            {
                VehicleStatusText.Text = "Проверьте автомобиль";
                DoorsStatusText.Text = d.TrunkOpen
                    ? $"Открыто дверей: {doors}, багажник"
                    : $"Открыто дверей: {doors}";
                DoorsStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 91, 97));
            }
        }

        private void UpdateTires(VehicleTelemetry d)
        {
            SetTire(FrontLeftTireText, d.FrontLeftTireBar);
            SetTire(FrontRightTireText, d.FrontRightTireBar);
            SetTire(RearLeftTireText, d.RearLeftTireBar);
            SetTire(RearRightTireText, d.RearRightTireBar);

            double[] p = { d.FrontLeftTireBar, d.FrontRightTireBar, d.RearLeftTireBar, d.RearRightTireBar };
            bool missing = p.Any(x => x <= 0);
            bool critical = p.Any(x => x > 0 && (x < 1.8 || x > 3.2));
            bool warning = p.Any(x => x > 0 && (x < 2.0 || x > 3.0));

            TireStatusText.Text = missing ? "НЕТ ДАННЫХ"
                : critical ? "КРИТИЧНО"
                : warning ? "ПРОВЕРИТЬ"
                : "НОРМА";

            TireStatusText.Foreground = missing
                ? new SolidColorBrush(Color.FromRgb(145, 154, 164))
                : critical
                    ? new SolidColorBrush(Color.FromRgb(255, 54, 62))
                    : warning
                        ? new SolidColorBrush(Color.FromRgb(255, 191, 50))
                        : new SolidColorBrush(Color.FromRgb(105, 214, 109));
        }

        private static void SetTire(TextBlock text, double pressure)
        {
            text.Text = pressure <= 0 ? "—" : pressure.ToString("0.0");
            text.Foreground = pressure <= 0
                ? new SolidColorBrush(Color.FromRgb(145, 154, 164))
                : pressure < 1.8 || pressure > 3.2
                    ? new SolidColorBrush(Color.FromRgb(255, 54, 62))
                    : pressure < 2.0 || pressure > 3.0
                        ? new SolidColorBrush(Color.FromRgb(255, 191, 50))
                        : new SolidColorBrush(Color.FromRgb(105, 214, 109));
        }

        private void UpdateIndicators(VehicleTelemetry d)
        {
            LowBeamIndicator.Opacity = d.LowBeamEnabled ? 1.0 : 0.18;
            HandbrakeIndicator.Opacity = d.HandbrakeEnabled ? 1.0 : 0.18;
            SeatbeltIndicator.Opacity = d.SeatbeltFastened ? 0.18 : 1.0;
        }

        private void UpdateCan(bool connected)
        {
            ConnectionStatusText.Text = connected ? "CAN подключён" : "Нет связи с CAN";
            ConnectionStatusText.Foreground = connected
                ? new SolidColorBrush(Color.FromRgb(131, 226, 135))
                : new SolidColorBrush(Color.FromRgb(255, 101, 107));
            CanStatusEllipse.Fill = connected
                ? new SolidColorBrush(Color.FromRgb(85, 199, 90))
                : new SolidColorBrush(Color.FromRgb(239, 41, 50));
        }

        public static string CalculateManualGear(double speedKmh, double rpm)
        {
            if (speedKmh < 3 || rpm < 650) return "N";
            double ratio = rpm / speedKmh;
            if (ratio >= 100) return "1";
            if (ratio >= 65) return "2";
            if (ratio >= 47) return "3";
            if (ratio >= 36) return "4";
            if (ratio >= 29) return "5";
            return "6";
        }

        private static double Clamp(double value, double min, double max) =>
            double.IsNaN(value) || double.IsInfinity(value) ? min : Math.Max(min, Math.Min(max, value));
    }

    public sealed class VehicleTelemetry
    {
        public double SpeedKmh { get; set; }
        public double EngineRpm { get; set; }
        public double CoolantTemperatureC { get; set; }
        public double OilTemperatureC { get; set; }
        public double BoostBar { get; set; }
        public double BatteryVoltage { get; set; }
        public double FuelLevelPercent { get; set; }
        public double InstantConsumption { get; set; }
        public double AverageConsumption { get; set; }
        public int RangeKm { get; set; }
        public double MafKgHour { get; set; }
        public double AcceleratorPedalPercent { get; set; }
        public int MileageKm { get; set; }
        public string Gear { get; set; } = "N";
        public double FrontLeftTireBar { get; set; }
        public double FrontRightTireBar { get; set; }
        public double RearLeftTireBar { get; set; }
        public double RearRightTireBar { get; set; }
        public bool FrontLeftDoorOpen { get; set; }
        public bool FrontRightDoorOpen { get; set; }
        public bool RearLeftDoorOpen { get; set; }
        public bool RearRightDoorOpen { get; set; }
        public bool TrunkOpen { get; set; }
        public bool LowBeamEnabled { get; set; }
        public bool HandbrakeEnabled { get; set; }
        public bool SeatbeltFastened { get; set; }
        public bool CanConnected { get; set; }
    }
}