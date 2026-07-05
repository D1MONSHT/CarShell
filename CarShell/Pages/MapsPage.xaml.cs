using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using NtsPoint = NetTopologySuite.Geometries.Point;
using NtsLineString = NetTopologySuite.Geometries.LineString;
using NtsCoordinate = NetTopologySuite.Geometries.Coordinate;

namespace CarShell.Pages
{
    public partial class MapsPage : UserControl
    {
        private readonly double carLat = 50.0413;
        private readonly double carLon = 21.9990;

        private static readonly HttpClient http = new HttpClient();
        private static bool userAgentAdded = false;

        private MemoryLayer carLayer;
        private MemoryLayer routeLayer;
        private MemoryLayer destinationLayer;

        private bool ignoreSearchTextChanged = false;
        private List<SearchPlace> currentSuggestions = new List<SearchPlace>();

        private class SearchPlace
        {
            public string DisplayName { get; set; }
            public double Lat { get; set; }
            public double Lon { get; set; }

            public SearchPlace()
            {
                DisplayName = "";
            }

            public override string ToString()
            {
                return DisplayName;
            }
        }

        public MapsPage(MainWindow mainWindow)
        {
            InitializeComponent();

            if (!userAgentAdded)
            {
                try
                {
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("CarShell/1.0");
                    userAgentAdded = true;
                }
                catch { }
            }

            Loaded += MapsPage_Loaded;
            SearchBox.GotFocus += SearchBox_GotFocus;
        }

        private void MapsPage_Loaded(object sender, RoutedEventArgs e)
        {
            InitMap();
        }

        private void InitMap()
        {
            Map.Map = new Mapsui.Map();

            Map.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

            AddCarMarker(carLat, carLon);

            var center = SphericalMercator.FromLonLat(carLon, carLat);
            Map.Map.Navigator.CenterOn(center.x, center.y);
            Map.Map.Navigator.ZoomTo(200);
        }

        private void AddCarMarker(double lat, double lon)
        {
            var point = SphericalMercator.FromLonLat(lon, lat);

            var feature = new GeometryFeature
            {
                Geometry = new NtsPoint(point.x, point.y)
            };

            feature.Styles.Add(new SymbolStyle
            {
                SymbolScale = 1.3,
                Fill = new Brush(Color.FromString("#168CFF")),
                Outline = new Pen(Color.White, 2)
            });

            carLayer = new MemoryLayer
            {
                Name = "Car",
                Features = new List<IFeature> { feature },
                Style = null
            };

            Map.Map.Layers.Add(carLayer);
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (QuickPanel != null)
                QuickPanel.Visibility = Visibility.Visible;

            if (SearchBox.Text == "Поиск места")
                SearchBox.Text = "";
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ignoreSearchTextChanged)
                return;

            if (!IsLoaded || SuggestionsPanel == null || SuggestionsList == null)
                return;

            string query = SearchBox.Text.Trim();

            if (query.Length < 3 || query == "Поиск места")
            {
                SuggestionsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            await LoadSuggestionsAsync(query);
        }

        private async Task LoadSuggestionsAsync(string query)
        {
            try
            {
                string url =
                    "https://nominatim.openstreetmap.org/search" +
                    "?format=json" +
                    "&limit=5" +
                    "&countrycodes=pl" +
                    "&q=" + Uri.EscapeDataString(query);

                string json = await http.GetStringAsync(url);
                JArray result = JArray.Parse(json);

                currentSuggestions.Clear();

                foreach (var item in result)
                {
                    currentSuggestions.Add(new SearchPlace
                    {
                        DisplayName = item["display_name"] != null ? item["display_name"].ToString() : "",
                        Lat = double.Parse(item["lat"].ToString(), CultureInfo.InvariantCulture),
                        Lon = double.Parse(item["lon"].ToString(), CultureInfo.InvariantCulture)
                    });
                }

                SuggestionsList.ItemsSource = null;
                SuggestionsList.ItemsSource = currentSuggestions;

                SuggestionsPanel.Visibility = currentSuggestions.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            catch
            {
                SuggestionsPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void SuggestionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchPlace place = SuggestionsList.SelectedItem as SearchPlace;

            if (place == null)
                return;

            ignoreSearchTextChanged = true;
            SearchBox.Text = place.DisplayName;
            ignoreSearchTextChanged = false;

            SuggestionsPanel.Visibility = Visibility.Collapsed;
            QuickPanel.Visibility = Visibility.Collapsed;

            await BuildRouteOnlineAsync(carLat, carLon, place.Lat, place.Lon);

            SuggestionsList.SelectedItem = null;
        }

        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            if (currentSuggestions.Count > 0)
            {
                SearchPlace place = currentSuggestions[0];

                SuggestionsPanel.Visibility = Visibility.Collapsed;
                QuickPanel.Visibility = Visibility.Collapsed;

                await BuildRouteOnlineAsync(carLat, carLon, place.Lat, place.Lon);
            }
            else
            {
                await SearchAndRouteAsync();
            }
        }

        private async Task SearchAndRouteAsync()
        {
            string query = SearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(query) || query == "Поиск места")
                return;

            await LoadSuggestionsAsync(query);

            if (currentSuggestions.Count > 0)
            {
                SearchPlace place = currentSuggestions[0];
                await BuildRouteOnlineAsync(carLat, carLon, place.Lat, place.Lon);
            }
        }

        private async void Home_Click(object sender, RoutedEventArgs e)
        {
            QuickPanel.Visibility = Visibility.Collapsed;
            SuggestionsPanel.Visibility = Visibility.Collapsed;

            await BuildRouteOnlineAsync(carLat, carLon, 50.0615, 21.9950);
        }

        private async void Work_Click(object sender, RoutedEventArgs e)
        {
            QuickPanel.Visibility = Visibility.Collapsed;
            SuggestionsPanel.Visibility = Visibility.Collapsed;

            await BuildRouteOnlineAsync(carLat, carLon, 50.0300, 22.0200);
        }

        private async Task BuildRouteOnlineAsync(
            double startLat,
            double startLon,
            double destLat,
            double destLon)
        {
            try
            {
                ClearRoute();

                string url =
                    "https://router.project-osrm.org/route/v1/driving/" +
                    startLon.ToString(CultureInfo.InvariantCulture) + "," +
                    startLat.ToString(CultureInfo.InvariantCulture) + ";" +
                    destLon.ToString(CultureInfo.InvariantCulture) + "," +
                    destLat.ToString(CultureInfo.InvariantCulture) +
                    "?overview=full&geometries=geojson&steps=true";

                string json = await http.GetStringAsync(url);
                JObject root = JObject.Parse(json);

                if (root["code"] == null || root["code"].ToString() != "Ok")
                {
                    MessageBox.Show("Маршрут не найден", "CarShell Maps");
                    return;
                }

                JArray coords = (JArray)root["routes"][0]["geometry"]["coordinates"];
                List<NtsCoordinate> routePoints = new List<NtsCoordinate>();

                foreach (JArray c in coords)
                {
                    double lon = c[0].Value<double>();
                    double lat = c[1].Value<double>();

                    var p = SphericalMercator.FromLonLat(lon, lat);
                    routePoints.Add(new NtsCoordinate(p.x, p.y));
                }

                if (routePoints.Count < 2)
                {
                    MessageBox.Show("Слишком короткий маршрут", "CarShell Maps");
                    return;
                }

                DrawRoute(routePoints, destLat, destLon);

                double distanceMeters = root["routes"][0]["distance"].Value<double>();
                double durationSeconds = root["routes"][0]["duration"].Value<double>();

                UpdateRoutePanel(root, distanceMeters, durationSeconds);

                RoutePanel.Visibility = Visibility.Visible;

                FitRouteToScreen(routePoints);

                Map.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка построения маршрута:\n" + ex.Message, "CarShell Maps");
            }
        }

        private void UpdateRoutePanel(JObject root, double distanceMeters, double durationSeconds)
        {
            int minutes = Math.Max(1, (int)Math.Round(durationSeconds / 60.0));
            double km = distanceMeters / 1000.0;

            RouteTimeText.Text = minutes + " мин";
            RouteDistanceText.Text = km.ToString("0.0", CultureInfo.InvariantCulture) + " км";

            try
            {
                var firstStep = root["routes"][0]["legs"][0]["steps"][0];

                double stepDistance = firstStep["distance"].Value<double>();
                string street = firstStep["name"] != null ? firstStep["name"].ToString() : "";

                string maneuverType = firstStep["maneuver"]["type"] != null
                    ? firstStep["maneuver"]["type"].ToString()
                    : "";

                string modifier = firstStep["maneuver"]["modifier"] != null
                    ? firstStep["maneuver"]["modifier"].ToString()
                    : "";

                NextDistanceText.Text = stepDistance < 1000
                    ? Math.Round(stepDistance) + " м"
                    : (stepDistance / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + " км";

                NextInstructionText.Text = GetInstruction(maneuverType, modifier);

                NextStreetText.Text = string.IsNullOrWhiteSpace(street)
                    ? ""
                    : "на " + street;
            }
            catch
            {
                NextDistanceText.Text = "-- м";
                NextInstructionText.Text = "Следуйте по маршруту";
                NextStreetText.Text = "";
            }
        }

        private string GetInstruction(string type, string modifier)
        {
            if (type == "depart") return "Начните движение";
            if (type == "arrive") return "Вы прибыли";
            if (type == "roundabout") return "На круговом движении";
            if (type == "merge") return "Перестройтесь";
            if (type == "new name") return "Продолжайте движение";

            if (modifier.Contains("right")) return "Поверните направо";
            if (modifier.Contains("left")) return "Поверните налево";
            if (modifier.Contains("straight")) return "Двигайтесь прямо";
            if (modifier.Contains("uturn")) return "Развернитесь";

            return "Следуйте по маршруту";
        }

        private void DrawRoute(List<NtsCoordinate> routePoints, double destLat, double destLon)
        {
            var line = new NtsLineString(routePoints.ToArray());

            var routeFeature = new GeometryFeature
            {
                Geometry = line
            };

            routeFeature.Styles.Add(new VectorStyle
            {
                Line = new Pen(Color.FromString("#168CFF"), 8)
            });

            routeLayer = new MemoryLayer
            {
                Name = "Route",
                Features = new List<IFeature> { routeFeature },
                Style = null
            };

            var end = SphericalMercator.FromLonLat(destLon, destLat);

            var destinationFeature = new GeometryFeature
            {
                Geometry = new NtsPoint(end.x, end.y)
            };

            destinationFeature.Styles.Add(new SymbolStyle
            {
                SymbolScale = 1.1,
                Fill = new Brush(Color.FromString("#00F06A")),
                Outline = new Pen(Color.White, 2)
            });

            destinationLayer = new MemoryLayer
            {
                Name = "Destination",
                Features = new List<IFeature> { destinationFeature },
                Style = null
            };

            Map.Map.Layers.Add(routeLayer);
            Map.Map.Layers.Add(destinationLayer);
        }

        private void FitRouteToScreen(List<NtsCoordinate> routePoints)
        {
            if (routePoints.Count == 0)
                return;

            double minX = routePoints[0].X;
            double maxX = routePoints[0].X;
            double minY = routePoints[0].Y;
            double maxY = routePoints[0].Y;

            foreach (var p in routePoints)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            double padding = 1200;

            var rect = new MRect(
                minX - padding,
                minY - padding,
                maxX + padding,
                maxY + padding
            );

            Map.Map.Navigator.ZoomToBox(rect);
        }

        private void ClearRoute()
        {
            if (routeLayer != null)
            {
                Map.Map.Layers.Remove(routeLayer);
                routeLayer = null;
            }

            if (destinationLayer != null)
            {
                Map.Map.Layers.Remove(destinationLayer);
                destinationLayer = null;
            }

            if (Map != null)
                Map.Refresh();
        }

        private void CancelRoute_Click(object sender, RoutedEventArgs e)
        {
            ClearRoute();
            RoutePanel.Visibility = Visibility.Collapsed;
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            Map.Map.Navigator.ZoomIn();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            Map.Map.Navigator.ZoomOut();
        }

        private void MyLocation_Click(object sender, RoutedEventArgs e)
        {
            var center = SphericalMercator.FromLonLat(carLon, carLat);
            Map.Map.Navigator.CenterOn(center.x, center.y);
            Map.Map.Navigator.ZoomTo(200);
        }
    }
}