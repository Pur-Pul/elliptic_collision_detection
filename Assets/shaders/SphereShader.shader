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

            float twistAngle(float4 q, float3 a) 
            {
                float ang = 0.0;
                float d = dot(q.xyz, a);
                float3 proj = a * d;

                if (abs(d) > 0.0) {
                    ang = acos(q.w / length(float4(proj, q.w))) * 2.0;
                }
                return ang;
            }
            

            half4 frag(Varyings IN) : SV_Target
            {
                half4 fragColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                float3 normal = normalize(IN.positionWS);
                int sphere_n = _Bodies[0][0].y;
                int obb_n = _Bodies[0][0].z;
                
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
                for (int i = 1+sphere_n; i < 1 + sphere_n + obb_n; i++)
                {
                    float3 center = _Bodies[i][0].xyz;
                    float3 forward = normalize(center);

                    float3 right = _Bodies[i][1].xyz;
                    float width = _Bodies[i][1].w;

                    float3 up = _Bodies[i][2].xyz;
                    float height = _Bodies[i][2].w;
                    float4 color = _Bodies[i][3];

                    float widthAngle = acos((width*width - 2.0) * -0.5) * 0.5;
                    float heightAngle = acos((height*height - 2.0) * -0.5) * 0.5;
                    
                    float4 q1 = float4(sin(widthAngle * 0.5) * up, cos(widthAngle * 0.5));
                    float4 q1Inverse = float4(-q1.xyz, q1.w);
                    float4 q2 = float4(sin(heightAngle * 0.5) * right, cos(heightAngle * 0.5));
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

                InputData lighting = (InputData) 0;
                lighting.positionWS = IN.positionWS;
                lighting.normalWS = normal;
                lighting.viewDirectionWS = GetWorldSpaceViewDir(IN.positionWS);

                SurfaceData surface = (SurfaceData) 0;
                surface.albedo = fragColor;
                surface.alpha = 1;
                surface.smoothness = .9;
                surface.specular = .9;

                return UniversalFragmentBlinnPhong(lighting, surface) + unity_AmbientSky * fragColor;
                
                
            }
            ENDHLSL
        }
    }
}
