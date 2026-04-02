/*
 * BurstPQS adapter for PQSMod_BoundedDecal
 *
 * This adapter bridges the bounded decal heightmap/colormap PQSMod with
 * BurstPQS's batched vertex processing pipeline. Unlike the Bicubic
 * variant, this mod uses standard bilinear-interpolated MapSO sampling
 * (via GetPixelFloat and GetPixelColor with normalized UV coords).
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
    [BatchPQSMod(typeof(PQSMod_BoundedDecal))]
    public class BatchPQSMod_BoundedDecal : BatchPQSMod<PQSMod_BoundedDecal>
    {
        public BatchPQSMod_BoundedDecal(PQSMod_BoundedDecal mod) : base(mod) { }

        public override void OnQuadPreBuild(PQ quad, BatchPQSJobSet jobSet)
        {
            base.OnQuadPreBuild(quad, jobSet);

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
            //  IBatchPQSHeightJob — samples height via GetPixelFloat
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

                    double height = heightMap.GetPixelFloat(newU, newV);

                    if (height != 0.0)
                    {
                        data.vertHeight[i] = decalMapOffset
                            + decalMapDeformity * height
                            + sphereRadius;
                    }
                }
            }

            // ─────────────────────────────────────────────────────────────
            //  IBatchPQSVertexJob — applies colormap decal + scatter flag
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

                        Color color = colorMap.GetPixelColor(newU, newV);

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
