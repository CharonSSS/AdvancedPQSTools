/*
 * BurstPQS adapter for PQSMod_VertexDefineCoastSmooth
 *
 * This adapter converts the smooth coastline definition PQSMod to work
 * with BurstPQS. The mod applies a 7th-order polynomial smoothstep to
 * vertex heights within a transition band around sea level, producing
 * smoother coastlines than stock VertexDefineCoast.
 *
 * No texture access is needed — this is pure arithmetic on vertHeight,
 * making it a clean Burst compilation target.
 *
 * Adapted from AdvancedPQSTools, originally based on KopernicusExpansion-Continued.
 */

using AdvancedPQSTools;
using BurstPQS;
using System;
using Unity.Burst;

namespace AdvancedPQSTools
{
    [BurstCompile]
    [BatchPQSMod(typeof(PQSMod_VertexDefineCoastSmooth))]
    public class BatchPQSMod_VertexDefineCoastSmooth
        : BatchPQSMod<PQSMod_VertexDefineCoastSmooth>
    {
        public BatchPQSMod_VertexDefineCoastSmooth(
            PQSMod_VertexDefineCoastSmooth mod) : base(mod) { }

        public override void OnQuadPreBuild(PQ quad, BatchPQSJobSet jobSet)
        {
            base.OnQuadPreBuild(quad, jobSet);

            double radius = Mod.sphere.radius;

            jobSet.Add(new BuildJob
            {
                minHeight = radius + Mod.minHeightOffset,
                maxHeight = radius + Mod.maxHeightOffset,
                slopeScale = Mod.slopeScale,
            });
        }

        [BurstCompile]
        struct BuildJob : IBatchPQSHeightJob
        {
            public double minHeight;
            public double maxHeight;
            public double slopeScale;

            public void BuildHeights(in BuildHeightsData data)
            {
                double range = maxHeight - minHeight;

                for (int i = 0; i < data.VertexCount; ++i)
                {
                    double h = data.vertHeight[i];

                    if (h <= minHeight || h >= maxHeight)
                        continue;

                    // Normalize to [0, 1], apply slope scaling around midpoint
                    double x = (h - minHeight) / range;
                    x = (x - 0.5) * slopeScale + 0.5;
                    if (x < 0.0) x = 0.0;
                    if (x > 1.0) x = 1.0;

                    // 7th-order polynomial smoothstep:
                    //   y = -20x^7 + 70x^6 - 84x^5 + 35x^4
                    double x2 = x * x;
                    double x4 = x2 * x2;
                    double x5 = x4 * x;
                    double x6 = x5 * x;
                    double x7 = x6 * x;
                    double y = -20.0 * x7 + 70.0 * x6 - 84.0 * x5 + 35.0 * x4;

                    data.vertHeight[i] = y * range + minHeight;
                }
            }
        }
    }
}
