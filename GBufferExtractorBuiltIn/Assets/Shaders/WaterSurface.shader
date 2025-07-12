Shader "Hidden/WaterSurfaceOnGBuffer"
{
    Properties
    {
        _WaterDisplacementMap("Displacement", 2D) = "black" {}
        _NormalsTex("Normals", 2D) = "bump" {}
        _WaterPatchLength("Patch Length", Float) = 256
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Name "GBUFFER"
            Tags { "LightMode" = "Deferred" }
            Offset 0, 150
            Cull Off
            ZWrite On

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_gbuffer
            #pragma fragment frag_gbuffer
            #include "UnityCG.cginc"

            #define MAX_CLIPS 64
            int _ClipBoxCount;
            float4x4 _ClipBoxWorldToLocal[MAX_CLIPS];
            float4 _ClipBoxExtents[MAX_CLIPS];

            struct gbuffer_out {
                float4 rt0 : SV_TARGET0;
                float4 rt1 : SV_TARGET1;
                float4 rt2 : SV_TARGET2;
                float4 rt3 : SV_TARGET3;
            };

            sampler2D _WaterDisplacementMap;
            sampler2D _NormalsTex;
            float _WaterPatchLength;

            struct v2f_gbuffer
            {
                float4 pos : SV_POSITION;
                float2 uv_norm : TEXCOORD0;
                float3 worldPos : TEXCOORD1; 
            };

            v2f_gbuffer vert_gbuffer(appdata_base v)
            {
                v2f_gbuffer o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv_norm = (worldPos.xz * 100.0) / _WaterPatchLength;
                float dist_sq = dot(worldPos.xz - _WorldSpaceCameraPos.xz, worldPos.xz - _WorldSpaceCameraPos.xz);
                float fade = saturate((200.0 - sqrt(dist_sq)) * 0.0052083);
                float3 displacement = tex2Dlod(_WaterDisplacementMap, float4(o.uv_norm, 0, 0)).xyz;
                displacement *= 0.01;
                float3 finalWorldPos = (displacement * fade) + worldPos;
                o.pos = mul(UNITY_MATRIX_VP, float4(finalWorldPos, 1.0));
                o.worldPos = finalWorldPos; 
                return o;
            }

            gbuffer_out frag_gbuffer(v2f_gbuffer i)
            {
                for (int j = 0; j < _ClipBoxCount; j++)
                {
                    float3 localPos = mul(_ClipBoxWorldToLocal[j], float4(i.worldPos, 1.0)).xyz;
                    if (all(abs(localPos) < float3(_ClipBoxExtents[j].xyz)))
                    {
                        clip(-1);
                    }
                }

                gbuffer_out o;
                o.rt0 = float4(0.05, 0.1, 0.15, 1.0); // Albedo
                o.rt1 = float4(0.8, 0.8, 0.8, 0.9); // Specular/Smoothness
                o.rt2 = float4(tex2D(_NormalsTex, i.uv_norm).xyz, 1.0);
                o.rt3 = float4(0, 0, 0, 0); // Emission
                return o;
            }
            ENDCG
        }
    }
}