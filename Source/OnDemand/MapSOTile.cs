using Kopernicus.OnDemand;
using System;

namespace AdvancedPQSTools.OnDemand
{
    public class MapSOTile : MapSODemand, ILoadOnDemand, IPreloadOnDemand
    {
        protected override void ConstructBilinearCoords(double x, double y)
        {
            // Clamp vs Wrap
            x = Math.Abs(x - Math.Floor(x));
            y = Math.Abs(y - Math.Floor(y));
            centerXD = x * _width;
            minX = (int)Math.Floor(centerXD);
            maxX = (int)Math.Ceiling(centerXD);
            midX = (float)centerXD - minX;
            if (maxX == _width)
                maxX = _width - 1;

            centerYD = y * _height;
            minY = (int)Math.Floor(centerYD);
            maxY = (int)Math.Ceiling(centerYD);
            midY = (float)centerYD - minY;
            if (maxY == _height)
                maxY = _height - 1;
        }
    }
}
