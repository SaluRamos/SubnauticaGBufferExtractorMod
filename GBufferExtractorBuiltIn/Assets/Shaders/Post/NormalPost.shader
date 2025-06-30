Shader "Hidden/NormalPost"
{
    Properties 
    {
        // _MainTex será preenchido pelo Blit com o GBuffer2
        _MainTex ("Texture", 2D) = "white" {} 
        _DepthCutoff ("Depth Cutoff", Float) = 150.0
    }
    SubShader
    {
        // Não precisa de culling, escrita de profundidade ou teste de profundidade.
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _CameraDepthTexture;
            sampler2D _MainTex; 

            float _DepthCutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv));
                float mask = 1.0 - step(_DepthCutoff, depth);
                float3 normalColor = tex2D(_MainTex, i.uv).rgb;
                return float4(normalColor * mask, 1.0);
            }
            ENDCG
        }
    }
}