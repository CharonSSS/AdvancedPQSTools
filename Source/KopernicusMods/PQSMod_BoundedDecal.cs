/* 
 * This code is adapted from KopernicusExpansion-Continued
 * Available from https://github.com/StollD/KopernicusExpansion-Continued
 */

using Kopernicus.Configuration;
using System;
using UnityEngine;

namespace AdvancedPQSTools
{
    /// <summary>
    /// A heightmap decal PQSMod that can parse encoded 16bpp textures
    /// </summary>
    public class PQSMod_BoundedDecal : PQSMod
    {
        public bool allowScatters = true;
        public MapSO heightMap;
        public MapSO colorMap;
        public double heightMapOffset;
        public double heightMapDeformity;
        public double maxLong;
        public double minLong;
        public double maxLat;
        public double minLat;

        public override void OnVertexBuildHeight(PQS.VertexBuildData data)
        {
            //Debug.Log($"Height before: {data.vertHeight}");
            double maxU;
            double minU;
            double maxV;
            double minV;
            double rangeU;
            double rangeV;

            maxU = 1 - ((maxLong + 270) / 360);
            if (maxU < 0)
                maxU += 1;
            minU = 1 - ((minLong + 270) / 360);
            if (minU < 0)
                minU += 1;
            maxV = (maxLat + 90) / 180;
            minV = (minLat + 90) / 180;
            rangeU = minU - maxU;
            rangeV = maxV - minV;

            if (data.u < minU && data.u > maxU)
            {
                if (data.v < maxV && data.v > minV)
                {
                    double newU = 1 - (data.u - maxU) / rangeU;
                    double newV = 1 - (data.v - minV) / rangeV;



                    // Get the HeightAlpha, not the Float-Value from the Map
                    // Clamp the v value to just shy of 1 to avoid sampling issues around the north pole.
                    MapSO.HeightAlpha ha = heightMap.GetPixelHeightAlpha(newU, newV);

                    // Get the height data from the terrain
                    Double height = (ha.height + ha.alpha * (Double)Byte.MaxValue) / (Double)(Byte.MaxValue + 1);
                    // Apply it
                    if (height != 0)
                    {
                        data.vertHeight = heightMapOffset + heightMapDeformity * height + sphere.radius;
                    }
                }
            }
            //Debug.Log($"Height after: {data.vertHeight}");
        }
        public override void OnVertexBuild(PQS.VertexBuildData data)
        {
            //Debug.Log($"Height before: {data.vertHeight}");
            double maxU;
            double minU;
            double maxV;
            double minV;
            double rangeU;
            double rangeV;

            maxU = 1 - ((maxLong + 270) / 360);
            if (maxU < 0)
                maxU += 1;
            minU = 1 - ((minLong + 270) / 360);
            if (minU < 0)
                minU += 1;
            maxV = (maxLat + 90) / 180;
            minV = (minLat + 90) / 180;
            rangeU = minU - maxU;
            rangeV = maxV - minV;

            if (data.u < minU && data.u > maxU)
            {
                if (data.v < maxV && data.v > minV)
                {
                    double newU = 1 - (data.u - maxU) / rangeU;
                    double newV = 1 - (data.v - minV) / rangeV;



                    // Get the HeightAlpha, not the Float-Value from the Map
                    // Clamp the v value to just shy of 1 to avoid sampling issues around the north pole.
                    Color color = colorMap.GetPixelColor(newU, newV);
                    if (color.r != 0 && color.g != 0 && color.b != 0)
                    {
                        data.vertColor = color;
                    }
                }
            }

            if (allowScatters)
                data.allowScatter = true;
        }
    }
}
