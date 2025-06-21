Shader "Hidden/Custom/SpecularShader"
{
    Properties
    {
        // Esta propriedade corresponde a _Glossiness (no setup Specular) 
        // ou _Smoothness (no setup Metallic) do material Padrão da Unity.
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            // O Unity preencherá esta variável com o valor da propriedade de mesmo nome
            // do material original do objeto.
            half _Glossiness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_TARGET
            {
                // Retorna o valor de suavidade/brilho como uma cor em escala de cinza.
                // Preto (0) = não especular, Branco (1) = totalmente especular/liso.
                return fixed4(_Glossiness, _Glossiness, _Glossiness, 1);
            }
            ENDCG
        }
    }
}