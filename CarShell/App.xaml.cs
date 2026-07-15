using System;
using System.IO;
using System.Windows;

namespace CarShell
{
    public partial class App : Application

    {
        public static string UpdateLockDirectory =>
           Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
               "CarShell");

        public static string UpdateLockPath =>
            Path.Combine(UpdateLockDirectory, "update.lock");
        private static readonly string LogPath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CarShell",
                "boot.log");

        protected override void OnStartup(StartupEventArgs e)
        {
            WriteBootLog("App.OnStartup START");

            base.OnStartup(e);

            WriteBootLog("App.OnStartup END");
        }

        public static void WriteBootLog(string message)
        {
            try
            {
                string? directory = Path.GetDirectoryName(LogPath);

                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.AppendAllText(
                    LogPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}{Environment.NewLine}");
            }
            catch
            {
                // Лог не должен мешать запуску приложения.
            }
        }
    }
}