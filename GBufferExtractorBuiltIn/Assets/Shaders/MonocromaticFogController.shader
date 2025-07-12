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
            float _DepthMaxDistance;
            float _DepthCutoffBelowWater;
            float4 _UweVsWaterPlane;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewRay : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.viewRay = mul(unity_CameraInvProjection, float4(v.vertex.xy * 2.0 - 1.0, 1.0, 1.0)).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv); //0 a 1
                float linearDepth = LinearEyeDepth(rawDepth); //converte para distancia real de mundo
                float3 viewPos = i.viewRay * linearDepth;

                float waterPlaneModifier = -1;
                if (_WorldSpaceCameraPos.y > -3)
                {
                    waterPlaneModifier = 1;
                }
                float distanceToWaterPlane = dot(viewPos, _UweVsWaterPlane.xyz) + _UweVsWaterPlane.w + waterPlaneModifier;
                float isAboveWater = step(0, distanceToWaterPlane);
                float clippedDepth = saturate(linearDepth / _DepthMaxDistance); //0 a 1 de acordo com a escala _DepthMaxDistance
                if (!isAboveWater && clippedDepth > _DepthCutoffBelowWater)
                {
                    return fixed4(1, 1, 1, 1);
                }
                return fixed4(clippedDepth, clippedDepth, clippedDepth, 1);
            }
            ENDCG
        }
    }
}