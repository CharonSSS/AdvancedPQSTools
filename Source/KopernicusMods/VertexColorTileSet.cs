﻿/* 
 * This code is adapted from KopernicusExpansion-Continued
 * Available from https://github.com/StollD/KopernicusExpansion-Continued
 */

using System;
using System.Diagnostics.CodeAnalysis;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.Configuration.Parsing;
using Kopernicus.Components;
using Kopernicus.Configuration.ModLoader;

namespace AdvancedPQSTools
{
    [RequireConfigType(ConfigType.Node)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class VertexColorTileSet : ModLoader<PQSMod_VertexColorTileSet>
    {
        // The map textures for the planet
        [ParserTarget("tilePath")]
        public StringCollectionParser tilepath
        {
            get { return Mod.path; }
            set { Mod.path = value; }
        }

        [ParserTarget("tileSize")]
        public NumericParser<int> tilesize
        {
            get { return Mod.size; }
            set { Mod.size = value; }
        }
    }
}
