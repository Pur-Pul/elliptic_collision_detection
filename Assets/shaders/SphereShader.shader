Shader "Custom/sphere"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Tags {"LightMode" = "UniversalForward"}
            HLSLPROGRAM

            static const float PI = 3.14159265359;
            static const float tan_limit = tan(PI * 0.5);
            static const float pos_infinity = asfloat(0x7F800000);
            static const float neg_infinity = asfloat(0xFF800000);


            #define _SPECULAR_COLOR
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _FORWARD_PLUS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD2;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END
            StructuredBuffer<float4x4> _Bodies;
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorld(IN.normalOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            float4 qProduct(float4 q1, float4 q2) {
                return float4(
                    q1.w*q2.x + q1.x*q2.w + q1.y*q2.z - q1.z*q2.y,
                    q1.w*q2.y - q1.x*q2.z + q1.y*q2.w + q1.z*q2.x,
                    q1.w*q2.z + q1.x*q2.y - q1.y*q2.x + q1.z*q2.w,
                    q1.w*q2.w - q1.x*q2.x - q1.y*q2.y - q1.z*q2.z
                );
            }
            
            float WrapAzimuth(float ang) {
                return ang > PI 
                    ? ang - PI
                    : (
                        ang < -PI
                            ? ang + PI
                            : ang
                    );
            }

            float2 CartesianToSpherical(float3 pos)
            {
                float polar = acos(pos.z);
                float azimuth = atan2(pos.y, pos.x);

                return float2(azimuth, polar);
            }

            float2 FastCartesianToSpherical(float3 pos)
            {
                float polar = acos(pos.z);
                float azimuth = pos.y/pos.x;

                if(pos.x == 0) {
                    azimuth = sign(pos.y) * tan_limit;
                }

                return float2(azimuth, polar);
            }

            float WrappedAngleDiff(float ang1, float ang2)
            {
                float d = abs(ang1 - ang2);
                return min(d, 2.0 * PI - d);
            }
            float LongitudeExtent(float2 c, float2 radii)
            {
                float sinPolar = sin(c.y);
                float cosPolar = cos(c.y);

                if (sinPolar <= sin(radii.y))
                {
                    return PI;
                }

                return asin(sin(radii.x) / sinPolar);
            }
            float FastLongitudeExtent(float3 c, float radius, float cosR)
            {
                /*
                    For small angles on the radius close to the equator of the sphere, function asin(sin(radius) / sqrt(1 - cos^2(polar)))
                    is rougly equal to radius/sin^2(polar). As we move closer to the poles or when the radius grows, the function diverge more and more.
                    To limit the diversion of the function, we can clamp the result of the second function. 
                    - The first function approaches π/2 close to the poles, which means we can limit clamp the second function at π/2 as we move closer to the poles.
                    - When the shapes overlap with the poles the value of the first function jumps to π in order to contain the whole shape, and we can do the same in the second function but with cosines instead of sines.
                    We end up with the following two functions:
                    f(polar)=If(
                        sin(polar)≤sin(radius), π,
                        asin(sin(radius) / sqrt(1 - cos^2(polar)))
                    )
                    g(polar)=If(
                        cos(polar) ≥ cos(radius), π,
                        Min(radius / Max(sin^2(polar), 0.001), π/2)
                    )
                    g(polar) does not actually require using any trigonometric functions since:
                    cos(polar) = pos.z
                    sin^2(polar) = 1 - cos(polar)
                    and cos(radius) can be precalculated.
                */

                //1 - (1 - cos(radius))/sin^2(polar) ~ cos(g(polar))
                //since sin^2(x)/cos(x)

                float sinSquaredC = max(1.0 - c.z * c.z, 1e-5);

                if (abs(c.z) >= abs(cosR)) { // When the shape overlaps with the pole, the SAABB is expanded to cover the spherical cap.
                    return -1e-5;
                }
                
                // Approximated longitude extent / Approximated cosine longitude extent 
                //  ~ longEx / cosLongEx
                //  ~ sin(longEx) / cos(longEx)
                //  = tan(longEx)
                return  min(radius / sinSquaredC, PI * 0.5) / max(1 - (1 - cosR) / sinSquaredC, 0); 
            }

            float TanAzimuthDifference(float a1, float a2, float3 c, float3 p)
            {
                // We require the absolute value of the angle difference, but that is not directly possible tangent difference function.
                // tan(alpha - beta) = (tan(alpha) - tan(beta)) / (1 + tan(alpha)tan(beta))
                // By multiplying tan(alpha) and tan(beta) with the sign of the angle difference, we can still obtain the absolute value.
                // The sign of the angle difference can be obtained with sign(dot(cross(center.xy, pos.xy), spole))
            
                float s = sign(c.x * p.y - c.y * p.x) * -1.0;
                return (s * a1 - s * a2) / (1.0 + a1 * a2);
            }
            float TangentDiff(float t1, float t2) { return (t1 - t2) / (1.0 + t1 * t2); }

            bool FastSAABBContains(float3 center, float3 fragment, float2 centerSP, float2 fragmentSP, float2 extents)
            {
                float borderWidth = tan(0.01);

                float tanAzimuthDiff = TanAzimuthDifference(centerSP.x, fragmentSP.x, center, fragment);
                float polarDiff = abs(centerSP.y - fragmentSP.y);

                if ((tanAzimuthDiff < 0 && extents.x < 0) || (tanAzimuthDiff > 0 && extents.x > 0)) {
                    return (tanAzimuthDiff <= extents.x && polarDiff < extents.y) && 
                        !(tanAzimuthDiff <= TangentDiff(extents.x, borderWidth) && polarDiff <= extents.y - borderWidth);
                } else {
                    return (tanAzimuthDiff > extents.x && polarDiff < extents.y) &&
                        !(tanAzimuthDiff > TangentDiff(extents.x, borderWidth) && polarDiff <= extents.y - borderWidth);
                }
            }

            bool SAABBContains(float2 c, float2 extents, float2 p)
            {
                float borderWidth = 0.01;

                float azimuthDiff = abs(c.x - p.x);
                if (azimuthDiff > PI) { azimuthDiff = 2.0 * PI - azimuthDiff; }

                float polarDiff = abs(c.y - p.y);
                return (azimuthDiff <= extents.x && polarDiff <= extents.y) && 
                !(azimuthDiff <= extents.x - borderWidth && polarDiff <= extents.y - borderWidth);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 fragColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                bool fullbright = false;
                float3 normal = normalize(IN.positionWS);
                int sphere_n = _Bodies[0][0].y;
                int obb_n = _Bodies[0][0].z;
                int saabb_n = _Bodies[0][0].w;
                
                for (int i = 1; i < 1 + sphere_n; i++)
                {
                    float3 position = _Bodies[i][0].xyz;
                    float cosineRad = _Bodies[i][1].x;
                    float4 color = _Bodies[i][2];
                    float d = dot(normal, position);

                    if (cosineRad < d)
                    {
                        fragColor *= color;
                    }
                }
                for (int i = 1 + sphere_n; i < 1 + sphere_n + obb_n; i++)
                {
                    float3 center = _Bodies[i][0].xyz;
                    float3 forward = normalize(center);

                    float3 right = _Bodies[i][1].xyz;
                    float width = _Bodies[i][1].w;

                    float3 up = _Bodies[i][2].xyz;
                    float height = _Bodies[i][2].w;
                    float4 color = _Bodies[i][3];

                    float widthAngle = acos((width*width - 2.0) * -0.5);
                    float heightAngle = acos((height*height - 2.0) * -0.5);
                    
                    float4 q1 = float4(sin(widthAngle * 0.25) * up, cos(widthAngle * 0.25));
                    float4 q1Inverse = float4(-q1.xyz, q1.w);
                    float4 q2 = float4(sin(heightAngle * 0.25) * right, cos(heightAngle * 0.25));
                    float4 q2Inverse = float4(-q2.xyz, q2.w);

                    float3 h1 = qProduct(qProduct(q1, float4(right, 0)), q1Inverse).xyz;
                    float3 h2 = qProduct(qProduct(q1Inverse, float4(-right, 0)), q1).xyz;
                    float3 h3 = qProduct(qProduct(q2, float4(-up, 0)), q2Inverse).xyz;
                    float3 h4 = qProduct(qProduct(q2Inverse, float4(up, 0)), q2).xyz;

                    if (
                        dot(normal, h1) >= 0.0 &&
                        dot(normal, h2) >= 0.0 &&
                        dot(normal, h3) >= 0.0 &&
                        dot(normal, h4) >= 0.0
                    )
                    {
                        fragColor *= color;
                    }
                }
                float2 fragSphericalPos = CartesianToSpherical(normal);
                float2 fastFragSphericalPos = FastCartesianToSpherical(normal);
                for (int i = 1 + sphere_n + obb_n; i < 1 + sphere_n + obb_n + saabb_n; i++)
                {
                    float2 sphericalPos = _Bodies[i][0].xy;
                    float2 fastSphericalPos = _Bodies[i][0].zy;
                    float2 extents = _Bodies[i][1].xy;
                    float3 position = _Bodies[i][2].xyz;
                    float2 fastExtents = _Bodies[i][1].zy;
                    
                    float4 color = _Bodies[i][3];
                    if (SAABBContains(sphericalPos, extents, fragSphericalPos))
                    {
                        fullbright = true;
                        fragColor = color;
                    }
                    if (FastSAABBContains(position, normal, fastSphericalPos, fastFragSphericalPos, fastExtents))
                    {
                        fullbright = true;
                        fragColor = float4(float3(1.0, 1.0, 1.0) - color.rgb, 1.0);
                    }
                }

                InputData lighting = (InputData) 0;
                lighting.positionWS = IN.positionWS;
                lighting.normalWS = normal;
                lighting.viewDirectionWS = GetWorldSpaceViewDir(IN.positionWS);

                SurfaceData surface = (SurfaceData) 0;
                surface.albedo = fragColor;
                surface.alpha = 1;
                surface.smoothness = .9;
                surface.specular = .9;

                return fullbright 
                    ? fragColor
                    : UniversalFragmentBlinnPhong(lighting, surface) + unity_AmbientSky * fragColor;
            }
            ENDHLSL
        }
    }
}
