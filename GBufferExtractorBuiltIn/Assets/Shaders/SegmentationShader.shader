Shader "Hidden/Custom/SegmentationShader"
{
    Properties
    {
        // ID numérico que representa a classe do objeto (ex: 1=Carro, 2=Prédio)
        _SegmentationID ("Segmentation ID", Float) = 0
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

            float _SegmentationID;

            // Função simples que converte um float em uma cor pseudo-aleatória, mas consistente.
            float3 idToColor(float id)
            {
                // Usa funções trigonométricas para criar cores distintas e bem distribuídas
                float r = frac(sin(id * 12.9898) * 43758.5453);
                float g = frac(sin(id * 78.233) * 12345.6789);
                float b = frac(sin(id * 34.435) * 9876.5432);
                return float3(r, g, b);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_TARGET
            {
                // Se o ID for 0 (fundo), retorna preto.
                if (_SegmentationID < 1.0)
                {
                    return fixed4(0, 0, 0, 1);
                }
                
                // Converte o ID da categoria em uma cor.
                fixed3 color = idToColor(_SegmentationID);
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
}