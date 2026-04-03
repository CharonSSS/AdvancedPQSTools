/*
 * BurstPQS adapter for PQSMod_VertexHeightCubeMap
 *
 * Uses GetPixelFloat(int, int) with manual bilinear interpolation that
 * correctly samples across cubemap face boundaries by converting pixel
 * offsets back to 3D directions and re-projecting onto the correct face.
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

            // ─────────────────────────────────────────────────────────
            //  Given a 3D direction, determine the cubemap face and
            //  return the integer pixel coordinate on that face's texture.
            //  Returns the face index (0-5) via out parameter.
            //
            //  This is the core projection used for every pixel sample,
            //  including bilinear neighbors that may land on adjacent faces.
            // ─────────────────────────────────────────────────────────
            private static void ProjectToFacePixel(
                double dx, double dy, double dz,
                int faceWidth, int faceHeight,
                out int faceIndex, out int px, out int py)
            {
                double ax = Math.Abs(dx);
                double ay = Math.Abs(dy);
                double az = Math.Abs(dz);

                bool isXPos = dx > 0;
                bool isYPos = dy > 0;
                bool isZPos = dz > 0;

                double maxAxis = 1;
                double faceU = 0, faceV = 0;
                faceIndex = 0;

                // Non-exclusive if-chains: last match wins (matches original)
                if (!isXPos && ax >= ay && ax >= az)
                { maxAxis = ax; faceU = dz; faceV = dy; faceIndex = 0; }
                if (isXPos && ax >= ay && ax >= az)
                { maxAxis = ax; faceU = -dz; faceV = dy; faceIndex = 1; }
                if (!isYPos && ay >= ax && ay >= az)
                { maxAxis = ay; faceU = dx; faceV = dz; faceIndex = 2; }
                if (isYPos && ay >= ax && ay >= az)
                { maxAxis = ay; faceU = dx; faceV = -dz; faceIndex = 3; }
                if (!isZPos && az >= ax && az >= ay)
                { maxAxis = az; faceU = -dx; faceV = dy; faceIndex = 4; }
                if (isZPos && az >= ax && az >= ay)
                { maxAxis = az; faceU = dx; faceV = dy; faceIndex = 5; }

                double su = 0.5 * (faceU / maxAxis + 1.0);
                double sv = 0.5 * (faceV / maxAxis + 1.0);

                // Apply per-face UV remapping (matches original face→texture mapping)
                double texU, texV;
                switch (faceIndex)
                {
                    case 0: texU = 1.0 - su; texV = 1.0 - sv; break;
                    case 1: texU = 1.0 - su; texV = 1.0 - sv; break;
                    case 2: texU = sv; texV = 1.0 - su; break;
                    case 3: texU = 1.0 - sv; texV = su; break;
                    case 4: texU = 1.0 - su; texV = 1.0 - sv; break;
                    default: texU = 1.0 - su; texV = 1.0 - sv; break; // face 5
                }

                // Clamp and convert to pixel coordinates
                if (texU < 0.0) texU = 0.0;
                if (texU > 1.0) texU = 1.0;
                if (texV < 0.0) texV = 0.0;
                if (texV > 1.0) texV = 1.0;

                px = (int)(texU * (faceWidth - 1) + 0.5);
                py = (int)(texV * (faceHeight - 1) + 0.5);

                if (px < 0) px = 0;
                if (px >= faceWidth) px = faceWidth - 1;
                if (py < 0) py = 0;
                if (py >= faceHeight) py = faceHeight - 1;
            }

            /// <summary>
            /// Sample a single integer pixel from the correct face map.
            /// </summary>
            private double SampleFace(int faceIndex, int px, int py)
            {
                switch (faceIndex)
                {
                    case 0: return mapZn.GetPixelFloat(px, py);
                    case 1: return mapZp.GetPixelFloat(px, py);
                    case 2: return mapYn.GetPixelFloat(px, py);
                    case 3: return mapYp.GetPixelFloat(px, py);
                    case 4: return mapXn.GetPixelFloat(px, py);
                    default: return mapXp.GetPixelFloat(px, py);
                }
            }

            /// <summary>
            /// Convert face index + face-local UV back to a 3D direction.
            /// This is the inverse of the cubemap projection.
            /// </summary>
            private static void FaceUVToDirection(
                int faceIndex, double faceU, double faceV,
                out double dx, out double dy, out double dz)
            {
                // faceU and faceV are in [-1, +1] range (before the 0.5*(x+1) mapping)
                switch (faceIndex)
                {
                    case 0: // Xn: dx=-1, faceU=dz, faceV=dy
                        dx = -1; dz = faceU; dy = faceV; break;
                    case 1: // Xp: dx=+1, faceU=-dz, faceV=dy
                        dx = 1; dz = -faceU; dy = faceV; break;
                    case 2: // Yn: dy=-1, faceU=dx, faceV=dz
                        dy = -1; dx = faceU; dz = faceV; break;
                    case 3: // Yp: dy=+1, faceU=dx, faceV=-dz
                        dy = 1; dx = faceU; dz = -faceV; break;
                    case 4: // Zn: dz=-1, faceU=-dx, faceV=dy
                        dz = -1; dx = -faceU; dy = faceV; break;
                    default: // Zp: dz=+1, faceU=dx, faceV=dy
                        dz = 1; dx = faceU; dy = faceV; break;
                }
            }

            public void BuildHeights(in BuildHeightsData data)
            {
                // All face textures should be the same size; grab from first valid one.
                int faceWidth = mapZn.Width;
                int faceHeight = mapZn.Height;

                for (int i = 0; i < data.VertexCount; ++i)
                {
                    double u = data.u[i];
                    double v = data.v[i];

                    // ── UVtoXYZ ──────────────────────────────────────
                    double theta = 2.0 * Math.PI * u;
                    double phi = Math.PI * v;

                    float cx = (float)(Math.Cos(theta) * Math.Sin(phi));
                    float cy = (float)(-Math.Cos(phi));
                    float cz = (float)(Math.Sin(theta) * Math.Sin(phi));

                    // ── Determine primary face and face-local UV ─────
                    double ax = Math.Abs((double)cx);
                    double ay = Math.Abs((double)cy);
                    double az = Math.Abs((double)cz);

                    bool isXPos = cx > 0;
                    bool isYPos = cy > 0;
                    bool isZPos = cz > 0;

                    double maxAxis = 1;
                    double rawFaceU = 0, rawFaceV = 0;
                    int primaryFace = 0;

                    if (!isXPos && ax >= ay && ax >= az)
                    { maxAxis = ax; rawFaceU = cz; rawFaceV = cy; primaryFace = 0; }
                    if (isXPos && ax >= ay && ax >= az)
                    { maxAxis = ax; rawFaceU = -cz; rawFaceV = cy; primaryFace = 1; }
                    if (!isYPos && ay >= ax && ay >= az)
                    { maxAxis = ay; rawFaceU = cx; rawFaceV = cz; primaryFace = 2; }
                    if (isYPos && ay >= ax && ay >= az)
                    { maxAxis = ay; rawFaceU = cx; rawFaceV = -cz; primaryFace = 3; }
                    if (!isZPos && az >= ax && az >= ay)
                    { maxAxis = az; rawFaceU = -cx; rawFaceV = cy; primaryFace = 4; }
                    if (isZPos && az >= ax && az >= ay)
                    { maxAxis = az; rawFaceU = cx; rawFaceV = cy; primaryFace = 5; }

                    // Face UV in [-1, +1] space (before normalization)
                    double normU = rawFaceU / maxAxis;
                    double normV = rawFaceV / maxAxis;

                    // Map to [0,1] for pixel coordinate computation
                    double su = 0.5 * (normU + 1.0);
                    double sv = 0.5 * (normV + 1.0);

                    // Apply per-face UV remapping
                    double texU, texV;
                    switch (primaryFace)
                    {
                        case 0: texU = 1.0 - su; texV = 1.0 - sv; break;
                        case 1: texU = 1.0 - su; texV = 1.0 - sv; break;
                        case 2: texU = sv; texV = 1.0 - su; break;
                        case 3: texU = 1.0 - sv; texV = su; break;
                        case 4: texU = 1.0 - su; texV = 1.0 - sv; break;
                        default: texU = 1.0 - su; texV = 1.0 - sv; break;
                    }

                    // Continuous pixel coordinates on this face
                    double px = texU * (faceWidth - 1);
                    double py = texV * (faceHeight - 1);

                    int x0 = (int)Math.Floor(px);
                    int y0 = (int)Math.Floor(py);
                    int x1 = x0 + 1;
                    int y1 = y0 + 1;

                    double fx = px - x0;
                    double fy = py - y0;

                    // ── Sample 4 bilinear corners ────────────────────
                    // If a corner pixel is within bounds, sample directly
                    // from this face. If it's out of bounds (past the edge),
                    // convert back to 3D direction and re-project to find
                    // the correct adjacent face and pixel.
                    double p00, p10, p01, p11;

                    if (x0 >= 0 && x0 < faceWidth && y0 >= 0 && y0 < faceHeight)
                    {
                        p00 = SampleFace(primaryFace, x0, y0);
                    }
                    else
                    {
                        p00 = SampleVia3D(primaryFace, x0, y0, faceWidth, faceHeight);
                    }

                    if (x1 >= 0 && x1 < faceWidth && y1 >= 0 && y1 < faceHeight)
                    {
                        p11 = SampleFace(primaryFace, x1, y1);
                    }
                    else
                    {
                        p11 = SampleVia3D(primaryFace, x1, y1, faceWidth, faceHeight);
                    }

                    if (x1 >= 0 && x1 < faceWidth && y0 >= 0 && y0 < faceHeight)
                    {
                        p10 = SampleFace(primaryFace, x1, y0);
                    }
                    else
                    {
                        p10 = SampleVia3D(primaryFace, x1, y0, faceWidth, faceHeight);
                    }

                    if (x0 >= 0 && x0 < faceWidth && y1 >= 0 && y1 < faceHeight)
                    {
                        p01 = SampleFace(primaryFace, x0, y1);
                    }
                    else
                    {
                        p01 = SampleVia3D(primaryFace, x0, y1, faceWidth, faceHeight);
                    }

                    double height = p00 * (1.0 - fx) * (1.0 - fy)
                                  + p10 * fx * (1.0 - fy)
                                  + p01 * (1.0 - fx) * fy
                                  + p11 * fx * fy;

                    if (double.IsNaN(height) || double.IsInfinity(height))
                        continue;

                    data.vertHeight[i] += heightMapOffset + heightMapDeformity * height;
                }
            }

            /// <summary>
            /// Sample a pixel that is outside the current face's bounds by
            /// converting the out-of-bounds pixel coordinate back to a 3D
            /// direction and re-projecting onto the correct adjacent face.
            /// </summary>
            private double SampleVia3D(
                int srcFace, int px, int py, int faceWidth, int faceHeight)
            {
                // Convert pixel coordinate back to face UV in [-1, +1]
                // First undo the per-face remapping to get su, sv in [0,1]
                // then convert to [-1, +1]
                double texU = (double)px / (faceWidth - 1);
                double texV = (double)py / (faceHeight - 1);

                // Undo the per-face UV remapping to get su, sv
                double su, sv;
                switch (srcFace)
                {
                    case 0: su = 1.0 - texU; sv = 1.0 - texV; break;
                    case 1: su = 1.0 - texU; sv = 1.0 - texV; break;
                    case 2: su = 1.0 - texV; sv = texU; break;
                    case 3: su = texV; sv = 1.0 - texU; break;
                    case 4: su = 1.0 - texU; sv = 1.0 - texV; break;
                    default: su = 1.0 - texU; sv = 1.0 - texV; break;
                }

                // su, sv in [0,1] → normU, normV in [-1, +1]
                double normU = su * 2.0 - 1.0;
                double normV = sv * 2.0 - 1.0;

                // Convert back to 3D direction
                double dx, dy, dz;
                FaceUVToDirection(srcFace, normU, normV, out dx, out dy, out dz);

                // Re-project onto the correct face and sample
                int newFace, newPx, newPy;
                ProjectToFacePixel(dx, dy, dz, faceWidth, faceHeight,
                    out newFace, out newPx, out newPy);

                return SampleFace(newFace, newPx, newPy);
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