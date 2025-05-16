using Kopernicus.OnDemand;
using System;
using UnityEngine;


namespace AdvancedPQSTools

{
    public class MapSOTileSet
    {
        public string tileSetPath { get; private set; }
        public int tileSetSize { get; private set; }
        private string tileSetExtension;
        private int tileSetLength;


        MapSODemand[] tileSet;

        public MapSOTileSet(string path, int size)
        {

            /* TILE ORDER
             * 0 - Zn
             * 1 - Zp
             * 2 - Yn
             * 3 - Yp
             * 4 - Xn
             * 5 - Xp
             */
            Debug.Log(OnDemandStorage.UseOnDemand);

            Debug.Log("Making a tileset with a path of " + path);
            tileSetPath = path;
            tileSetSize = size;
            tileSetLength = (size * size * 6);
            tileSetExtension = ".dds";//extension;
            tileSet = new MapSODemand[tileSetLength];
            for (int index = 0; index < tileSetLength; index++)
            {
                int X = index % size;
                int Y = (int)(Math.Floor((double)index / size) % size);
                int F = (int)(Math.Floor((double)index / (size * size)));

                tileSet[index] = ScriptableObject.CreateInstance<MapSODemand>();
                Debug.Log(tileSetPath + "_" + F + "-" + Y + "-" + X + tileSetExtension);
                tileSet[index].Depth = MapSO.MapDepth.RGB;
                tileSet[index].Path = tileSetPath + "_" + F + "-" + Y + "-" + X + tileSetExtension;
                tileSet[index].AutoLoad = OnDemandStorage.OnDemandLoadOnMissing;
            }
        }


        public MapSODemand GetTile(int face, double x, double y)
        {
            x = Math.Min(x, 0.99999);
            y = Math.Min(y, 0.99999);


            int a = face * tileSetSize * tileSetSize;
            int b = (int)(Math.Floor(tileSetSize * y) * tileSetSize);
            int c = (int)(Math.Floor(tileSetSize * x));
            int tileIndex = a + b + c;

            //Debug.Log($"Face: {face}, X: {x} Y: {y} -> A: {a}, B: {b}, C:{c}.\n Index: {tileIndex}");
            if (tileIndex == tileSetLength)
                tileIndex -= 1;
            return tileSet[tileIndex];
        }

        public Color GetPixelColor(int face, double x, double y)
        {
            MapSODemand tile = GetTile(face, x, y);

            double newX = (tileSetSize * x) - Math.Floor(tileSetSize * x);
            double newY = (tileSetSize * y) - Math.Floor(tileSetSize * y);

            tile.Load();
            return tile.GetPixelColor(newX, newY);
        }
    }
}
