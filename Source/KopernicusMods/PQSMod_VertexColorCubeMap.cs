﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AdvancedPQSTools
{
    /// <summary>
    /// A colormap PQSMod that can parse cubemapped textures.
    /// </summary>
    public class PQSMod_VertexColorCubeMap : PQSMod
    {

        public MapSO vertexColorMapXn;
        public MapSO vertexColorMapXp;
        public MapSO vertexColorMapYn;
        public MapSO vertexColorMapYp;
        public MapSO vertexColorMapZn;
        public MapSO vertexColorMapZp;
        public float edgeClampRange;
        protected int tileWidth = 1;
        protected float pixelClamp;

        public Vector3 UVtoXYZ(double u, double v)
        {
            Vector3 coords = new Vector3();

            double theta = 2.0 * Math.PI * u;
            double phi = Math.PI * v;

            coords.x = (float)(Math.Cos(theta) * Math.Sin(phi));
            coords.y = (float)(-Math.Cos(phi));
            coords.z = (float)(Math.Sin(theta) * Math.Sin(phi));

            return coords;
        }
        public Vector3 XYZtoFaceUVI(Vector3 coords)
        {
            //X = U, Y = V, Z = FaceIndex 
            Vector3 uvIndex = new Vector3();
            Vector3 absCoords = new Vector3(Math.Abs(coords.x), Math.Abs(coords.y), Math.Abs(coords.z));

            Boolean isXPositive = coords.x > 0 ? true : false;
            Boolean isYPositive = coords.y > 0 ? true : false;
            Boolean isZPositive = coords.z > 0 ? true : false;

            float maxAxis = 1;

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

        public Color GetCubeMapColor(MapSO texXn, MapSO texXp, MapSO texYn, MapSO texYp, MapSO texZn, MapSO texZp, double u, double v)
        {
            Color col = new Color(255, 0, 255);
            Vector3 coords = UVtoXYZ(u, v);
            Vector3 uvIndex = XYZtoFaceUVI(coords);

            //Clamp values near edges to prevent wrapping


            //Xn -> Zn
            uvIndex.x = Math.Max(uvIndex.x, pixelClamp);
            uvIndex.x = Math.Min(uvIndex.x, 1 - pixelClamp);
            uvIndex.y = Math.Max(uvIndex.y, pixelClamp);
            uvIndex.y = Math.Min(uvIndex.y, 1 - pixelClamp);

            if (uvIndex.z == 0)
                return texZn.GetPixelColor((1 - uvIndex.x), (1 - uvIndex.y));


            if (uvIndex.z == 1)
                return texZp.GetPixelColor((1 - uvIndex.x), (1 - uvIndex.y));

            if (uvIndex.z == 2)
                return texYn.GetPixelColor((uvIndex.y), (1 - uvIndex.x));

            if (uvIndex.z == 3)
                return texYp.GetPixelColor((1 - uvIndex.y), (uvIndex.x));

            if (uvIndex.z == 4)
                return texXn.GetPixelColor((1 - uvIndex.x), (1 - uvIndex.y));

            if (uvIndex.z == 5)
                return texXp.GetPixelColor((1 - uvIndex.x), (1 - uvIndex.y));

            return col;
        }

        protected void updateTileWidths()
        {
            int maxWidthX = Math.Max(vertexColorMapXn.Width, vertexColorMapXp.Width);
            int maxWidthY = Math.Max(vertexColorMapYn.Width, vertexColorMapYp.Width);
            int maxWidthZ = Math.Max(vertexColorMapZn.Width, vertexColorMapZp.Width);
            int maxWidth = Math.Max(Math.Max(maxWidthX, maxWidthY), maxWidthZ);

            tileWidth = Math.Max(maxWidth, tileWidth);
            pixelClamp = 1.0f / (tileWidth / edgeClampRange);
        }


        public override void OnSetup()
        {
            base.requirements = PQS.ModiferRequirements.MeshColorChannel;
            updateTileWidths();
        }
        public override void OnVertexBuildHeight(PQS.VertexBuildData data)
        {
            data.vertColor = GetCubeMapColor(vertexColorMapXn, vertexColorMapXp, vertexColorMapYn, vertexColorMapYp, vertexColorMapZn, vertexColorMapZp, (data.u), (data.v));
        }
    }
}
