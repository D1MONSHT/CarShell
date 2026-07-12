using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CarShell.Services
{
    public static class SystemDateTimeService
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Milliseconds;
        }

        [DllImport(
            "kernel32.dll",
            SetLastError = true)]
        private static extern bool SetLocalTime(
            ref SYSTEMTIME systemTime);

        public static bool SetDateTime(DateTime dateTime)
        {
            var systemTime = new SYSTEMTIME
            {
                Year = (ushort)dateTime.Year,
                Month = (ushort)dateTime.Month,
                Day = (ushort)dateTime.Day,
                DayOfWeek = (ushort)dateTime.DayOfWeek,
                Hour = (ushort)dateTime.Hour,
                Minute = (ushort)dateTime.Minute,
                Second = (ushort)dateTime.Second,
                Milliseconds = (ushort)dateTime.Millisecond
            };

            return SetLocalTime(ref systemTime);
        }

        public static void SynchronizeTime()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "w32tm.exe",
                Arguments = "/resync /force",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
    }
}