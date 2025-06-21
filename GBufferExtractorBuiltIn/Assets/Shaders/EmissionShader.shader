Shader "Hidden/Custom/EmissionShader"
{
    Properties
    {
        _EmissionColor ("Emission Color", Color) = (0,0,0,0)
        _EmissionMap ("Emission", 2D) = "white" {}
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

            sampler2D _EmissionMap;
            float4 _EmissionMap_ST;
            fixed4 _EmissionColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _EmissionMap);
                return o;
            }

            fixed4 frag (v2f i) : SV_TARGET
            {
                // VERIFICAÇÃO: Se a cor de emissão é efetivamente preta,
                // o objeto não é emissivo. Retorne preto imediatamente.
                // (Usamos dot product para somar os canais RGB de forma eficiente).
                if (dot(_EmissionColor.rgb, float3(1,1,1)) < 0.001)
                {
                    return fixed4(0, 0, 0, 1);
                }

                // Se a cor não for preta, calcule a emissão normalmente.
                fixed4 emission = tex2D(_EmissionMap, i.uv) * _EmissionColor;
                return emission;
            }
            ENDCG
        }
    }
}