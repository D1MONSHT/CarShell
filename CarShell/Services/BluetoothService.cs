using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;

namespace CarShell.Services
{
    public sealed class BluetoothDeviceInfo
    {
        public string Id { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public bool IsPaired { get; init; }

        public bool IsConnected { get; init; }

        public bool IsLowEnergy { get; init; }

        public string DeviceType { get; init; } =
            "Устройство";

        public string Icon
        {
            get
            {
                return DeviceType switch
                {
                    "Телефон" => "📱",
                    "Наушники" => "🎧",
                    "Аудиоустройство" => "🔊",
                    "Компьютер" => "💻",
                    "Автомобиль" => "🚗",
                    "OBD-адаптер" => "🔌",
                    "Клавиатура" => "⌨",
                    "Мышь" => "🖱",
                    _ => "ᛒ"
                };
            }
        }

        public string StatusText
        {
            get
            {
                if (IsConnected)
                {
                    return "Подключено";
                }

                if (IsPaired)
                {
                    return "Сопряжено";
                }

                return "Доступно";
            }
        }
    }

    public sealed class BluetoothOperationResult
    {
        public bool Success { get; init; }

        public string Message { get; init; } =
            string.Empty;

        public static BluetoothOperationResult Ok(
            string message)
        {
            return new BluetoothOperationResult
            {
                Success = true,
                Message = message
            };
        }

        public static BluetoothOperationResult Error(
            string message)
        {
            return new BluetoothOperationResult
            {
                Success = false,
                Message = message
            };
        }
    }

    public static class BluetoothService
    {
        private const string BluetoothProtocolId =
            "{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}";

        private static readonly Dictionary<
            string,
            BluetoothLEDevice> connectedLeDevices =
                new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<
            string,
            GattSession> connectedGattSessions =
                new(StringComparer.OrdinalIgnoreCase);

        private static readonly object connectionLock =
            new();

        // =========================================================
        // ОСНОВНАЯ ИНФОРМАЦИЯ
        // =========================================================

        public static string GetComputerName()
        {
            return Environment.MachineName;
        }

        public static async Task<bool>
            IsBluetoothAvailableAsync()
        {
            Radio? radio =
                await GetBluetoothRadioAsync();

            return radio != null;
        }

        public static async Task<bool>
            IsBluetoothEnabledAsync()
        {
            Radio? radio =
                await GetBluetoothRadioAsync();

            return radio?.State ==
                   RadioState.On;
        }

        public static async Task<bool>
            SetBluetoothEnabledAsync(
                bool enabled)
        {
            RadioAccessStatus access =
                await Radio.RequestAccessAsync();

            if (access !=
                RadioAccessStatus.Allowed)
            {
                return false;
            }

            Radio? radio =
                await GetBluetoothRadioAsync();

            if (radio == null)
            {
                return false;
            }

            RadioAccessStatus result =
                await radio.SetStateAsync(
                    enabled
                        ? RadioState.On
                        : RadioState.Off);

            return result ==
                   RadioAccessStatus.Allowed;
        }

        // =========================================================
        // БЫСТРЫЙ ПОИСК DEVICE WATCHER
        // =========================================================

        public static async Task<
            IReadOnlyList<BluetoothDeviceInfo>>
            ScanDevicesAsync(
                TimeSpan timeout,
                Action<BluetoothDeviceInfo>?
                    onDeviceFound = null)
        {
            string selector =
                $"System.Devices.Aep.ProtocolId:=\"{BluetoothProtocolId}\"";

            string[] properties =
            {
                "System.Devices.Aep.IsPaired",
                "System.Devices.Aep.IsConnected",
                "System.Devices.Aep.Bluetooth.Le.IsConnectable",
                "System.Devices.Aep.DeviceAddress",
                "System.Devices.Icon"
            };

            var devices =
                new Dictionary<
                    string,
                    BluetoothDeviceInfo>(
                    StringComparer.OrdinalIgnoreCase);

            DeviceWatcher watcher =
                DeviceInformation.CreateWatcher(
                    selector,
                    properties,
                    DeviceInformationKind
                        .AssociationEndpoint);

            var enumerationCompleted =
                new TaskCompletionSource<bool>(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

            watcher.Added +=
                (
                    DeviceWatcher sender,
                    DeviceInformation device) =>
                {
                    if (string.IsNullOrWhiteSpace(
                            device.Name))
                    {
                        return;
                    }

                    BluetoothDeviceInfo info =
                        CreateBluetoothDeviceInfo(
                            device);

                    lock (devices)
                    {
                        devices[info.Id] = info;
                    }

                    onDeviceFound?.Invoke(info);
                };

            watcher.Updated +=
                (
                    DeviceWatcher sender,
                    DeviceInformationUpdate update) =>
                {
                    BluetoothDeviceInfo? updatedInfo =
                        null;

                    lock (devices)
                    {
                        if (!devices.TryGetValue(
                                update.Id,
                                out BluetoothDeviceInfo?
                                    existing))
                        {
                            return;
                        }

                        bool paired =
                            GetBooleanProperty(
                                update.Properties,
                                "System.Devices.Aep.IsPaired",
                                existing.IsPaired);

                        bool connected =
                            GetBooleanProperty(
                                update.Properties,
                                "System.Devices.Aep.IsConnected",
                                existing.IsConnected);

                        updatedInfo =
                            new BluetoothDeviceInfo
                            {
                                Id = existing.Id,
                                Name = existing.Name,
                                IsPaired = paired,
                                IsConnected =
                                    connected ||
                                    IsLocallyConnected(
                                        existing.Id),
                                IsLowEnergy =
                                    existing.IsLowEnergy,
                                DeviceType =
                                    existing.DeviceType
                            };

                        devices[update.Id] =
                            updatedInfo;
                    }

                    if (updatedInfo != null)
                    {
                        onDeviceFound?.Invoke(
                            updatedInfo);
                    }
                };

            watcher.EnumerationCompleted +=
                (sender, args) =>
                {
                    enumerationCompleted
                        .TrySetResult(true);
                };

            watcher.Stopped +=
                (sender, args) =>
                {
                    enumerationCompleted
                        .TrySetResult(true);
                };

            watcher.Start();

            try
            {
                await Task.WhenAny(
                    enumerationCompleted.Task,
                    Task.Delay(timeout));
            }
            finally
            {
                if (watcher.Status is
                    DeviceWatcherStatus.Started or
                    DeviceWatcherStatus
                        .EnumerationCompleted)
                {
                    watcher.Stop();
                }
            }

            lock (devices)
            {
                return devices.Values
                    .Select(device =>
                    {
                        if (!IsLocallyConnected(
                                device.Id))
                        {
                            return device;
                        }

                        return new BluetoothDeviceInfo
                        {
                            Id = device.Id,
                            Name = device.Name,
                            IsPaired =
                                device.IsPaired,
                            IsConnected = true,
                            IsLowEnergy =
                                device.IsLowEnergy,
                            DeviceType =
                                device.DeviceType
                        };
                    })
                    .OrderByDescending(
                        device =>
                            device.IsConnected)
                    .ThenByDescending(
                        device =>
                            device.IsPaired)
                    .ThenBy(
                        device =>
                            device.Name)
                    .ToList();
            }
        }

        // =========================================================
        // СОПРЯЖЕНИЕ
        // =========================================================

        public static async Task<
            BluetoothOperationResult>
            PairAsync(
                string deviceId)
        {
            DeviceInformation? device =
                await DeviceInformation
                    .CreateFromIdAsync(
                        deviceId);

            if (device == null)
            {
                return BluetoothOperationResult
                    .Error(
                        "Устройство больше недоступно.");
            }

            if (device.Pairing.IsPaired)
            {
                return BluetoothOperationResult
                    .Ok(
                        "Устройство уже сопряжено.");
            }

            DevicePairingResult result =
                await device.Pairing.PairAsync(
                    DevicePairingProtectionLevel
                        .Default);

            return result.Status switch
            {
                DevicePairingResultStatus.Paired =>
                    BluetoothOperationResult.Ok(
                        "Сопряжение выполнено."),

                DevicePairingResultStatus
                    .AlreadyPaired =>
                    BluetoothOperationResult.Ok(
                        "Устройство уже сопряжено."),

                DevicePairingResultStatus
                    .NotReadyToPair =>
                    BluetoothOperationResult.Error(
                        "Устройство не готово к сопряжению. Включите режим обнаружения."),

                DevicePairingResultStatus
                    .AuthenticationFailure =>
                    BluetoothOperationResult.Error(
                        "Ошибка подтверждения PIN-кода."),

                DevicePairingResultStatus
                    .AuthenticationTimeout =>
                    BluetoothOperationResult.Error(
                        "Истекло время подтверждения сопряжения."),

                DevicePairingResultStatus
                    .ConnectionRejected =>
                    BluetoothOperationResult.Error(
                        "Устройство отклонило подключение."),

                DevicePairingResultStatus
                    .Failed =>
                    BluetoothOperationResult.Error(
                        "Windows не удалось выполнить сопряжение."),

                _ =>
                    BluetoothOperationResult.Error(
                        $"Сопряжение не выполнено: {result.Status}.")
            };
        }

        public static async Task<
            BluetoothOperationResult>
            UnpairAsync(
                string deviceId)
        {
            DeviceInformation? device =
                await DeviceInformation
                    .CreateFromIdAsync(
                        deviceId);

            if (device == null)
            {
                return BluetoothOperationResult
                    .Error(
                        "Устройство не найдено.");
            }

            if (!device.Pairing.IsPaired)
            {
                return BluetoothOperationResult
                    .Ok(
                        "Сопряжение уже удалено.");
            }

            await DisconnectAsync(
                new BluetoothDeviceInfo
                {
                    Id = deviceId,
                    Name = device.Name,
                    IsPaired = true,
                    IsLowEnergy =
                        IsBluetoothLeId(deviceId)
                });

            DeviceUnpairingResult result =
                await device.Pairing
                    .UnpairAsync();

            if (result.Status ==
                DeviceUnpairingResultStatus
                    .Unpaired)
            {
                return BluetoothOperationResult
                    .Ok(
                        "Сопряжение удалено.");
            }

            return BluetoothOperationResult
                .Error(
                    $"Не удалось удалить сопряжение: {result.Status}.");
        }

        // =========================================================
        // ПОДКЛЮЧЕНИЕ БЕЗ УДАЛЕНИЯ СОПРЯЖЕНИЯ
        // =========================================================

        public static async Task<
            BluetoothOperationResult>
            ConnectAsync(
                BluetoothDeviceInfo device)
        {
            if (!device.IsPaired)
            {
                return BluetoothOperationResult
                    .Error(
                        "Сначала выполните сопряжение.");
            }

            if (device.IsConnected ||
                IsLocallyConnected(device.Id))
            {
                return BluetoothOperationResult
                    .Ok(
                        "Устройство уже подключено.");
            }

            if (!device.IsLowEnergy)
            {
                return BluetoothOperationResult
                    .Error(
                        "Это классическое Bluetooth-устройство. " +
                        "Подключением аудио, телефона, клавиатуры или мыши управляет Windows. " +
                        "Сопряжение сохранено и удалять его не требуется.");
            }

            BluetoothLEDevice? bluetoothDevice =
                await BluetoothLEDevice
                    .FromIdAsync(device.Id);

            if (bluetoothDevice == null)
            {
                return BluetoothOperationResult
                    .Error(
                        "Не удалось открыть Bluetooth LE-устройство.");
            }

            GattDeviceServicesResult servicesResult =
                await bluetoothDevice
                    .GetGattServicesAsync(
                        BluetoothCacheMode.Uncached);

            if (servicesResult.Status !=
                GattCommunicationStatus.Success)
            {
                bluetoothDevice.Dispose();

                return BluetoothOperationResult
                    .Error(
                        $"Не удалось получить GATT-сервисы: {servicesResult.Status}.");
            }

            GattSession? session = null;

            try
            {
                session =
                    await GattSession.FromDeviceIdAsync(
                        bluetoothDevice.BluetoothDeviceId);

                if (session == null)
                {
                    bluetoothDevice.Dispose();

                    return BluetoothOperationResult.Error(
                        "Не удалось создать GATT-сессию.");
                }

                if (!session.CanMaintainConnection)
                {
                    session.Dispose();
                    bluetoothDevice.Dispose();

                    return BluetoothOperationResult.Error(
                        "Устройство не поддерживает постоянное соединение.");
                }

                session.MaintainConnection = true;

                lock (connectionLock)
                {
                    DisposeLocalConnection(
                        device.Id);

                    connectedLeDevices[
                        device.Id] =
                        bluetoothDevice;

                    connectedGattSessions[
                        device.Id] =
                        session;
                }

                return BluetoothOperationResult
                    .Ok(
                        "Bluetooth LE-устройство подключено.");
            }
            finally
            {
                foreach (
                    GattDeviceService service
                    in servicesResult.Services)
                {
                    service.Dispose();
                }
            }
        }

        public static Task<
            BluetoothOperationResult>
            DisconnectAsync(
                BluetoothDeviceInfo device)
        {
            if (!device.IsLowEnergy &&
                !IsLocallyConnected(device.Id))
            {
                return Task.FromResult(
                    BluetoothOperationResult.Error(
                        "Для классических Bluetooth-профилей отключением управляет Windows. " +
                        "Сопряжение останется сохранённым."));
            }

            bool removed;

            lock (connectionLock)
            {
                removed =
                    DisposeLocalConnection(
                        device.Id);
            }

            if (removed)
            {
                return Task.FromResult(
                    BluetoothOperationResult.Ok(
                        "Bluetooth LE-устройство отключено. Сопряжение сохранено."));
            }

            return Task.FromResult(
                BluetoothOperationResult.Ok(
                    "Соединение уже закрыто. Сопряжение сохранено."));
        }

        // =========================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // =========================================================

        private static BluetoothDeviceInfo
            CreateBluetoothDeviceInfo(
                DeviceInformation device)
        {
            bool paired =
                GetBooleanProperty(
                    device,
                    "System.Devices.Aep.IsPaired") ||
                device.Pairing.IsPaired;

            bool connected =
                GetBooleanProperty(
                    device,
                    "System.Devices.Aep.IsConnected");

            bool isLowEnergy =
                IsBluetoothLeId(device.Id);

            return new BluetoothDeviceInfo
            {
                Id = device.Id,
                Name = device.Name,
                IsPaired = paired,
                IsConnected =
                    connected ||
                    IsLocallyConnected(device.Id),
                IsLowEnergy =
                    isLowEnergy,
                DeviceType =
                    DetectDeviceType(
                        device.Name)
            };
        }

        private static bool
            IsBluetoothLeId(
                string deviceId)
        {
            return
                deviceId.Contains(
                    "BTHLE",
                    StringComparison
                        .OrdinalIgnoreCase) ||
                deviceId.Contains(
                    "BluetoothLE",
                    StringComparison
                        .OrdinalIgnoreCase);
        }

        private static bool
            IsLocallyConnected(
                string deviceId)
        {
            lock (connectionLock)
            {
                return connectedLeDevices
                           .ContainsKey(deviceId) ||
                       connectedGattSessions
                           .ContainsKey(deviceId);
            }
        }

        private static bool
            DisposeLocalConnection(
                string deviceId)
        {
            bool removed = false;

            if (connectedGattSessions.Remove(
                    deviceId,
                    out GattSession? session))
            {
                try
                {
                    session.MaintainConnection =
                        false;

                    session.Dispose();
                }
                catch
                {
                    // Игнорируем ошибку закрытия.
                }

                removed = true;
            }

            if (connectedLeDevices.Remove(
                    deviceId,
                    out BluetoothLEDevice?
                        bluetoothDevice))
            {
                try
                {
                    bluetoothDevice.Dispose();
                }
                catch
                {
                    // Игнорируем ошибку закрытия.
                }

                removed = true;
            }

            return removed;
        }

        private static async Task<Radio?>
            GetBluetoothRadioAsync()
        {
            IReadOnlyList<Radio> radios =
                await Radio.GetRadiosAsync();

            return radios.FirstOrDefault(
                radio =>
                    radio.Kind ==
                    RadioKind.Bluetooth);
        }

        private static bool
            GetBooleanProperty(
                DeviceInformation device,
                string propertyName)
        {
            if (!device.Properties
                    .TryGetValue(
                        propertyName,
                        out object? value))
            {
                return false;
            }

            return value is bool boolValue &&
                   boolValue;
        }

        private static bool
            GetBooleanProperty(
                IReadOnlyDictionary<
                    string,
                    object> properties,
                string propertyName,
                bool defaultValue)
        {
            if (!properties.TryGetValue(
                    propertyName,
                    out object? value))
            {
                return defaultValue;
            }

            return value is bool boolValue
                ? boolValue
                : defaultValue;
        }

        private static string DetectDeviceType(
            string name)
        {
            string normalized =
                name.ToLowerInvariant();

            if (normalized.Contains("iphone") ||
                normalized.Contains("android") ||
                normalized.Contains("xiaomi") ||
                normalized.Contains("samsung") ||
                normalized.Contains("pixel") ||
                normalized.Contains("phone") ||
                normalized.Contains("телефон"))
            {
                return "Телефон";
            }

            if (normalized.Contains("headphone") ||
                normalized.Contains("headset") ||
                normalized.Contains("buds") ||
                normalized.Contains("airpods") ||
                normalized.Contains("наушник"))
            {
                return "Наушники";
            }

            if (normalized.Contains("speaker") ||
                normalized.Contains("jbl") ||
                normalized.Contains("audio") ||
                normalized.Contains("колонка"))
            {
                return "Аудиоустройство";
            }

            if (normalized.Contains("obd") ||
                normalized.Contains("elm327") ||
                normalized.Contains("op-com"))
            {
                return "OBD-адаптер";
            }

            if (normalized.Contains("keyboard") ||
                normalized.Contains("клавиатур"))
            {
                return "Клавиатура";
            }

            if (normalized.Contains("mouse") ||
                normalized.Contains("мыш"))
            {
                return "Мышь";
            }

            if (normalized.Contains("car") ||
                normalized.Contains("auto") ||
                normalized.Contains("автомобиль"))
            {
                return "Автомобиль";
            }

            if (normalized.Contains("pc") ||
                normalized.Contains("laptop") ||
                normalized.Contains("surface") ||
                normalized.Contains("computer"))
            {
                return "Компьютер";
            }

            return "Устройство";
        }
    }
}