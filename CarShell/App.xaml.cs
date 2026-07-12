using System;
using System.Globalization;
using System.IO;
using System.Windows;

namespace CarShell
{
    public partial class App : Application
    {
        public static readonly string UpdateLockDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData),
                "CarShell");

        public static readonly string UpdateLockPath =
            Path.Combine(
                UpdateLockDirectory,
                "update.lock");

        protected override void OnStartup(
            StartupEventArgs e)
        {
            try
            {
                if (IsUpdateInProgress())
                {
                    // CarShell мог автоматически запуститься,
                    // пока Updater заменяет файлы.
                    Shutdown(20);
                    return;
                }
            }
            catch
            {
                // Ошибка чтения блокировки не должна
                // полностью блокировать запуск приложения.
            }

            base.OnStartup(e);
        }

        private static bool IsUpdateInProgress()
        {
            if (!File.Exists(UpdateLockPath))
            {
                return false;
            }

            try
            {
                string lockContent =
                    File.ReadAllText(UpdateLockPath);

                if (DateTime.TryParse(
                        lockContent,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out DateTime createdAt))
                {
                    TimeSpan lockAge =
                        DateTime.UtcNow -
                        createdAt.ToUniversalTime();

                    // Если блокировка старше 30 минут,
                    // считаем её оставшейся после сбоя.
                    if (lockAge > TimeSpan.FromMinutes(30))
                    {
                        File.Delete(UpdateLockPath);
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                // Если файл существует, но его не получается прочитать,
                // безопаснее считать, что обновление ещё выполняется.
                return true;
            }
        }
    }
}