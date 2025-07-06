//trying to use waterscapefog.shader concept to define if pixel should have 1000.0f depth range or _DepthCutoff depth range

Shader "Hidden/DepthPost"
{
    SubShader
    {
        Cull Off 
        ZWrite Off 
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            sampler2D _CameraDepthTexture;
            sampler2D _MainTex;

            float4x4 _UweCameraToVolumeMatrix;
            sampler2D _CameraGBufferTexture2;

            float4x4 _CameraProj;
            float4x4 _CameraInvProj;
            float4x4 CameraToWorld;
            float4 _UweVsWaterPlane;

            float _DepthCutoff;
            float _WaterLevel;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 texcoord1 : TEXCOORD1;
            };

            v2f vert (appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;

                float4 tmp0;
                float4 tmp1;

                tmp0 = v.vertex.yyyy * unity_ObjectToWorld._m01_m11_m21_m31;
                tmp0 = unity_ObjectToWorld._m00_m10_m20_m30 * v.vertex.xxxx + tmp0;
                tmp0 = unity_ObjectToWorld._m02_m12_m22_m32 * v.vertex.zzzz + tmp0;
                tmp0 = tmp0 + unity_ObjectToWorld._m03_m13_m23_m33;
                tmp1 = tmp0.yyyy * unity_MatrixVP._m01_m11_m21_m31;
                tmp1 = unity_MatrixVP._m00_m10_m20_m30 * tmp0.xxxx + tmp1;
                tmp1 = unity_MatrixVP._m02_m12_m22_m32 * tmp0.zzzz + tmp1;
                tmp0 = unity_MatrixVP._m03_m13_m23_m33 * tmp0.wwww + tmp1;
                o.vertex = tmp0;
                tmp0.y = tmp0.y * _ProjectionParams.x;
                tmp0 = tmp0 * _ProjectionParams;
                tmp1.xyz = tmp0.yyy * _CameraInvProj._m01_m11_m21;
                tmp1.xyz = _CameraInvProj._m00_m10_m20 * tmp0.xxx + tmp1.xyz;
                tmp0.xyz = _CameraInvProj._m02_m12_m22 * tmp0.zzz + tmp1.xyz;
                o.texcoord1.xyz = _CameraInvProj._m03_m13_m23 * tmp0.www + tmp0.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv); //0 a 1
                float worldDepth = LinearEyeDepth(rawDepth); //converte para distancia real de mundo

                float3 viewDir = normalize(i.texcoord1);
                float3 viewPos = viewDir * worldDepth;
                float4 worldPos = mul(CameraToWorld, float4(viewPos, 1.0));

                float distanceToWater = dot(worldPos.xyz, _UweVsWaterPlane.xyz) + _UweVsWaterPlane.w;

                if (distanceToWater < 0.0)
                    return float4(0, 0, 1, 1); // Submerso
                else
                    return float4(1, 0, 0, 1); // Acima da água

                // float4 tmp0;
                // float4 tmp1;
                // float4 tmp2;
                // float4 tmp3;
                // float4 tmp4;
                // float4 tmp5;
                // float4 tmp6;
                // float4 tmp7;
                // float4 tmp8;
                // float4 tmp9;
                // float4 tmp10;
                // float4 tmp11;
                // tmp0 = tex2D(_MainTex, i.uv.xy);
                // tmp1 = tex2D(_CameraGBufferTexture2, i.uv.xy);
                // tmp1.x = tmp1.w * 1.5;
                // tmp1.y = tmp1.x >= -tmp1.x;
                // tmp1.x = frac(abs(tmp1.x));
                // tmp1.x = tmp1.y ? tmp1.x : -tmp1.x;
                // tmp1.x = tmp1.x < 0.25;
                // tmp1.y = true;
                // if (tmp1.y) 
                // {
                //     tmp2 = tex2D(_CameraDepthTexture, i.uv.xy);
                //     tmp1.y = _ZBufferParams.x * tmp2.x + _ZBufferParams.y;
                //     tmp1.y = 1.0 / tmp1.y;
                //     tmp1.z = tmp2.x == 1.0;
                //     tmp2.xyz = tmp1.yyy * i.texcoord1.xyz;
                //     tmp1.y = dot(tmp2.xyz, tmp2.xyz);
                //     tmp0.w = sqrt(tmp1.y);
                //     tmp2.xyz = tmp2.xyz / tmp0.www;
                //     // tmp3.xyz = tmp2.xyz * settingsSampleDistance.xxx;
                //     // tmp4.xyz = tmp3.yyy * _UweCameraToVolumeMatrix._m01_m11_m21;
                //     // tmp3.xyw = _UweCameraToVolumeMatrix._m00_m10_m20 * tmp3.xxx + tmp4.xyz;
                //     // tmp3.xyz = _UweCameraToVolumeMatrix._m02_m12_m22 * tmp3.zzz + tmp3.xyw;
                //     // tmp3.xyz = saturate(tmp3.xyz + _UweCameraToVolumeMatrix._m03_m13_m23);
                //     // tmp4.y = 1.0 / _UweVolumeTextureSlices;
                //     // tmp1.y = tmp3.y * _UweVolumeTextureSlices + -0.5;
                //     // tmp1.w = frac(tmp1.y);
                //     // tmp1.y = tmp1.y - tmp1.w;
                //     // tmp1.y = tmp1.y + tmp3.z;
                //     // tmp3.w = tmp4.y * tmp1.y;
                //     // tmp4.x = 0.0;
                //     // tmp3.yz = tmp3.xw + tmp4.xy;
                //     // tmp4 = tex2Dlod(_UweExtinctionTexture, float4(tmp3.xw, 0, 0.0));
                //     // tmp5 = tex2Dlod(_UweExtinctionTexture, float4(tmp3.yz, 0, 0.0));
                //     // tmp5 = tmp5 - tmp4;
                //     // tmp4 = tmp1.wwww * tmp5.wxyz + tmp4.wxyz;
                //     // tmp1.y = dot(tmp2.xyz, _UweVsWaterPlane.xyz);
                //     tmp2.w = _UweVsWaterPlane.w > 0.0;
                //     if (tmp2.w) {
                //         return float4(1, 0, 0, 1);
                //     } else {
                //         return float4(0, 0, 1, 1);
                //     }
                // }
            }
            ENDCG
        }
    }
}