
// We're at the sampler2D limit, so we need to group the terrain texture samplers into one
// Usually you'd be able to do sampler_linear_repeat_anisoX but that's not a thing in Unity 2019 (:
// What's worse is Unity locks down SamplerStates so you can't even define your own!
// Mickey mouse engine

// Rules for this workaround - all textures using another texture's samplerstate MUST contribute to the result, or Unity will error out

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
#define PARALLAX_SAMPLER_STAGE sampler_linear_repeat
#endif

SamplerState PARALLAX_SAMPLER_STATE;

// Samplers
Texture2D _ColorMapXn;
Texture2D _ColorMapXnBR;
Texture2D _ColorMapXnTL;
Texture2D _ColorMapXnTR;

Texture2D _ColorMapXpBL;
Texture2D _ColorMapXpBR;
Texture2D _ColorMapXpTL;
Texture2D _ColorMapXpTR;

Texture2D _ColorMapYnBL;
Texture2D _ColorMapYnBR;
Texture2D _ColorMapYnTL;
Texture2D _ColorMapYnTR;

Texture2D _ColorMapYpBL;
Texture2D _ColorMapYpBR;
Texture2D _ColorMapYpTL;
Texture2D _ColorMapYpTR;

Texture2D _ColorMapZnBL;
Texture2D _ColorMapZnBR;
Texture2D _ColorMapZnTL;
Texture2D _ColorMapZnTR;

Texture2D _ColorMapZpBL;
Texture2D _ColorMapZpBR;
Texture2D _ColorMapZpTL;
Texture2D _ColorMapZpTR;

Texture2D _BumpMapXnBL;
Texture2D _BumpMapXnBR;
Texture2D _BumpMapXnTL;
Texture2D _BumpMapXnTR;

Texture2D _BumpMapXpBL;
Texture2D _BumpMapXpBR;
Texture2D _BumpMapXpTL;
Texture2D _BumpMapXpTR;

Texture2D _BumpMapYnBL;
Texture2D _BumpMapYnBR;
Texture2D _BumpMapYnTL;
Texture2D _BumpMapYnTR;

Texture2D _BumpMapYpBL;
Texture2D _BumpMapYpBR;
Texture2D _BumpMapYpTL;
Texture2D _BumpMapYpTR;

Texture2D _BumpMapZnBL;
Texture2D _BumpMapZnBR;
Texture2D _BumpMapZnTL;
Texture2D _BumpMapZnTR;

Texture2D _BumpMapZpBL;
Texture2D _BumpMapZpBR;
Texture2D _BumpMapZpTL;
Texture2D _BumpMapZpTR;

Texture2D _HeightMapXn;
Texture2D _HeightMapXp;
Texture2D _HeightMapYn;
Texture2D _HeightMapYp;
Texture2D _HeightMapZn;
Texture2D _HeightMapZp;
sampler2D _HeightMap;

SamplerState default_trilinear_clamp_aniso16_sampler;
SamplerState sampler_ColorMapXn;
SamplerState sampler_HeightMapXn;
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
//sampler2D _AtmosphereRimMap;

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