using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CarShell.Services
{
    public sealed class WifiNetworkInfo
    {
        public string Ssid { get; set; } = string.Empty;

        public int Signal { get; set; }

        public bool IsSecured { get; set; } = true;

        public bool IsConnected { get; set; }
    }

    public static class WifiService
    {
        // =========================================================
        // СОСТОЯНИЕ АДАПТЕРА
        // =========================================================

        public static async Task<bool> IsWifiEnabledAsync()
        {
            WifiAdapterInfo? adapter =
                await GetWifiAdapterAsync();

            if (adapter == null)
            {
                return false;
            }

            return adapter.Status.Equals(
                       "Up",
                       StringComparison.OrdinalIgnoreCase) ||
                   adapter.Status.Equals(
                       "Connected",
                       StringComparison.OrdinalIgnoreCase) ||
                   adapter.Status.Equals(
                       "Disconnected",
                       StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<bool> SetWifiEnabledAsync(
            bool enabled)
        {
            WifiAdapterInfo? adapter =
                await GetWifiAdapterAsync();

            if (adapter == null)
            {
                return false;
            }

            string interfaceName =
                EscapePowerShellString(adapter.Name);

            string command = enabled
                ? $"Enable-NetAdapter -Name '{interfaceName}' -Confirm:$false -ErrorAction Stop"
                : $"Disable-NetAdapter -Name '{interfaceName}' -Confirm:$false -ErrorAction Stop";

            CommandResult result =
                await RunPowerShellAsync(command);

            if (result.ExitCode != 0)
            {
                Debug.WriteLine(
                    $"Ошибка изменения Wi-Fi: {result.Output}");

                return false;
            }

            await Task.Delay(1000);

            WifiAdapterInfo? updatedAdapter =
                await GetWifiAdapterAsync();

            if (updatedAdapter == null)
            {
                return false;
            }

            if (enabled)
            {
                return !updatedAdapter.Status.Equals(
                    "Disabled",
                    StringComparison.OrdinalIgnoreCase);
            }

            return updatedAdapter.Status.Equals(
                       "Disabled",
                       StringComparison.OrdinalIgnoreCase) ||
                   updatedAdapter.Status.Equals(
                       "Not Present",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<WifiAdapterInfo?>
            GetWifiAdapterAsync()
        {
            /*
             * Сначала ищем физический адаптер по его описанию.
             * Это не зависит от языка имени интерфейса:
             *
             * Беспроводная сеть
             * Wi-Fi
             * WLAN
             * Sieć bezprzewodowa
             */

            const string command = """
                $adapter = Get-NetAdapter -IncludeHidden |
                    Where-Object {
                        $_.HardwareInterface -eq $true -and
                        (
                            $_.InterfaceDescription -match 'Wireless|Wi-Fi|WLAN|802\.11|AX[0-9]+|AC[0-9]+' -or
                            $_.Name -match 'Wi-Fi|WLAN|Wireless|Беспровод|Bezprzewod'
                        )
                    } |
                    Select-Object -First 1;

                if ($null -ne $adapter) {
                    Write-Output ($adapter.Name + '|' + $adapter.Status)
                }
                """;

            CommandResult result =
                await RunPowerShellAsync(command);

            if (result.ExitCode != 0)
            {
                return null;
            }

            string? line = result.Output
                .Split(
                    new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .FirstOrDefault(value =>
                    value.Contains('|'));

            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            string[] parts =
                line.Split(
                    '|',
                    2,
                    StringSplitOptions.TrimEntries);

            if (parts.Length != 2 ||
                string.IsNullOrWhiteSpace(parts[0]))
            {
                return null;
            }

            return new WifiAdapterInfo
            {
                Name = parts[0],
                Status = parts[1]
            };
        }

        // =========================================================
        // ТЕКУЩЕЕ ПОДКЛЮЧЕНИЕ
        // =========================================================

        public static async Task<string?>
            GetConnectedNetworkAsync()
        {
            CommandResult result =
                await RunNetshAsync(
                    "wlan show interfaces");

            if (result.ExitCode != 0)
            {
                return null;
            }

            string[] lines =
                result.Output.Split(
                    new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string trimmed =
                    line.Trim();

                if (trimmed.StartsWith(
                        "BSSID",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!trimmed.StartsWith(
                        "SSID",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int separatorIndex =
                    trimmed.IndexOf(':');

                if (separatorIndex < 0)
                {
                    continue;
                }

                string ssid =
                    trimmed
                        .Substring(separatorIndex + 1)
                        .Trim();

                if (!string.IsNullOrWhiteSpace(ssid))
                {
                    return ssid;
                }
            }

            return null;
        }

        // =========================================================
        // ДОСТУПНЫЕ СЕТИ
        // =========================================================

        public static async Task<IReadOnlyList<WifiNetworkInfo>>
            GetAvailableNetworksAsync()
        {
            CommandResult result =
                await RunNetshAsync(
                    "wlan show networks mode=bssid");

            if (result.ExitCode != 0)
            {
                return Array.Empty<WifiNetworkInfo>();
            }

            string? connectedSsid =
                await GetConnectedNetworkAsync();

            var networks =
                new Dictionary<string, WifiNetworkInfo>(
                    StringComparer.OrdinalIgnoreCase);

            WifiNetworkInfo? currentNetwork =
                null;

            string[] lines =
                result.Output.Split(
                    new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);

            var ssidRegex = new Regex(
                @"^\s*SSID\s+\d+\s*:\s*(.*)$",
                RegexOptions.IgnoreCase);

            var signalRegex = new Regex(
                @"(\d{1,3})\s*%");

            foreach (string line in lines)
            {
                Match ssidMatch =
                    ssidRegex.Match(line);

                if (ssidMatch.Success)
                {
                    string ssid =
                        ssidMatch.Groups[1]
                            .Value
                            .Trim();

                    if (string.IsNullOrWhiteSpace(ssid))
                    {
                        currentNetwork = null;
                        continue;
                    }

                    if (!networks.TryGetValue(
                            ssid,
                            out currentNetwork))
                    {
                        currentNetwork =
                            new WifiNetworkInfo
                            {
                                Ssid = ssid,

                                IsConnected =
                                    string.Equals(
                                        ssid,
                                        connectedSsid,
                                        StringComparison.OrdinalIgnoreCase)
                            };

                        networks.Add(
                            ssid,
                            currentNetwork);
                    }

                    continue;
                }

                if (currentNetwork == null)
                {
                    continue;
                }

                Match signalMatch =
                    signalRegex.Match(line);

                if (signalMatch.Success &&
                    int.TryParse(
                        signalMatch.Groups[1].Value,
                        out int signal))
                {
                    currentNetwork.Signal =
                        Math.Max(
                            currentNetwork.Signal,
                            signal);
                }

                string normalized =
                    line.Trim().ToLowerInvariant();

                if (normalized.Contains("authentication") ||
                    normalized.Contains("проверка подлинности") ||
                    normalized.Contains("аутентификация") ||
                    normalized.Contains("uwierzytelnianie"))
                {
                    currentNetwork.IsSecured =
                        !normalized.Contains("open") &&
                        !normalized.Contains("открыт") &&
                        !normalized.Contains("otwarta");
                }
            }

            return networks.Values
                .OrderByDescending(
                    network => network.IsConnected)
                .ThenByDescending(
                    network => network.Signal)
                .ToList();
        }

        // =========================================================
        // СОХРАНЁННЫЕ ПРОФИЛИ
        // =========================================================

        public static async Task<bool>
            HasSavedProfileAsync(
                string ssid)
        {
            CommandResult result =
                await RunNetshAsync(
                    $"wlan show profile name=\"{EscapeCommandArgument(ssid)}\"");

            return result.ExitCode == 0;
        }

        public static async Task<bool>
            ConnectSavedProfileAsync(
                string ssid)
        {
            CommandResult result =
                await RunNetshAsync(
                    $"wlan connect name=\"{EscapeCommandArgument(ssid)}\" " +
                    $"ssid=\"{EscapeCommandArgument(ssid)}\"");

            return result.ExitCode == 0;
        }

        // =========================================================
        // ПОДКЛЮЧЕНИЕ С ПАРОЛЕМ
        // =========================================================

        public static async Task<bool>
            ConnectWithPasswordAsync(
                string ssid,
                string password)
        {
            if (string.IsNullOrWhiteSpace(ssid) ||
                string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            string profileXml =
                CreateSecuredProfileXml(
                    ssid,
                    password);

            return await AddProfileAndConnectAsync(
                ssid,
                profileXml);
        }

        public static async Task<bool>
            ConnectOpenNetworkAsync(
                string ssid)
        {
            if (string.IsNullOrWhiteSpace(ssid))
            {
                return false;
            }

            string profileXml =
                CreateOpenProfileXml(ssid);

            return await AddProfileAndConnectAsync(
                ssid,
                profileXml);
        }

        public static async Task<bool>
            DisconnectAsync()
        {
            CommandResult result =
                await RunNetshAsync(
                    "wlan disconnect");

            return result.ExitCode == 0;
        }

        private static async Task<bool>
            AddProfileAndConnectAsync(
                string ssid,
                string profileXml)
        {
            string tempFile =
                Path.Combine(
                    Path.GetTempPath(),
                    $"CarShell-WiFi-{Guid.NewGuid():N}.xml");

            try
            {
                await File.WriteAllTextAsync(
                    tempFile,
                    profileXml,
                    new UTF8Encoding(false));

                CommandResult addResult =
                    await RunNetshAsync(
                        $"wlan add profile filename=\"{tempFile}\" user=current");

                if (addResult.ExitCode != 0)
                {
                    Debug.WriteLine(
                        $"Ошибка добавления профиля: {addResult.Output}");

                    return false;
                }

                await Task.Delay(400);

                return await ConnectSavedProfileAsync(
                    ssid);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch
                {
                    // Не блокируем работу из-за временного файла.
                }
            }
        }

        // =========================================================
        // XML-ПРОФИЛИ
        // =========================================================

        private static string CreateSecuredProfileXml(
            string ssid,
            string password)
        {
            string safeSsid =
                WebUtility.HtmlEncode(ssid);

            string safePassword =
                WebUtility.HtmlEncode(password);

            return $"""
                    <?xml version="1.0"?>
                    <WLANProfile xmlns="http://www.microsoft.com/networking/WLAN/profile/v1">
                      <name>{safeSsid}</name>
                      <SSIDConfig>
                        <SSID>
                          <name>{safeSsid}</name>
                        </SSID>
                      </SSIDConfig>
                      <connectionType>ESS</connectionType>
                      <connectionMode>auto</connectionMode>
                      <MSM>
                        <security>
                          <authEncryption>
                            <authentication>WPA2PSK</authentication>
                            <encryption>AES</encryption>
                            <useOneX>false</useOneX>
                          </authEncryption>
                          <sharedKey>
                            <keyType>passPhrase</keyType>
                            <protected>false</protected>
                            <keyMaterial>{safePassword}</keyMaterial>
                          </sharedKey>
                        </security>
                      </MSM>
                    </WLANProfile>
                    """;
        }

        private static string CreateOpenProfileXml(
            string ssid)
        {
            string safeSsid =
                WebUtility.HtmlEncode(ssid);

            return $"""
                    <?xml version="1.0"?>
                    <WLANProfile xmlns="http://www.microsoft.com/networking/WLAN/profile/v1">
                      <name>{safeSsid}</name>
                      <SSIDConfig>
                        <SSID>
                          <name>{safeSsid}</name>
                        </SSID>
                      </SSIDConfig>
                      <connectionType>ESS</connectionType>
                      <connectionMode>auto</connectionMode>
                      <MSM>
                        <security>
                          <authEncryption>
                            <authentication>open</authentication>
                            <encryption>none</encryption>
                            <useOneX>false</useOneX>
                          </authEncryption>
                        </security>
                      </MSM>
                    </WLANProfile>
                    """;
        }

        // =========================================================
        // ЗАПУСК КОМАНД
        // =========================================================

        private static async Task<CommandResult>
            RunNetshAsync(
                string arguments)
        {
            return await RunProcessAsync(
                "netsh.exe",
                arguments);
        }

        private static async Task<CommandResult>
            RunPowerShellAsync(
                string command)
        {
            string encodedCommand =
                Convert.ToBase64String(
                    Encoding.Unicode.GetBytes(
                        """
                        [Console]::OutputEncoding =
                            [System.Text.Encoding]::UTF8;
                        $OutputEncoding =
                            [System.Text.Encoding]::UTF8;
                        """ +
                        Environment.NewLine +
                        command));

            return await RunProcessAsync(
                "powershell.exe",
                $"-NoProfile -NonInteractive " +
                $"-ExecutionPolicy Bypass " +
                $"-EncodedCommand {encodedCommand}");
        }

        private static async Task<CommandResult>
            RunProcessAsync(
                string fileName,
                string arguments)
        {
            var startInfo =
                new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

            using var process =
                new Process
                {
                    StartInfo = startInfo
                };

            process.Start();

            Task<string> outputTask =
                process.StandardOutput.ReadToEndAsync();

            Task<string> errorTask =
                process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            string output =
                await outputTask;

            string error =
                await errorTask;

            string combinedOutput =
                string.IsNullOrWhiteSpace(error)
                    ? output
                    : output +
                      Environment.NewLine +
                      error;

            return new CommandResult(
                process.ExitCode,
                combinedOutput);
        }

        private static string EscapePowerShellString(
            string value)
        {
            return value.Replace(
                "'",
                "''");
        }

        private static string EscapeCommandArgument(
            string value)
        {
            return value.Replace(
                "\"",
                "\\\"");
        }

        private sealed class WifiAdapterInfo
        {
            public string Name { get; set; } =
                string.Empty;

            public string Status { get; set; } =
                string.Empty;
        }

        private readonly record struct CommandResult(
            int ExitCode,
            string Output);
    }
}