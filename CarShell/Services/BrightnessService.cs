using System;
using System.Management;

namespace CarShell.Services
{
    public static class BrightnessService
    {
        public static int GetBrightness()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    @"root\WMI",
                    "SELECT CurrentBrightness FROM WmiMonitorBrightness");

                foreach (ManagementObject item in searcher.Get())
                {
                    return Convert.ToInt32(item["CurrentBrightness"]);
                }
            }
            catch
            {
                // На внешнем мониторе или неподдерживаемом драйвере
                // WMI-управление яркостью может отсутствовать.
            }

            return -1;
        }

        public static bool SetBrightness(int brightness)
        {
            brightness = Math.Clamp(brightness, 0, 100);

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    @"root\WMI",
                    "SELECT * FROM WmiMonitorBrightnessMethods");

                foreach (ManagementObject item in searcher.Get())
                {
                    item.InvokeMethod(
                        "WmiSetBrightness",
                        new object[]
                        {
                            0,
                            (byte)brightness
                        });

                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}