/*
 * BurstPQS adapter for PQSMod_VertexColorCubeMap
 *
 * Converts equirectangular UV to cubemap face coordinates and samples
 * the appropriate face's colormap. Same projection math as the height
 * variant but outputs to vertColor instead of vertHeight.
 *
 * Note: The original mod overrides OnVertexBuildHeight to set vertColor,
 * which is unconventional (color is normally set in OnVertexBuild).
 * BurstPQS separates these properly: we use IBatchPQSVertexJob since
 * we're writing to vertColor.
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
    [BatchPQSMod(typeof(PQSMod_VertexColorCubeMap))]
    public class BatchPQSMod_VertexColorCubeMap
        : BatchPQSMod<PQSMod_VertexColorCubeMap>
    {
        public BatchPQSMod_VertexColorCubeMap(
            PQSMod_VertexColorCubeMap mod) : base(mod) { }

        public override void OnQuadPreBuild(PQ quad, BatchPQSJobSet jobSet)
        {
            base.OnQuadPreBuild(quad, jobSet);

            jobSet.Add(new BuildJob
            {
                mapXn = BurstMapSO.Create(Mod.vertexColorMapXn),
                mapXp = BurstMapSO.Create(Mod.vertexColorMapXp),
                mapYn = BurstMapSO.Create(Mod.vertexColorMapYn),
                mapYp = BurstMapSO.Create(Mod.vertexColorMapYp),
                mapZn = BurstMapSO.Create(Mod.vertexColorMapZn),
                mapZp = BurstMapSO.Create(Mod.vertexColorMapZp),
            });
        }

        [BurstCompile]
        struct BuildJob : IBatchPQSVertexJob, IDisposable
        {
            public BurstMapSO mapXn, mapXp, mapYn, mapYp, mapZn, mapZp;

            public void BuildVertices(in BuildVerticesData data)
            {
                for (int i = 0; i < data.VertexCount; ++i)
                {
                    double u = data.u[i];
                    double v = data.v[i];

                    // UV to 3D direction
                    double theta = 2.0 * Math.PI * u;
                    double phi = Math.PI * v;
                    double sinPhi = Math.Sin(phi);

                    double cx = Math.Cos(theta) * sinPhi;
                    double cy = -Math.Cos(phi);
                    double cz = Math.Sin(theta) * sinPhi;

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

                    // Sample the correct face
                    Color color;
                    switch (faceIndex)
                    {
                        case 0: color = mapZn.GetPixelColor(1.0 - faceU, 1.0 - faceV); break;
                        case 1: color = mapZp.GetPixelColor(1.0 - faceU, 1.0 - faceV); break;
                        case 2: color = mapYn.GetPixelColor(faceV, 1.0 - faceU); break;
                        case 3: color = mapYp.GetPixelColor(1.0 - faceV, faceU); break;
                        case 4: color = mapXn.GetPixelColor(1.0 - faceU, 1.0 - faceV); break;
                        default: color = mapXp.GetPixelColor(1.0 - faceU, 1.0 - faceV); break;
                    }

                    data.vertColor[i] = color;
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
