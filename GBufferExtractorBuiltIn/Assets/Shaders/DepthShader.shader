Shader "Unlit/DepthMap_Corrected"
{
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
                float4 pos   : SV_POSITION;
                float  depth : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                
                // Calcula a distância linear do objeto até a câmera (sempre positiva)
                o.depth = -UnityObjectToViewPos(v.vertex).z;

                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // CORREÇÃO AQUI:
                // Normalizamos a profundidade dividindo-a pela distância do far plane da câmera.
                // _ProjectionParams.z é uma variável interna do Unity que contém o valor do "far".
                // `saturate` garante que o valor fique entre 0 e 1.
                float depthValue = saturate(i.depth / _ProjectionParams.z);
                
                // Retorna o valor como escala de cinza.
                // Perto (depth ~ 0) -> depthValue ~ 0 -> Preto
                // Longe (depth ~ far) -> depthValue ~ 1 -> Branco
                return fixed4(depthValue, depthValue, depthValue, 1.0);
            }
            ENDCG
        }
    }
    Fallback "VertexLit"
}