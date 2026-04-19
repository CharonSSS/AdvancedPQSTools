/*
 * PQSMod_BoundedDecalSet.cs
 *
 * A single PQSMod that replaces N individual BoundedDecalBicubic mods with
 * one spatial-accelerated set. Uses a 1 deg lat/lon grid for O(1) candidate
 * lookup, then exact bounds check + bicubic (Mitchell-Netravali) sampling.
 *
 * DecalData is a ScriptableObject, so the decals[] array survives prefab
 * instantiation as serialized references. The grid is marked [SerializeField]
 * so it also persists across the clone.
 *
 * Texture sampling uses clamp-to-edge behavior (no wrapping).
 */

using AdvancedPQSTools.OnDemand;
using System;
using UnityEngine;

namespace AdvancedPQSTools
{
    public class PQSMod_BoundedDecalSet : PQSMod
    {
        // ─── Configuration ────────────────────────────────────────────
        public bool allowScatters = true;
        public DecalData[] decals;

        // ─── Spatial lookup grid ──────────────────────────────────────
        private const int GRID_LON = 360;
        private const int GRID_LAT = 180;
        [SerializeField] private short[] grid;

        // ─── Mitchell-Netravali constants (B=1/3, C=1/3) ─────────────
        private const double K_A3_P0 = -1.0 / 6.0;
        private const double K_A3_P1 = 1.0 / 2.0;
        private const double K_A3_P2 = -1.0 / 2.0;
        private const double K_A3_P3 = 1.0 / 6.0;
        private const double K_A2_P0 = 1.0 / 2.0;
        private const double K_A2_P1 = -1.0;
        private const double K_A2_P2 = 1.0 / 2.0;
        private const double K_A1_P0 = -1.0 / 2.0;
        private const double K_A1_P2 = 1.0 / 2.0;
        private const double K_A0_P0 = 1.0 / 6.0;
        private const double K_A0_P1 = 2.0 / 3.0;
        private const double K_A0_P2 = 1.0 / 6.0;

        // ─────────────────────────────────────────────────────────────
        //  Grid
        // ─────────────────────────────────────────────────────────────

        public void BuildGrid()
        {
            grid = new short[GRID_LON * GRID_LAT];
            for (int i = 0; i < grid.Length; i++)
                grid[i] = -1;

            if (decals == null)
                return;

            for (int d = 0; d < decals.Length; d++)
            {
                DecalData decal = decals[d];

                int lonMin = LonToBucket(decal.minLong);
                int lonMax = LonToBucket(decal.maxLong);
                int latMin = LatToBucket(decal.minLat);
                int latMax = LatToBucket(decal.maxLat);

                lonMin = Math.Max(0, Math.Min(lonMin, GRID_LON - 1));
                lonMax = Math.Max(0, Math.Min(lonMax, GRID_LON - 1));
                latMin = Math.Max(0, Math.Min(latMin, GRID_LAT - 1));
                latMax = Math.Max(0, Math.Min(latMax, GRID_LAT - 1));

                for (int lon = lonMin; lon <= lonMax; lon++)
                    for (int lat = latMin; lat <= latMax; lat++)
                        grid[lon * GRID_LAT + lat] = (short)d;
            }
        }

        private static int LonToBucket(double lon)
        {
            return (int)Math.Floor(lon) + 180;
        }

        private static int LatToBucket(double lat)
        {
            return (int)Math.Floor(lat) + 90;
        }

        private int GridLookup(double u, double v)
        {
            double lon = 360.0 * (1.0 - u) - 270.0;
            double lat = v * 180.0 - 90.0;

            int lonBucket = (int)Math.Floor(lon) + 180;
            int latBucket = (int)Math.Floor(lat) + 90;

            if (lonBucket < 0 || lonBucket >= GRID_LON ||
                latBucket < 0 || latBucket >= GRID_LAT)
                return -1;

            return grid[lonBucket * GRID_LAT + latBucket];
        }

        // ─────────────────────────────────────────────────────────────
        //  Interpolation helpers
        // ─────────────────────────────────────────────────────────────

        private static double MitchellNetravali(
            double P0, double P1, double P2, double P3, double d)
        {
            return (K_A3_P0 * P0 + K_A3_P1 * P1 + K_A3_P2 * P2 + K_A3_P3 * P3) * d * d * d
                 + (K_A2_P0 * P0 + K_A2_P1 * P1 + K_A2_P2 * P2) * d * d
                 + (K_A1_P0 * P0 + K_A1_P2 * P2) * d
                 + K_A0_P0 * P0 + K_A0_P1 * P1 + K_A0_P2 * P2;
        }

        private static Color MitchellNetravaliColor(
            Color P0, Color P1, Color P2, Color P3, float d)
        {
            return ((float)K_A3_P0 * P0 + (float)K_A3_P1 * P1
                  + (float)K_A3_P2 * P2 + (float)K_A3_P3 * P3) * d * d * d
                 + ((float)K_A2_P0 * P0 + (float)K_A2_P1 * P1
                  + (float)K_A2_P2 * P2) * d * d
                 + ((float)K_A1_P0 * P0 + (float)K_A1_P2 * P2) * d
                 + (float)K_A0_P0 * P0 + (float)K_A0_P1 * P1 + (float)K_A0_P2 * P2;
        }

        private static int ClampEdge(int value, int min, int max)
        {
            return value < min ? min : (value >= max ? max - 1 : value);
        }

        // ─────────────────────────────────────────────────────────────
        //  Bicubic sampling (clamp-to-edge)
        // ─────────────────────────────────────────────────────────────

        private static double InterpolateHeight(MapSOTile map, double u, double v)
        {
            int w = map.Width;
            int h = map.Height;

            u = Math.Max(0.0, Math.Min(u, 1.0 - 1e-10));
            v = Math.Max(0.0, Math.Min(v, 1.0 - 1e-10));

            int x0 = (int)Math.Floor(u * w);
            int y0 = (int)Math.Floor(v * h);
            double u0 = x0 / (double)w;
            double v0 = y0 / (double)h;

            double uD = (u - u0) * w;
            double vD = (v - v0) * h;

            // Read all 16 samples into a 4x4 grid. We need them upfront
            // to check for fallthrough (DN=0) pixels — if any sample in
            // the kernel is a fallthrough sentinel, the bicubic interp
            // would produce an incorrect result by blending real terrain
            // heights toward zero (which gets mapped to `offset` meters,
            // creating a visible seam). Instead, we return 0 to signal
            // fallthrough to the caller.
            double s00, s01, s02, s03;
            double s10, s11, s12, s13;
            double s20, s21, s22, s23;
            double s30, s31, s32, s33;

            {
                int y = ClampEdge(y0 - 1, 0, h);
                s00 = map.GetPixelFloat(ClampEdge(x0 - 1, 0, w), y);
                s01 = map.GetPixelFloat(ClampEdge(x0, 0, w), y);
                s02 = map.GetPixelFloat(ClampEdge(x0 + 1, 0, w), y);
                s03 = map.GetPixelFloat(ClampEdge(x0 + 2, 0, w), y);
            }
            {
                int y = ClampEdge(y0, 0, h);
                s10 = map.GetPixelFloat(ClampEdge(x0 - 1, 0, w), y);
                s11 = map.GetPixelFloat(ClampEdge(x0, 0, w), y);
                s12 = map.GetPixelFloat(ClampEdge(x0 + 1, 0, w), y);
                s13 = map.GetPixelFloat(ClampEdge(x0 + 2, 0, w), y);
            }
            {
                int y = ClampEdge(y0 + 1, 0, h);
                s20 = map.GetPixelFloat(ClampEdge(x0 - 1, 0, w), y);
                s21 = map.GetPixelFloat(ClampEdge(x0, 0, w), y);
                s22 = map.GetPixelFloat(ClampEdge(x0 + 1, 0, w), y);
                s23 = map.GetPixelFloat(ClampEdge(x0 + 2, 0, w), y);
            }
            {
                int y = ClampEdge(y0 + 2, 0, h);
                s30 = map.GetPixelFloat(ClampEdge(x0 - 1, 0, w), y);
                s31 = map.GetPixelFloat(ClampEdge(x0, 0, w), y);
                s32 = map.GetPixelFloat(ClampEdge(x0 + 1, 0, w), y);
                s33 = map.GetPixelFloat(ClampEdge(x0 + 2, 0, w), y);
            }

            // Fallthrough check: if any sample is 0, skip this vertex.
            // The 4 center samples (s11, s12, s21, s22) being zero means
            // the vertex itself is in fallthrough territory. The outer 12
            // being zero means we're within 1-2 pixels of a seam, where
            // bicubic would produce bad values.
            if (s00 == 0.0 || s01 == 0.0 || s02 == 0.0 || s03 == 0.0 ||
                s10 == 0.0 || s11 == 0.0 || s12 == 0.0 || s13 == 0.0 ||
                s20 == 0.0 || s21 == 0.0 || s22 == 0.0 || s23 == 0.0 ||
                s30 == 0.0 || s31 == 0.0 || s32 == 0.0 || s33 == 0.0)
            {
                return 0.0;
            }

            double PY0 = MitchellNetravali(s00, s01, s02, s03, uD);
            double PY1 = MitchellNetravali(s10, s11, s12, s13, uD);
            double PY2 = MitchellNetravali(s20, s21, s22, s23, uD);
            double PY3 = MitchellNetravali(s30, s31, s32, s33, uD);

            return MitchellNetravali(PY0, PY1, PY2, PY3, vD);
        }

        private static Color InterpolateColor(MapSOTile map, double u, double v)
        {
            int w = map.Width;
            int h = map.Height;

            u = Math.Max(0.0, Math.Min(u, 1.0 - 1e-10));
            v = Math.Max(0.0, Math.Min(v, 1.0 - 1e-10));

            int x0 = (int)Math.Floor(u * w);
            int y0 = (int)Math.Floor(v * h);
            double u0 = x0 / (double)w;
            double v0 = y0 / (double)h;

            float uD = (float)((u - u0) * w);
            float vD = (float)((v - v0) * h);

            Color PY0, PY1, PY2, PY3;

            {
                int y = ClampEdge(y0 - 1, 0, h);
                PY0 = MitchellNetravaliColor(
                    map.GetPixelColor(ClampEdge(x0 - 1, 0, w), y),
                    map.GetPixelColor(ClampEdge(x0, 0, w), y),
                    map.GetPixelColor(ClampEdge(x0 + 1, 0, w), y),
                    map.GetPixelColor(ClampEdge(x0 + 2, 0, w), y),
                    uD);
            }
            {
                int y = ClampEdge(y0, 0, h);
                PY1 = MitchellNetravaliColor(
                    map.GetPixelColor(ClampEdge(x0 - 1, 0, w), y),
                    map.GetPixelColor(ClampEdge(x0, 0, w), y),
                    map.GetPixelColor(ClampEdge(x0 + 1, 0, w), y),
                    map.GetPixelColor(ClampEdge(x0 + 2, 0, w), y),
                    uD);
            }
            {
                int y = ClampEdge(y0 + 1, 0, h);
                PY2 = MitchellNetravaliColor(
                    map.GetPixelColor(ClampEdge(x0 - 1, 0, w), y),
                    map.GetPixelColor(ClampEdge(x0, 0, w), y),
                    map.GetPixelColor(ClampEdge(x0 + 1, 0, w), y),
                    map.GetPixelColor(ClampEdge(x0 + 2, 0, w), y),
                    uD);
            }
            {
                int y = ClampEdge(y0 + 2, 0, h);
                PY3 = MitchellNetravaliColor(
                    map.GetPixelColor(ClampEdge(x0 - 1, 0, w), y),
                    map.GetPixelColor(ClampEdge(x0, 0, w), y),
                    map.GetPixelColor(ClampEdge(x0 + 1, 0, w), y),
                    map.GetPixelColor(ClampEdge(x0 + 2, 0, w), y),
                    uD);
            }

            return MitchellNetravaliColor(PY0, PY1, PY2, PY3, vD);
        }

        // ─────────────────────────────────────────────────────────────
        //  PQSMod overrides
        // ─────────────────────────────────────────────────────────────

        public override void OnVertexBuildHeight(PQS.VertexBuildData data)
        {
            if (decals == null || grid == null)
                return;

            int idx = GridLookup(data.u, data.v);
            if (idx < 0)
                return;

            DecalData decal = decals[idx];

            if (data.u >= decal.minU || data.u <= decal.maxU)
                return;
            if (data.v >= decal.maxV || data.v <= decal.minV)
                return;

            if (decal.heightMap == null)
                return;

            double newU = 1.0 - (data.u - decal.maxU) / decal.rangeU;
            double newV = 1.0 - (data.v - decal.minV) / decal.rangeV;

            double height = InterpolateHeight(decal.heightMap, newU, newV);

            if (height != 0.0)
            {
                data.vertHeight = decal.offset + decal.deformity * height + sphere.radius;
            }
        }

        public override void OnVertexBuild(PQS.VertexBuildData data)
        {
            if (decals == null || grid == null)
                return;

            int idx = GridLookup(data.u, data.v);
            if (idx < 0)
                return;

            DecalData decal = decals[idx];

            if (data.u >= decal.minU || data.u <= decal.maxU)
                return;
            if (data.v >= decal.maxV || data.v <= decal.minV)
                return;

            if (decal.colorMap != null)
            {
                double newU = 1.0 - (data.u - decal.maxU) / decal.rangeU;
                double newV = 1.0 - (data.v - decal.minV) / decal.rangeV;

                Color color = InterpolateColor(decal.colorMap, newU, newV);

                if (color.r != 0f && color.g != 0f && color.b != 0f)
                {
                    data.vertColor = color;
                }
            }

            if (allowScatters)
                data.allowScatter = true;
        }
    }
}