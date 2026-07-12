using System;
using System.IO;
using System.Text.Json;

namespace CarShell.Services
{
    public sealed class CarShellUserSettings
    {
        public int Brightness { get; set; } = 70;

        public int Volume { get; set; } = 50;

        public bool IsMuted { get; set; }
    }

    public static class UserSettingsService
    {
        private static readonly string SettingsDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "CarShell");

        private static readonly string SettingsFile =
            Path.Combine(
                SettingsDirectory,
                "settings.json");

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                WriteIndented = true
            };

        public static CarShellUserSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFile))
                {
                    return new CarShellUserSettings();
                }

                string json =
                    File.ReadAllText(SettingsFile);

                CarShellUserSettings? settings =
                    JsonSerializer.Deserialize<CarShellUserSettings>(
                        json,
                        JsonOptions);

                return settings ??
                       new CarShellUserSettings();
            }
            catch
            {
                return new CarShellUserSettings();
            }
        }

        public static void Save(
            CarShellUserSettings settings)
        {
            try
            {
                Directory.CreateDirectory(
                    SettingsDirectory);

                string json =
                    JsonSerializer.Serialize(
                        settings,
                        JsonOptions);

                File.WriteAllText(
                    SettingsFile,
                    json);
            }
            catch
            {
                // Ошибка сохранения не должна останавливать CarShell.
            }
        }
    }
}