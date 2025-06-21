Shader "Unlit/Albedo"
{
    // Propriedades que o shader expõe no Inspector do Material.
    // Ele vai usar os valores dos materiais originais dos objetos.
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }

    SubShader
    {
        // Tag crucial para que o SetReplacementShader aplique este
        // shader em todos os objetos opacos padrão.
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            // Variáveis que recebem os valores das Properties
            sampler2D _MainTex;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0; // Coordenadas de textura (UV)
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            // O Vertex Shader apenas prepara os dados para o Fragment Shader
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; // Passa as coordenadas UV adiante
                return o;
            }
            
            // O Fragment Shader calcula a cor de cada pixel
            fixed4 frag (v2f i) : SV_Target
            {
                // Pega a cor da textura nas coordenadas UV do pixel atual
                fixed4 col = tex2D(_MainTex, i.uv);
                // Multiplica pela cor de tintura do material
                col *= _Color;
                
                // Retorna a cor final, sem iluminação
                return col;
            }
            ENDCG
        }
    }
    Fallback "VertexLit"
}