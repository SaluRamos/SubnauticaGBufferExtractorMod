Shader "Hidden/Custom/ViewSpaceNormalShader"
{
    Properties
    {
        // Nenhuma propriedade � necess�ria
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                // Vamos passar a normal no espa�o da c�mera (View-Space)
                float3 viewNormal : TEXCOORD0; 
            };

            v2f vert (appdata v)
            {
                v2f o;
                // Transforma a posi��o para o espa�o de clipe (tela)
                o.pos = UnityObjectToClipPos(v.vertex);
                
                // --- ESTA � A MUDAN�A PRINCIPAL ---
                // Transforma a normal do espa�o do objeto para o espa�o de vis�o da c�mera.
                // UNITY_MATRIX_IT_MV � a matriz correta para transformar normais.
                // (Inverse Transpose of the ModelView matrix)
                o.viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Normaliza o vetor ap�s a interpola��o.
                fixed3 normal = normalize(i.viewNormal);

                // Mapeia o intervalo da normal [-1, 1] para o intervalo de cor [0, 1].
                fixed3 color = normal * 0.5 + 0.5;

                // Retorna a cor final.
                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}