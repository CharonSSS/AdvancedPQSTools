/*
 * DecalData.cs
 *
 * Per-decal parameters stored as a ScriptableObject so that Unity
 * serializes references to it across prefab instantiation. Mod DLLs
 * loaded after Unity's assembly scan don't get [Serializable] picked
 * up for inline struct serialization, but ScriptableObject references
 * are always serialized correctly.
 */

using AdvancedPQSTools.OnDemand;
using UnityEngine;

namespace AdvancedPQSTools
{
    public class DecalData : ScriptableObject
    {
        // ─── Original geographic bounds ───────────────────────────────
        public double maxLong;
        public double minLong;
        public double maxLat;
        public double minLat;

        // ─── Precomputed PQS UV bounds ────────────────────────────────
        public double maxU;
        public double minU;
        public double maxV;
        public double minV;
        public double rangeU;
        public double rangeV;

        // ─── Height parameters ────────────────────────────────────────
        public double offset;
        public double deformity;

        // ─── Texture maps ─────────────────────────────────────────────
        public MapSOTile heightMap;
        public MapSOTile colorMap;

        /// <summary>
        /// Precompute UV bounds from the geographic bounds.
        /// Call once after all bounds are set.
        /// </summary>
        public void PrecomputeUVBounds()
        {
            maxU = 1.0 - ((maxLong + 270.0) / 360.0);
            minU = 1.0 - ((minLong + 270.0) / 360.0);
            maxV = (maxLat + 90.0) / 180.0;
            minV = (minLat + 90.0) / 180.0;
            rangeU = minU - maxU;
            rangeV = maxV - minV;
        }
    }
}