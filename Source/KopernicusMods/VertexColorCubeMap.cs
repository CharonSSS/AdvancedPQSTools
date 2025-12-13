using AdvancedPQSTools.OnDemand;
using Kopernicus.Components;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.Configuration.ModLoader;
using Kopernicus.Configuration.Parsing;
using System;
using System.Diagnostics.CodeAnalysis;

namespace AdvancedPQSTools
{
    [RequireConfigType(ConfigType.Node)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class VertexColorCubeMap : ModLoader<PQSMod_VertexColorCubeMap>
    {
        // The map textures for the planet
        [ParserTarget("mapXn")]
        public MapSOTileParserRGB<MapSOTile> vertexColorMapXn
        {
            get { return Mod.vertexColorMapXn; }
            set { Mod.vertexColorMapXn = value; }
        }
        [ParserTarget("mapXp")]
        public MapSOTileParserRGB<MapSOTile> vertexColorMapXp
        {
            get { return Mod.vertexColorMapXp; }
            set { Mod.vertexColorMapXp = value; }
        }
        [ParserTarget("mapYn")]
        public MapSOTileParserRGB<MapSOTile> vertexColorMapYn
        {
            get { return Mod.vertexColorMapYn; }
            set { Mod.vertexColorMapYn = value; }
        }
        [ParserTarget("mapYp")]
        public MapSOTileParserRGB<MapSOTile> vertexColorMapYp
        {
            get { return Mod.vertexColorMapYp; }
            set { Mod.vertexColorMapYp = value; }
        }
        [ParserTarget("mapZn")]
        public MapSOTileParserRGB<MapSOTile> vertexColorMapZn
        {
            get { return Mod.vertexColorMapZn; }
            set { Mod.vertexColorMapZn = value; }
        }
        [ParserTarget("mapZp")]
        public MapSOTileParserRGB<MapSOTile> vertexColorMapZp
        {
            get { return Mod.vertexColorMapZp; }
            set { Mod.vertexColorMapZp = value; }
        }
        [ParserTarget("clampRange")]
        public NumericParser<Single> clampRange
        {
            get { return Mod.edgeClampRange; }
            set { Mod.edgeClampRange = value; }
        }
    }
}
