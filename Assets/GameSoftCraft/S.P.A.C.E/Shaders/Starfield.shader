Shader "GameSoftCraft/S.P.A.C.E"
{

    Properties
    {
        _Seed ("Seed", int) = 1
        _Gamma("Gamma", Range(0.5, 2.2)) = 1
        _Orientation("Orientation", Vector) = (0, 0, 0)
        _FarStarDens ("FarStarDens", Range(0, 1)) = 1
        _FarStarTwinkle ("FarStarTwinkle", Range(0, 1)) = 1
        _MidStarDens ("MidStarDens", Range(0, 1)) = 1
        _MidStarTwinkle ("MidStarTwinkle", Range(0, 1)) = 1
        _NearStarDens ("NearStarDens", Range(0, 1)) = 1
        _NebulaColOffset("NebulaColOffset", Color) = (0, 0, 0)
        _SunSize ("SunSize", Range(0, 1)) = 1
        _SunCoronaSpeed ("SunCoronaSpeed", Range(0, 1)) = 1
        _SunDir ("SunDir", Vector) = (1, 1, 1)
        _SunTint ("SunTint", Color) = (1, 1, 0.7)
        _PlanetSize ("PlanetSize", Range(0, 1)) = 1
        _PlanetDir ("PlanetDir", Vector) = (-1, -5, 1)
        _PlanetAngle ("PlanetAngle", float) = 0
        _PlanetSpeed ("PlanetSpeed", Range(0, 1)) = 0.5
        _PlanetTint ("PlanetTint", Color) = (0.6, 0.35, 0.2)
        _PlanetAtmoTint ("PlanetAtmoTint", Color) = (0.6, 0.35, 0.2)
        _PlanetAtmoThick ("PlanetAtmoThick", Range(0, 1)) = 1
        _PlanetBrightness ("PlanetBrightness", Range(0, 1)) = 1
        _ShadowAngle ("ShadowAngle", float) = 0
        _ShadowDepth ("ShadowDepth", Range(0, 1)) = 0
        _MetTint("MetTint", Color) = (0.7, 0.5, 0.6)
        _MetBrightness("MetBrightness", Range(0, 1)) = 1
        _MetSpeed("MetSpeed", Range(0, 1)) = 1
        _SkyboxBgKind ("Background Kind", Float) = 0
        _SkyboxCube ("Background Cubemap", Cube) = "" {}
        _SkyboxTint ("Cubemap Tint", Color) = (1, 1, 1, 1)
        _SkyboxExposure ("Cubemap Exposure", Range(0, 4)) = 1
        _SkyboxBlend ("Underlay Blend", Range(0, 1)) = 0
        [NoScaleOffset] _FrontTex ("Front Z Plus", 2D) = "grey" {}
        [NoScaleOffset] _BackTex ("Back Z Minus", 2D) = "grey" {}
        [NoScaleOffset] _LeftTex ("Left X Plus", 2D) = "grey" {}
        [NoScaleOffset] _RightTex ("Right X Minus", 2D) = "grey" {}
        [NoScaleOffset] _UpTex ("Up Y Plus", 2D) = "grey" {}
        [NoScaleOffset] _DownTex ("Down Y Minus", 2D) = "grey" {}
        _SixSidedTint ("6-Sided Tint", Color) = (0.5, 0.5, 0.5, 0.5)
        _SixSidedExposure ("6-Sided Exposure", Range(0, 8)) = 1
        _SixSidedRotation ("6-Sided Y Rotation", Range(0, 360)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma multi_compile __ SUN_ON 
            #pragma multi_compile __ PLANET_ON 
            #pragma multi_compile __ DEBRIS_ON

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityStandardUtils.cginc"

            int _Seed;
            fixed _Gamma;   
            fixed3 _Orientation;

            fixed _FarStarDens;
            fixed _FarStarTwinkle;
            fixed _MidStarDens;
            fixed _MidStarTwinkle;
            fixed _NearStarDens;
            fixed3 _NebulaColOffset;

            fixed _SunSize;
            fixed _SunCoronaSpeed;
            fixed3 _SunDir;
            fixed3 _SunTint;

            fixed3 _PlanetDir;
            fixed3 _PlanetTint;
            fixed3 _PlanetAtmoTint;
            fixed _PlanetAtmoThick;
            fixed _PlanetBrightness;
            fixed _PlanetAngle;
            fixed _PlanetSpeed;
            fixed _ShadowAngle;
            fixed _ShadowDepth;
            fixed _PlanetSize;

            fixed3 _MetTint;
            fixed _MetBrightness;
            fixed _MetSpeed;

            samplerCUBE _SkyboxCube;
            fixed4 _SkyboxTint;
            fixed _SkyboxExposure;
            fixed _SkyboxBlend;
            int _SkyboxBgKind;

            sampler2D _FrontTex;
            sampler2D _BackTex;
            sampler2D _LeftTex;
            sampler2D _RightTex;
            sampler2D _UpTex;
            sampler2D _DownTex;
            half4 _FrontTex_HDR;
            half4 _BackTex_HDR;
            half4 _LeftTex_HDR;
            half4 _RightTex_HDR;
            half4 _UpTex_HDR;
            half4 _DownTex_HDR;
            half4 _SixSidedTint;
            half _SixSidedExposure;
            half _SixSidedRotation;

            struct appdata
            {
                fixed4 posOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                fixed4 posCS : SV_POSITION;
                fixed3 viewDirWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed3x3 getOrientation (fixed3 euler) 
            {
                fixed sx = sin(euler.x);
                fixed cx = cos(euler.x);
                fixed sy = sin(euler.y);
                fixed cy = cos(euler.y);
                fixed sz = sin(euler.z);
                fixed cz = cos(euler.z);

                /// Rotation matrix combining X, Y, Z rotations
                return fixed3x3(
                    cy * cz,                 -cy * sz,                sy,
                    sx * sy * cz + cx * sz,  -sx * sy * sz + cx * cz, -sx * cy,
                    -cx * sy * cz + sx * sz, cx * sy * sz + sx * cz,  cx * cy
                );
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.posCS = UnityObjectToClipPos(v.posOS);
                o.viewDirWS = normalize(UnityObjectToWorldDir(v.posOS.xyz));
                o.viewDirWS = mul(getOrientation(_Orientation), o.viewDirWS); 

                return o;
            }

            fixed3 rotateAroundAxis(in fixed3 vec, in fixed3 axis, in fixed angle)
            {
                fixed c = cos(angle);
                fixed s = sin(angle);

                // Rodrigues' rotation formula
                return vec * c + cross(axis, vec) * s + axis * dot(axis, vec) * (1 - c);
            }

            fixed magnSqr (in fixed3 v)
            {
                return dot(v, v);
            }

            fixed hash21 (in fixed2 p)
            {
                p = frac(p * fixed2(123.34, 233.53));
                p += dot(p, p + 23.234 + _Seed);

                return frac(p.x * p.y + (int(p.x) ^ int(p.y)));
            }

            fixed2 hash22 (in fixed2 st)
            {
                st = fixed2(dot(st, fixed2(127.1 + _Seed, 311.7)), dot(st, fixed2(269.5, 183.3 - _Seed)));
                return -1.0 + 2.0 * frac(sin(st) * 43758.5453123 - _Seed);
            }

            fixed noise (in fixed x)
            {
                fixed i = floor(x);
                fixed f = frac(x);
                fixed u = f * f * (3. - 2. * f);
                fixed a = hash21(i);
                fixed b = hash21(i + 1.);
                return lerp(a, b, u);
            }

            fixed perlin (in fixed2 st)
            {
                fixed2 i = floor(st);
                fixed2 f = frac(st);
                fixed2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(dot(hash22(i + fixed2(0, 0)), f - fixed2(0, 0)),
                        dot(hash22(i + fixed2(1, 0)), f - fixed2(1, 0)), u.x),
                    lerp(dot(hash22(i + fixed2(0, 1)), f - fixed2(0, 1)),
                        dot(hash22(i + fixed2(1, 1)), f - fixed2(1, 1)), u.x),
                    u.y);
            }

            fixed snoise (in fixed3 uv, in fixed res)
            {
                fixed3 s = fixed3(1e0, 1e2, 1e4);

                uv = frac(uv - .5) * .996;
                uv *= res;

                fixed3 uv0 = floor(fmod(uv, res)) * s;
                fixed3 uv1 = floor(fmod(uv + fixed3(1.0, 1.0, 1.0), res)) * s;

                fixed3 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                fixed4 v = fixed4(
                    uv0.x + uv0.y + uv0.z, 
                    uv1.x + uv0.y + uv0.z, 
                    uv0.x + uv1.y + uv0.z, 
                    uv1.x + uv1.y + uv0.z
                    ) + _Seed;

                fixed4 r = frac(sin(v * 1e-3) * 1e5);
                fixed r0 = lerp(lerp(r.x, r.y, f.x), lerp(r.z, r.w, f.x), f.y);

                r = frac(sin((v + uv1.z - uv0.z) * 1e-3) * 1e5);
                fixed r1 = lerp(lerp(r.x, r.y, f.x), lerp(r.z, r.w, f.x), f.y);

                return lerp(r0, r1, f.z) * 2.0 - 1.0;
            }

            fixed3 worldToCube (in fixed3 p)
            {
                fixed3 ap = abs(p);
                fixed3 sp = sign(p);
                fixed m = max(max(ap.x, ap.y), ap.z);
                fixed3 st;

                if (m == ap.x)
                    st = fixed3(p.z, p.y, 1. * sp.x);
                else if (m == ap.y)
                    st = fixed3(p.z, p.x, 2. * sp.y);
                else
                    st = fixed3(p.x, p.y, 3. * sp.z);

                return st;
            }

            fixed3 toSpherical (in fixed3 p)
            {
                fixed r = length(p);
                fixed theta = atan2(sqrt(p.x * p.x + p.y * p.y), p.z);
                fixed phi = atan2(p.y, p.x);
                return fixed3(r, theta, phi);
            }

            fixed2 toRadial (in fixed2 uv)
            {
                fixed radial = length(uv);
                fixed angle = atan2(uv.y, uv.x);
                return fixed2(radial, angle);
            }

            fixed3 getRayDir (in fixed2 uv, in fixed3 p, in fixed3 l, in fixed z, in fixed s)
            {
                fixed3 f = normalize(l - p),
                    r = normalize(cross(fixed3(0, 1, 0), f)),
                    u = cross(f, r),
                    c = f * z,
                    i = c * s + uv.x * r + uv.y * u,
                    d = normalize(i);

                return d;
            }

            fixed gyroid (in fixed3 pos, in fixed scale)
            {
                fixed x = pos.x * scale;
                fixed y = pos.y * scale;
                fixed z = pos.z * scale;

                return sin(x) * cos(y) + sin(y) * cos(z) + sin(z) * cos(x);
            }

            fixed3 sunBody (in fixed3 viewDir)
            {
                fixed sunSize = 1.05 - _SunSize;
                fixed3 dir = viewDir + fixed3(0, _Time.x * .15, _Time.x * .1) * _SunCoronaSpeed;
                fixed3 sunDir = normalize(_SunDir);
                fixed3 sunColor = _SunTint - snoise(dir, 60 + int(40 * sunSize)) * sqrt(_SunSize);

                fixed sunStep = .77;
                fixed coronaSize1 = 0.03 + snoise(viewDir, 70) * .004;
                fixed coronaSize2 = 0.6;

                fixed sunDF = dot(viewDir, sunDir);
                fixed sundot = pow(sunDF, (32 * (1 - _SunSize * .25))) * _SunSize * 9;

                fixed haloFactor = smoothstep(sunStep + perlin((dir.yz - dir.x) * 100) * .01, sunStep - 0.01, sundot);
                fixed coronaFactor1 = smoothstep(sunStep + 0.01, sunStep - coronaSize1, sundot);
                fixed coronaFactor2 = smoothstep(sunStep + coronaSize1, sunStep - coronaSize2, sundot * .85) * 1.5;

                fixed3 col = sunColor * (1. - haloFactor);
                col += 1. - coronaFactor1;
                col += (1.2 - coronaFactor2) * .2;
        
                return col;
            }

            fixed3 sunAtmo (in fixed3 viewDir)
            {
                const fixed atm1F = .005;
                const fixed atm2F = .0001;
                fixed sk = 1.05 - _SunSize;
                fixed3 dir = viewDir + fixed3(0, _Time.x, _Time.x) * _SunCoronaSpeed;
                fixed noise1 = snoise(dir, 20 + int(40 * sk));
                fixed noise2 = snoise(dir, 80 + int(160 * sk));
                fixed3 sunDir = normalize(_SunDir);
                fixed3 sunCol = saturate(_SunTint * fixed3(0.8, 0.35, 0.1)) * 2.5;
                fixed3 coronaColor1 = normalize(_SunTint + fixed3(0.6, 0.5, 0.3));

                fixed sunDF = dot(viewDir, sunDir);
                fixed atmo = pow(sunDF, (32 * (1 - _SunSize * .25) + noise1)) * _SunSize * 9;
                fixed3 cor1 = smoothstep(.35 + noise1 * .3 * _SunSize, 1 - _SunSize * .15, atmo);
                fixed3 cor2 = lerp(atmo, cor1, .5) * sunCol;
                fixed3 f1 = smoothstep(atm1F * _SunSize * 3, -atm1F, pow(.75 - atmo, 2 - noise2 * 1.2)) * coronaColor1;
                fixed3 f2 = smoothstep(atm2F * _SunSize, -atm2F, pow(.77 - atmo, 4)) * 2;
               
                fixed3 col = cor2 + (sunDF * pow(_SunSize + .3, 2) * sunCol * .05) + saturate(f1 + f2);

                return col;
            }

            fixed3 nebula (in fixed3 pos, in fixed3 color, in fixed size)
            {
                fixed3 spherePos = toSpherical(pos + 1);
                fixed factor = perlin(spherePos.xy * size + spherePos.z);
                color *= factor * saturate(pos + _NebulaColOffset);

                return saturate(color);
            }

            fixed4 planet (in fixed3 viewDir) 
            {
                fixed size = _PlanetSize * .25 + 1e-8;
                fixed invSize = 1 - _PlanetSize;
                fixed3 planDir = normalize(_PlanetDir);
                fixed3 planCross = cross(planDir, viewDir);
                fixed3 lightDir = rotateAroundAxis(
                    cross(planDir, fixed3(_ShadowDepth * (size), 0, 0)), 
                    planDir, _ShadowAngle
                    );

                fixed bright = _PlanetBrightness;
                fixed d = dot(viewDir, planDir);
                fixed a = floor(d + size * .75);
                fixed diff = dot(viewDir, planDir + lightDir) * a;
                fixed dfB = smoothstep(1 - size, (1 + size), d);
                fixed dfA = smoothstep(1 + size, (1 - size), d);
                fixed3 col = saturate(dfB * 12);
    
                fixed3 gradDir = rotateAroundAxis(planCross, planDir, _PlanetAngle);
                fixed sn1 = snoise(gradDir.zxy + fixed3(0, (_Time.x * _PlanetSpeed * .7), 0), 64 + 128 * invSize) * (1 - dfA * d) * .02;
                fixed sn2 = snoise(gradDir.y + sn1, 128 * size + 64 * invSize) * (1 - dfA) * .25;
                fixed sn3 = noise(d * gradDir.y * 500) * abs(.5 - dfA);

                col *= _PlanetTint;
                col = max(col, (dfA + sn3 * .45) * _PlanetAtmoTint * _PlanetAtmoThick);     
                col *= smoothstep(1 - size, 1 + size, diff);
                col += (sn2) * dfB * 3 + sn1;
                col *= _PlanetBrightness;

                return saturate(fixed4(col, dfB * 16));
            }

            fixed3 farStars (in fixed3 cubePos)
            {
                fixed scale = 400;
                fixed2 cuv = cubePos.xy * scale + cubePos.z * scale;
                fixed2 cgv = frac(cuv) - .5;
                fixed2 cid = floor(cuv);
                fixed hash = hash21(cid);
                fixed hash2 = hash21(cid + (3.0 * hash));
                fixed hash3 = hash21(cid + (4.0 * hash2));
                fixed twinkle = abs(sin(hash3 * _Time.z * (_FarStarTwinkle + .1) * 4.0));

                fixed f = smoothstep(-(1. - hash), twinkle * hash, length(cgv + hash3));

                fixed3 col = (1 - f) * (hash + hash3) * _FarStarDens;

                col.r *= hash3 * .75;
                col.g *= hash2 * .65;
                col.b *= hash * .5;

                col.b = min(col.b, .75);

                return col;
            }

            fixed3 midStars (in fixed3 cubePos)
            {
                fixed scale = 90;
                fixed2 cuv = cubePos.xy * scale + cubePos.z * scale;
                fixed2 cgv = frac(cuv) - .5;
                fixed2 cid = floor(cuv);
                fixed hash = hash21(cid);
                fixed hash2 = hash21(cid + (5.0 * hash));
                fixed hash3 = hash21(cid + (5.0 * hash2));
                fixed twinkle = sin(hash3 * _Time.z * (_MidStarTwinkle + .1) * 5.0) * 0.125;

                cgv -= (0.5 - hash) * 0.3 * hash3;
                cgv *= max(hash2 * 3.5, 1.1);

                fixed f = smoothstep(-(1.0 - hash), twinkle + 0.5, length(cgv + hash3));
                fixed3 col = (1.0 - hash - f) * min(hash2, hash3) * (8.0 * hash3) * _MidStarDens;

                col -= pow(col, fixed3(hash, hash2, hash3)) * 0.125;

                col.r *= hash * .75;
                col.g *= hash2 * .65;
                col.b *= hash3 * .5;

                col *= 2.0;

                return fixed4(saturate(col), 1.0);
            }

            fixed3 nearStars (in fixed3 cubePos)
            {
                fixed scale = 40;
                fixed2 t = _Time.xy;
                fixed2 cuv = cubePos.xy * scale + cubePos.z * scale;
                fixed2 cgv = frac(cuv) - .5;
                fixed2 cid = floor(cuv);
                fixed hash1 = hash21(cid);
                fixed hash2 = hash21(cid * hash1);
                fixed hash3 = hash21(cid * hash2);
                fixed hash4 = floor((hash1 * hash2 + hash3 * .25) + _NearStarDens * .25);
                fixed2 uvC = toRadial(cgv);
                fixed3 col = 0;
                fixed msk = pow(1 - dot(cgv, cgv), 16 + 64 * hash1) * hash2;
                fixed stp1 = .2 * snoise((uvC.y) - t.y * pow(hash3, 8), 10);
                fixed stp2 = lerp(.5 * snoise((uvC - t * (hash1 + hash2)).xyx, 5 + hash1 * 2), stp1, .5);
                fixed d = 1 - smoothstep(uvC.x + .05 * hash1, .1 * perlin(uvC * 10 - cid), lerp(stp1, stp2, floor(hash1 + .05)));

                col += d * hash4 * msk;
                col += smoothstep(0, 1, min(.7, pow(msk, 16))) * hash4;
                col /= fixed3(hash1, hash2, hash3);

                return saturate(col);
            }

            fixed3 rotateAroundYInDegreesSixSided (fixed3 v, fixed degrees)
            {
                fixed alpha = degrees * UNITY_PI / 180.0;
                fixed sina, cosa;
                sincos(alpha, sina, cosa);
                fixed2x2 m = fixed2x2(cosa, -sina, sina, cosa);
                return fixed3(mul(m, v.xz), v.y).xzy;
            }

            // Single-pass 6-sided sampling (Unity slot names: Left=+X, Right=-X, Front=+Z, Back=-Z).
            // UV layout follows common D3D/OpenGL cube-face formulas so edges match Unity's built-in 6-sided skybox.
            // tex2Dlod mip 0 avoids discontinuous screen-space derivatives at face boundaries (extra seams).
            fixed3 sampleSixSidedSkybox (fixed3 dirWS)
            {
                fixed3 dir = normalize(rotateAroundYInDegreesSixSided(dirWS, _SixSidedRotation));
                fixed3 a = abs(dir);
                fixed2 uv;
                half4 tex;
                half3 c;

                if (a.x > a.y) {
                    if (a.x > a.z) {
                        if (dir.x > 0) {
                            uv = fixed2(-dir.z, -dir.y) / a.x * 0.5 + 0.5;
                            tex = tex2Dlod(_LeftTex, half4(uv, 0, 0));
                            c = DecodeHDR(tex, _LeftTex_HDR);
                        }
                        else {
                            uv = fixed2(dir.z, -dir.y) / a.x * 0.5 + 0.5;
                            tex = tex2Dlod(_RightTex, half4(uv, 0, 0));
                            c = DecodeHDR(tex, _RightTex_HDR);
                        }
                    }
                    else {
                        if (dir.z > 0) {
                            uv = fixed2(dir.x, dir.y) / a.z * 0.5 + 0.5;
                            tex = tex2Dlod(_FrontTex, half4(uv, 0, 0));
                            c = DecodeHDR(tex, _FrontTex_HDR);
                        }
                        else {
                            uv = fixed2(-dir.x, dir.y) / a.z * 0.5 + 0.5;
                            tex = tex2Dlod(_BackTex, half4(uv, 0, 0));
                            c = DecodeHDR(tex, _BackTex_HDR);
                        }
                    }
                }
                else {
                    if (a.y > a.z) {
                        if (dir.y > 0) {
                            uv = fixed2(dir.x, dir.z) / a.y * 0.5 + 0.5;
                            tex = tex2Dlod(_UpTex, half4(uv, 0, 0));
                            c = DecodeHDR(tex, _UpTex_HDR);
                        }
                        else {
                            uv = fixed2(dir.x, -dir.z) / a.y * 0.5 + 0.5;
                            tex = tex2Dlod(_DownTex, half4(uv, 0, 0));
                            c = DecodeHDR(tex, _DownTex_HDR);
                        }
                    }
                    else {
                        if (dir.z > 0) {
                            uv = fixed2(dir.x, dir.y) / a.z * 0.5 + 0.5;
                            tex = tex2Dlod(_FrontTex, half4(uv, 0, 0));
                            c = DecodeHDR(tex, _FrontTex_HDR);
                        }
                        else {
                            uv = fixed2(-dir.x, dir.y) / a.z * 0.5 + 0.5;
                            tex = tex2Dlod(_BackTex, half4(uv, 0, 0));
                            c = DecodeHDR(tex, _BackTex_HDR);
                        }
                    }
                }

                c *= _SixSidedTint.rgb * unity_ColorSpaceDouble.rgb;
                c *= _SixSidedExposure;
                return fixed3(c);
            }

            fixed3 compose (in fixed2 uv, in fixed3 viewDir)
            {
                fixed3 cubePos = worldToCube(viewDir);
                fixed gyroid2 = (gyroid(cubePos + viewDir.zxy, 6.0));
                fixed gyroid1 = (1.4 - gyroid(cubePos + viewDir + fixed3(1, 1, 0), 8.0)) * 4;
                fixed3 nebula1 = nebula(viewDir, fixed3(0.8, 0.6, 0.9), 4) * .125;
                fixed3 nebula2 = nebula(viewDir - nebula1, fixed3(0.7, 0.5, 0.6), 1) * .5;
                fixed3 nebulaCol = nebula1 + nebula2 * .6;
                fixed3 starsBG = pow(farStars(cubePos), 2);
                fixed3 starsFG = nearStars(cubePos);
                fixed3 col = max(starsBG * gyroid2, starsBG * .4);

                nebulaCol = nebulaCol * (1.05 - length(nebulaCol)) * 2;
                col = max(col, midStars(cubePos) * gyroid1);
                col = max(col, starsFG);
                col = lerp(col, nebulaCol, nebulaCol);

                // Optional underlay: baked cubemap (kind 1) uses texCUBElod mip0 + max for sharpness; six-sided (kind 2) keeps softer merge to hide face seams.
                if (_SkyboxBlend > 0.0001 && _SkyboxBgKind > 0)
                {
                    fixed3 bgCol = 0;
                    if (_SkyboxBgKind == 2) {
                        bgCol = sampleSixSidedSkybox(viewDir);
                        fixed lum = dot(col, fixed3(0.299, 0.587, 0.114));
                        fixed fill = saturate(1.0 - lum * 1.8);
                        fixed3 lifted = saturate(col + bgCol * fill * 0.65);
                        fixed3 hard = max(col, bgCol);
                        fixed3 merged = lerp(hard, lifted, 0.35);
                        col = lerp(col, merged, _SkyboxBlend);
                    }
                    else {
                        half4 cubeSample = texCUBElod(_SkyboxCube, half4(viewDir, 0));
                        bgCol = cubeSample.rgb * _SkyboxTint.rgb * _SkyboxExposure;
                        fixed3 merged = max(col, bgCol);
                        col = lerp(col, merged, _SkyboxBlend);
                    }
                }

                #ifdef SUN_ON
                fixed3 sunD = saturate(lerp(sunBody(viewDir) * .5, sunAtmo(viewDir) * normalize(viewDir + _SunTint), .5));
                col = max(col, sunD);
                #endif
    
                #ifdef PLANET_ON
                fixed4 planet1 = planet(viewDir);
                col = lerp(col, planet1.rgb, planet1.a);
                #endif

                col = pow(col, 1 / _Gamma);
    
                return col;
            }

            fixed3 meteors (in fixed2 uv, in fixed3 dir, in fixed3 col, in fixed scale, in fixed tScale, in fixed density)
            {
                fixed3 cuv = toSpherical(dir);

                cuv.z *= 1.1;
                cuv.y *= 3.1416;
                cuv.y += _Time.y * _MetSpeed * tScale;
                
                fixed2 cgv = frac(cuv.yz * scale) - .5;
                fixed2 cid = floor(cuv.yz * scale);
                fixed hash1 = hash21(cid);
                fixed hash2 = perlin(cid.x + cid * hash1 * 10);
                fixed tex = perlin((cgv - cid) * 5);
                fixed d = pow(1 - hash1 - dot(cgv, cgv), 8 + hash2);
                fixed mask = pow(dot(uv, uv), .2);

                d += smoothstep(.1, .3, d) * tex;

                col *= mask * d * floor(hash2 * density);

                return saturate(max(col, col * (1 - hash2)));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed2 uv = i.posCS.xy / _ScreenParams.xy;
                fixed2 st = (i.posCS.xy - .5 * _ScreenParams.xy) / _ScreenParams.y;
                fixed3 fixedDir = normalize(getRayDir(st, fixed3(0, 0, 1), 0, -1, -.2));
                fixed3 viewDir = normalize(i.viewDirWS);
                fixed3 col = compose(uv, viewDir);

                #ifdef DEBRIS_ON
                fixed3 metTint = _MetTint;
                fixed3 metTint2 = metTint + .3f;
                fixed3 metDf = meteors(st, fixedDir, metTint, 16, 5.5, 2);
                fixed3 metDf2 = meteors(st, fixedDir, metTint2, 64, 2.35, 1.5);
                fixed3 meteors3 = max(metDf, metDf2) * _MetBrightness;
                col = max(min(col, 1 - smoothstep(magnSqr(meteors3), .9, -10.51)), meteors3);
                #endif
                
                return fixed4(col, 1);
            }

            ENDCG
        }
    }
}
