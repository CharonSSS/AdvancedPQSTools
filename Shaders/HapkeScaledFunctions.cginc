// Double-lobed Henyey-Greenstein function (6.7a)
float PI_HG2(float g, float b, float c)
{
    float backwardLobe = ((1.0+c)/2.0) * ((1-b*b) /  pow(1.0-2.0*b*cos(g)+b*b, 1.5));
    float forwardLobe =  ((1.0-c)/2.0) * ((1-b*b) /  pow(1.0+2.0*b*cos(g)+b*b, 1.5));
    return backwardLobe + forwardLobe;
}
// Shadow Hiding Opposition function (9.22)
float B_s(float g, float h_s)
{
    return 1.0/(1.0+(1.0/h_s)*tan(g/2.0));
}
// (9.43)
float B_c(float g, float h_c, float K)
{
    return 1.0 / (1.0 + (1.3 + K)*(1.0/h_c * tan(g/2.0) + pow(1.0/h_c * tan(g/2.0), 2.0)));
}
// Albedo Factor: (8.22b)
float gamma(float w)
{
    return sqrt(1.0-w);
}
// Diffusive Reflectance: (8.25)
float r_0(float w)
{
    return 2.0 / (1.0 +  gamma(w));
}
// Ambartsumian–Chandrasekhar H function: (8.56)
float H_Func(float x, float w)
{
    float value = 1.0/(1.0 - w * x * (r_0(w) + (1.0-2.0*r_0(w)*x)/2.0 * log((1.0+x)/x)));
    return value;
}
// (12.45b)
// Verified
float E_1(float x, float theta)
{
    return exp(-1.0*(2.0/3.14159)*(1.0/tan(theta))*(1.0/tan(x)));
    
}
// (12.45c)
float E_2(float x, float theta)
{
    return exp( -1.0 * (1.0/3.14159) * pow(1.0/tan(theta), 2.0) * pow(1.0/tan(x), 2.0)); 
}
// (12.45a)
float X(float theta)
{
    return 1.0 /sqrt(1.0 + 3.14159*pow(tan(theta), 2.0));
}
// (12.48)
float n_e(float x, float theta)
{
    return X(theta)*(cos(x)+sin(x)*tan(theta) * (E_2(x, theta) / (2.0 - E_1(x, theta))));
}
// (12.51)
float f(float psi)
{
    return exp(-2.0 * tan(psi/2.0));
}
// Section (12.2.2 - 12.2.3)
float S(float mu_0, float mu, float psi, float theta)
{
    float i = acos(mu_0);
    float e = acos(mu);
    if (e > i)
    {
        // (12.47)
        // Verified
        float mu_e = X(theta) * (cos(e) + sin(e)*tan(theta) * (E_2(e, theta) - pow(sin(psi/2.0), 2.0) * E_2(i, theta)) / (2.0 - E_1(e, theta) - (psi / 3.14159) * E_1(i, theta)));
        
        // (12.50)
        // Verified        
        return ((mu_e/n_e(e, theta))  * (mu_0 / n_e(i, theta)) * (X(theta) / (1.0 - f(psi) + f(psi)*X(theta) * (mu_0 / n_e(i, theta)))));
    }
    else
    {
        // (12.53)
        // Verified
        float mu_e = X(theta) * (cos(e) + sin(e)*tan(theta) * (cos(psi) * E_2(i, theta) + pow(sin(psi/2.0), 2.0) * E_2(e, theta)) / (2.0 - E_1(i, theta) - (psi / 3.14159) * E_1(e, theta)));

        // (12.54)
        // Verified
        return ((mu_e/n_e(e, theta))  * (mu_0 / n_e(i, theta)) * (X(theta) / (1.0 - f(psi) + f(psi)*X(theta) * (mu / n_e(e, theta)))));
    }
}

float3 CalculateHapkeLighting(float4 col, float3 worldNormal, float3 viewDir, float shadow, float3 lightDir,
                              float _Blend, float _GammaBoost, float _LightBoost, float _porosityCoeffient, float _Theta, float4 scatterData, float4 surgeData)
{
	// Main light
    float NdotL = saturate(dot(worldNormal, lightDir)) * shadow;
    NdotL = pow(NdotL, _Hapke);
    float3 H = normalize(lightDir + viewDir);
    float NdotH = saturate(dot(worldNormal, H));

    float mu_0 = clamp(dot(lightDir,worldNormal),0.0001,1);
    float mu = clamp(dot(viewDir,worldNormal),0.0001,1);
    float g = clamp(acos(dot(lightDir,viewDir)), 0, 3.14159);

    float w = scatterData.r;
    float b = scatterData.g;
    float c = 2*(scatterData.b)-1;

    float b_s0 = 2*(surgeData.r);
    float h_s = surgeData.g;
    float b_c0 = 2*(surgeData.b);
    float h_c = (surgeData.a);

    float theta = (_Theta * 3.14159)/180;
    
	// Fresnel reflections
    GET_REFLECTION_COLOR
    GET_REFRACTION_COLOR
    float fresnel = FresnelEffect(worldNormal, viewDir, _FresnelPower);

    float spec = pow(NdotH, _SpecularPower) * _LightColor0.rgb * _SpecularIntensity * col.a * shadow;

    float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * col.rgb;
    float3 diffuse = _LightColor0.rgb * GammaToLinearSpace(col.rgb);
    float3 specular = spec * _LightColor0.rgb;
    float3 reflection = fresnel * reflColor * col.a * _EnvironmentMapFactor; // For refraction
    float3 refraction = (1 - fresnel) * refrColor * _RefractionIntensity;
    //reflection *= shadow + UNITY_LIGHTMODEL_AMBIENT;

    float lighting = ((_Blend)*(mu_0 / (mu_0+mu)) + (1-_Blend)*(mu_0)) * shadow;
    float3 reflectance = (diffuse/4) * (_porosityCoeffient * (lighting) * (PI_HG2(g, b, c)*(1+b_s0*B_s(g, h_s))+(H_Func(mu_0/_porosityCoeffient, w)*H_Func(mu/_porosityCoeffient, w)-1)) * (1+b_c0*B_c(g, h_c, _porosityCoeffient)) * S(mu_0, mu, g, theta));
    reflectance = pow(reflectance, _GammaBoost);
    reflectance *= _LightBoost;

    float3 scattering = 0;
    
    return LinearToGammaSpace(reflectance) + ambient + specular + reflection + refraction * NdotL + scattering;
}

float4 CubeDerivatives(float2 uv, float scale)
{
    //Make the UV continuous.
    float2 uvS = abs(uv - (.5*scale));

    float2 uvCont;
    uvCont.x = max(uvS.x, uvS.y);
    uvCont.y = min(uvS.x, uvS.y);

    return float4(ddx(uvCont), ddy(uvCont));
}
half4 SampleCubeMapLevel(Texture2D texXn, Texture2D texXp, Texture2D texYn, Texture2D texYp, Texture2D texZn, Texture2D texZp, float3 cubeVect)
{
    float3 cubeVectNorm = normalize(cubeVect);
    float3 cubeVectNormAbs = abs(cubeVectNorm);

    half zxlerp = step(cubeVectNormAbs.x, cubeVectNormAbs.z);
    half nylerp = step(cubeVectNormAbs.y, max(cubeVectNormAbs.x, cubeVectNormAbs.z));

    half s = lerp(cubeVectNorm.x, cubeVectNorm.z, zxlerp);
    s = sign(lerp(cubeVectNorm.y, s, nylerp));

    half3 detailCoords = lerp(half3(1, -s, -1)*cubeVectNorm.xzy, half3(1, s, -1)*cubeVectNorm.zxy, zxlerp);
    detailCoords = lerp(half3(1, 1, s)*cubeVectNorm.yxz, detailCoords, nylerp);

    half2 uv = ((.5*detailCoords.yz) / abs(detailCoords.x)) + .5;

    //this fixes UV discontinuity on Y-X seam by swapping uv coords in derivative calcs when in the X quadrants.
    float4 uvdd = CubeDerivatives(uv, 1);


    half4 sampxn = texXn.SampleLevel(sampler_HeightMapXn, uv, 0);
    half4 sampxp = texXp.SampleLevel(sampler_HeightMapXn, uv, 0);
    half4 sampyn = texYn.SampleLevel(sampler_HeightMapXn, uv, 0);
    half4 sampyp = texYp.SampleLevel(sampler_HeightMapXn, uv, 0);
    half4 sampzn = texZn.SampleLevel(sampler_HeightMapXn, uv, 0);
    half4 sampzp = texZp.SampleLevel(sampler_HeightMapXn, uv, 0);

    half4 sampx = lerp(sampxn, sampxp, step(0, s));
    half4 sampy = lerp(sampyn, sampyp, step(0, s));
    half4 sampz = lerp(sampzn, sampzp, step(0, s));

    half4 samp = lerp(sampx, sampz, zxlerp);
    samp = lerp(sampy, samp, nylerp);
    return samp;
}
half4 SampleCubeMapGrad(Texture2D texXn, Texture2D texXp, Texture2D texYn, Texture2D texYp, Texture2D texZn, Texture2D texZp, float3 cubeVect)
{
    float3 cubeVectNorm = normalize(cubeVect);
    float3 cubeVectNormAbs = abs(cubeVectNorm);

    half zxlerp = step(cubeVectNormAbs.x, cubeVectNormAbs.z);
    half nylerp = step(cubeVectNormAbs.y, max(cubeVectNormAbs.x, cubeVectNormAbs.z));

    half s = lerp(cubeVectNorm.x, cubeVectNorm.z, zxlerp);
    s = sign(lerp(cubeVectNorm.y, s, nylerp));

    half3 detailCoords = lerp(half3(1, -s, -1)*cubeVectNorm.xzy, half3(1, s, -1)*cubeVectNorm.zxy, zxlerp);
    detailCoords = lerp(half3(1, 1, s)*cubeVectNorm.yxz, detailCoords, nylerp);

    half2 uv = ((.5*detailCoords.yz) / abs(detailCoords.x)) + .5;

    //this fixes UV discontinuity on Y-X seam by swapping uv coords in derivative calcs when in the X quadrants.
    float4 uvdd = CubeDerivatives(uv, 1);

    // half4 sampxn = texXn.SampleLevel(sampler_ColorMapXn, uv, 0);
    // half4 sampxp = texXn.SampleLevel(sampler_ColorMapXn, uv, 0);
    // half4 sampyn = texXn.SampleLevel(sampler_ColorMapXn, uv, 0);
    // half4 sampyp = texXn.SampleLevel(sampler_ColorMapXn, uv, 0);
    // half4 sampzn = texXn.SampleLevel(sampler_ColorMapXn, uv, 0);
    // half4 sampzp = texXn.SampleLevel(sampler_ColorMapXn, uv, 0);

    half4 sampxn = texXn.SampleGrad(sampler_ColorMapXn, uv, uvdd.xy, uvdd.zw);
    half4 sampxp = texXp.SampleGrad(sampler_ColorMapXn, uv, uvdd.xy, uvdd.zw);
    half4 sampyn = texYn.SampleGrad(sampler_ColorMapXn, uv, uvdd.xy, uvdd.zw);
    half4 sampyp = texYp.SampleGrad(sampler_ColorMapXn, uv, uvdd.xy, uvdd.zw);
    half4 sampzn = texZn.SampleGrad(sampler_ColorMapXn, uv, uvdd.xy, uvdd.zw);
    half4 sampzp = texZp.SampleGrad(sampler_ColorMapXn, uv, uvdd.xy, uvdd.zw);

    half4 sampx = lerp(sampxn, sampxp, step(0, s));
    half4 sampy = lerp(sampyn, sampyp, step(0, s));
    half4 sampz = lerp(sampzn, sampzp, step(0, s));

    half4 samp = lerp(sampx, sampz, zxlerp);
    samp = lerp(sampy, samp, nylerp);
    return samp;
}
half4 SampleDoubleCubeMapGrad(Texture2D _ColorMapXnBL, Texture2D _ColorMapXnBR, Texture2D _ColorMapXnTL, Texture2D _ColorMapXnTR,
                              Texture2D _ColorMapXpBL, Texture2D _ColorMapXpBR, Texture2D _ColorMapXpTL, Texture2D _ColorMapXpTR,
                              Texture2D _ColorMapYnBL, Texture2D _ColorMapYnBR, Texture2D _ColorMapYnTL, Texture2D _ColorMapYnTR,
                              Texture2D _ColorMapYpBL, Texture2D _ColorMapYpBR, Texture2D _ColorMapYpTL, Texture2D _ColorMapYpTR,
                              Texture2D _ColorMapZnBL, Texture2D _ColorMapZnBR, Texture2D _ColorMapZnTL, Texture2D _ColorMapZnTR,
                              Texture2D _ColorMapZpBL, Texture2D _ColorMapZpBR, Texture2D _ColorMapZpTL, Texture2D _ColorMapZpTR,
                              float3 cubeVect)
{
    float3 cubeVectNorm = normalize(cubeVect);
    float3 cubeVectNormAbs = abs(cubeVectNorm);

    half zxlerp = step(cubeVectNormAbs.x, cubeVectNormAbs.z);
    half nylerp = step(cubeVectNormAbs.y, max(cubeVectNormAbs.x, cubeVectNormAbs.z));

    half s = lerp(cubeVectNorm.x, cubeVectNorm.z, zxlerp);
    s = sign(lerp(cubeVectNorm.y, s, nylerp));

    half3 detailCoords = lerp(half3(1, -s, -1)*cubeVectNorm.xzy, half3(1, s, -1)*cubeVectNorm.zxy, zxlerp);
    detailCoords = lerp(half3(1, 1, s)*cubeVectNorm.yxz, detailCoords, nylerp);

    half2 uv = ((.5*detailCoords.yz) / abs(detailCoords.x)) + .5;

    //this fixes UV discontinuity on Y-X seam by swapping uv coords in derivative calcs when in the X quadrants.
    float4 uvdd = CubeDerivatives(uv, 1);

    // half4 sampxn = texXn.SampleLevel(sampler_ColorMapXn, uv, 0);
    // half4 sampxp = texXn.SampleLevel(sampler_ColorMapXn, uv, 0);
    // half4 sampyn = texXn.SampleLevel(sampler_ColorMapXn, uv, 0);
    // half4 sampyp = texXn.SampleLevel(sampler_ColorMapXn, uv, 0);
    // half4 sampzn = texXn.SampleLevel(sampler_ColorMapXn, uv, 0);
    // half4 sampzp = texXn.SampleLevel(sampler_ColorMapXn, uv, 0);

    // Xn
    half4 sampxn_BL = _ColorMapXnBL.SampleGrad(sampler_ColorMapXn, half2(uv.x*2,uv.y*2), uvdd.xy, uvdd.zw);
    half4 sampxn_BR = _ColorMapXnBR.SampleGrad(sampler_ColorMapXn, half2(uv.x*2-1,uv.y*2), uvdd.xy, uvdd.zw);
    half4 sampxn_TL = _ColorMapXnTL.SampleGrad(sampler_ColorMapXn, half2(uv.x*2,uv.y*2-1), uvdd.xy, uvdd.zw);
    half4 sampxn_TR = _ColorMapXnTR.SampleGrad(sampler_ColorMapXn, half2(uv.x*2-1,uv.y*2-1), uvdd.xy, uvdd.zw);
    half4 sampxn_B = lerp(sampxn_BL, sampxn_BR, step(0, floor(uv.x*2)-1));
    half4 sampxn_T = lerp(sampxn_TL, sampxn_TR, step(0, floor(uv.x*2)-1));
    half4 sampxn = lerp(sampxn_T, sampxn_B, step(0, floor(uv.y*2)-1));
    
    // Xp
    half4 sampxp_BL = _ColorMapXpBL.SampleGrad(sampler_ColorMapXn, half2(uv.x*2,uv.y*2), uvdd.xy, uvdd.zw);
    half4 sampxp_BR = _ColorMapXpBR.SampleGrad(sampler_ColorMapXn, half2(uv.x*2-1,uv.y*2), uvdd.xy, uvdd.zw);
    half4 sampxp_TL = _ColorMapXpTL.SampleGrad(sampler_ColorMapXn, half2(uv.x*2,uv.y*2-1), uvdd.xy, uvdd.zw);
    half4 sampxp_TR = _ColorMapXpTR.SampleGrad(sampler_ColorMapXn, half2(uv.x*2-1,uv.y*2-1), uvdd.xy, uvdd.zw);
    half4 sampxp_B = lerp(sampxp_BL, sampxp_BR, step(0, floor(uv.x*2)-1));
    half4 sampxp_T = lerp(sampxp_TL, sampxp_TR, step(0, floor(uv.x*2)-1));
    half4 sampxp = lerp(sampxp_T, sampxp_B, step(0, floor(uv.y*2)-1));

    // Yn
    half4 sampyn_BL = _ColorMapYnBL.SampleGrad(sampler_ColorMapXn, half2(uv.x*2,uv.y*2), uvdd.xy, uvdd.zw);
    half4 sampyn_BR = _ColorMapYnBR.SampleGrad(sampler_ColorMapXn, half2(uv.x*2-1,uv.y*2), uvdd.xy, uvdd.zw);
    half4 sampyn_TL = _ColorMapYnTL.SampleGrad(sampler_ColorMapXn, half2(uv.x*2,uv.y*2-1), uvdd.xy, uvdd.zw);
    half4 sampyn_TR = _ColorMapYnTR.SampleGrad(sampler_ColorMapXn, half2(uv.x*2-1,uv.y*2-1), uvdd.xy, uvdd.zw);
    half4 sampyn_B = lerp(sampyn_BL, sampyn_BR, step(0, floor(uv.x*2)-1));
    half4 sampyn_T = lerp(sampyn_TL, sampyn_TR, step(0, floor(uv.x*2)-1));
    half4 sampyn = lerp(sampyn_T, sampyn_B, step(0, floor(uv.y*2)-1));
    
    // Yp
    half4 sampyp_BL = _ColorMapYpBL.SampleGrad(sampler_ColorMapXn, half2(uv.x*2,uv.y*2), uvdd.xy, uvdd.zw);
    half4 sampyp_BR = _ColorMapYpBR.SampleGrad(sampler_ColorMapXn, half2(uv.x*2-1,uv.y*2), uvdd.xy, uvdd.zw);
    half4 sampyp_TL = _ColorMapYpTL.SampleGrad(sampler_ColorMapXn, half2(uv.x*2,uv.y*2-1), uvdd.xy, uvdd.zw);
    half4 sampyp_TR = _ColorMapYpTR.SampleGrad(sampler_ColorMapXn, half2(uv.x*2-1,uv.y*2-1), uvdd.xy, uvdd.zw);
    half4 sampyp_B = lerp(sampyp_BL, sampyp_BR, step(0, floor(uv.x*2)-1));
    half4 sampyp_T = lerp(sampyp_TL, sampyp_TR, step(0, floor(uv.x*2)-1));
    half4 sampyp = lerp(sampyp_T, sampyp_B, step(0, floor(uv.y*2)-1));
    
    // Yn
    half4 sampzn_BL = _ColorMapZnBL.SampleGrad(sampler_ColorMapXn, half2(uv.x*2,uv.y*2), uvdd.xy, uvdd.zw);
    half4 sampzn_BR = _ColorMapZnBR.SampleGrad(sampler_ColorMapXn, half2(uv.x*2-1,uv.y*2), uvdd.xy, uvdd.zw);
    half4 sampzn_TL = _ColorMapZnTL.SampleGrad(sampler_ColorMapXn, half2(uv.x*2,uv.y*2-1), uvdd.xy, uvdd.zw);
    half4 sampzn_TR = _ColorMapZnTR.SampleGrad(sampler_ColorMapXn, half2(uv.x*2-1,uv.y*2-1), uvdd.xy, uvdd.zw);
    half4 sampzn_B = lerp(sampzn_BL, sampzn_BR, step(0, floor(uv.x*2)-1));
    half4 sampzn_T = lerp(sampzn_TL, sampzn_TR, step(0, floor(uv.x*2)-1));
    half4 sampzn = lerp(sampzn_T, sampzn_B, step(0, floor(uv.y*2)-1));
    
    // Yp
    half4 sampzp_BL = _ColorMapZpBL.SampleGrad(sampler_ColorMapXn, half2(uv.x*2,uv.y*2), uvdd.xy, uvdd.zw);
    half4 sampzp_BR = _ColorMapZpBR.SampleGrad(sampler_ColorMapXn, half2(uv.x*2-1,uv.y*2), uvdd.xy, uvdd.zw);
    half4 sampzp_TL = _ColorMapZpTL.SampleGrad(sampler_ColorMapXn, half2(uv.x*2,uv.y*2-1), uvdd.xy, uvdd.zw);
    half4 sampzp_TR = _ColorMapZpTR.SampleGrad(sampler_ColorMapXn, half2(uv.x*2-1,uv.y*2-1), uvdd.xy, uvdd.zw);
    
    half4 sampzp_B = lerp(sampzp_BL, sampzp_BR, step(0, floor(uv.x*2)-1));
    half4 sampzp_T = lerp(sampzp_TL, sampzp_TR, step(0, floor(uv.x*2)-1));
    half4 sampzp = lerp(sampzp_T, sampzp_B, step(0, floor(uv.y*2)-1));

    half4 sampx = lerp(sampxn, sampxp, step(0, s));
    half4 sampy = lerp(sampyn, sampyp, step(0, s));
    half4 sampz = lerp(sampzn, sampzp, step(0, s));

    half4 samp = lerp(sampx, sampz, zxlerp);
    samp = lerp(sampy, samp, nylerp);
    return samp;
}
