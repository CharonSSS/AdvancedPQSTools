
// Sampler state workaround for Unity 2019's per-stage sampler budget
// Terrain detail textures still need shared sampler states since they're 2D
// Cubemaps use their own built-in sampler via samplerCUBE — no slot pressure

#if defined (SHADER_STAGE_DOMAIN)
#define PARALLAX_SAMPLER_STATE sampler_HeightMap
#endif

#if defined (SHADER_STAGE_FRAGMENT)

#if defined (PARALLAX_SINGLE_LOW)
#define PARALLAX_SAMPLER_STATE sampler_MainTexLow
#endif
#if defined (PARALLAX_SINGLE_MID)
#define PARALLAX_SAMPLER_STATE sampler_MainTexMid
#endif
#if defined (PARALLAX_SINGLE_HIGH)
#define PARALLAX_SAMPLER_STATE sampler_MainTexHigh
#endif
#if defined (PARALLAX_DOUBLE_LOWMID)
#define PARALLAX_SAMPLER_STATE sampler_MainTexMid
#endif
#if defined (PARALLAX_DOUBLE_MIDHIGH)
#define PARALLAX_SAMPLER_STATE sampler_MainTexMid
#endif
#if defined (PARALLAX_FULL)
#define PARALLAX_SAMPLER_STATE sampler_MainTexMid
#endif

#endif

#if !defined (PARALLAX_SAMPLER_STATE)
#define PARALLAX_SAMPLER_STATE sampler_linear_repeat
#endif

SamplerState PARALLAX_SAMPLER_STATE;

// Planet cubemaps — replaces 6x Texture2D per map (18 total -> 3 cubemaps)
// Hardware handles face selection, filtering, and seam correction natively
samplerCUBE _ColorCube;
samplerCUBE _BumpCube;
samplerCUBE _HeightCube;

// Legacy 2D height sampler for terrain detail / domain stage displacement
sampler2D _HeightMap;

sampler2D _ScatteringTex;
sampler2D _SurgeTex;
sampler2D _ResourceMap;

#if defined (SCALED_EMISSIVE_MAP)
sampler2D _EmissiveMap;
float _EmissiveIntensity;
#endif

// Default sampler used for most textures
SamplerState default_linear_repeat_sampler;

#if defined (ATMOSPHERE)
Texture2D _AtmosphereRimMap;
SamplerState point_clamp_sampler_AtmosphereRimMap;
#endif

float4 _OceanColor;

float _OceanSpecularPower;
float _OceanSpecularIntensity;

float _MinRadialAltitude;
float _MaxRadialAltitude;
float _WorldPlanetRadius;

float _Blend;
float _PlanetBumpScale;
float _LightBoost;
float _GammaBoost;
float _porosityCoeffient;
float _Theta;

float4x4 unity_ObjectToWorldNoTR;
textureCUBE _Skybox;
SamplerState sampler_Skybox;
half4 _Skybox_HDR;
float4x4 _SkyboxRotation;

float _AtmosphereThickness;

float _OceanAltitude;

// To save us on a multicompile
uint _DisableDisplacement;
