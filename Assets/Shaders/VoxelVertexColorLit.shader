Shader "Labyrinth/Voxel Vertex Color Lit"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Color ("Color", Color) = (1, 1, 1, 1)
        _VoxelLightColor ("Voxel Light Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _Color;
                float4 _VoxelLightColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                float4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionOS = input.positionOS.xyz;
                output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(input.shadowCoord);
                half mainTerm = saturate(dot(normalWS, mainLight.direction));
                half shadow = lerp(half(0.32), half(1.0), mainLight.shadowAttenuation);
                half3 lighting = mainLight.color * (half(0.32) + mainTerm * half(0.82)) * shadow;

                #if defined(_ADDITIONAL_LIGHTS)
                    uint lightCount = GetAdditionalLightsCount();
                    for (uint i = 0u; i < lightCount; i++)
                    {
                        Light light = GetAdditionalLight(i, input.positionWS);
                        half term = saturate(dot(normalWS, light.direction));
                        lighting += light.color * light.distanceAttenuation * (half(0.16) + term * half(0.84));
                    }
                #endif

                float3 absNormal = abs(normalWS);
                float2 objectUv = absNormal.y > absNormal.x && absNormal.y > absNormal.z
                    ? input.positionOS.xz
                    : (absNormal.x > absNormal.z ? input.positionOS.zy : input.positionOS.xy);
                objectUv = objectUv * _BaseMap_ST.xy * 3.35 + _BaseMap_ST.zw;
                half4 textureColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, objectUv);
                half4 baseColor = _BaseColor * _VoxelLightColor * input.color * textureColor;
                return half4(baseColor.rgb * lighting, baseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            fixed4 _BaseColor;
            fixed4 _Color;
            fixed4 _VoxelLightColor;

            struct AppData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 objectPos : TEXCOORD2;
                fixed4 color : COLOR;
            };

            Varyings Vert(AppData input)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.normal = UnityObjectToWorldNormal(input.normal);
                output.objectPos = input.vertex.xyz;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                fixed3 normal = normalize(input.normal);
                fixed light = 0.48 + saturate(dot(normal, normalize(_WorldSpaceLightPos0.xyz))) * 0.62;
                float3 absNormal = abs(normal);
                float2 worldUv = absNormal.y > absNormal.x && absNormal.y > absNormal.z
                    ? input.objectPos.xz
                    : (absNormal.x > absNormal.z ? input.objectPos.zy : input.objectPos.xy);
                worldUv = worldUv * _BaseMap_ST.xy * 2.65 + _BaseMap_ST.zw;
                fixed4 color = _BaseColor * _VoxelLightColor * input.color * tex2D(_BaseMap, worldUv);
                color.rgb *= light;
                return color;
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
