using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CarShell.Services
{
    public static class UpdateService
    {
        private const string ReleasesUrl =
            "https://api.github.com/repos/D1MONSHT/CarShell/releases";

        private const string LatestReleaseUrl =
            "https://api.github.com/repos/D1MONSHT/CarShell/releases/latest";

        private const string AssetName =
            "CarShell-win-x64.zip";

        private static readonly HttpClient HttpClient =
            CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(30)
            };

            string version =
                string.IsNullOrWhiteSpace(VersionInfo.Version)
                    ? "1.0.0"
                    : NormalizeVersion(VersionInfo.Version);

            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(
                    "CarShell",
                    version));

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/vnd.github+json"));

            client.DefaultRequestHeaders.Add(
                "X-GitHub-Api-Version",
                "2022-11-28");

            return client;
        }

        public static async Task<UpdateInfo> CheckAsync(
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response =
                await HttpClient.GetAsync(
                    LatestReleaseUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            await EnsureSuccessAsync(
                response,
                "Не удалось проверить обновления",
                cancellationToken);

            string json =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            using JsonDocument document =
                JsonDocument.Parse(json);

            UpdateInfo? update =
                ParseRelease(
                    document.RootElement);

            if (update == null)
            {
                throw new InvalidOperationException(
                    $"В последнем GitHub Release не найден файл {AssetName}.");
            }

            update.HasUpdate =
                IsNewerVersion(
                    update.Version,
                    VersionInfo.Version);

            return update;
        }

        public static async Task<List<UpdateInfo>> GetVersionsAsync(
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response =
                await HttpClient.GetAsync(
                    ReleasesUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            await EnsureSuccessAsync(
                response,
                "Не удалось получить список версий",
                cancellationToken);

            string json =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            using JsonDocument document =
                JsonDocument.Parse(json);

            if (document.RootElement.ValueKind !=
                JsonValueKind.Array)
            {
                throw new InvalidOperationException(
                    "GitHub вернул некорректный список релизов.");
            }

            var versions =
                new List<UpdateInfo>();

            foreach (JsonElement release in
                     document.RootElement.EnumerateArray())
            {
                UpdateInfo? update =
                    ParseRelease(
                        release);

                if (update == null)
                {
                    continue;
                }

                update.HasUpdate =
                    IsNewerVersion(
                        update.Version,
                        VersionInfo.Version);

                versions.Add(
                    update);
            }

            versions.Sort(
                (first, second) =>
                    CompareVersions(
                        second.Version,
                        first.Version));

            return versions;
        }

        public static async Task<string> DownloadAsync(
            string downloadUrl,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(
                    downloadUrl))
            {
                throw new ArgumentException(
                    "Ссылка на файл обновления отсутствует.",
                    nameof(downloadUrl));
            }

            if (!Uri.TryCreate(
                    downloadUrl,
                    UriKind.Absolute,
                    out Uri? uri))
            {
                throw new ArgumentException(
                    "Ссылка на обновление имеет неверный формат.",
                    nameof(downloadUrl));
            }

            string updateDirectory =
                Path.Combine(
                    Path.GetTempPath(),
                    "CarShell",
                    "Updates");

            Directory.CreateDirectory(
                updateDirectory);

            CleanupOldUpdateFiles(
                updateDirectory);

            string uniqueFileName =
                $"CarShell-" +
                $"{DateTime.Now:yyyyMMdd-HHmmss}-" +
                $"{Guid.NewGuid():N}.zip";

            string destinationPath =
                Path.Combine(
                    updateDirectory,
                    uniqueFileName);

            string temporaryPath =
                destinationPath +
                ".download";

            try
            {
                using HttpResponseMessage response =
                    await HttpClient.GetAsync(
                        uri,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                await EnsureSuccessAsync(
                    response,
                    "Не удалось скачать обновление",
                    cancellationToken);

                await using Stream sourceStream =
                    await response.Content.ReadAsStreamAsync(
                        cancellationToken);

                await using (
                    var destinationStream =
                        new FileStream(
                            temporaryPath,
                            FileMode.CreateNew,
                            FileAccess.Write,
                            FileShare.None,
                            bufferSize: 81920,
                            useAsync: true))
                {
                    await sourceStream.CopyToAsync(
                        destinationStream,
                        81920,
                        cancellationToken);

                    await destinationStream.FlushAsync(
                        cancellationToken);
                }

                if (!File.Exists(
                        temporaryPath))
                {
                    throw new FileNotFoundException(
                        "Временный файл обновления не был создан.",
                        temporaryPath);
                }

                long fileSize =
                    new FileInfo(
                        temporaryPath).Length;

                if (fileSize <= 0)
                {
                    throw new InvalidOperationException(
                        "Скачанный файл обновления пуст.");
                }

                await MoveDownloadedFileAsync(
                    temporaryPath,
                    destinationPath,
                    cancellationToken);

                return destinationPath;
            }
            catch
            {
                DeleteFileIfExists(
                    temporaryPath);

                throw;
            }
        }

        private static async Task MoveDownloadedFileAsync(
            string temporaryPath,
            string destinationPath,
            CancellationToken cancellationToken)
        {
            const int maximumAttempts = 10;

            Exception? lastException =
                null;

            for (int attempt = 1;
                 attempt <= maximumAttempts;
                 attempt++)
            {
                try
                {
                    File.Move(
                        temporaryPath,
                        destinationPath);

                    return;
                }
                catch (IOException ex)
                {
                    lastException = ex;
                }
                catch (UnauthorizedAccessException ex)
                {
                    lastException = ex;
                }

                if (attempt < maximumAttempts)
                {
                    await Task.Delay(
                        500,
                        cancellationToken);
                }
            }

            throw new IOException(
                $"Не удалось завершить сохранение файла обновления:\n" +
                $"{destinationPath}",
                lastException);
        }

        private static void CleanupOldUpdateFiles(
            string updateDirectory)
        {
            try
            {
                DateTime deleteBefore =
                    DateTime.Now.AddDays(-3);

                foreach (string file in
                         Directory.GetFiles(
                             updateDirectory,
                             "*.download"))
                {
                    TryDeleteOldFile(
                        file,
                        deleteBefore);
                }

                foreach (string file in
                         Directory.GetFiles(
                             updateDirectory,
                             "*.zip"))
                {
                    TryDeleteOldFile(
                        file,
                        deleteBefore);
                }
            }
            catch
            {
                // Ошибка очистки старых файлов
                // не должна блокировать скачивание.
            }
        }

        private static void TryDeleteOldFile(
            string filePath,
            DateTime deleteBefore)
        {
            try
            {
                DateTime lastWriteTime =
                    File.GetLastWriteTime(
                        filePath);

                if (lastWriteTime <
                    deleteBefore)
                {
                    File.Delete(
                        filePath);
                }
            }
            catch
            {
                // Файл может использоваться Updater.
                // В этом случае оставляем его до следующей очистки.
            }
        }

        private static UpdateInfo? ParseRelease(
            JsonElement release)
        {
            bool isDraft =
                release.TryGetProperty(
                    "draft",
                    out JsonElement draftElement) &&
                draftElement.ValueKind ==
                JsonValueKind.True;

            if (isDraft)
            {
                return null;
            }

            bool isPrerelease =
                release.TryGetProperty(
                    "prerelease",
                    out JsonElement prereleaseElement) &&
                prereleaseElement.ValueKind ==
                JsonValueKind.True;

            if (isPrerelease)
            {
                return null;
            }

            string version =
                GetStringProperty(
                    release,
                    "tag_name");

            if (string.IsNullOrWhiteSpace(
                    version))
            {
                return null;
            }

            string notes =
                GetStringProperty(
                    release,
                    "body");

            string downloadUrl =
                FindAssetDownloadUrl(
                    release);

            if (string.IsNullOrWhiteSpace(
                    downloadUrl))
            {
                return null;
            }

            return new UpdateInfo
            {
                Version = version,
                Notes = notes,
                DownloadUrl = downloadUrl,
                HasUpdate = false
            };
        }

        private static string FindAssetDownloadUrl(
            JsonElement release)
        {
            if (!release.TryGetProperty(
                    "assets",
                    out JsonElement assetsElement))
            {
                return string.Empty;
            }

            if (assetsElement.ValueKind !=
                JsonValueKind.Array)
            {
                return string.Empty;
            }

            foreach (JsonElement asset in
                     assetsElement.EnumerateArray())
            {
                string assetFileName =
                    GetStringProperty(
                        asset,
                        "name");

                if (!string.Equals(
                        assetFileName,
                        AssetName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return GetStringProperty(
                    asset,
                    "browser_download_url");
            }

            return string.Empty;
        }

        private static string GetStringProperty(
            JsonElement element,
            string propertyName)
        {
            if (!element.TryGetProperty(
                    propertyName,
                    out JsonElement property))
            {
                return string.Empty;
            }

            if (property.ValueKind ==
                JsonValueKind.Null)
            {
                return string.Empty;
            }

            return property.GetString()
                   ?? string.Empty;
        }

        private static async Task EnsureSuccessAsync(
            HttpResponseMessage response,
            string errorMessage,
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string responseText =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(
                    responseText))
            {
                responseText =
                    response.ReasonPhrase
                    ?? "Неизвестная ошибка GitHub.";
            }

            throw new HttpRequestException(
                $"{errorMessage}. " +
                $"HTTP {(int)response.StatusCode} " +
                $"{response.StatusCode}.\n\n" +
                responseText);
        }

        private static bool IsNewerVersion(
            string remoteVersion,
            string currentVersion)
        {
            string remote =
                NormalizeVersion(
                    remoteVersion);

            string current =
                NormalizeVersion(
                    currentVersion);

            if (!Version.TryParse(
                    remote,
                    out Version? remoteParsed))
            {
                return false;
            }

            if (!Version.TryParse(
                    current,
                    out Version? currentParsed))
            {
                return false;
            }

            return remoteParsed >
                   currentParsed;
        }

        private static int CompareVersions(
            string firstVersion,
            string secondVersion)
        {
            string first =
                NormalizeVersion(
                    firstVersion);

            string second =
                NormalizeVersion(
                    secondVersion);

            bool firstParsedSuccessfully =
                Version.TryParse(
                    first,
                    out Version? firstParsed);

            bool secondParsedSuccessfully =
                Version.TryParse(
                    second,
                    out Version? secondParsed);

            if (firstParsedSuccessfully &&
                secondParsedSuccessfully)
            {
                return firstParsed!
                    .CompareTo(
                        secondParsed);
            }

            return string.Compare(
                first,
                second,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeVersion(
            string version)
        {
            if (string.IsNullOrWhiteSpace(
                    version))
            {
                return "0.0.0";
            }

            string normalized =
                version.Trim();

            if (normalized.StartsWith(
                    "v",
                    StringComparison.OrdinalIgnoreCase))
            {
                normalized =
                    normalized.Substring(1);
            }

            int separatorIndex =
                normalized.IndexOfAny(
                    new[]
                    {
                        '-',
                        '+'
                    });

            if (separatorIndex >= 0)
            {
                normalized =
                    normalized.Substring(
                        0,
                        separatorIndex);
            }

            return normalized;
        }

        private static void DeleteFileIfExists(
            string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }
}