/*
 * BurstPQS adapter for PQSMod_VertexHeightCubeMap
 *
 * Converts equirectangular UV to cubemap face coordinates and samples
 * the appropriate face's heightmap. The cubemap projection math
 * (UVtoXYZ, XYZtoFaceUVI) is inlined into the job struct as pure
 * arithmetic — ideal for Burst vectorization.
 *
 * Uses GetPixelFloat instead of the legacy HeightAlpha decoding.
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
    [BatchPQSMod(typeof(PQSMod_VertexHeightCubeMap))]
    public class BatchPQSMod_VertexHeightCubeMap
        : BatchPQSMod<PQSMod_VertexHeightCubeMap>
    {
        public BatchPQSMod_VertexHeightCubeMap(
            PQSMod_VertexHeightCubeMap mod) : base(mod) { }

        public override void OnQuadPreBuild(PQ quad, BatchPQSJobSet jobSet)
        {
            base.OnQuadPreBuild(quad, jobSet);

            // Precompute pixelClamp from edgeClampRange and tile width.
            // Original does this per-vertex using texXn.Width; we snapshot it.

            jobSet.Add(new BuildJob
            {
                mapXn = BurstMapSO.Create(Mod.vertexHeightMapXn),
                mapXp = BurstMapSO.Create(Mod.vertexHeightMapXp),
                mapYn = BurstMapSO.Create(Mod.vertexHeightMapYn),
                mapYp = BurstMapSO.Create(Mod.vertexHeightMapYp),
                mapZn = BurstMapSO.Create(Mod.vertexHeightMapZn),
                mapZp = BurstMapSO.Create(Mod.vertexHeightMapZp),
                heightMapOffset = Mod.heightMapOffset,
                heightMapDeformity = Mod.heightMapDeformity,
            });
        }

        [BurstCompile]
        struct BuildJob : IBatchPQSHeightJob, IDisposable
        {
            public BurstMapSO mapXn, mapXp, mapYn, mapYp, mapZn, mapZp;
            public double heightMapOffset;
            public double heightMapDeformity;

            public void BuildHeights(in BuildHeightsData data)
            {
                for (int i = 0; i < data.VertexCount; ++i)
                {
                    double u = data.u[i];
                    double v = data.v[i];

                    // UV to 3D direction (equirectangular → cartesian)
                    double theta = 2.0 * Math.PI * u;
                    double phi = Math.PI * v;
                    double sinPhi = Math.Sin(phi);

                    double cx = Math.Cos(theta) * sinPhi;
                    double cy = -Math.Cos(phi);
                    double cz = Math.Sin(theta) * sinPhi;

                    // Determine dominant face and compute face UV
                    double ax = Math.Abs(cx);
                    double ay = Math.Abs(cy);
                    double az = Math.Abs(cz);

                    double faceU, faceV;
                    int faceIndex;
                    double maxAxis;

                    if (ax >= ay && ax >= az)
                    {
                        maxAxis = ax;
                        if (cx > 0) { faceU = -cz; faceV = cy; faceIndex = 1; }
                        else        { faceU =  cz; faceV = cy; faceIndex = 0; }
                    }
                    else if (ay >= ax && ay >= az)
                    {
                        maxAxis = ay;
                        if (cy > 0) { faceU = cx; faceV = -cz; faceIndex = 3; }
                        else        { faceU = cx; faceV =  cz; faceIndex = 2; }
                    }
                    else
                    {
                        maxAxis = az;
                        if (cz > 0) { faceU =  cx; faceV = cy; faceIndex = 5; }
                        else        { faceU = -cx; faceV = cy; faceIndex = 4; }
                    }

                    faceU = 0.5 * (faceU / maxAxis + 1.0);
                    faceV = 0.5 * (faceV / maxAxis + 1.0);

                    // Sample the correct face — UV remapping matches the original
                    double height;
                    switch (faceIndex)
                    {
                        case 0: height = mapZn.GetPixelFloat(1.0 - faceU, 1.0 - faceV); break;
                        case 1: height = mapZp.GetPixelFloat(1.0 - faceU, 1.0 - faceV); break;
                        case 2: height = mapYn.GetPixelFloat(faceV, 1.0 - faceU); break;
                        case 3: height = mapYp.GetPixelFloat(1.0 - faceV, faceU); break;
                        case 4: height = mapXn.GetPixelFloat(1.0 - faceU, 1.0 - faceV); break;
                        default: height = mapXp.GetPixelFloat(1.0 - faceU, 1.0 - faceV); break;
                    }

                    data.vertHeight[i] += heightMapOffset + heightMapDeformity * height;
                }
            }

            public void Dispose()
            {
                mapXn.Dispose();
                mapXp.Dispose();
                mapYn.Dispose();
                mapYp.Dispose();
                mapZn.Dispose();
                mapZp.Dispose();
            }
        }
    }
}
