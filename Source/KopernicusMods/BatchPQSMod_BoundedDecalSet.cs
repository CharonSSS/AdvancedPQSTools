/*
 * BatchPQSMod_BoundedDecalSet.cs
 *
 * BurstPQS adapter for PQSMod_BoundedDecalSet.
 * Uses NativeArray grid + NativeArray<BurstMapSO> for full Burst sampling.
 */

using AdvancedPQSTools;
using BurstPQS;
using BurstPQS.Map;
using System;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace AdvancedPQSTools
{
    [BurstCompile]
    [BatchPQSMod(typeof(PQSMod_BoundedDecalSet))]
    public class BatchPQSMod_BoundedDecalSet : BatchPQSMod<PQSMod_BoundedDecalSet>
    {
        public BatchPQSMod_BoundedDecalSet(PQSMod_BoundedDecalSet mod) : base(mod) { }

        public override void OnQuadPreBuild(PQ quad, BatchPQSJobSet jobSet)
        {
            base.OnQuadPreBuild(quad, jobSet);

            if (Mod.decals == null || Mod.decals.Length == 0)
                return;

            int count = Mod.decals.Length;

            var gridNative = new NativeArray<short>(360 * 180, Allocator.TempJob);
            var maxU = new NativeArray<double>(count, Allocator.TempJob);
            var minU = new NativeArray<double>(count, Allocator.TempJob);
            var maxV = new NativeArray<double>(count, Allocator.TempJob);
            var minV = new NativeArray<double>(count, Allocator.TempJob);
            var rangeU = new NativeArray<double>(count, Allocator.TempJob);
            var rangeV = new NativeArray<double>(count, Allocator.TempJob);
            var offsets = new NativeArray<double>(count, Allocator.TempJob);
            var deformities = new NativeArray<double>(count, Allocator.TempJob);
            var heightMaps = new NativeArray<BurstMapSO>(count, Allocator.TempJob);
            var colorMaps = new NativeArray<BurstMapSO>(count, Allocator.TempJob);
            var hasHeight = new NativeArray<bool>(count, Allocator.TempJob);
            var hasColor = new NativeArray<bool>(count, Allocator.TempJob);

            short[] managedGrid = BuildGrid(Mod.decals);
            NativeArray<short>.Copy(managedGrid, gridNative, managedGrid.Length);

            for (int i = 0; i < count; i++)
            {
                DecalData d = Mod.decals[i];
                maxU[i] = d.maxU;
                minU[i] = d.minU;
                maxV[i] = d.maxV;
                minV[i] = d.minV;
                rangeU[i] = d.rangeU;
                rangeV[i] = d.rangeV;
                offsets[i] = d.offset;
                deformities[i] = d.deformity;

                bool hHeight = d.heightMap != null;
                bool hColor = d.colorMap != null;
                hasHeight[i] = hHeight;
                hasColor[i] = hColor;

                heightMaps[i] = hHeight ? BurstMapSO.Create(d.heightMap) : default;
                colorMaps[i] = hColor ? BurstMapSO.Create(d.colorMap) : default;
            }

            var job = new BuildJob
            {
                grid = gridNative,
                maxU = maxU,
                minU = minU,
                maxV = maxV,
                minV = minV,
                rangeU = rangeU,
                rangeV = rangeV,
                offsets = offsets,
                deformities = deformities,
                heightMaps = heightMaps,
                colorMaps = colorMaps,
                hasHeight = hasHeight,
                hasColor = hasColor,
                decalCount = count,
                sphereRadius = Mod.sphere.radius,
                allowScatters = Mod.allowScatters,
            };

            jobSet.Add(job);
        }

        private static short[] BuildGrid(DecalData[] decals)
        {
            const int GRID_LON = 360;
            const int GRID_LAT = 180;
            short[] grid = new short[GRID_LON * GRID_LAT];
            for (int i = 0; i < grid.Length; i++)
                grid[i] = -1;

            for (int d = 0; d < decals.Length; d++)
            {
                int lonMin = Math.Max(0, Math.Min((int)Math.Floor(decals[d].minLong) + 180, GRID_LON - 1));
                int lonMax = Math.Max(0, Math.Min((int)Math.Floor(decals[d].maxLong) + 180, GRID_LON - 1));
                int latMin = Math.Max(0, Math.Min((int)Math.Floor(decals[d].minLat) + 90, GRID_LAT - 1));
                int latMax = Math.Max(0, Math.Min((int)Math.Floor(decals[d].maxLat) + 90, GRID_LAT - 1));

                for (int lon = lonMin; lon <= lonMax; lon++)
                    for (int lat = latMin; lat <= latMax; lat++)
                        grid[lon * GRID_LAT + lat] = (short)d;
            }

            return grid;
        }

        [BurstCompile]
        struct BuildJob : IBatchPQSHeightJob, IBatchPQSVertexJob, IDisposable
        {
            [ReadOnly] public NativeArray<short> grid;
            [ReadOnly] public NativeArray<double> maxU;
            [ReadOnly] public NativeArray<double> minU;
            [ReadOnly] public NativeArray<double> maxV;
            [ReadOnly] public NativeArray<double> minV;
            [ReadOnly] public NativeArray<double> rangeU;
            [ReadOnly] public NativeArray<double> rangeV;
            [ReadOnly] public NativeArray<double> offsets;
            [ReadOnly] public NativeArray<double> deformities;
            [ReadOnly] public NativeArray<BurstMapSO> heightMaps;
            [ReadOnly] public NativeArray<BurstMapSO> colorMaps;
            [ReadOnly] public NativeArray<bool> hasHeight;
            [ReadOnly] public NativeArray<bool> hasColor;

            public int decalCount;
            public double sphereRadius;
            public bool allowScatters;

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

            private const int GRID_LON = 360;
            private const int GRID_LAT = 180;

            private static double MN(double P0, double P1, double P2, double P3, double d)
            {
                return (K_A3_P0 * P0 + K_A3_P1 * P1 + K_A3_P2 * P2 + K_A3_P3 * P3) * d * d * d
                     + (K_A2_P0 * P0 + K_A2_P1 * P1 + K_A2_P2 * P2) * d * d
                     + (K_A1_P0 * P0 + K_A1_P2 * P2) * d
                     + K_A0_P0 * P0 + K_A0_P1 * P1 + K_A0_P2 * P2;
            }

            private static Color MNColor(Color P0, Color P1, Color P2, Color P3, float d)
            {
                return ((float)K_A3_P0 * P0 + (float)K_A3_P1 * P1
                      + (float)K_A3_P2 * P2 + (float)K_A3_P3 * P3) * d * d * d
                     + ((float)K_A2_P0 * P0 + (float)K_A2_P1 * P1
                      + (float)K_A2_P2 * P2) * d * d
                     + ((float)K_A1_P0 * P0 + (float)K_A1_P2 * P2) * d
                     + (float)K_A0_P0 * P0 + (float)K_A0_P1 * P1 + (float)K_A0_P2 * P2;
            }

            private static int Clamp(int value, int min, int max)
            {
                return value < min ? min : (value >= max ? max - 1 : value);
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

            private static double InterpolateHeight(BurstMapSO map, double u, double v)
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

                double PY0, PY1, PY2, PY3;

                {
                    int y = Clamp(y0 - 1, 0, h);
                    PY0 = MN(
                        map.GetPixelFloat(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 2, 0, w), y), uD);
                }
                {
                    int y = Clamp(y0, 0, h);
                    PY1 = MN(
                        map.GetPixelFloat(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 2, 0, w), y), uD);
                }
                {
                    int y = Clamp(y0 + 1, 0, h);
                    PY2 = MN(
                        map.GetPixelFloat(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 2, 0, w), y), uD);
                }
                {
                    int y = Clamp(y0 + 2, 0, h);
                    PY3 = MN(
                        map.GetPixelFloat(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 2, 0, w), y), uD);
                }

                return MN(PY0, PY1, PY2, PY3, vD);
            }

            private static Color InterpolateColor(BurstMapSO map, double u, double v)
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
                    int y = Clamp(y0 - 1, 0, h);
                    PY0 = MNColor(
                        map.GetPixelColor(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 2, 0, w), y), uD);
                }
                {
                    int y = Clamp(y0, 0, h);
                    PY1 = MNColor(
                        map.GetPixelColor(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 2, 0, w), y), uD);
                }
                {
                    int y = Clamp(y0 + 1, 0, h);
                    PY2 = MNColor(
                        map.GetPixelColor(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 2, 0, w), y), uD);
                }
                {
                    int y = Clamp(y0 + 2, 0, h);
                    PY3 = MNColor(
                        map.GetPixelColor(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 2, 0, w), y), uD);
                }

                return MNColor(PY0, PY1, PY2, PY3, vD);
            }

            public void BuildHeights(in BuildHeightsData data)
            {
                for (int i = 0; i < data.VertexCount; ++i)
                {
                    double u = data.u[i];
                    double v = data.v[i];

                    int idx = GridLookup(u, v);
                    if (idx < 0) continue;

                    if (u >= minU[idx] || u <= maxU[idx]) continue;
                    if (v >= maxV[idx] || v <= minV[idx]) continue;

                    if (!hasHeight[idx]) continue;

                    BurstMapSO map = heightMaps[idx];
                    if (!map.IsValid) continue;

                    double newU = 1.0 - (u - maxU[idx]) / rangeU[idx];
                    double newV = 1.0 - (v - minV[idx]) / rangeV[idx];

                    double height = InterpolateHeight(map, newU, newV);

                    if (height != 0.0)
                    {
                        data.vertHeight[i] = offsets[idx]
                            + deformities[idx] * height
                            + sphereRadius;
                    }
                }
            }

            public void BuildVertices(in BuildVerticesData data)
            {
                for (int i = 0; i < data.VertexCount; ++i)
                {
                    double u = data.u[i];
                    double v = data.v[i];

                    int idx = GridLookup(u, v);

                    if (idx >= 0)
                    {
                        if (!(u >= minU[idx] || u <= maxU[idx]) &&
                            !(v >= maxV[idx] || v <= minV[idx]))
                        {
                            if (hasColor[idx])
                            {
                                BurstMapSO map = colorMaps[idx];
                                if (map.IsValid)
                                {
                                    double newU = 1.0 - (u - maxU[idx]) / rangeU[idx];
                                    double newV = 1.0 - (v - minV[idx]) / rangeV[idx];

                                    Color color = InterpolateColor(map, newU, newV);

                                    if (color.r != 0f && color.g != 0f && color.b != 0f)
                                    {
                                        data.vertColor[i] = color;
                                    }
                                }
                            }
                        }
                    }

                    if (allowScatters)
                        data.allowScatter[i] = true;
                }
            }

            public void Dispose()
            {
                for (int i = 0; i < heightMaps.Length; i++)
                {
                    if (hasHeight[i])
                        heightMaps[i].Dispose();
                }
                for (int i = 0; i < colorMaps.Length; i++)
                {
                    if (hasColor[i])
                        colorMaps[i].Dispose();
                }

                grid.Dispose();
                maxU.Dispose();
                minU.Dispose();
                maxV.Dispose();
                minV.Dispose();
                rangeU.Dispose();
                rangeV.Dispose();
                offsets.Dispose();
                deformities.Dispose();
                heightMaps.Dispose();
                colorMaps.Dispose();
                hasHeight.Dispose();
                hasColor.Dispose();
            }
        }
    }
}