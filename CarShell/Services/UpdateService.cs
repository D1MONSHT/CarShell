using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CarShell.Services
{
    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string Notes { get; set; } = "";
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

            return new UpdateInfo
            {
                Version = latestVersion,
                Notes = notes,
                HasUpdate = latestVersion != VersionInfo.Version
            };
        }
    }
}