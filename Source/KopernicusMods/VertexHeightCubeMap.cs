using AdvancedPQSTools.OnDemand;
using Kopernicus.Components;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.Configuration.ModLoader;
using Kopernicus.Configuration.Parsing;
using Kopernicus.OnDemand;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedPQSTools
{
    [RequireConfigType(ConfigType.Node)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class VertexHeightCubeMap : ModLoader<PQSMod_VertexHeightCubeMap>
    {
        // The map textures for the planet
        [ParserTarget("mapXn")]
        public MapSOParserGreyScale<MapSOTile> vertexHeightMapXn
        {
            get { return Mod.vertexHeightMapXn; }
            set { Mod.vertexHeightMapXn = value; }
        }
        [ParserTarget("mapXp")]
        public MapSOParserGreyScale<MapSOTile> vertexHeightMapXp
        {
            get { return Mod.vertexHeightMapXp; }
            set { Mod.vertexHeightMapXp = value; }
        }
        [ParserTarget("mapYn")]
        public MapSOParserGreyScale<MapSOTile> vertexHeightMapYn
        {
            get { return Mod.vertexHeightMapYn; }
            set { Mod.vertexHeightMapYn = value; }
        }
        [ParserTarget("mapYp")]
        public MapSOParserGreyScale<MapSOTile> vertexHeightMapYp
        {
            get { return Mod.vertexHeightMapYp; }
            set { Mod.vertexHeightMapYp = value; }
        }
        [ParserTarget("mapZn")]
        public MapSOParserGreyScale<MapSOTile> vertexHeightMapZn
        {
            get { return Mod.vertexHeightMapZn; }
            set { Mod.vertexHeightMapZn = value; }
        }
        [ParserTarget("mapZp")]
        public MapSOParserGreyScale<MapSOTile> vertexHeightMapZp
        {
            get { return Mod.vertexHeightMapZp; }
            set { Mod.vertexHeightMapZp = value; }
        }
        // Height map offset
        [ParserTarget("offset")]
        public NumericParser<Double> heightMapOffset
        {
            get { return Mod.heightMapOffset; }
            set { Mod.heightMapOffset = value; }
        }

        // Height map offset
        [ParserTarget("deformity")]
        public NumericParser<Double> heightMapDeformity
        {
            get { return Mod.heightMapDeformity; }
            set { Mod.heightMapDeformity = value; }
        }
        [ParserTarget("clampRange")]
        public NumericParser<Double> clampRange
        {
            get { return Mod.edgeClampRange; }
            set { Mod.edgeClampRange = value; }
        }
        // Height map offset
        [ParserTarget("scaleDeformityByRadius")]
        public NumericParser<Boolean> scaleDeformityByRadius
        {
            get { return Mod.scaleDeformityByRadius; }
            set { Mod.scaleDeformityByRadius = value; }
        }
    }
}
