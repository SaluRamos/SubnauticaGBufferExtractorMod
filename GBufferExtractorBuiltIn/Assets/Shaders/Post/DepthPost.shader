Shader "Hidden/DepthPost"
{
    Properties
    {
        // _MainTex é a imagem colorida da cena, fornecida automaticamente pelo Graphics.Blit.
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // Sem culling, sem ZWrite, etc. É um passe 2D.
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            // O Unity preenche esta textura automaticamente por causa de 'depthTextureMode'.
            sampler2D _CameraDepthTexture;
            
            // Variável opcional que você pode definir via script C#
            // float _MaxDepth;

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

            // O vertex shader para um pós-processamento é quase sempre o mesmo.
            // Ele apenas prepara o quad que cobrirá a tela.
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Pega o valor bruto da textura de profundidade.
                //    SAMPLE_DEPTH_TEXTURE é uma macro que lida com diferenças entre plataformas.
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);

                // 2. Converte o valor bruto (não-linear) para um valor linear entre [0, 1].
                //    0 = perto (near clip plane), 1 = longe (far clip plane).
                //    Esta é a forma mais fácil e robusta de visualizar a profundidade.
                float linearDepth = Linear01Depth(rawDepth);
                
                // Retorna o valor de profundidade como uma cor em escala de cinza.
                return fixed4(linearDepth, linearDepth, linearDepth, 1.0);
            }
            ENDCG
        }
    }
}