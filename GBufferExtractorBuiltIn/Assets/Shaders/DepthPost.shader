Shader "Hidden/DepthPost"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            sampler2D _CameraDepthTexture;
            
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

            float _DepthCutoff;

            fixed4 frag (v2f i) : SV_Target
            {
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float worldDepth = LinearEyeDepth(rawDepth);
                float clippedDepth = saturate(worldDepth / _DepthCutoff);
                return fixed4(clippedDepth, clippedDepth, clippedDepth, 1.0);
            }
            ENDCG
        }
    }
}