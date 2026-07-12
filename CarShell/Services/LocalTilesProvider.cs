using System;
using System.IO;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;
using PureImage = GMap.NET.PureImage;

namespace CarShell.Services
{
    public class LocalTilesProvider : GMapProvider
    {
        public static readonly LocalTilesProvider Instance = new();

        public override Guid Id { get; } =
            new Guid("7D9B2C10-8E11-4E5A-9E22-111111111111");

        public override string Name => "LocalTiles";

        public override PureProjection Projection => MercatorProjection.Instance;

        public override GMapProvider[] Overlays => new GMapProvider[]
        {
            this
        };

        private readonly string tilesRoot =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Maps", "tiles");

        public override PureImage? GetTileImage(GPoint pos, int zoom)
        {
            string file = Path.Combine(
                tilesRoot,
                zoom.ToString(),
                pos.X.ToString(),
                pos.Y + ".png"
            );

            if (!File.Exists(file))
                return null;

            return GetTileImageFromFile(file);
        }
    }
}