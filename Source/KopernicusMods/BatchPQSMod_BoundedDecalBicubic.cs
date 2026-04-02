/*
 * BurstPQS adapter for PQSMod_BoundedDecalBicubic
 *
 * This adapter bridges the bounded decal bicubic heightmap/colormap PQSMod
 * with BurstPQS's batched vertex processing pipeline. It implements both
 * IBatchPQSHeightJob (for heightmap application) and IBatchPQSVertexJob
 * (for colormap application), with full Burst compilation support.
 *
 * The original PQSMod applies a Mitchell-Netravali bicubic-interpolated
 * heightmap and colormap decal within a bounded lat/lon region. This adapter
 * preserves that behavior while processing all vertices in a quad at once
 * on a background thread.
 *
 * MapSOTile's BurstMapSO factory is registered via its static constructor,
 * so BurstMapSO.Create(MapSO) resolves MapSOTile instances automatically.
 *
 * Adapted from AdvancedPQSTools, originally based on KopernicusExpansion-Continued.
 */

using AdvancedPQSTools;
using BurstPQS;
using BurstPQS.Map;
using System;
using Unity.Burst;
using UnityEngine;

namespace AdvancedPQSTools
{
    [BurstCompile]
    [BatchPQSMod(typeof(PQSMod_BoundedDecalBicubic))]
    public class BatchPQSMod_BoundedDecalBicubic : BatchPQSMod<PQSMod_BoundedDecalBicubic>
    {
        public BatchPQSMod_BoundedDecalBicubic(PQSMod_BoundedDecalBicubic mod) : base(mod) { }

        public override void OnQuadPreBuild(PQ quad, BatchPQSJobSet jobSet)
        {
            base.OnQuadPreBuild(quad, jobSet);

            // Precompute the UV bounds from lat/lon once on the main thread.
            double maxU = 1.0 - ((Mod.maxLong + 270.0) / 360.0);
            double minU = 1.0 - ((Mod.minLong + 270.0) / 360.0);
            double maxV = (Mod.maxLat + 90.0) / 180.0;
            double minV = (Mod.minLat + 90.0) / 180.0;
            double rangeU = minU - maxU;
            double rangeV = maxV - minV;

            bool hasHeightMap = Mod.heightMap != null;
            bool hasColorMap = Mod.colorMap != null;

            var job = new BuildJob
            {
                maxU = maxU,
                minU = minU,
                maxV = maxV,
                minV = minV,
                rangeU = rangeU,
                rangeV = rangeV,
                decalMapOffset = Mod.decalMapOffset,
                decalMapDeformity = Mod.decalMapDeformity,
                sphereRadius = Mod.sphere.radius,
                allowScatters = Mod.allowScatters,
                hasHeightMap = hasHeightMap,
                hasColorMap = hasColorMap,
            };

            // BurstMapSO.Create(MapSO) dispatches through the registry.
            // MapSOTile's static constructor registers its own factory, so
            // this resolves correctly for both MapSOTile and stock MapSO types.
            if (hasHeightMap)
                job.heightMap = BurstMapSO.Create(Mod.heightMap);

            if (hasColorMap)
                job.colorMap = BurstMapSO.Create(Mod.colorMap);

            jobSet.Add(job);
        }

        [BurstCompile]
        struct BuildJob : IBatchPQSHeightJob, IBatchPQSVertexJob, IDisposable
        {
            // ─── UV bounding region (precomputed from lat/lon) ───────────
            public double maxU;
            public double minU;
            public double maxV;
            public double minV;
            public double rangeU;
            public double rangeV;

            // ─── Height parameters ───────────────────────────────────────
            public double decalMapOffset;
            public double decalMapDeformity;
            public double sphereRadius;

            // ─── Feature flags ───────────────────────────────────────────
            public bool allowScatters;
            public bool hasHeightMap;
            public bool hasColorMap;

            // ─── Burst-compatible map accessors ──────────────────────────
            public BurstMapSO heightMap;
            public BurstMapSO colorMap;

            // ─────────────────────────────────────────────────────────────
            //  Mitchell-Netravali constants (B = 1/3, C = 1/3)
            // ─────────────────────────────────────────────────────────────
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

            private static int Clamp(int value, int min, int max)
            {
                return value < min ? min : (value >= max ? max - 1 : value);
            }

            // ─────────────────────────────────────────────────────────────
            //  Bicubic height interpolation via GetPixelFloat(int, int)
            // ─────────────────────────────────────────────────────────────
            private static double InterpolateHeight(BurstMapSO map, double u, double v)
            {
                int w = map.Width;
                int h = map.Height;

                int x0 = (int)Math.Floor(u * w);
                int y0 = (int)Math.Floor(v * h);
                double u0 = x0 / (double)w;
                double v0 = y0 / (double)h;

                double uD = (u - u0) * w;
                double vD = (v - v0) * h;

                // 4 rows of horizontal bicubic, then vertical bicubic.
                // Fully unrolled — no managed arrays (Burst/HPC# requirement).
                double PY0, PY1, PY2, PY3;

                {
                    int y = Clamp(y0 - 1, 0, h);
                    PY0 = MitchellNetravali(
                        map.GetPixelFloat(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 2, 0, w), y),
                        uD);
                }
                {
                    int y = Clamp(y0, 0, h);
                    PY1 = MitchellNetravali(
                        map.GetPixelFloat(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 2, 0, w), y),
                        uD);
                }
                {
                    int y = Clamp(y0 + 1, 0, h);
                    PY2 = MitchellNetravali(
                        map.GetPixelFloat(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 2, 0, w), y),
                        uD);
                }
                {
                    int y = Clamp(y0 + 2, 0, h);
                    PY3 = MitchellNetravali(
                        map.GetPixelFloat(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelFloat(Clamp(x0 + 2, 0, w), y),
                        uD);
                }

                return MitchellNetravali(PY0, PY1, PY2, PY3, vD);
            }

            // ─────────────────────────────────────────────────────────────
            //  Bicubic color interpolation via GetPixelColor(int, int)
            // ─────────────────────────────────────────────────────────────
            private static Color InterpolateColor(BurstMapSO map, double u, double v)
            {
                int w = map.Width;
                int h = map.Height;

                int x0 = (int)Math.Floor(u * w);
                int y0 = (int)Math.Floor(v * h);
                double u0 = x0 / (double)w;
                double v0 = y0 / (double)h;

                float uD = (float)((u - u0) * w);
                float vD = (float)((v - v0) * h);

                Color PY0, PY1, PY2, PY3;

                {
                    int y = Clamp(y0 - 1, 0, h);
                    PY0 = MitchellNetravaliColor(
                        map.GetPixelColor(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 2, 0, w), y),
                        uD);
                }
                {
                    int y = Clamp(y0, 0, h);
                    PY1 = MitchellNetravaliColor(
                        map.GetPixelColor(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 2, 0, w), y),
                        uD);
                }
                {
                    int y = Clamp(y0 + 1, 0, h);
                    PY2 = MitchellNetravaliColor(
                        map.GetPixelColor(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 2, 0, w), y),
                        uD);
                }
                {
                    int y = Clamp(y0 + 2, 0, h);
                    PY3 = MitchellNetravaliColor(
                        map.GetPixelColor(Clamp(x0 - 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 1, 0, w), y),
                        map.GetPixelColor(Clamp(x0 + 2, 0, w), y),
                        uD);
                }

                return MitchellNetravaliColor(PY0, PY1, PY2, PY3, vD);
            }

            // ─────────────────────────────────────────────────────────────
            //  IBatchPQSHeightJob
            // ─────────────────────────────────────────────────────────────
            public void BuildHeights(in BuildHeightsData data)
            {
                if (!hasHeightMap || !heightMap.IsValid)
                    return;

                for (int i = 0; i < data.VertexCount; ++i)
                {
                    double u = data.u[i];
                    double v = data.v[i];

                    if (u >= minU || u <= maxU)
                        continue;
                    if (v >= maxV || v <= minV)
                        continue;

                    double newU = 1.0 - (u - maxU) / rangeU;
                    double newV = 1.0 - (v - minV) / rangeV;

                    double height = InterpolateHeight(heightMap, newU, newV);

                    if (height != 0.0)
                    {
                        data.vertHeight[i] = decalMapOffset
                            + decalMapDeformity * height
                            + sphereRadius;
                    }
                }
            }

            // ─────────────────────────────────────────────────────────────
            //  IBatchPQSVertexJob
            // ─────────────────────────────────────────────────────────────
            public void BuildVertices(in BuildVerticesData data)
            {
                if (hasColorMap && colorMap.IsValid)
                {
                    for (int i = 0; i < data.VertexCount; ++i)
                    {
                        double u = data.u[i];
                        double v = data.v[i];

                        if (u >= minU || u <= maxU)
                            continue;
                        if (v >= maxV || v <= minV)
                            continue;

                        double newU = 1.0 - (u - maxU) / rangeU;
                        double newV = 1.0 - (v - minV) / rangeV;

                        Color color = InterpolateColor(colorMap, newU, newV);

                        if (color.r != 0f && color.g != 0f && color.b != 0f)
                        {
                            data.vertColor[i] = color;
                        }
                    }
                }

                if (allowScatters)
                {
                    for (int i = 0; i < data.VertexCount; ++i)
                    {
                        data.allowScatter[i] = true;
                    }
                }
            }

            // ─────────────────────────────────────────────────────────────
            //  IDisposable
            // ─────────────────────────────────────────────────────────────
            public void Dispose()
            {
                if (hasHeightMap)
                    heightMap.Dispose();
                if (hasColorMap)
                    colorMap.Dispose();
            }
        }
    }
}