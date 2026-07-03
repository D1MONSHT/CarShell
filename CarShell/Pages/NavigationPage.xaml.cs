using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CarShell.Pages
{
    public partial class NavigationPage : UserControl
    {
        private readonly DispatcherTimer gpsDemoTimer;
        private readonly DispatcherTimer navigationTimer;
        private readonly Random random = new Random();
        private static readonly HttpClient httpClient = new HttpClient();

        private double lat = 50.5720;
        private double lon = 21.6790;

        private double lastLat = 50.5720;
        private double lastLon = 21.6790;
        private double heading = 90.0;

        private double? destinationLat = null;
        private double? destinationLon = null;

        private bool mapReady = false;
        private bool navigationActive = false;

        private readonly List<(double Lat, double Lon)> routePoints = new();
        private readonly List<RouteStep> routeSteps = new();

        private int currentRouteIndex = 0;
        private int currentStepIndex = 0;

        private double routeDistanceKm = 0;
        private double routeDurationMin = 0;

        private int currentSpeed = 0;
        private int currentZoom = 16;

        public NavigationPage(MainWindow mainWindow)
        {
            InitializeComponent();

            Loaded += NavigationPage_Loaded;

            gpsDemoTimer = new DispatcherTimer();
            gpsDemoTimer.Interval = TimeSpan.FromSeconds(1);
            gpsDemoTimer.Tick += GpsDemoTimer_Tick;

            navigationTimer = new DispatcherTimer();
            navigationTimer.Interval = TimeSpan.FromMilliseconds(700);
            navigationTimer.Tick += NavigationTimer_Tick;

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CarShell/0.1");
        }

        private async void NavigationPage_Loaded(object sender, RoutedEventArgs e)
        {
            await MapWebView.EnsureCoreWebView2Async();

            LoadMap();
            mapReady = true;

            await Task.Delay(500);

            if (routePoints.Count > 0)
            {
                await RedrawRouteFromCurrentPosition();
                NavigationInfoPanel.Visibility = Visibility.Visible;
                ShowCurrentStep();
            }

            if (!navigationActive)
                gpsDemoTimer.Start();
        }

        private void LoadMap()
        {
            string html = @"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'/>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>

<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>

<style>
html, body, #map {
    height: 100%;
    width: 100%;
    margin: 0;
    background: #101923;
    overflow: hidden;
}

.leaflet-control-attribution {
    display: none;
}

.car-arrow {
    width: 0;
    height: 0;
    border-left: 14px solid transparent;
    border-right: 14px solid transparent;
    border-bottom: 30px solid #2196F3;
    filter: drop-shadow(0 0 4px #000)
            drop-shadow(0 0 8px #000);
    transform-origin: 50% 55%;
}
</style>
</head>

<body>
<div id='map'></div>

<script>
var map = L.map('map', {
    zoomControl: false
}).setView([50.5720, 21.6790], 14);

var streetLayer = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19
});

var satelliteLayer = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
    maxZoom: 19
});

streetLayer.addTo(map);

var currentLayer = 'street';

var carIcon = L.divIcon({
    className: '',
    html: '<div id=""carArrow"" class=""car-arrow"" style=""transform: rotate(90deg);""></div>',
    iconSize: [34, 34],
    iconAnchor: [17, 17]
});

var gpsMarker = L.marker([50.5720, 21.6790], {
    icon: carIcon
}).addTo(map);

var searchMarker = null;
var routeLine = null;

function rotateCar(heading) {
    var el = document.getElementById('carArrow');
    if (el) {
        el.style.transform = 'rotate(' + heading + 'deg)';
    }
}

function updatePosition(lat, lon, heading) {
    gpsMarker.setLatLng([lat, lon]);
    rotateCar(heading);
}

function followPosition(lat, lon, heading, zoom) {
    gpsMarker.setLatLng([lat, lon]);
    rotateCar(heading);
    map.setView([lat, lon], zoom, { animate: true });
}

function moveTo(lat, lon, zoom) {
    map.setView([lat, lon], zoom);

    if (searchMarker !== null) {
        map.removeLayer(searchMarker);
    }

    searchMarker = L.marker([lat, lon]).addTo(map);
}

function drawRoute(coords) {
    if (routeLine !== null) {
        map.removeLayer(routeLine);
    }

    routeLine = L.polyline(coords, {
        color: '#2EE66B',
        weight: 8,
        opacity: 0.95
    }).addTo(map);

    if (coords.length > 1) {
        map.fitBounds(routeLine.getBounds(), {
            padding: [60, 60]
        });
    }
}

function updateRemainingRoute(coords) {
    if (routeLine !== null) {
        map.removeLayer(routeLine);
    }

    if (coords.length < 2) {
        return;
    }

    routeLine = L.polyline(coords, {
        color: '#2EE66B',
        weight: 8,
        opacity: 0.95
    }).addTo(map);
}

function clearRoute() {
    if (routeLine !== null) {
        map.removeLayer(routeLine);
        routeLine = null;
    }
}

function toggleMapLayer() {
    if (currentLayer === 'street') {
        map.removeLayer(streetLayer);
        satelliteLayer.addTo(map);
        currentLayer = 'satellite';
    } else {
        map.removeLayer(satelliteLayer);
        streetLayer.addTo(map);
        currentLayer = 'street';
    }
}
</script>
</body>
</html>";

            MapWebView.NavigateToString(html);
        }

        private async void GpsDemoTimer_Tick(object? sender, EventArgs e)
        {
            if (!mapReady || navigationActive)
                return;

            lastLat = lat;
            lastLon = lon;

            lat = 50.5720 + random.NextDouble() / 1000;
            lon = 21.6790 + random.NextDouble() / 1000;

            currentSpeed = 0;
            currentZoom = GetNavigationZoom(currentSpeed);

            UpdateHeadingFromGps();
            UpdateGpsText(currentSpeed);

            await UpdateMapPosition(false);
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            string query = SearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(query))
                return;

            await SearchAddress(query);
        }

        private async Task SearchAddress(string query)
        {
            try
            {
                GpsStatusText.Text = "SEARCH...";
                GpsStatusText.Foreground = System.Windows.Media.Brushes.DeepSkyBlue;

                string url =
                    "https://nominatim.openstreetmap.org/search?format=json&limit=1&q=" +
                    Uri.EscapeDataString(query);

                string json = await httpClient.GetStringAsync(url);

                using JsonDocument doc = JsonDocument.Parse(json);

                if (doc.RootElement.GetArrayLength() == 0)
                {
                    GpsStatusText.Text = "НЕ НАЙДЕНО";
                    GpsStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                    return;
                }

                JsonElement item = doc.RootElement[0];

                double foundLat = double.Parse(item.GetProperty("lat").GetString() ?? "0", CultureInfo.InvariantCulture);
                double foundLon = double.Parse(item.GetProperty("lon").GetString() ?? "0", CultureInfo.InvariantCulture);

                destinationLat = foundLat;
                destinationLon = foundLon;

                DestinationText.Text = query;

                string js =
                    $"moveTo({foundLat.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{foundLon.ToString(CultureInfo.InvariantCulture)}, 15);";

                await MapWebView.ExecuteScriptAsync(js);

                GpsStatusText.Text = "ТОЧКА НАЙДЕНА";
                GpsStatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }
            catch
            {
                GpsStatusText.Text = "ОШИБКА ПОИСКА";
                GpsStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
        }

        private async void Route_Click(object sender, RoutedEventArgs e)
        {
            if (destinationLat == null || destinationLon == null)
            {
                GpsStatusText.Text = "СНАЧАЛА ПОИСК";
                GpsStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                return;
            }

            await BuildRoute(lat, lon, destinationLat.Value, destinationLon.Value);
        }

        private async Task BuildRoute(double startLat, double startLon, double endLat, double endLon)
        {
            try
            {
                GpsStatusText.Text = "ROUTE...";
                GpsStatusText.Foreground = System.Windows.Media.Brushes.DeepSkyBlue;

                routePoints.Clear();
                routeSteps.Clear();

                currentRouteIndex = 0;
                currentStepIndex = 0;

                string url =
                    "https://router.project-osrm.org/route/v1/driving/" +
                    startLon.ToString(CultureInfo.InvariantCulture) + "," +
                    startLat.ToString(CultureInfo.InvariantCulture) + ";" +
                    endLon.ToString(CultureInfo.InvariantCulture) + "," +
                    endLat.ToString(CultureInfo.InvariantCulture) +
                    "?overview=full&geometries=geojson&steps=true";

                string json = await httpClient.GetStringAsync(url);

                using JsonDocument doc = JsonDocument.Parse(json);

                JsonElement routes = doc.RootElement.GetProperty("routes");

                if (routes.GetArrayLength() == 0)
                {
                    GpsStatusText.Text = "МАРШРУТ НЕ НАЙДЕН";
                    GpsStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                    return;
                }

                JsonElement route = routes[0];

                routeDistanceKm = route.GetProperty("distance").GetDouble() / 1000.0;
                routeDurationMin = route.GetProperty("duration").GetDouble() / 60.0;

                JsonElement coordinates = route.GetProperty("geometry").GetProperty("coordinates");

                foreach (JsonElement point in coordinates.EnumerateArray())
                {
                    double routeLon = point[0].GetDouble();
                    double routeLat = point[1].GetDouble();

                    routePoints.Add((routeLat, routeLon));
                }

                ParseRouteSteps(route);

                await DrawFullRoute();

                GpsStatusText.Text =
                    routeDistanceKm.ToString("F1") + " km / " +
                    routeDurationMin.ToString("F0") + " min";

                GpsStatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;

                NavigationInfoPanel.Visibility = Visibility.Visible;
                ShowCurrentStep();

                RouteInfoText.Text =
                    routeDistanceKm.ToString("F1") + " km • " +
                    routeDurationMin.ToString("F0") + " min";
            }
            catch
            {
                GpsStatusText.Text = "ОШИБКА МАРШРУТА";
                GpsStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
        }

        private async Task DrawFullRoute()
        {
            string jsArray = BuildRouteJsArray(0);
            await MapWebView.ExecuteScriptAsync($"drawRoute({jsArray});");
        }

        private async Task RedrawRouteFromCurrentPosition()
        {
            if (routePoints.Count == 0)
                return;

            string jsArray = BuildRouteJsArray(currentRouteIndex);
            await MapWebView.ExecuteScriptAsync($"updateRemainingRoute({jsArray});");

            if (destinationLat != null && destinationLon != null)
            {
                string js =
                    $"moveTo({destinationLat.Value.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{destinationLon.Value.ToString(CultureInfo.InvariantCulture)}, 15);";

                await MapWebView.ExecuteScriptAsync(js);
            }

            await UpdateMapPosition(navigationActive);
        }

        private string BuildRouteJsArray(int startIndex)
        {
            if (startIndex < 0)
                startIndex = 0;

            if (startIndex >= routePoints.Count)
                startIndex = routePoints.Count - 1;

            string jsArray = "[";

            for (int i = startIndex; i < routePoints.Count; i++)
            {
                jsArray += "[" +
                           routePoints[i].Lat.ToString(CultureInfo.InvariantCulture) + "," +
                           routePoints[i].Lon.ToString(CultureInfo.InvariantCulture) + "],";
            }

            if (jsArray.EndsWith(","))
                jsArray = jsArray.Substring(0, jsArray.Length - 1);

            jsArray += "]";

            return jsArray;
        }

        private void ParseRouteSteps(JsonElement route)
        {
            routeSteps.Clear();

            JsonElement legs = route.GetProperty("legs");

            foreach (JsonElement leg in legs.EnumerateArray())
            {
                JsonElement steps = leg.GetProperty("steps");

                foreach (JsonElement step in steps.EnumerateArray())
                {
                    double distance = step.GetProperty("distance").GetDouble();
                    double duration = step.GetProperty("duration").GetDouble();

                    JsonElement maneuver = step.GetProperty("maneuver");

                    string type = maneuver.TryGetProperty("type", out JsonElement typeEl)
                        ? typeEl.GetString() ?? ""
                        : "";

                    string modifier = maneuver.TryGetProperty("modifier", out JsonElement modEl)
                        ? modEl.GetString() ?? ""
                        : "";

                    string name = step.TryGetProperty("name", out JsonElement nameEl)
                        ? nameEl.GetString() ?? ""
                        : "";

                    string instruction = BuildInstruction(type, modifier, name, distance);
                    string icon = BuildIcon(type, modifier);

                    routeSteps.Add(new RouteStep
                    {
                        Instruction = instruction,
                        Icon = icon,
                        DistanceMeters = distance,
                        DurationSeconds = duration
                    });
                }
            }

            if (routeSteps.Count == 0)
            {
                routeSteps.Add(new RouteStep
                {
                    Instruction = "Двигайтесь по маршруту",
                    Icon = "↑",
                    DistanceMeters = 0,
                    DurationSeconds = 0
                });
            }
        }

        private string BuildInstruction(string type, string modifier, string roadName, double distanceMeters)
        {
            string distanceText = FormatDistance(distanceMeters);

            string road = string.IsNullOrWhiteSpace(roadName)
                ? ""
                : " на " + roadName;

            if (type == "depart")
                return "Начинайте движение" + road;

            if (type == "arrive")
                return "Вы прибыли";

            if (type == "turn")
            {
                if (modifier.Contains("right"))
                    return "Через " + distanceText + " поверните направо" + road;

                if (modifier.Contains("left"))
                    return "Через " + distanceText + " поверните налево" + road;

                return "Через " + distanceText + " выполните поворот" + road;
            }

            if (type == "roundabout" || type == "rotary")
                return "Через " + distanceText + " въезд на круговое движение";

            if (type == "merge")
                return "Через " + distanceText + " перестройтесь" + road;

            if (type == "new name")
                return "Продолжайте движение" + road;

            if (modifier.Contains("right"))
                return "Через " + distanceText + " держитесь правее" + road;

            if (modifier.Contains("left"))
                return "Через " + distanceText + " держитесь левее" + road;

            return "Продолжайте движение" + road;
        }

        private string BuildIcon(string type, string modifier)
        {
            if (type == "arrive")
                return "●";

            if (type == "roundabout" || type == "rotary")
                return "↻";

            if (modifier.Contains("right"))
                return "↱";

            if (modifier.Contains("left"))
                return "↰";

            if (modifier.Contains("straight"))
                return "↑";

            return "↑";
        }

        private string FormatDistance(double meters)
        {
            if (meters < 1000)
                return Math.Round(meters).ToString("F0") + " м";

            return (meters / 1000.0).ToString("F1") + " км";
        }

        private async void StartNavigation_Click(object sender, RoutedEventArgs e)
        {
            if (routePoints.Count == 0)
            {
                GpsStatusText.Text = "НЕТ МАРШРУТА";
                GpsStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                return;
            }

            navigationActive = true;
            currentRouteIndex = 0;
            currentStepIndex = 0;

            currentSpeed = 45;
            currentZoom = GetNavigationZoom(currentSpeed);

            NavigationInfoPanel.Visibility = Visibility.Visible;

            GpsStatusText.Text = "НАВИГАЦИЯ";
            GpsStatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;

            gpsDemoTimer.Stop();
            navigationTimer.Start();

            ShowCurrentStep();
            await UpdateRemainingRouteOnMap();
            await FollowCurrentPoint();
        }

        private void StopNavigation_Click(object sender, RoutedEventArgs e)
        {
            navigationActive = false;
            navigationTimer.Stop();
            gpsDemoTimer.Start();

            NavigationInfoPanel.Visibility = Visibility.Collapsed;

            GpsStatusText.Text = "GPS: DEMO";
            GpsStatusText.Foreground = System.Windows.Media.Brushes.Orange;
        }

        private async void NavigationTimer_Tick(object? sender, EventArgs e)
        {
            if (!navigationActive || routePoints.Count == 0)
                return;

            if (currentRouteIndex >= routePoints.Count - 1)
            {
                navigationTimer.Stop();
                navigationActive = false;

                TurnIconText.Text = "●";
                NextTurnText.Text = "Вы прибыли";
                RouteInfoText.Text = "Маршрут завершён";

                GpsStatusText.Text = "ПРИБЫЛИ";
                GpsStatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;

                await MapWebView.ExecuteScriptAsync("clearRoute();");
                return;
            }

            lastLat = lat;
            lastLon = lon;

            currentRouteIndex += 3;

            if (currentRouteIndex >= routePoints.Count)
                currentRouteIndex = routePoints.Count - 1;

            lat = routePoints[currentRouteIndex].Lat;
            lon = routePoints[currentRouteIndex].Lon;

            UpdateHeadingFromGps();

            double progress = (double)currentRouteIndex / routePoints.Count;

            currentSpeed = GetDemoSpeed(progress);
            currentZoom = GetNavigationZoom(currentSpeed);

            double remainingKm = routeDistanceKm * (1.0 - progress);
            double remainingMin = routeDurationMin * (1.0 - progress);

            UpdateGpsText(currentSpeed);
            UpdateCurrentStepByProgress(progress);

            RouteInfoText.Text =
                remainingKm.ToString("F1") + " km • " +
                remainingMin.ToString("F0") + " min";

            await UpdateRemainingRouteOnMap();
            await FollowCurrentPoint();
        }

        private int GetDemoSpeed(double progress)
        {
            if (progress < 0.10)
                return 20;

            if (progress < 0.35)
                return 45;

            if (progress < 0.70)
                return 80;

            if (progress < 0.90)
                return 55;

            return 25;
        }

        private int GetNavigationZoom(int speed)
        {
            if (speed <= 20)
                return 17;

            if (speed <= 50)
                return 16;

            if (speed <= 90)
                return 15;

            return 14;
        }

        private async Task UpdateRemainingRouteOnMap()
        {
            if (routePoints.Count == 0)
                return;

            string jsArray = BuildRouteJsArray(currentRouteIndex);
            await MapWebView.ExecuteScriptAsync($"updateRemainingRoute({jsArray});");
        }

        private void UpdateCurrentStepByProgress(double progress)
        {
            if (routeSteps.Count == 0)
                return;

            int targetStep = (int)(progress * routeSteps.Count);

            if (targetStep >= routeSteps.Count)
                targetStep = routeSteps.Count - 1;

            if (targetStep != currentStepIndex)
            {
                currentStepIndex = targetStep;
                ShowCurrentStep();
            }
        }

        private void ShowCurrentStep()
        {
            if (routeSteps.Count == 0)
                return;

            RouteStep step = routeSteps[currentStepIndex];

            TurnIconText.Text = step.Icon;
            NextTurnText.Text = step.Instruction;
        }

        private async Task FollowCurrentPoint()
        {
            await UpdateMapPosition(true);
        }

        private async Task UpdateMapPosition(bool follow)
        {
            string js;

            if (follow)
            {
                js =
                    $"followPosition({lat.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{lon.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{heading.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{currentZoom.ToString(CultureInfo.InvariantCulture)});";
            }
            else
            {
                js =
                    $"updatePosition({lat.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{lon.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{heading.ToString(CultureInfo.InvariantCulture)});";
            }

            try
            {
                await MapWebView.ExecuteScriptAsync(js);
            }
            catch
            {
            }
        }

        private void UpdateHeadingFromGps()
        {
            double distance = DistanceMeters(lastLat, lastLon, lat, lon);

            if (distance < 0.5)
                return;

            heading = CalculateBearing(lastLat, lastLon, lat, lon);
        }

        private double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            double phi1 = DegreesToRadians(lat1);
            double phi2 = DegreesToRadians(lat2);
            double deltaLon = DegreesToRadians(lon2 - lon1);

            double y = Math.Sin(deltaLon) * Math.Cos(phi2);
            double x =
                Math.Cos(phi1) * Math.Sin(phi2) -
                Math.Sin(phi1) * Math.Cos(phi2) * Math.Cos(deltaLon);

            double bearingRad = Math.Atan2(y, x);
            double bearingDeg = RadiansToDegrees(bearingRad);

            return (bearingDeg + 360.0) % 360.0;
        }

        private double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 6371000.0;

            double phi1 = DegreesToRadians(lat1);
            double phi2 = DegreesToRadians(lat2);

            double deltaPhi = DegreesToRadians(lat2 - lat1);
            double deltaLambda = DegreesToRadians(lon2 - lon1);

            double a =
                Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadius * c;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        private double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        private void UpdateGpsText(int speed)
        {
            LatText.Text = "lat: " + lat.ToString("F6");
            LonText.Text = "lon: " + lon.ToString("F6");
            GpsSpeedText.Text = "speed: " + speed + " km/h";
        }

        private async void Satellite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await MapWebView.ExecuteScriptAsync("toggleMapLayer();");
            }
            catch
            {
            }
        }

        private class RouteStep
        {
            public string Instruction { get; set; } = "";
            public string Icon { get; set; } = "↑";
            public double DistanceMeters { get; set; }
            public double DurationSeconds { get; set; }
        }
    }
}