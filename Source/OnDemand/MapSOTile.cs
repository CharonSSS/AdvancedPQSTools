using Kopernicus;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.ConfigParser.Interfaces;
using Kopernicus.Configuration.Parsing;
using Kopernicus.OnDemand;
using KSPTextureLoader;
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;


namespace AdvancedPQSTools.OnDemand
{
    public class MapSOTile : MapSODemand, ILoadOnDemand, IPreloadOnDemand
    {
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

    public class MapSOTileParserGrayScale<T> : BaseLoader, IParsable, ITypeParser<T> where T : MapSO
    {
        /// <summary>
        /// The value that is being parsed
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Parse the Value from a string
        /// </summary>
        public void SetFromString(String s)
        {
            // Should we use OnDemand?
            Boolean useOnDemand = OnDemandStorage.UseOnDemand;

            if (s.StartsWith("BUILTIN/"))
            {
                s = s.Substring(8);
                Value = Utility.FindMapSO<T>(s);
            }
            else
            {
                // are we on-demand? Don't load now.
                if (useOnDemand)
                {
                    if (!Utility.TextureExists(s))
                    {
                        return;
                    }

                    MapSOTile map = ScriptableObject.CreateInstance<MapSOTile>();
                    map.Path = s;
                    map.Depth = MapSO.MapDepth.Greyscale;
                    map.AutoLoad = OnDemandStorage.OnDemandLoadOnMissing;
                    OnDemandStorage.AddMap(generatedBody.name, map);
                    Value = map as T;
                }
                else // Load the texture
                {
                    var options = new TextureLoadOptions
                    {
                        Hint = TextureLoadHint.Synchronous,
                        Unreadable = false
                    };
                    var handle = TextureLoader.LoadTexture<Texture2D>(s, options);
                    Texture2D map;
                    try
                    {
                        map = handle.TakeTexture();
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"Failed to load texture {s}");
                        Debug.LogException(e);
                        return;
                    }

                    // Create a new map script object
                    Value = ScriptableObject.CreateInstance<T>();
                    Value.CreateMap(MapSO.MapDepth.Greyscale, map);
                }
            }

            if (Value != null)
            {
                Value.name = s;
            }
        }

        /// <summary>
        /// Convert the value to a parsable String
        /// </summary>
        public String ValueToString()
        {
            if (Value == null)
            {
                return null;
            }

            if (GameDatabase.Instance.ExistsTexture(Value.name) || TextureLoader.TextureExists(Value.name))
            {
                return Value.name;
            }

            return "BUILTIN/" + Value.name;
        }

        /// <summary>
        /// Create a new MapSOParser_GreyScale
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public MapSOTileParserGrayScale()
        {

        }

        /// <summary>
        /// Create a new MapSOParser_GreyScale from an already existing Texture
        /// </summary>
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public MapSOTileParserGrayScale(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Convert Parser to Value
        /// </summary>
        public static implicit operator T(MapSOTileParserGrayScale<T> parser)
        {
            return parser.Value;
        }

        /// <summary>
        /// Convert Value to Parser
        /// </summary>
        public static implicit operator MapSOTileParserGrayScale<T>(T value)
        {
            return new MapSOTileParserGrayScale<T>(value);
        }
    }

    // Parser for a MapSO RGB
    [RequireConfigType(ConfigType.Value)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class MapSOTileParserRGB<T> : BaseLoader, IParsable, ITypeParser<T> where T : MapSO
    {
        /// <summary>
        /// The value that is being parsed
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Parse the Value from a string
        /// </summary>
        public void SetFromString(String s)
        {
            // Should we use OnDemand?
            Boolean useOnDemand = OnDemandStorage.UseOnDemand;

            if (s.StartsWith("BUILTIN/"))
            {
                s = s.Substring(8);
                Value = Utility.FindMapSO<T>(s);
            }
            else
            {
                // check if OnDemand.
                if (useOnDemand)
                {
                    if (!Utility.TextureExists(s))
                    {
                        return;
                    }

                    MapSOTile map = ScriptableObject.CreateInstance<MapSOTile>();
                    map.Path = s;
                    map.Depth = MapSO.MapDepth.RGB;
                    map.AutoLoad = OnDemandStorage.OnDemandLoadOnMissing;
                    OnDemandStorage.AddMap(generatedBody.name, map);
                    Value = map as T;
                }
                else
                {
                    var options = new TextureLoadOptions
                    {
                        Hint = TextureLoadHint.Synchronous,
                        Unreadable = false
                    };
                    var handle = TextureLoader.LoadTexture<Texture2D>(s, options);
                    Texture2D map;
                    try
                    {
                        map = handle.TakeTexture();
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"Failed to load texture {s}");
                        Debug.LogException(e);
                        return;
                    }

                    // Create a new map script object
                    Value = ScriptableObject.CreateInstance<T>();
                    Value.CreateMap(MapSO.MapDepth.RGB, map);
                }
            }

            if (Value != null)
            {
                Value.name = s;
            }
        }

        /// <summary>
        /// Convert the value to a parsable String
        /// </summary>
        public String ValueToString()
        {
            if (Value == null)
            {
                return null;
            }

            if (GameDatabase.Instance.ExistsTexture(Value.name) || TextureLoader.TextureExists(Value.name))
            {
                return Value.name;
            }

            return "BUILTIN/" + Value.name;
        }

        /// <summary>
        /// Create a new MapSOParser_RGB
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public MapSOTileParserRGB()
        {

        }

        /// <summary>
        /// Create a new MapSOParser_RGB from an already existing Texture
        /// </summary>
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public MapSOTileParserRGB(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Convert Parser to Value
        /// </summary>
        public static implicit operator T(MapSOTileParserRGB<T> parser)
        {
            return parser.Value;
        }

        /// <summary>
        /// Convert Value to Parser
        /// </summary>
        public static implicit operator MapSOTileParserRGB<T>(T value)
        {
            return new MapSOTileParserRGB<T>(value);
        }
    }

    // Parser for a MapSO RGBA
    [RequireConfigType(ConfigType.Value)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class MapSOTileParserRGBA<T> : BaseLoader, IParsable, ITypeParser<T> where T : MapSO
    {
        /// <summary>
        /// The value that is being parsed
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Parse the Value from a string
        /// </summary>
        public void SetFromString(String s)
        {
            // Should we use OnDemand?
            Boolean useOnDemand = OnDemandStorage.UseOnDemand;

            if (s.StartsWith("BUILTIN/"))
            {
                s = s.Substring(8);
                Value = Utility.FindMapSO<T>(s);
            }
            else
            {
                // check if OnDemand.
                if (useOnDemand)
                {
                    if (!Utility.TextureExists(s))
                    {
                        return;
                    }

                    MapSOTile map = ScriptableObject.CreateInstance<MapSOTile>();
                    map.Path = s;
                    map.Depth = MapSO.MapDepth.RGBA;
                    map.AutoLoad = OnDemandStorage.OnDemandLoadOnMissing;
                    OnDemandStorage.AddMap(generatedBody.name, map);
                    Value = map as T;
                }
                else
                {
                    var options = new TextureLoadOptions
                    {
                        Hint = TextureLoadHint.Synchronous,
                        Unreadable = false
                    };
                    var handle = TextureLoader.LoadTexture<Texture2D>(s, options);
                    Texture2D map;
                    try
                    {
                        map = handle.TakeTexture();
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"Failed to load texture {s}");
                        Debug.LogException(e);
                        return;
                    }

                    // Create a new map script object
                    Value = ScriptableObject.CreateInstance<T>();
                    Value.CreateMap(MapSO.MapDepth.RGBA, map);
                }
            }

            if (Value != null)
            {
                Value.name = s;
            }
        }

        /// <summary>
        /// Convert the value to a parsable String
        /// </summary>
        public String ValueToString()
        {
            if (Value == null)
            {
                return null;
            }

            if (GameDatabase.Instance.ExistsTexture(Value.name) || TextureLoader.TextureExists(Value.name))
            {
                return Value.name;
            }

            return "BUILTIN/" + Value.name;
        }

        /// <summary>
        /// Create a new MapSOParser_RGBA
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public MapSOTileParserRGBA()
        {

        }

        /// <summary>
        /// Create a new MapSOParser_RGB from an already existing Texture
        /// </summary>
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public MapSOTileParserRGBA(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Convert Parser to Value
        /// </summary>
        public static implicit operator T(MapSOTileParserRGBA<T> parser)
        {
            return parser.Value;
        }

        /// <summary>
        /// Convert Value to Parser
        /// </summary>
        public static implicit operator MapSOTileParserRGBA<T>(T value)
        {
            return new MapSOTileParserRGBA<T>(value);
        }
    }

    // Parser for a MapSO
    [RequireConfigType(ConfigType.Value)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class MapSOTileParserHeightAlpha<T> : BaseLoader, IParsable, ITypeParser<T> where T : MapSO
    {
        /// <summary>
        /// The value that is being parsed
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Parse the Value from a string
        /// </summary>
        public void SetFromString(String s)
        {
            // Should we use OnDemand?
            Boolean useOnDemand = OnDemandStorage.UseOnDemand;

            if (s.StartsWith("BUILTIN/"))
            {
                s = s.Substring(8);
                Value = Utility.FindMapSO<T>(s);
            }
            else
            {
                // are we on-demand? Don't load now.
                if (useOnDemand)
                {
                    Debug.Log("ONDEMAND!");
                    if (!Utility.TextureExists(s))
                    {
                        return;
                    }

                    MapSOTile map = ScriptableObject.CreateInstance<MapSOTile>();
                    map.Path = s;
                    map.Depth = MapSO.MapDepth.HeightAlpha;
                    map.AutoLoad = OnDemandStorage.OnDemandLoadOnMissing;
                    OnDemandStorage.AddMap(generatedBody.name, map);
                    Value = map as T;
                }
                else // Load the texture
                {
                    Debug.Log("FALLBACK!");
                    var options = new TextureLoadOptions
                    {
                        Hint = TextureLoadHint.Synchronous,
                        Unreadable = false
                    };
                    var handle = TextureLoader.LoadTexture<Texture2D>(s, options);
                    Texture2D map;
                    try
                    {
                        map = handle.TakeTexture();
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"Failed to load texture {s}");
                        Debug.LogException(e);
                        return;
                    }

                    // Create a new map script object
                    Value = ScriptableObject.CreateInstance<T>();
                    Value.CreateMap(MapSO.MapDepth.HeightAlpha, map);
                }
            }

            if (Value != null)
            {
                Value.name = s;
            }
        }

        /// <summary>
        /// Convert the value to a parsable String
        /// </summary>
        public String ValueToString()
        {
            if (Value == null)
            {
                return null;
            }

            if (GameDatabase.Instance.ExistsTexture(Value.name) || TextureLoader.TextureExists(Value.name))
            {
                return Value.name;
            }

            return "BUILTIN/" + Value.name;
        }

        /// <summary>
        /// Create a new MapSOParser_GreyScale
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public MapSOTileParserHeightAlpha()
        {

        }

        /// <summary>
        /// Create a new MapSOParser_GreyScale from an already existing Texture
        /// </summary>
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public MapSOTileParserHeightAlpha(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Convert Parser to Value
        /// </summary>
        public static implicit operator T(MapSOTileParserHeightAlpha<T> parser)
        {
            return parser.Value;
        }

        /// <summary>
        /// Convert Value to Parser
        /// </summary>
        public static implicit operator MapSOTileParserHeightAlpha<T>(T value)
        {
            return new MapSOTileParserHeightAlpha<T>(value);
        }
    }
}
