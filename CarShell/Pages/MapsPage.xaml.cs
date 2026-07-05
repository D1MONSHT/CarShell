using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CarShell.Pages
{
    public partial class MapsPage : UserControl
    {
        private readonly double startLat = 50.0413;
        private readonly double startLon = 21.9990;

        public MapsPage(MainWindow mainWindow)
        {
            InitializeComponent();

            Loaded += MapsPage_Loaded;
            SearchBox.GotFocus += SearchBox_GotFocus;
        }

        private void MapsPage_Loaded(object sender, RoutedEventArgs e)
        {
            InitMap();
        }

        private void InitMap()
        {
            Map.Map ??= new Mapsui.Map();

            // Пока онлайн OSM для теста.
            // Потом заменим на MBTiles офлайн.
            Map.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

            AddCarMarker(startLat, startLon);

            var center = SphericalMercator.FromLonLat(startLon, startLat);
            Map.Map.Navigator.CenterOn(center.x, center.y);
            Map.Map.Navigator.ZoomTo(25000);
        }

        private void AddCarMarker(double lat, double lon)
        {
            var point = SphericalMercator.FromLonLat(lon, lat);

            var feature = new Mapsui.Nts.GeometryFeature
            {
                Geometry = new NetTopologySuite.Geometries.Point(point.x, point.y)
            };

            feature.Styles.Add(new SymbolStyle
            {
                SymbolScale = 1.2,
                Fill = new Brush(Color.FromString("#168CFF")),
                Outline = new Pen(Color.White, 2)
            });

            var layer = new MemoryLayer
            {
                Name = "Car",
                Features = new List<IFeature> { feature },
                Style = null
            };

            Map.Map.Layers.Add(layer);
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            QuickPanel.Visibility = Visibility.Visible;

            if (SearchBox.Text == "Поиск места")
                SearchBox.Text = "";
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            QuickPanel.Visibility = Visibility.Collapsed;
            BuildDemoRoute();
        }

        private void Work_Click(object sender, RoutedEventArgs e)
        {
            QuickPanel.Visibility = Visibility.Collapsed;
            BuildDemoRoute();
        }

        private void BuildDemoRoute()
        {
            // Пока демо-режим.
            // Настоящий маршрут добавим через GraphHopper.
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
            var center = SphericalMercator.FromLonLat(startLon, startLat);
            Map.Map.Navigator.CenterOn(center.x, center.y);
        }
    }
}