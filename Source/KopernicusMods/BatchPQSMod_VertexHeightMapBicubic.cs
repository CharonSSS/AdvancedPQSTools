/*
 * BurstPQS adapter for PQSMod_VertexMitchellNetravaliHeightMap16
 *
 * This adapter preserves the configurable Mitchell-Netravali bicubic
 * interpolation (arbitrary B/C parameters) from Niako's original mod,
 * while running the per-vertex work on background threads with Burst
 * compilation.
 *
 * Key differences from the original:
 *   - B/C constants are snapshotted in OnQuadPreBuild (main thread)
 *     and baked into the job struct, avoiding shared mutable state.
 *   - The 4x4 sampling loop is unrolled into scalar locals (no managed
 *     arrays) to satisfy Burst/HPC# constraints.
 *   - GetPixelFloat(int, int) replaces HeightAlpha decoding.
 *   - U-axis uses wrap-around (ClampLoop) matching the original; V-axis
 *     uses edge-clamp matching the original.
 *
 * Derived from Niako's Heightmap utils (MIT License).
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
    [BatchPQSMod(typeof(PQSMod_VertexHeightMapBicubic))]
    public class BatchPQSMod_VertexHeightMapBicubic
        : BatchPQSMod<PQSMod_VertexHeightMapBicubic>
    {
        public BatchPQSMod_VertexHeightMapBicubic(
            PQSMod_VertexHeightMapBicubic mod) : base(mod) { }

        public override void OnQuadPreBuild(PQ quad, BatchPQSJobSet jobSet)
        {
            base.OnQuadPreBuild(quad, jobSet);

            // Snapshot B/C and precompute the 11 filter constants on the
            // main thread, exactly as PrecalculateConstants() does.
            double B = Mod.B;
            double C = Mod.C;

            jobSet.Add(new BuildJob
            {
                heightMap = BurstMapSO.Create(Mod.heightMap),
                heightMapOffset = Mod.heightMapOffset,
                heightMapDeformity = Mod.heightMapDeformity,

                // Mitchell-Netravali constants (parameterized by B, C)
                k_a3_p0 = (-1.0 / 6.0) * B - C,
                k_a3_p1 = (-3.0 / 2.0) * B - C + 2.0,
                k_a3_p2 = (3.0 / 2.0) * B + C - 2.0,
                k_a3_p3 = (1.0 / 6.0) * B + C,
                k_a2_p0 = 0.5 * B + 2.0 * C,
                k_a2_p1 = 2.0 * B + C - 3.0,
                k_a2_p2 = (-5.0 / 2.0) * B - 2.0 * C + 3.0,
                k_a2_p3 = -C,
                k_a1_p0 = -0.5 * B - C,
                k_a1_p2 = 0.5 * B + C,
                k_a0_p0 = (1.0 / 6.0) * B,
                k_a0_p1 = (-1.0 / 3.0) * B + 1.0,
                k_a0_p2 = (1.0 / 6.0) * B,
            });
        }

        [BurstCompile]
        struct BuildJob : IBatchPQSHeightJob, IDisposable
        {
            public BurstMapSO heightMap;
            public double heightMapOffset;
            public double heightMapDeformity;

            // ─── Mitchell-Netravali coefficients (per B/C) ───────────────
            // f(d) = (a3·P)d³ + (a2·P)d² + (a1·P)d + (a0·P)
            public double k_a3_p0, k_a3_p1, k_a3_p2, k_a3_p3;
            public double k_a2_p0, k_a2_p1, k_a2_p2, k_a2_p3;
            public double k_a1_p0, k_a1_p2;
            public double k_a0_p0, k_a0_p1, k_a0_p2;

            private static double MitchellNetravali(
                double P0, double P1, double P2, double P3, double d,
                double a3p0, double a3p1, double a3p2, double a3p3,
                double a2p0, double a2p1, double a2p2, double a2p3,
                double a1p0, double a1p2,
                double a0p0, double a0p1, double a0p2)
            {
                return (a3p0 * P0 + a3p1 * P1 + a3p2 * P2 + a3p3 * P3) * d * d * d
                     + (a2p0 * P0 + a2p1 * P1 + a2p2 * P2 + a2p3 * P3) * d * d
                     + (a1p0 * P0 + a1p2 * P2) * d
                     + a0p0 * P0 + a0p1 * P1 + a0p2 * P2;
            }

            /// <summary>
            /// Clamp with wrap-around for the U (horizontal) axis.
            /// Matches the original ClampLoop behavior.
            /// </summary>
            private static int ClampLoop(int value, int min, int max)
            {
                int d = max - min;
                return value < min ? value + d : (value >= max ? value - d : value);
            }

            /// <summary>
            /// Edge-clamp for the V (vertical) axis.
            /// </summary>
            private static int Clamp(int value, int min, int max)
            {
                return value < min ? min : (value >= max ? max - 1 : value);
            }

            private static double InterpolateHeight(
                BurstMapSO map, double u, double v,
                double a3p0, double a3p1, double a3p2, double a3p3,
                double a2p0, double a2p1, double a2p2, double a2p3,
                double a1p0, double a1p2,
                double a0p0, double a0p1, double a0p2)
            {
                int w = map.Width;
                int h = map.Height;

                int x0 = (int)Math.Floor(u * w);
                int y0 = (int)Math.Floor(v * h);
                double u0 = x0 / (double)w;
                double v0 = y0 / (double)h;

                double uD = (u - u0) * w;
                double vD = (v - v0) * h;

                double PY0, PY1, PY2, PY3;

                {
                    int y = Clamp(y0 - 1, 0, h);
                    PY0 = MitchellNetravali(
                        map.GetPixelFloat(ClampLoop(x0 - 1, 0, w), y),
                        map.GetPixelFloat(ClampLoop(x0, 0, w), y),
                        map.GetPixelFloat(ClampLoop(x0 + 1, 0, w), y),
                        map.GetPixelFloat(ClampLoop(x0 + 2, 0, w), y),
                        uD, a3p0, a3p1, a3p2, a3p3, a2p0, a2p1, a2p2, a2p3, a1p0, a1p2, a0p0, a0p1, a0p2);
                }
                {
                    int y = Clamp(y0, 0, h);
                    PY1 = MitchellNetravali(
                        map.GetPixelFloat(ClampLoop(x0 - 1, 0, w), y),
                        map.GetPixelFloat(ClampLoop(x0, 0, w), y),
                        map.GetPixelFloat(ClampLoop(x0 + 1, 0, w), y),
                        map.GetPixelFloat(ClampLoop(x0 + 2, 0, w), y),
                        uD, a3p0, a3p1, a3p2, a3p3, a2p0, a2p1, a2p2, a2p3, a1p0, a1p2, a0p0, a0p1, a0p2);
                }
                {
                    int y = Clamp(y0 + 1, 0, h);
                    PY2 = MitchellNetravali(
                        map.GetPixelFloat(ClampLoop(x0 - 1, 0, w), y),
                        map.GetPixelFloat(ClampLoop(x0, 0, w), y),
                        map.GetPixelFloat(ClampLoop(x0 + 1, 0, w), y),
                        map.GetPixelFloat(ClampLoop(x0 + 2, 0, w), y),
                        uD, a3p0, a3p1, a3p2, a3p3, a2p0, a2p1, a2p2, a2p3, a1p0, a1p2, a0p0, a0p1, a0p2);
                }
                {
                    int y = Clamp(y0 + 2, 0, h);
                    PY3 = MitchellNetravali(
                        map.GetPixelFloat(ClampLoop(x0 - 1, 0, w), y),
                        map.GetPixelFloat(ClampLoop(x0, 0, w), y),
                        map.GetPixelFloat(ClampLoop(x0 + 1, 0, w), y),
                        map.GetPixelFloat(ClampLoop(x0 + 2, 0, w), y),
                        uD, a3p0, a3p1, a3p2, a3p3, a2p0, a2p1, a2p2, a2p3, a1p0, a1p2, a0p0, a0p1, a0p2);
                }

                return MitchellNetravali(PY0, PY1, PY2, PY3, vD,
                    a3p0, a3p1, a3p2, a3p3, a2p0, a2p1, a2p2, a2p3, a1p0, a1p2, a0p0, a0p1, a0p2);
            }

            public void BuildHeights(in BuildHeightsData data)
            {
                if (!heightMap.IsValid)
                    return;

                for (int i = 0; i < data.VertexCount; ++i)
                {
                    data.vertHeight[i] += heightMapOffset
                        + heightMapDeformity * InterpolateHeight(
                            heightMap, data.u[i], data.v[i],
                            k_a3_p0, k_a3_p1, k_a3_p2, k_a3_p3,
                            k_a2_p0, k_a2_p1, k_a2_p2, k_a2_p3,
                            k_a1_p0, k_a1_p2,
                            k_a0_p0, k_a0_p1, k_a0_p2);
                }
            }

            public void Dispose()
            {
                heightMap.Dispose();
            }
        }
    }
}