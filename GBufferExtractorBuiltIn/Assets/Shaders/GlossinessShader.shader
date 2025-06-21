Shader "Custom/GlossinessShader"
{
    Properties
    {
        _GlossMap ("Glossiness Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _GlossMap;
            float4 _GlossMap_ST;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _GlossMap);
                return o;
            }

            fixed4 frag (v2f i) : SV_TARGET
            {
                float gloss = tex2D(_GlossMap, i.uv).r;
                return fixed4(gloss, gloss, gloss, 1); // Escala de cinza
            }
            ENDCG
        }
    }
    FallBack Off
}
