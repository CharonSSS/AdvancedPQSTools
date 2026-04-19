using AdvancedPQSTools.OnDemand;
using System;
using UnityEngine;

namespace AdvancedPQSTools
{
    /// <summary>
    /// A colormap PQSMod that can parse cubemapped textures.
    /// Uses manual bilinear interpolation with cross-face boundary sampling
    /// to produce seamless results across cubemap face edges.
    /// </summary>
    public class PQSMod_VertexColorCubeMap : PQSMod
    {
        public MapSOTile vertexColorMapXn;
        public MapSOTile vertexColorMapXp;
        public MapSOTile vertexColorMapYn;
        public MapSOTile vertexColorMapYp;
        public MapSOTile vertexColorMapZn;
        public MapSOTile vertexColorMapZp;
        public float edgeClampRange;

        /// <summary>
        /// Convert face index + face-local UV (in [-1,+1]) back to a 3D direction.
        /// Inverse of the cubemap projection.
        /// </summary>
        private static void FaceUVToDirection(
            int faceIndex, double faceU, double faceV,
            out double dx, out double dy, out double dz)
        {
            dx = 0; dy = 0; dz = 0;
            switch (faceIndex)
            {
                case 0: dx = -1; dz = faceU; dy = faceV; break;
                case 1: dx = 1; dz = -faceU; dy = faceV; break;
                case 2: dy = -1; dx = faceU; dz = faceV; break;
                case 3: dy = 1; dx = faceU; dz = -faceV; break;
                case 4: dz = -1; dx = -faceU; dy = faceV; break;
                default: dz = 1; dx = faceU; dy = faceV; break;
            }
        }

        /// <summary>
        /// Given a 3D direction, determine the cubemap face and the integer
        /// pixel coordinate on that face's texture.
        /// </summary>
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

            double texU, texV;
            switch (faceIndex)
            {
                case 0: texU = 1.0 - su; texV = 1.0 - sv; break;
                case 1: texU = 1.0 - su; texV = 1.0 - sv; break;
                case 2: texU = sv; texV = 1.0 - su; break;
                case 3: texU = 1.0 - sv; texV = su; break;
                case 4: texU = 1.0 - su; texV = 1.0 - sv; break;
                default: texU = 1.0 - su; texV = 1.0 - sv; break;
            }

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

        private Color SampleFace(int faceIndex, int px, int py)
        {
            switch (faceIndex)
            {
                case 0: return vertexColorMapZn.GetPixelColor(px, py);
                case 1: return vertexColorMapZp.GetPixelColor(px, py);
                case 2: return vertexColorMapYn.GetPixelColor(px, py);
                case 3: return vertexColorMapYp.GetPixelColor(px, py);
                case 4: return vertexColorMapXn.GetPixelColor(px, py);
                default: return vertexColorMapXp.GetPixelColor(px, py);
            }
        }

        /// <summary>
        /// Sample a pixel that falls outside the primary face's bounds by
        /// converting back to a 3D direction and re-projecting onto the
        /// correct adjacent face.
        /// </summary>
        private Color SampleVia3D(
            int srcFace, int px, int py, int faceWidth, int faceHeight)
        {
            double texU = (double)px / (faceWidth - 1);
            double texV = (double)py / (faceHeight - 1);

            // Undo the per-face UV remapping to recover su, sv in [0,1]
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

            // Map [0,1] → [-1,+1] face-local space
            double normU = su * 2.0 - 1.0;
            double normV = sv * 2.0 - 1.0;

            double dx, dy, dz;
            FaceUVToDirection(srcFace, normU, normV, out dx, out dy, out dz);

            int newFace, newPx, newPy;
            ProjectToFacePixel(dx, dy, dz, faceWidth, faceHeight,
                out newFace, out newPx, out newPy);

            return SampleFace(newFace, newPx, newPy);
        }

        public override void OnSetup()
        {
            base.requirements = PQS.ModiferRequirements.MeshColorChannel;
        }

        public override void OnVertexBuildHeight(PQS.VertexBuildData data)
        {
            int faceWidth = vertexColorMapZn.Width;
            int faceHeight = vertexColorMapZn.Height;

            double u = data.u;
            double v = data.v;

            // ── UVtoXYZ ──────────────────────────────────────────────
            double theta = 2.0 * Math.PI * u;
            double phi = Math.PI * v;

            float cx = (float)(Math.Cos(theta) * Math.Sin(phi));
            float cy = (float)(-Math.Cos(phi));
            float cz = (float)(Math.Sin(theta) * Math.Sin(phi));

            // ── XYZtoFaceUVI ─────────────────────────────────────────
            float ax = Math.Abs(cx);
            float ay = Math.Abs(cy);
            float az = Math.Abs(cz);

            bool isXPos = cx > 0;
            bool isYPos = cy > 0;
            bool isZPos = cz > 0;

            float maxAxis = 1;
            float faceU = 0, faceV = 0;
            int primaryFace = 0;

            if (!isXPos && ax >= ay && ax >= az)
            { maxAxis = ax; faceU = cz; faceV = cy; primaryFace = 0; }
            if (isXPos && ax >= ay && ax >= az)
            { maxAxis = ax; faceU = -cz; faceV = cy; primaryFace = 1; }
            if (!isYPos && ay >= ax && ay >= az)
            { maxAxis = ay; faceU = cx; faceV = cz; primaryFace = 2; }
            if (isYPos && ay >= ax && ay >= az)
            { maxAxis = ay; faceU = cx; faceV = -cz; primaryFace = 3; }
            if (!isZPos && az >= ax && az >= ay)
            { maxAxis = az; faceU = -cx; faceV = cy; primaryFace = 4; }
            if (isZPos && az >= ax && az >= ay)
            { maxAxis = az; faceU = cx; faceV = cy; primaryFace = 5; }

            double su = 0.5 * (faceU / maxAxis + 1.0);
            double sv = 0.5 * (faceV / maxAxis + 1.0);

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

            double px = texU * (faceWidth - 1);
            double py = texV * (faceHeight - 1);

            int x0 = (int)Math.Floor(px);
            int y0 = (int)Math.Floor(py);
            int x1 = x0 + 1;
            int y1 = y0 + 1;

            float fx = (float)(px - x0);
            float fy = (float)(py - y0);

            // ── Sample 4 bilinear corners with cross-face fallback ───
            Color p00 = (x0 >= 0 && x0 < faceWidth && y0 >= 0 && y0 < faceHeight)
                ? SampleFace(primaryFace, x0, y0)
                : SampleVia3D(primaryFace, x0, y0, faceWidth, faceHeight);

            Color p10 = (x1 >= 0 && x1 < faceWidth && y0 >= 0 && y0 < faceHeight)
                ? SampleFace(primaryFace, x1, y0)
                : SampleVia3D(primaryFace, x1, y0, faceWidth, faceHeight);

            Color p01 = (x0 >= 0 && x0 < faceWidth && y1 >= 0 && y1 < faceHeight)
                ? SampleFace(primaryFace, x0, y1)
                : SampleVia3D(primaryFace, x0, y1, faceWidth, faceHeight);

            Color p11 = (x1 >= 0 && x1 < faceWidth && y1 >= 0 && y1 < faceHeight)
                ? SampleFace(primaryFace, x1, y1)
                : SampleVia3D(primaryFace, x1, y1, faceWidth, faceHeight);

            data.vertColor = p00 * ((1f - fx) * (1f - fy))
                           + p10 * (fx * (1f - fy))
                           + p01 * ((1f - fx) * fy)
                           + p11 * (fx * fy);
        }
    }
}