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

            half4 frag(Varyings IN) : SV_Target
            {
                half4 fragColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                float3 normal = normalize(IN.normalWS);
                int sphere_n = _Bodies[0][0].y;
                int obb_n = _Bodies[0][0].z;
                
                for (int i = 1; i < 1 + sphere_n; i++)
                {
                    float3 position = _Bodies[i][0].xyz;
                    float radius = _Bodies[i][1].x;
                    float4 color = _Bodies[i][2];
                    float d = dot(normal, position);
                    if (radius >= (d - 1.0f)/(-2.0f))
                    {
                        fragColor *= color;
                    }
                }
                for (int i = 1+sphere_n; i < 1 + sphere_n + obb_n; i++)
                {
                    float3 center = normalize(_Bodies[i][0].xyz);

                    float3 right = normalize(_Bodies[i][1].xyz);
                    float width = _Bodies[i][1].w;

                    float3 up = normalize(_Bodies[i][2].xyz);
                    float height = _Bodies[i][2].w;

                    float4 color = _Bodies[i][3];

                    float x = dot(normal, right);
                    float y = dot(normal, up);
                    float z = dot(normal, center);

                    if (z > 0)
                    {
                        if (abs(x) <= width * 0.5 &&
                            abs(y) <= height * 0.5)
                        {
                            fragColor *= color;
                        }
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
