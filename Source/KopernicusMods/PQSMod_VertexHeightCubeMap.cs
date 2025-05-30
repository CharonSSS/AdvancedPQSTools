﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedPQSTools
{
    /// <summary>
    /// A heightmap PQSMod that can parse encoded 16bpp cubemap textures for an effective resolution of 65536x32768
    /// </summary>
    public class PQSMod_VertexHeightCubeMap : PQSMod_VertexHeightMap
    {
        public MapSODemandLarge vertexHeightMapXn;
        public MapSODemandLarge vertexHeightMapXp;
        public MapSODemandLarge vertexHeightMapYn;
        public MapSODemandLarge vertexHeightMapYp;
        public MapSODemandLarge vertexHeightMapZn;
        public MapSODemandLarge vertexHeightMapZp;
        public double edgeClampRange;


        public Vector3d UVtoXYZ(double u, double v)
        {
            Vector3d coords = new Vector3d();

            double theta = 2.0 * Math.PI * u;
            double phi = Math.PI * v;

            coords.x = (float)(Math.Cos(theta) * Math.Sin(phi));
            coords.y = (float)(-Math.Cos(phi));
            coords.z = (float)(Math.Sin(theta) * Math.Sin(phi));

            return coords;
        }
        public Vector3d XYZtoFaceUVI(Vector3d coords)
        {
            //X = U, Y = V, Z = FaceIndex 
            Vector3d uvIndex = new Vector3d();
            Vector3d absCoords = new Vector3d(Math.Abs(coords.x), Math.Abs(coords.y), Math.Abs(coords.z));

            Boolean isXPositive = coords.x > 0 ? true : false;
            Boolean isYPositive = coords.y > 0 ? true : false;
            Boolean isZPositive = coords.z > 0 ? true : false;

            double maxAxis = 1;

            //Negative X
            if (!isXPositive && absCoords.x >= absCoords.y && absCoords.x >= absCoords.z)
            {
                maxAxis = absCoords.x;
                uvIndex.x = coords.z;
                uvIndex.y = coords.y;
                uvIndex.z = 0;
            }
            //Positive X
            if (isXPositive && absCoords.x >= absCoords.y && absCoords.x >= absCoords.z)
            {
                maxAxis = absCoords.x;
                uvIndex.x = -coords.z;
                uvIndex.y = coords.y;
                uvIndex.z = 1;
            }
            //Negative Y
            if (!isYPositive && absCoords.y >= absCoords.x && absCoords.y >= absCoords.z)
            {
                maxAxis = absCoords.y;
                uvIndex.x = coords.x;
                uvIndex.y = coords.z;
                uvIndex.z = 2;
            }
            //Positive Y
            if (isYPositive && absCoords.y >= absCoords.x && absCoords.y >= absCoords.z)
            {
                maxAxis = absCoords.y;
                uvIndex.x = coords.x;
                uvIndex.y = -coords.z;
                uvIndex.z = 3;
            }
            //Negative Z
            if (!isZPositive && absCoords.z >= absCoords.x && absCoords.z >= absCoords.y)
            {
                maxAxis = absCoords.z;
                uvIndex.x = -coords.x;
                uvIndex.y = coords.y;
                uvIndex.z = 4;
            }
            if (isZPositive && absCoords.z >= absCoords.x && absCoords.z >= absCoords.y)
            {
                maxAxis = absCoords.z;
                uvIndex.x = coords.x;
                uvIndex.y = coords.y;
                uvIndex.z = 5;
            }

            uvIndex.x = 0.5f * (uvIndex.x / maxAxis + 1.0f);
            uvIndex.y = 0.5f * (uvIndex.y / maxAxis + 1.0f);


            return uvIndex;
        }

        public MapSO.HeightAlpha GetCubeMapHeight(MapSO texXn, MapSO texXp, MapSO texYn, MapSO texYp, MapSO texZn, MapSO texZp, double u, double v)
        {
            MapSO.HeightAlpha ha = new MapSO.HeightAlpha();
            Vector3d coords = UVtoXYZ(u, v);
            Vector3d uvIndex = XYZtoFaceUVI(coords);

            double pixelClamp = 1.0d / (texXn.Width / edgeClampRange);
            //Clamp values near edges to prevent wrapping
            uvIndex.x = Math.Max(uvIndex.x, pixelClamp);
            uvIndex.x = Math.Min(uvIndex.x, 1 - pixelClamp);
            uvIndex.y = Math.Max(uvIndex.y, pixelClamp);
            uvIndex.y = Math.Min(uvIndex.y, 1 - pixelClamp);

            //Xn -> Zn
            if (uvIndex.z == 0)
                return texZn.GetPixelHeightAlpha((1 - uvIndex.x), (1 - uvIndex.y));

            if (uvIndex.z == 1)
                return texZp.GetPixelHeightAlpha((1 - uvIndex.x), (1 - uvIndex.y));

            if (uvIndex.z == 2)
                return texYn.GetPixelHeightAlpha((uvIndex.y), (1 - uvIndex.x));

            if (uvIndex.z == 3)
                return texYp.GetPixelHeightAlpha((1 - uvIndex.y), (uvIndex.x));

            if (uvIndex.z == 4)
                return texXn.GetPixelHeightAlpha((1 - uvIndex.x), (1 - uvIndex.y));

            if (uvIndex.z == 5)
                return texXp.GetPixelHeightAlpha((1 - uvIndex.x), (1 - uvIndex.y));


            return ha;
        }

        public override void OnVertexBuildHeight(PQS.VertexBuildData data)
        {
            // Get the HeightAlpha, not the Float-Value from the Map
            // Clamp the v value to just shy of 1 to avoid sampling issues around the north pole.
            vertexHeightMapXn.Load();
            vertexHeightMapXp.Load();
            vertexHeightMapYn.Load();
            vertexHeightMapYp.Load();
            vertexHeightMapZn.Load();
            vertexHeightMapZp.Load();


            MapSO.HeightAlpha ha = GetCubeMapHeight(vertexHeightMapXn, vertexHeightMapXp, vertexHeightMapYn, vertexHeightMapYp, vertexHeightMapZn, vertexHeightMapZp, data.u, data.v);

            // Get the height data from the terrain
            Double height = (ha.height + ha.alpha * (Double)Byte.MaxValue) / (Double)(Byte.MaxValue + 1);

            // Apply it
            data.vertHeight += heightMapOffset + heightMapDeformity * height;
        }
    }
}
