// ============================================================================
//  Hapke BRDF Functions for Scaled Space Rendering
//  Uses native cubemap sampling — no manual face selection required
// ============================================================================

// Precomputed constants to avoid repeated division by PI
#define INV_PI    0.31830988618f
#define TWO_INV_PI 0.63661977236f

// Double-lobed Henyey-Greenstein phase function (Hapke 6.7a)
float PI_HG2(float g, float b, float c)
{
    float b2 = b * b;
    float oneMinusB2 = 1.0 - b2;
    float cosG = cos(g);
    
    float backDenom = pow(1.0 - 2.0 * b * cosG + b2, 1.5);
    float forwDenom = pow(1.0 + 2.0 * b * cosG + b2, 1.5);
    
    float backwardLobe = ((1.0 + c) * 0.5) * (oneMinusB2 / backDenom);
    float forwardLobe  = ((1.0 - c) * 0.5) * (oneMinusB2 / forwDenom);
    return backwardLobe + forwardLobe;
}

// Shadow Hiding Opposition Effect (Hapke 9.22)
float B_s(float g, float h_s)
{
    return 1.0 / (1.0 + (1.0 / h_s) * tan(g * 0.5));
}

// Coherent Backscatter Opposition Effect (Hapke 9.43)
float B_c(float g, float h_c, float K)
{
    float tanHalfG_hc = (1.0 / h_c) * tan(g * 0.5);
    return 1.0 / (1.0 + (1.3 + K) * (tanHalfG_hc + tanHalfG_hc * tanHalfG_hc));
}

// Albedo factor (Hapke 8.22b)
float gamma(float w)
{
    return sqrt(1.0 - w);
}

// Diffusive reflectance (Hapke 8.25)
float r_0(float w)
{
    return 2.0 / (1.0 + gamma(w));
}

// Ambartsumian-Chandrasekhar H function (Hapke 8.56)
float H_Func(float x, float w)
{
    float r0 = r_0(w);
    return 1.0 / (1.0 - w * x * (r0 + (1.0 - 2.0 * r0 * x) * 0.5 * log((1.0 + x) / x)));
}

// Roughness helper functions (Hapke 12.45a-c)
float E_1(float x, float theta)
{
    float cotProduct = (1.0 / tan(theta)) * (1.0 / tan(x));
    return exp(-TWO_INV_PI * cotProduct);
}

float E_2(float x, float theta)
{
    float cotProduct = (1.0 / tan(theta)) * (1.0 / tan(x));
    return exp(-INV_PI * cotProduct * cotProduct);
}

float X(float theta)
{
    float tanTheta = tan(theta);
    return rsqrt(1.0 + UNITY_PI * tanTheta * tanTheta);
}

// Effective cosine of emission/incidence (Hapke 12.48)
float n_e(float x, float theta)
{
    float e2 = E_2(x, theta);
    float e1 = E_1(x, theta);
    return X(theta) * (cos(x) + sin(x) * tan(theta) * (e2 / (2.0 - e1)));
}

// Azimuthal roughness factor (Hapke 12.51)
float f(float psi)
{
    return exp(-2.0 * tan(psi * 0.5));
}

// Surface roughness correction (Hapke 12.2.2-12.2.3)
float S(float mu_0, float mu, float psi, float theta)
{
    float i_angle = acos(mu_0);
    float e_angle = acos(mu);
    
    float xTheta = X(theta);
    float ne_e = n_e(e_angle, theta);
    float ne_i = n_e(i_angle, theta);
    float fPsi = f(psi);
    float sinHalfPsi2 = pow(sin(psi * 0.5), 2.0);
    
    float mu_e_over_ne_e;
    float mu_0_over_ne_i = mu_0 / ne_i;
    
    if (e_angle > i_angle)
    {
        // (12.47) + (12.50)
        float e1_e = E_1(e_angle, theta);
        float e2_e = E_2(e_angle, theta);
        float e1_i = E_1(i_angle, theta);
        float e2_i = E_2(i_angle, theta);
        
        float mu_e = xTheta * (cos(e_angle) + sin(e_angle) * tan(theta) * 
                     (e2_e - sinHalfPsi2 * e2_i) / (2.0 - e1_e - (psi * INV_PI) * e1_i));
        
        mu_e_over_ne_e = mu_e / ne_e;
        return mu_e_over_ne_e * mu_0_over_ne_i * (xTheta / (1.0 - fPsi + fPsi * xTheta * mu_0_over_ne_i));
    }
    else
    {
        // (12.53) + (12.54)
        float e1_e = E_1(e_angle, theta);
        float e2_e = E_2(e_angle, theta);
        float e1_i = E_1(i_angle, theta);
        float e2_i = E_2(i_angle, theta);
        float cosPsi = cos(psi);
        
        float mu_e = xTheta * (cos(e_angle) + sin(e_angle) * tan(theta) * 
                     (cosPsi * e2_i + sinHalfPsi2 * e2_e) / (2.0 - e1_i - (psi * INV_PI) * e1_e));
        
        mu_e_over_ne_e = mu_e / ne_e;
        return mu_e_over_ne_e * mu_0_over_ne_i * (xTheta / (1.0 - fPsi + fPsi * xTheta * (mu / ne_e)));
    }
}

// ============================================================================
//  Main Hapke Lighting Function
// ============================================================================

float3 CalculateHapkeLighting(float4 col, float3 worldNormal, float3 viewDir, float shadow, float3 lightDir,
                              float _Blend, float _GammaBoost, float _LightBoost, float _porosityCoeffient, float _Theta, float4 scatterData, float4 surgeData)
{
    // Main light
    float NdotL = saturate(dot(worldNormal, lightDir)) * shadow;

    NdotL = pow(NdotL, _Hapke);
    float3 H = normalize(lightDir + viewDir);
    float NdotH = saturate(dot(worldNormal, H));

    float mu_0 = clamp(dot(lightDir, worldNormal), 0.0001, 1);
    float mu   = clamp(dot(viewDir, worldNormal), 0.0001, 1);
    float g    = clamp(acos(dot(lightDir, viewDir)), 0, UNITY_PI);

    // Unpack scattering parameters
    float w = scatterData.r;
    float b = scatterData.g;
    float c = 2.0 * scatterData.b - 1.0;

    // Unpack surge parameters
    float b_s0 = 2.0 * surgeData.r;
    float h_s  = surgeData.g;
    float b_c0 = 2.0 * surgeData.b;
    float h_c  = surgeData.a;

    float theta = (_Theta * UNITY_PI) / 180.0;
    
    // Fresnel reflections
    GET_REFLECTION_COLOR
    GET_REFRACTION_COLOR
    float fresnel = FresnelEffect(worldNormal, viewDir, _FresnelPower);

    float spec = pow(NdotH, _SpecularPower) * _SpecularIntensity * col.a * shadow;

    float3 ambient    = UNITY_LIGHTMODEL_AMBIENT.rgb * col.rgb;
    float3 diffuse    = _LightColor0.rgb * GammaToLinearSpace(col.rgb);
    float3 specular   = spec * _LightColor0.rgb;
    float3 reflection = fresnel * reflColor * col.a * _EnvironmentMapFactor;
    float3 refraction = (1.0 - fresnel) * refrColor * _RefractionIntensity;

    // Hapke BRDF
    float lighting = (_Blend * (mu_0 / (mu_0 + mu)) + (1.0 - _Blend) * mu_0) * shadow;
    
    float K = _porosityCoeffient;
    float phaseAndSHOE = PI_HG2(g, b, c) * (1.0 + b_s0 * B_s(g, h_s));
    float chandrasekhar = H_Func(mu_0 / K, w) * H_Func(mu / K, w) - 1.0;
    float CBOE = 1.0 + b_c0 * B_c(g, h_c, K);
    float roughness = S(mu_0, mu, g, theta);
    
    float3 reflectance = (diffuse * 0.25) * (K * lighting * (phaseAndSHOE + chandrasekhar) * CBOE * roughness);
    reflectance = pow(reflectance, _GammaBoost);
    reflectance *= _LightBoost;

    return LinearToGammaSpace(reflectance) + ambient + specular + reflection + refraction * NdotL;
}
