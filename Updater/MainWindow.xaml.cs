using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Updater
{
    public partial class MainWindow : Window
    {
        private const string MainApplicationFileName =
            "CarShell.exe";

        private const string UpdaterBaseName =
            "Updater";

        private string appDirectory =
            string.Empty;

        private string zipPath =
            string.Empty;

        private static readonly string UpdateLockDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData),
                "CarShell");

        private static readonly string UpdateLockPath =
            Path.Combine(
                UpdateLockDirectory,
                "update.lock");

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                string[] args =
                    Environment.GetCommandLineArgs();

                if (args.Length < 3)
                {
                    SetStatus(
                        "Ошибка: отсутствуют параметры обновления.");

                    MessageBox.Show(
                        "Updater не получил путь к CarShell и ZIP-файлу.",
                        "CarShell Updater",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                appDirectory =
                    Path.GetFullPath(args[1]);

                zipPath =
                    Path.GetFullPath(args[2]);

                await RunUpdateAsync();
            }
            catch (Exception ex)
            {
                await HandleUpdateErrorAsync(ex);
            }
        }

        private async Task RunUpdateAsync()
        {
            ValidateArguments();

            CreateUpdateLock();

            SetStatus(
                "Закрытие CarShell...");

            await WaitForCarShellToCloseAsync();

            string extractionDirectory =
                Path.Combine(
                    Path.GetTempPath(),
                    "CarShell",
                    "ExtractedUpdate_" +
                    Guid.NewGuid().ToString("N"));

            string pendingUpdaterDirectory =
                Path.Combine(
                    Path.GetTempPath(),
                    "CarShell",
                    "PendingUpdater_" +
                    Guid.NewGuid().ToString("N"));

            bool helperStarted = false;

            try
            {
                Directory.CreateDirectory(
                    extractionDirectory);

                Directory.CreateDirectory(
                    pendingUpdaterDirectory);

                SetStatus(
                    "Распаковка обновления...");

                await Task.Run(
                    () =>
                    {
                        ZipFile.ExtractToDirectory(
                            zipPath,
                            extractionDirectory,
                            overwriteFiles: true);
                    });

                string payloadDirectory =
                    ResolvePayloadDirectory(
                        extractionDirectory);

                string expectedApplicationPath =
                    Path.Combine(
                        payloadDirectory,
                        MainApplicationFileName);

                if (!File.Exists(
                        expectedApplicationPath))
                {
                    throw new FileNotFoundException(
                        $"В архиве не найден {MainApplicationFileName}.",
                        expectedApplicationPath);
                }

                SetStatus(
                    "Установка файлов CarShell...");

                await CopyUpdateFilesAsync(
                    payloadDirectory,
                    appDirectory,
                    pendingUpdaterDirectory);

                bool hasUpdaterUpdate =
                    Directory
                        .EnumerateFiles(
                            pendingUpdaterDirectory,
                            "*",
                            SearchOption.AllDirectories)
                        .Any();

                if (hasUpdaterUpdate)
                {
                    SetStatus(
                        "Подготовка обновления Updater...");
                }
                else
                {
                    SetStatus(
                        "Завершение обновления...");
                }

                string helperPath =
                    CreateCompletionScript(
                        pendingUpdaterDirectory,
                        hasUpdaterUpdate);

                StartCompletionScript(
                    helperPath);

                helperStarted = true;

                SetStatus(
                    hasUpdaterUpdate
                        ? "Updater будет заменён после закрытия."
                        : "Обновление установлено.");

                await Task.Delay(500);

                Application.Current.Shutdown();
            }
            finally
            {
                TryDeleteDirectory(
                    extractionDirectory);

                // Если скрипт не был запущен, временная папка
                // с новой версией Updater больше не нужна.
                if (!helperStarted)
                {
                    TryDeleteDirectory(
                        pendingUpdaterDirectory);
                }
            }
        }

        private void ValidateArguments()
        {
            if (string.IsNullOrWhiteSpace(
                    appDirectory))
            {
                throw new InvalidOperationException(
                    "Не указан путь к папке CarShell.");
            }

            if (!Directory.Exists(
                    appDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"Папка CarShell не найдена:\n{appDirectory}");
            }

            if (string.IsNullOrWhiteSpace(
                    zipPath))
            {
                throw new InvalidOperationException(
                    "Не указан путь к ZIP-файлу.");
            }

            if (!File.Exists(zipPath))
            {
                throw new FileNotFoundException(
                    "ZIP-файл обновления не найден.",
                    zipPath);
            }
        }

        private static void CreateUpdateLock()
        {
            Directory.CreateDirectory(
                UpdateLockDirectory);

            File.WriteAllText(
                UpdateLockPath,
                DateTime.UtcNow.ToString(
                    "O",
                    CultureInfo.InvariantCulture));
        }

        private static void DeleteUpdateLock()
        {
            try
            {
                if (File.Exists(UpdateLockPath))
                {
                    File.Delete(UpdateLockPath);
                }
            }
            catch
            {
            }
        }

        private static async Task WaitForCarShellToCloseAsync()
        {
            const int maximumAttempts = 40;

            for (int attempt = 0;
                 attempt < maximumAttempts;
                 attempt++)
            {
                Process[] processes =
                    Process.GetProcessesByName(
                        "CarShell");

                if (processes.Length == 0)
                {
                    await Task.Delay(700);
                    return;
                }

                foreach (Process process in processes)
                {
                    try
                    {
                        if (process.HasExited)
                        {
                            continue;
                        }

                        try
                        {
                            process.CloseMainWindow();
                        }
                        catch
                        {
                        }

                        await Task.Delay(250);

                        if (!process.HasExited)
                        {
                            process.Kill(
                                entireProcessTree: true);
                        }

                        await process.WaitForExitAsync();
                    }
                    catch
                    {
                        // Процесс мог завершиться между проверками.
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                await Task.Delay(500);
            }

            throw new InvalidOperationException(
                "Не удалось закрыть CarShell перед обновлением.");
        }

        private static string ResolvePayloadDirectory(
            string extractionDirectory)
        {
            string applicationInRoot =
                Path.Combine(
                    extractionDirectory,
                    MainApplicationFileName);

            if (File.Exists(applicationInRoot))
            {
                return extractionDirectory;
            }

            string[] directories =
                Directory.GetDirectories(
                    extractionDirectory);

            string[] rootFiles =
                Directory.GetFiles(
                    extractionDirectory);

            if (directories.Length == 1 &&
                rootFiles.Length == 0)
            {
                string nestedApplication =
                    Path.Combine(
                        directories[0],
                        MainApplicationFileName);

                if (File.Exists(nestedApplication))
                {
                    return directories[0];
                }
            }

            string? foundApplication =
                Directory
                    .EnumerateFiles(
                        extractionDirectory,
                        MainApplicationFileName,
                        SearchOption.AllDirectories)
                    .FirstOrDefault();

            if (foundApplication == null)
            {
                return extractionDirectory;
            }

            return Path.GetDirectoryName(
                       foundApplication)
                   ?? extractionDirectory;
        }

        private static async Task CopyUpdateFilesAsync(
            string sourceDirectory,
            string destinationDirectory,
            string pendingUpdaterDirectory)
        {
            await Task.Run(
                () =>
                {
                    CopyDirectoryRecursive(
                        sourceDirectory,
                        destinationDirectory,
                        pendingUpdaterDirectory,
                        string.Empty);
                });
        }

        private static void CopyDirectoryRecursive(
            string sourceDirectory,
            string destinationDirectory,
            string pendingUpdaterDirectory,
            string relativePath)
        {
            Directory.CreateDirectory(
                destinationDirectory);

            foreach (string sourceFile in
                     Directory.GetFiles(sourceDirectory))
            {
                string fileName =
                    Path.GetFileName(sourceFile);

                if (fileName.EndsWith(
                        ".pdb",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Файлы Updater нельзя перезаписать,
                // пока текущий Updater работает.
                if (string.IsNullOrWhiteSpace(relativePath) &&
                    IsUpdaterFile(fileName))
                {
                    string pendingFile =
                        Path.Combine(
                            pendingUpdaterDirectory,
                            fileName);

                    File.Copy(
                        sourceFile,
                        pendingFile,
                        overwrite: true);

                    continue;
                }

                string destinationFile =
                    Path.Combine(
                        destinationDirectory,
                        fileName);

                CopyFileWithRetries(
                    sourceFile,
                    destinationFile);
            }

            foreach (string sourceSubdirectory in
                     Directory.GetDirectories(sourceDirectory))
            {
                string directoryName =
                    Path.GetFileName(
                        sourceSubdirectory);

                string childRelativePath =
                    string.IsNullOrWhiteSpace(relativePath)
                        ? directoryName
                        : Path.Combine(
                            relativePath,
                            directoryName);

                if (ShouldSkipDirectory(
                        childRelativePath))
                {
                    continue;
                }

                string destinationSubdirectory =
                    Path.Combine(
                        destinationDirectory,
                        directoryName);

                CopyDirectoryRecursive(
                    sourceSubdirectory,
                    destinationSubdirectory,
                    pendingUpdaterDirectory,
                    childRelativePath);
            }
        }

        private static bool IsUpdaterFile(
            string fileName)
        {
            if (string.Equals(
                    fileName,
                    "Updater.exe",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(
                    fileName,
                    "Updater.dll",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(
                    fileName,
                    "Updater.deps.json",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(
                    fileName,
                    "Updater.runtimeconfig.json",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(
                    fileName,
                    "Updater.exe.config",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return fileName.StartsWith(
                UpdaterBaseName + ".",
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldSkipDirectory(
            string relativePath)
        {
            string normalized =
                relativePath.Replace(
                    Path.AltDirectorySeparatorChar,
                    Path.DirectorySeparatorChar);

            string firstDirectory =
                normalized.Split(
                        Path.DirectorySeparatorChar,
                        StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault()
                ?? string.Empty;

            // Сохраняем локальные офлайн-карты.
            if (string.Equals(
                    firstDirectory,
                    "Maps",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(
                    firstDirectory,
                    "bin",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    firstDirectory,
                    "obj",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    firstDirectory,
                    ".git",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static void CopyFileWithRetries(
            string sourceFile,
            string destinationFile)
        {
            const int maximumAttempts = 10;

            Exception? lastException = null;

            for (int attempt = 1;
                 attempt <= maximumAttempts;
                 attempt++)
            {
                try
                {
                    string? destinationDirectory =
                        Path.GetDirectoryName(
                            destinationFile);

                    if (!string.IsNullOrWhiteSpace(
                            destinationDirectory))
                    {
                        Directory.CreateDirectory(
                            destinationDirectory);
                    }

                    File.Copy(
                        sourceFile,
                        destinationFile,
                        overwrite: true);

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

                Task.Delay(500)
                    .GetAwaiter()
                    .GetResult();
            }

            throw new IOException(
                $"Не удалось заменить файл:\n{destinationFile}",
                lastException);
        }

        private string CreateCompletionScript(
            string pendingUpdaterDirectory,
            bool hasUpdaterUpdate)
        {
            int updaterProcessId =
                Environment.ProcessId;

            string scriptDirectory =
                Path.Combine(
                    Path.GetTempPath(),
                    "CarShell",
                    "UpdateCompletion_" +
                    Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(
                scriptDirectory);

            string scriptPath =
                Path.Combine(
                    scriptDirectory,
                    "complete-update.cmd");

            string carShellPath =
                Path.Combine(
                    appDirectory,
                    MainApplicationFileName);

            var script =
                new StringBuilder();

            script.AppendLine("@echo off");
            script.AppendLine("setlocal");
            script.AppendLine();

            // Ждём закрытия текущего Updater.
            script.AppendLine(":waitUpdater");
            script.AppendLine(
                $"tasklist /FI \"PID eq {updaterProcessId}\" " +
                $"2>NUL | find \"{updaterProcessId}\" >NUL");

            script.AppendLine(
                "if not errorlevel 1 (");

            script.AppendLine(
                "    timeout /t 1 /nobreak >NUL");

            script.AppendLine(
                "    goto waitUpdater");

            script.AppendLine(")");
            script.AppendLine();

            if (hasUpdaterUpdate)
            {
                script.AppendLine(
                    "timeout /t 1 /nobreak >NUL");

                script.AppendLine();

                // Копируем новую версию Updater после закрытия старой.
                script.AppendLine(
                    $"copy /Y \"{pendingUpdaterDirectory}\\*\" " +
                    $"\"{appDirectory}\\\" >NUL");

                script.AppendLine();

                // Если копирование не удалось, повторяем ещё раз.
                script.AppendLine(
                    "if errorlevel 1 (");

                script.AppendLine(
                    "    timeout /t 2 /nobreak >NUL");

                script.AppendLine(
                    $"    copy /Y \"{pendingUpdaterDirectory}\\*\" " +
                    $"\"{appDirectory}\\\" >NUL");

                script.AppendLine(")");
                script.AppendLine();
            }

            // Снимаем блокировку только после замены Updater.
            script.AppendLine(
                $"if exist \"{UpdateLockPath}\" " +
                $"del /F /Q \"{UpdateLockPath}\"");

            script.AppendLine();

            script.AppendLine(
                $"start \"\" \"{carShellPath}\"");

            script.AppendLine();

            if (hasUpdaterUpdate)
            {
                script.AppendLine(
                    $"rmdir /S /Q \"{pendingUpdaterDirectory}\"");
            }

            script.AppendLine();

            // Скрипт удаляет собственную временную папку.
            script.AppendLine(
                $"cd /D \"{Path.GetTempPath()}\"");

            script.AppendLine(
                $"rmdir /S /Q \"{scriptDirectory}\"");

            File.WriteAllText(
                scriptPath,
                script.ToString(),
                Encoding.Default);

            return scriptPath;
        }

        private static void StartCompletionScript(
            string scriptPath)
        {
            Process? process =
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "cmd.exe",

                        Arguments =
                            $"/C \"\"{scriptPath}\"\"",

                        WorkingDirectory =
                            Path.GetDirectoryName(scriptPath)
                            ?? Path.GetTempPath(),

                        UseShellExecute = true,

                        CreateNoWindow = true,

                        WindowStyle =
                            ProcessWindowStyle.Hidden
                    });

            if (process == null)
            {
                throw new InvalidOperationException(
                    "Не удалось запустить завершение обновления.");
            }
        }

        private void StartCarShell()
        {
            string applicationPath =
                Path.Combine(
                    appDirectory,
                    MainApplicationFileName);

            if (!File.Exists(applicationPath))
            {
                throw new FileNotFoundException(
                    $"Не найден {MainApplicationFileName}.",
                    applicationPath);
            }

            Process? process =
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            applicationPath,

                        WorkingDirectory =
                            appDirectory,

                        UseShellExecute =
                            true
                    });

            if (process == null)
            {
                throw new InvalidOperationException(
                    "Не удалось запустить CarShell.");
            }
        }

        private async Task HandleUpdateErrorAsync(
            Exception exception)
        {
            SetStatus(
                "Ошибка обновления.");

            DeleteUpdateLock();

            MessageBox.Show(
                exception.Message,
                "Ошибка обновления CarShell",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            try
            {
                StartCarShell();
            }
            catch
            {
            }

            await Task.Delay(300);
        }

        private void SetStatus(
            string text)
        {
            StatusText.Text =
                text;
        }

        private static void TryDeleteDirectory(
            string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(
                        directory,
                        recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}