using AdvancedPQSTools.OnDemand;
using System;

namespace AdvancedPQSTools
{
    /// <summary>
    /// A heightmap PQSMod that can parse cubemap textures for an effective resolution of 65536x32768.
    /// Uses manual bilinear interpolation with cross-face boundary sampling.
    /// </summary>
    public class PQSMod_VertexHeightCubeMap : PQSMod_VertexHeightMap
    {
        public MapSOTile vertexHeightMapXn;
        public MapSOTile vertexHeightMapXp;
        public MapSOTile vertexHeightMapYn;
        public MapSOTile vertexHeightMapYp;
        public MapSOTile vertexHeightMapZn;
        public MapSOTile vertexHeightMapZp;
        public double edgeClampRange;

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

        private double SampleFace(int faceIndex, int px, int py)
        {
            switch (faceIndex)
            {
                case 0: return vertexHeightMapZn.GetPixelFloat(px, py);
                case 1: return vertexHeightMapZp.GetPixelFloat(px, py);
                case 2: return vertexHeightMapYn.GetPixelFloat(px, py);
                case 3: return vertexHeightMapYp.GetPixelFloat(px, py);
                case 4: return vertexHeightMapXn.GetPixelFloat(px, py);
                default: return vertexHeightMapXp.GetPixelFloat(px, py);
            }
        }

        private double SampleVia3D(
            int srcFace, int px, int py, int faceWidth, int faceHeight)
        {
            double texU = (double)px / (faceWidth - 1);
            double texV = (double)py / (faceHeight - 1);

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

            double normU = su * 2.0 - 1.0;
            double normV = sv * 2.0 - 1.0;

            double dx, dy, dz;
            FaceUVToDirection(srcFace, normU, normV, out dx, out dy, out dz);

            int newFace, newPx, newPy;
            ProjectToFacePixel(dx, dy, dz, faceWidth, faceHeight,
                out newFace, out newPx, out newPy);

            return SampleFace(newFace, newPx, newPy);
        }

        public override void OnVertexBuildHeight(PQS.VertexBuildData data)
        {
            int faceWidth = vertexHeightMapZn.Width;
            int faceHeight = vertexHeightMapZn.Height;

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

            double fx = px - x0;
            double fy = py - y0;

            // ── Sample 4 bilinear corners with cross-face fallback ───
            double p00 = (x0 >= 0 && x0 < faceWidth && y0 >= 0 && y0 < faceHeight)
                ? SampleFace(primaryFace, x0, y0)
                : SampleVia3D(primaryFace, x0, y0, faceWidth, faceHeight);

            double p10 = (x1 >= 0 && x1 < faceWidth && y0 >= 0 && y0 < faceHeight)
                ? SampleFace(primaryFace, x1, y0)
                : SampleVia3D(primaryFace, x1, y0, faceWidth, faceHeight);

            double p01 = (x0 >= 0 && x0 < faceWidth && y1 >= 0 && y1 < faceHeight)
                ? SampleFace(primaryFace, x0, y1)
                : SampleVia3D(primaryFace, x0, y1, faceWidth, faceHeight);

            double p11 = (x1 >= 0 && x1 < faceWidth && y1 >= 0 && y1 < faceHeight)
                ? SampleFace(primaryFace, x1, y1)
                : SampleVia3D(primaryFace, x1, y1, faceWidth, faceHeight);

            double height = p00 * (1.0 - fx) * (1.0 - fy)
                          + p10 * fx * (1.0 - fy)
                          + p01 * (1.0 - fx) * fy
                          + p11 * fx * fy;

            if (Double.IsNaN(height) || Double.IsInfinity(height))
                return;

            data.vertHeight += heightMapOffset + heightMapDeformity * height;
        }
    }
}