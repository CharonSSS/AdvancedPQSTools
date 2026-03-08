/* 
 * This code is adapted from KopernicusExpansion-Continued
 * Available from https://github.com/StollD/KopernicusExpansion-Continued
 */

using AdvancedPQSTools.OnDemand;
using Kopernicus.Configuration;
using System;
using UnityEngine;

namespace AdvancedPQSTools
{
    /// <summary>
    /// A heightmap decal PQSMod that can parse encoded 16bpp textures
    /// </summary>
    public class PQSMod_BoundedDecalBicubic : PQSMod
    {
        public bool allowScatters = true;
        public MapSOTile heightMap;
        public MapSOTile colorMap;
        public double heightMapOffset;
        public double heightMapDeformity;
        public double maxLong;
        public double minLong;
        public double maxLat;
        public double minLat;

        private double[] PY_Height = new double[4];
        private double[] PX_Height = new double[4];
        private Color[] PY_Color = new Color[4];
        private Color[] PX_Color = new Color[4];

        private const float _n6BnC = -1 / 6.0f;
        private const float _n32BnC2 = 1 / 2.0f;
        private const float _32BCn2 = -_n32BnC2;
        private const float _6BC = -_n6BnC;
        private const float _2B2C = 0.5f;
        private const float _2BCn3 = -1;
        private const float _n52Bn2C3 = 1 / 2.0f;
        private const float _n2BnC = -0.5f;
        private const float _2BC = -_n2BnC;
        private const float _6B = 1 / 6.0f;
        private const float _n3B1 = 2 / 3.0f;

        public double RunMitchellNetravali_Height(double P0, double P1, double P2, double P3, double d)
        {
            double output = (_n6BnC * P0 + _n32BnC2 * P1 + _32BCn2 * P2 + _6BC * P3) * d * d * d
                            + (_2B2C * P0 + _2BCn3 * P1 + _n52Bn2C3 * P2) * d * d
                            + (_n2BnC * P0 + _2BC * P2) * d
                            + _6B * P0 + _n3B1 * P1 + _6B * P2;

            return output;
        }

        public Color RunMitchellNetravali_Color(Color P0, Color P1, Color P2, Color P3, float d)
        {
            Color output = (_n6BnC * P0 + _n32BnC2 * P1 + _32BCn2 * P2 + _6BC * P3) * d * d * d
                            + (_2B2C * P0 + _2BCn3 * P1 + _n52Bn2C3 * P2) * d * d
                            + (_n2BnC * P0 + _2BC * P2) * d
                            + _6B * P0 + _n3B1 * P1 + _6B * P2;

            return output;
        }

        /// <summary>
        /// Clamp an <see cref="int"/> between two values
        /// </summary>
        protected int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value >= max ? max - 1 : value);
        }

        /// <summary>
        /// Calculates a bicubic interpolated sample of the heightmap <see cref="PQSMod_VertexHeightMap.heightMap"/> 
        /// </summary>
        /// <param name="u">U coordinate of the UV to sample, as handled by KSP's PQS system</param>
        /// <param name="v">V coordinate of the UV to sample, as handled by KSP's PQS system</param>
        public double InterpolateHeights(double u, double v)
        {
            //Calculate necesary variables
            int x0 = (int)Math.Floor(u * heightMap.Width);
            int y0 = (int)Math.Floor(v * heightMap.Height);
            double u0 = x0 / (double)heightMap.Width;
            double v0 = y0 / (double)heightMap.Height;

            double uD = (u - u0) * heightMap.Width;
            double vD = (v - v0) * heightMap.Height;

            //Calculate height (Interpolate)
            for (int j = -1; j < 3; j++)
            {
                Int32 y = Clamp(y0 + j, 0, heightMap.Height);
                for (int i = -1; i < 3; i++)
                {
                    Int32 x = Clamp(x0 + i, 0, heightMap.Width);
                    PX_Height[i + 1] = heightMap.GetPixelFloat(x, y);
                }
                PY_Height[j + 1] = RunMitchellNetravali_Height(PX_Height[0], PX_Height[1], PX_Height[2], PX_Height[3], uD);
            }

            double output = RunMitchellNetravali_Height(PY_Height[0], PY_Height[1], PY_Height[2], PY_Height[3], vD);

            return output;
        }

        public Color InterpolateColors(double u, double v)
        {
            //Calculate necesary variables
            int x0 = (int)Math.Floor(u * colorMap.Width);
            int y0 = (int)Math.Floor(v * colorMap.Height);
            double u0 = x0 / (double)colorMap.Width;
            double v0 = y0 / (double)colorMap.Height;

            double uD = (u - u0) * colorMap.Width;
            double vD = (v - v0) * colorMap.Height;

            //Calculate height (Interpolate)
            for (int j = -1; j < 3; j++)
            {
                Int32 y = Clamp(y0 + j, 0, colorMap.Height);
                for (int i = -1; i < 3; i++)
                {
                    Int32 x = Clamp(x0 + i, 0, colorMap.Width);
                    PX_Color[i + 1] = colorMap.GetPixelColor(x, y);
                }
                PY_Color[j + 1] = RunMitchellNetravali_Color(PX_Color[0], PX_Color[1], PX_Color[2], PX_Color[3], (float)uD);
            }

            Color output = RunMitchellNetravali_Color(PY_Color[0], PY_Color[1], PY_Color[2], PY_Color[3], (float)vD);

            return output;
        }

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
            minU = 1 - ((minLong + 270) / 360);
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


                    double height = (double)InterpolateHeights(newU, newV);

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
            minU = 1 - ((minLong + 270) / 360);
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
                    Color color = InterpolateColors(newU, newV);
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
