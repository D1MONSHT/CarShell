using System;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CarShell.Pages
{
    public partial class HomePage : UserControl
    {
        private SerialPort? serialPort;

        private readonly DispatcherTimer readTimer;
        private readonly DispatcherTimer reconnectTimer;
        private readonly DispatcherTimer screenOffTimer;

        private string serialBuffer = "";

        private const string TargetPort = "COM9";
        private bool accState = true;

        [DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint esFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        [DllImport("user32.dll")]
        private static extern void mouse_event(
            uint dwFlags,
            uint dx,
            uint dy,
            uint dwData,
            UIntPtr dwExtraInfo);

        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;

        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;

        private const uint SMTO_ABORTIFHUNG = 0x0002;
        private const uint MOUSEEVENTF_MOVE = 0x0001;

        public HomePage(MainWindow mainWindow)
        {
            InitializeComponent();

            KeepScreenOn();

            readTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            readTimer.Tick += ReadTimer_Tick;

            reconnectTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            reconnectTimer.Tick += ReconnectTimer_Tick;
            reconnectTimer.Start();

            screenOffTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            screenOffTimer.Tick += ScreenOffTimer_Tick;

            ShowDisconnected();
            TryConnectSerial();
        }

        private void ReconnectTimer_Tick(object? sender, EventArgs e)
        {
            ClockText.Text = DateTime.Now.ToString("HH:mm");

            if (serialPort == null || !serialPort.IsOpen)
                TryConnectSerial();

            if (accState)
                KeepScreenOn();
        }

        private void TryConnectSerial()
        {
            try
            {
                readTimer.Stop();

                serialPort?.Close();
                serialPort?.Dispose();

                serialPort = new SerialPort(TargetPort, 115200)
                {
                    NewLine = "\n",
                    Encoding = Encoding.ASCII,
                    ReadTimeout = 100,
                    DtrEnable = true,
                    RtsEnable = true
                };

                serialPort.Open();
                serialPort.DiscardInBuffer();

                VoltageText.Text = "CONNECTED";
                BoostText.Text = "WAIT DATA";

                readTimer.Start();
            }
            catch
            {
                ShowDisconnected();
            }
        }

        private void ReadTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (serialPort == null || !serialPort.IsOpen)
                    return;

                string data = serialPort.ReadExisting();

                if (string.IsNullOrEmpty(data))
                    return;

                serialBuffer += data;

                while (serialBuffer.Contains("\n"))
                {
                    int index = serialBuffer.IndexOf("\n", StringComparison.Ordinal);
                    string line = serialBuffer.Substring(0, index).Trim();
                    serialBuffer = serialBuffer.Substring(index + 1);

                    if (line.StartsWith("{") && line.EndsWith("}"))
                        UpdateFromJson(line);
                }
            }
            catch
            {
                ShowDisconnected();
            }
        }

        private void UpdateFromJson(string json)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                int acc = root.GetProperty("acc").GetInt32();

                int rpm = root.GetProperty("rpm").GetInt32();
                int speed = root.GetProperty("speed").GetInt32();
                int temp = root.GetProperty("temp").GetInt32();
                double boost = root.GetProperty("boost").GetDouble();
                double voltage = root.GetProperty("voltage").GetDouble();

                HandleAcc(acc);

                SpeedText.Text = speed.ToString();
                RpmText.Text = rpm.ToString();
                TempText.Text = temp + "°C";
                BoostText.Text = boost.ToString("F2") + " bar";
                VoltageText.Text = voltage.ToString("F1") + " V";

                ClockText.Text = DateTime.Now.ToString("HH:mm");
            }
            catch
            {
                BoostText.Text = "JSON ERR";
            }
        }

        private void HandleAcc(int acc)
        {
            bool newAccState = acc == 1;

            if (newAccState == accState)
            {
                if (accState)
                    KeepScreenOn();

                return;
            }

            accState = newAccState;

            if (accState)
            {
                screenOffTimer.Stop();

                KeepScreenOn();
                WakeScreenByVirtualMouse();

                BoostText.Text = "ACC ON";
            }
            else
            {
                AllowScreenOff();

                screenOffTimer.Stop();
                screenOffTimer.Start();

                BoostText.Text = "SCREEN OFF 30s";
            }
        }

        private void ScreenOffTimer_Tick(object? sender, EventArgs e)
        {
            screenOffTimer.Stop();

            if (!accState)
                TurnScreenOff();
        }

        private void KeepScreenOn()
        {
            SetThreadExecutionState(
                ES_CONTINUOUS |
                ES_SYSTEM_REQUIRED |
                ES_DISPLAY_REQUIRED);
        }

        private void AllowScreenOff()
        {
            SetThreadExecutionState(ES_CONTINUOUS);
        }

        private void TurnScreenOff()
        {
            try
            {
                AllowScreenOff();

                SendMessageTimeout(
                    HWND_BROADCAST,
                    WM_SYSCOMMAND,
                    new IntPtr(SC_MONITORPOWER),
                    new IntPtr(2),
                    SMTO_ABORTIFHUNG,
                    1000,
                    out _);

                BoostText.Text = "SCREEN OFF";
            }
            catch
            {
                BoostText.Text = "SCREEN ERR";
            }
        }
        
        private void WakeScreenByVirtualMouse()
        {
            try
            {
                KeepScreenOn();

                mouse_event(MOUSEEVENTF_MOVE, 1, 0, 0, UIntPtr.Zero);
                mouse_event(MOUSEEVENTF_MOVE, unchecked((uint)-1), 0, 0, UIntPtr.Zero);
            }
            catch
            {
                BoostText.Text = "WAKE ERR";
            }
        }

        private void ShowDisconnected()
        {
            SpeedText.Text = "--";
            RpmText.Text = "--";
            TempText.Text = "--°C";
            BoostText.Text = "-- bar";
            VoltageText.Text = "NO COM";
            ClockText.Text = DateTime.Now.ToString("HH:mm");
        }
    }
}