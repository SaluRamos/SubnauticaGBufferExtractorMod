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

            float4x4 _CameraProj;
            float4x4 CameraToWorld;
            float _DepthCutoff;
            float _WaterLevel;
            
            struct appdata
            {
                float4 vertex : POSITION; // Vértice do quad que cobre a tela
                float2 uv : TEXCOORD0;    // Coordenada UV na imagem
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv); //0 a 1
                float worldDepth = LinearEyeDepth(rawDepth); //converte para distancia real de mundo
                rawDepth = 1 - rawDepth;

                const float2 p11_22 = float2(_CameraProj._11, _CameraProj._22);
                const float2 p13_31 = float2(_CameraProj._13, _CameraProj._23);
                const float isOrtho = 0.0;
                const float near = _ProjectionParams.y;
                const float far = _ProjectionParams.z;

                float zOrtho = lerp(near, far, rawDepth);
                float zPers = near * far / lerp(far, near, rawDepth);
                float vz = lerp(zPers, zOrtho, isOrtho);

                float3 vpos = float3((i.uv * 2 - 1 - p13_31) / p11_22 * lerp(vz, 1, isOrtho), -vz);
                float4 wpos = mul(CameraToWorld, float4(vpos, 1));

                if (wpos.y > _WaterLevel)
                {
                    float clippedDepth = saturate(worldDepth / 2000.0); //saturate restringe de 0 a 1
                    return fixed4(clippedDepth, clippedDepth, clippedDepth, 1.0);
                }
                float clippedDepth = saturate(worldDepth / _DepthCutoff);
                return fixed4(clippedDepth, clippedDepth, clippedDepth, 1.0);
            }
            ENDCG
        }
    }
}