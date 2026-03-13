using BurstPQS.Map;
using Kopernicus.OnDemand;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AdvancedPQSTools.OnDemand
{
    public class MapSOTile : MapSODemand, ILoadOnDemand, IPreloadOnDemand
    {
        /// <summary>
        /// Register a <see cref="BurstMapSO"/> factory for <see cref="MapSOTile"/>
        /// with the BurstPQS registry. The registry uses exact-type matching,
        /// so <see cref="MapSODemand"/>'s registration does not cover us.
        /// </summary>
        static MapSOTile()
        {
            BurstMapSO.RegisterMapSOFactoryFunc<MapSOTile>(CreateBurstMapSO);
            Debug.Log("[AdvancedPQSTools] Registered BurstMapSO factory for MapSOTile");
        }

        /// <summary>
        /// Cached delegate from the BurstMapSO registry for MapSODemand.
        /// We invoke this for MapSOTile since it IS-A MapSODemand and needs
        /// the same depth-aware pixel interpretation that BurstPQS.Kopernicus
        /// provides (not TextureMapSO's .grayscale interpretation).
        /// </summary>
        private static Func<MapSO, BurstMapSO> _mapSODemandFactory;

        /// <summary>
        /// Factory function for the BurstMapSO registry.
        /// Delegates to the MapSODemand factory registered by BurstPQS.Kopernicus,
        /// which correctly interprets pixel data based on MapDepth.
        /// </summary>
        private static BurstMapSO CreateBurstMapSO(MapSOTile tile)
        {
            // On first call, grab the MapSODemand factory from the registry
            // via reflection. The registry is internal, but we only need to
            // read from it once.
            if (_mapSODemandFactory == null)
            {
                var registryType = typeof(BurstMapSO).Assembly
                    .GetType("BurstPQS.Map.BurstMapSORegistry");

                if (registryType != null)
                {
                    var registryField = registryType.GetField(
                        "Registry",
                        BindingFlags.Static | BindingFlags.NonPublic);

                    if (registryField != null)
                    {
                        var dict = registryField.GetValue(null)
                            as Dictionary<Type, Func<MapSO, BurstMapSO>>;

                        if (dict != null)
                        {
                            dict.TryGetValue(typeof(MapSODemand), out _mapSODemandFactory);
                        }
                    }
                }

                if (_mapSODemandFactory == null)
                {
                    Debug.LogError(
                        "[AdvancedPQSTools] Failed to find MapSODemand factory in "
                        + "BurstMapSORegistry. Falling back to TextureMapSO.Create "
                        + "which may produce incorrect height values.");
                    _mapSODemandFactory = mapSO =>
                    {
                        var demand = (MapSODemand)mapSO;
                        if (!demand.IsLoaded) demand.Load();
                        if (!demand.IsLoaded) return new BurstMapSO();
                        var tex = demand.Texture;
                        return tex != null ? TextureMapSO.Create(tex) : new BurstMapSO();
                    };
                }
            }

            // The MapSODemand factory handles Load(), Texture access, depth,
            // and all CPUTexture2D format dispatch correctly.
            return _mapSODemandFactory(tile);
        }

        protected override void ConstructBilinearCoords(double x, double y)
        {
            // Clamp vs Wrap
            x = Math.Abs(x - Math.Floor(x));
            y = Math.Abs(y - Math.Floor(y));
            centerXD = x * _width;
            minX = (int)Math.Floor(centerXD);
            maxX = (int)Math.Ceiling(centerXD);
            midX = (float)centerXD - minX;
            if (maxX == _width)
                maxX = _width - 1;

            centerYD = y * _height;
            minY = (int)Math.Floor(centerYD);
            maxY = (int)Math.Ceiling(centerYD);
            midY = (float)centerYD - minY;
            if (maxY == _height)
                maxY = _height - 1;
        }
    }
}
