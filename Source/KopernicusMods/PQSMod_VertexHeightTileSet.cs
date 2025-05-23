﻿/* 
* This code is adapted from KopernicusExpansion-Continued
* Available from https://github.com/StollD/KopernicusExpansion-Continued
*/

using Kopernicus.OnDemand;
using System;
using UnityEngine;
using System.Collections.Generic;
using static Kopernicus.ConfigParser.ParserOptions;

namespace AdvancedPQSTools
{
    /// <summary>
    /// A heightmap PQSMod that can parse encoded 16bpp cubemap textures for an effective resolution of 65536x32768
    /// </summary>
    public class PQSMod_VertexHeightTileSet : PQSMod_VertexHeightMap
    {
        private MapSOLargeTileSet vertexHeightTileSet;
        bool onStart = true;
        public List<string> path;
        public int size;
        public double edgeClampRange;


        public Vector3d UVtoXYZ(double u, double v)
        {
            Vector3d coords = new Vector3d();

            double theta = 2.0 * Math.PI * u;
            double phi = Math.PI * v;

            coords.x = Math.Cos(theta) * Math.Sin(phi);
            coords.y = -Math.Cos(phi);
            coords.z = Math.Sin(theta) * Math.Sin(phi);

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
            //Negative Z
            if (!isZPositive && absCoords.z >= absCoords.x && absCoords.z >= absCoords.y)
            {
                maxAxis = absCoords.z;
                uvIndex.x = -coords.x;
                uvIndex.y = coords.y;
                uvIndex.z = 4;
            }
            //Positive Z
            if (isZPositive && absCoords.z >= absCoords.x && absCoords.z >= absCoords.y)
            {
                maxAxis = absCoords.z;
                uvIndex.x = coords.x;
                uvIndex.y = coords.y;
                uvIndex.z = 5;
            }
            //Negative Y
            if (!isYPositive && absCoords.y > absCoords.x && absCoords.y > absCoords.z)
            {
                maxAxis = absCoords.y;
                uvIndex.x = coords.x;
                uvIndex.y = coords.z;
                uvIndex.z = 2;
            }
            //Positive Y
            if (isYPositive && absCoords.y > absCoords.x && absCoords.y > absCoords.z)
            {
                maxAxis = absCoords.y;
                uvIndex.x = coords.x;
                uvIndex.y = -coords.z;
                uvIndex.z = 3;
            }

            uvIndex.x = 0.5d * (uvIndex.x / maxAxis + 1.0d);
            uvIndex.y = 0.5d * (uvIndex.y / maxAxis + 1.0d);


            return uvIndex;
        }

        public MapSO.HeightAlpha GetCubeMapHeight(MapSOLargeTileSet vertexHeightTileSet, double u, double v)
        {
            MapSO.HeightAlpha ha = new MapSO.HeightAlpha();
            Vector3d coords = UVtoXYZ(u, v);
            Vector3d uvIndex = XYZtoFaceUVI(coords);

            //double pixelClamp = 1.0d / (4096 / edgeClampRange);
            //Clamp values near edges to prevent wrapping
            //uvIndex.x = Math.Max(uvIndex.x, pixelClamp);
            //uvIndex.x = Math.Min(uvIndex.x, 1 - pixelClamp);
            //uvIndex.y = Math.Max(uvIndex.y, pixelClamp);
            //uvIndex.y = Math.Min(uvIndex.y, 1 - pixelClamp);

            if (uvIndex.z == 0)
                uvIndex = new Vector3d(1 - uvIndex.x, 1 - uvIndex.y, uvIndex.z);
            if (uvIndex.z == 1)
                uvIndex = new Vector3d(1 - uvIndex.x, 1 - uvIndex.y, uvIndex.z);
            if (uvIndex.z == 2)
                uvIndex = new Vector3d(uvIndex.y, 1 - uvIndex.x, uvIndex.z);
            if (uvIndex.z == 3)
                uvIndex = new Vector3d(1 - uvIndex.y, uvIndex.x, uvIndex.z);
            if (uvIndex.z == 4)
                uvIndex = new Vector3d(1 - uvIndex.x, 1 - uvIndex.y, uvIndex.z);
            if (uvIndex.z == 5)
                uvIndex = new Vector3d(1 - uvIndex.x, 1 - uvIndex.y, uvIndex.z);

            MapSO.HeightAlpha ha2 = vertexHeightTileSet.GetPixelHeightAlpha((int)uvIndex.z, uvIndex.x, uvIndex.y);
            Double height = heightMapOffset + heightMapDeformity * (ha2.height + ha2.alpha * (Double)Byte.MaxValue) / (Double)(Byte.MaxValue + 1);


            //Debug.Log($"Sampling {u}, {v}: UVF: {uvIndex.x}, {uvIndex.y}, on face {uvIndex.z}, height: {height}");
            return vertexHeightTileSet.GetPixelHeightAlpha((int)uvIndex.z, uvIndex.x, uvIndex.y);
        }


        public override void OnSetup()
        {
            base.OnSetup();
            Debug.Log("Running setup");
            if (onStart)
            {
                onStart = false;
                Debug.Log("Creating tileset");
                if (path == null)
                {
                    Debug.Log("Path is null");
                }
                Debug.Log(path[0]);
                Debug.Log(size);
                vertexHeightTileSet = new MapSOLargeTileSet(path[0], size);

            }
        }


        public override void OnVertexBuildHeight(PQS.VertexBuildData data)
        {
            // Get the HeightAlpha, not the Float-Value from the Map
            // Clamp the v value to just shy of 1 to avoid sampling issues around the north pole.
            MapSO.HeightAlpha ha = GetCubeMapHeight(vertexHeightTileSet, data.u, data.v);

            // Get the height data from the terrain
            Double height = (ha.height + ha.alpha * (Double)Byte.MaxValue) / (Double)(Byte.MaxValue + 1);

            // Apply it
            data.vertHeight += heightMapOffset + heightMapDeformity * height;
        }
    }
}
