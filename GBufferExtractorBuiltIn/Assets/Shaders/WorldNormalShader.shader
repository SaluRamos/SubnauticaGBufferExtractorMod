Shader "Hidden/Custom/GlobalNormalShader"
{
    Properties {}
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
            };

            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // 1. Calculamos a normal no espaço do mundo (World-Space), como no primeiro shader.
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 normal = normalize(i.worldNormal);

                // 2. <<< MUDANÇA PRINCIPAL: Reordenando os eixos (Swizzling) >>>
                // Pegamos a normal (X,Y,Z) e a mapeamos para a cor (R,G,B)
                // de acordo com a convenção da imagem da cidade: R=Z, G=X, B=Y.
                // Em CG/HLSL, isso é escrito como "normal.zxy".
                fixed3 remappedNormal = normal.zxy;

                // 3. Convertemos o vetor remapeado [-1, 1] para o intervalo de cor [0, 1]
                fixed3 color = remappedNormal * 0.5 + 0.5;

                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}