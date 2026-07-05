using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CarShell.Services
{
    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string Notes { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public bool HasUpdate { get; set; }
    }

    public static class UpdateService
    {
        private const string RepoApiUrl =
            "https://api.github.com/repos/D1MONSHT/CarShell/releases/latest";

        public static async Task<UpdateInfo> CheckAsync()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CarShell-Updater");

            string json = await client.GetStringAsync(RepoApiUrl);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string tag = root.GetProperty("tag_name").GetString() ?? "v0.0.0";
            string notes = root.GetProperty("body").GetString() ?? "";
            string latestVersion = tag.TrimStart('v');

            string downloadUrl = "";

            if (root.TryGetProperty("assets", out var assets))
            {
                var asset = assets.EnumerateArray()
                    .FirstOrDefault(x => x.GetProperty("name").GetString() == "CarShell-win-x64.zip");

                if (asset.ValueKind != JsonValueKind.Undefined)
                    downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
            }

            return new UpdateInfo
            {
                Version = latestVersion,
                Notes = notes,
                DownloadUrl = downloadUrl,
                HasUpdate = latestVersion != VersionInfo.Version
            };
        }

        public static async Task<string> DownloadAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("В релизе нет файла CarShell.zip");

            string updatesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updates");
            Directory.CreateDirectory(updatesDir);

            string zipPath = Path.Combine(updatesDir, "CarShell.zip");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CarShell-Updater");

            byte[] data = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(zipPath, data);

            return zipPath;
        }
    }
}
