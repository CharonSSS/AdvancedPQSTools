/*
 * BoundedDecalSet.cs
 *
 * Kopernicus ModLoader for PQSMod_BoundedDecalSet.
 *
 * Uses Steamroller's pattern:
 *   - DecalDataLoader: per-child-node parser using Kopernicus's own
 *     MapSOParserGreyScale/RGB for texture loading (handles OnDemand,
 *     KSPTextureLoader, and BUILTIN/ paths correctly).
 *   - ParserTargetCollection on the parent to collect all children.
 *   - IParserPostApplyEventSubscriber to wire up the PQSMod after
 *     all children are parsed.
 *
 * DecalData is a ScriptableObject so references survive prefab
 * instantiation (mod DLLs don't get [Serializable] picked up for
 * inline struct serialization).
 */

using AdvancedPQSTools.OnDemand;
using Kopernicus;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.ConfigParser.Interfaces;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.Configuration.ModLoader;
using Kopernicus.Configuration.Parsing;
using Kopernicus.OnDemand;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedPQSTools
{
    [RequireConfigType(ConfigType.Node)]
    public class DecalDataLoader : BaseLoader, ITypeParser<DecalData>, IParserPostApplyEventSubscriber
    {
        public DecalData Value { get => data; set => data = value; }
        DecalData data = ScriptableObject.CreateInstance<DecalData>();

        [ParserTarget("maxLong")]
        public NumericParser<double> MaxLong
        {
            get => data.maxLong;
            set => data.maxLong = value;
        }

        [ParserTarget("minLong")]
        public NumericParser<double> MinLong
        {
            get => data.minLong;
            set => data.minLong = value;
        }

        [ParserTarget("maxLat")]
        public NumericParser<double> MaxLat
        {
            get => data.maxLat;
            set => data.maxLat = value;
        }

        [ParserTarget("minLat")]
        public NumericParser<double> MinLat
        {
            get => data.minLat;
            set => data.minLat = value;
        }

        [ParserTarget("offset")]
        public NumericParser<double> Offset
        {
            get => data.offset;
            set => data.offset = value;
        }

        [ParserTarget("deformity")]
        public NumericParser<double> Deformity
        {
            get => data.deformity;
            set => data.deformity = value;
        }

        [ParserTarget("heightMap")]
        public MapSOParserGreyScale<MapSOTile> HeightMap
        {
            get => data.heightMap;
            set => data.heightMap = value;
        }

        [ParserTarget("colorMap")]
        public MapSOParserRGB<MapSOTile> ColorMap
        {
            get => data.colorMap;
            set => data.colorMap = value;
        }

        void IParserPostApplyEventSubscriber.PostApply(ConfigNode node) =>
            data.PrecomputeUVBounds();
    }

    [RequireConfigType(ConfigType.Node)]
    public class BoundedDecalSet : ModLoader<PQSMod_BoundedDecalSet>, IParserPostApplyEventSubscriber
    {
        [ParserTarget("allowScatters")]
        public NumericParser<Boolean> allowScatters
        {
            get { return Mod.allowScatters; }
            set { Mod.allowScatters = value; }
        }

        [ParserTargetCollection("decals", Key = "BoundedDecalBicubic", NameSignificance = NameSignificance.Key)]
        public List<DecalDataLoader> Decals { get; set; }

        void IParserPostApplyEventSubscriber.PostApply(ConfigNode node)
        {
            Mod.decals = Decals
                .Select(decal => decal.Value)
                .ToArray();

            Mod.BuildGrid();

            Debug.Log($"[BoundedDecalSet] Loaded {Decals.Count} bounded decals, grid built.");
        }
    }
}