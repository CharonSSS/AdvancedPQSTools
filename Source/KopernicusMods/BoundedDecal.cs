/* 
 * This code is adapted from KopernicusExpansion-Continued
 * Available from https://github.com/StollD/KopernicusExpansion-Continued
 */

using System;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.Configuration.ModLoader;
using Kopernicus.Configuration.Parsing;

namespace AdvancedPQSTools
{
    public class BoundedDecal : ModLoader<PQSMod_BoundedDecal>
    {
        // The map texture for the planet
        [ParserTarget("allowScatters")]
        public NumericParser<Boolean> allowScatters
        {
            get { return Mod.allowScatters; }
            set { Mod.allowScatters = value; }
        }
        // The map texture for the planet
        [ParserTarget("heightMap")]
        public MapSOParserLarge<MapSO> heightMap
        {
            get { return Mod.heightMap; }
            set { Mod.heightMap = value; }
        }
        [ParserTarget("colorMap")]
        public MapSOParserRGB<MapSO> colorMap
        {
            get { return Mod.colorMap; }
            set { Mod.colorMap = value; }
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

        // Max Longitude of the decal
        [ParserTarget("maxLong")]
        public NumericParser<Double> maxLong
        {
            get { return Mod.maxLong; }
            set { Mod.maxLong = value; }
        }
        // Min Longitude of the decal
        [ParserTarget("minLong")]
        public NumericParser<Double> minLong
        {
            get { return Mod.minLong; }
            set { Mod.minLong = value; }
        }
        // Max Latitude of the decal
        [ParserTarget("maxLat")]
        public NumericParser<Double> maxLat
        {
            get { return Mod.maxLat; }
            set { Mod.maxLat = value; }
        }
        // Max Latitude of the decal
        [ParserTarget("minLat")]
        public NumericParser<Double> minLat
        {
            get { return Mod.minLat; }
            set { Mod.minLat = value; }
        }
    }
}