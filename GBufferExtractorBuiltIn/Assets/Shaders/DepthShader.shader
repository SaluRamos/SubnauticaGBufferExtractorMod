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
                
                o.depth = -UnityObjectToViewPos(v.vertex).z;

                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float depthValue = saturate(i.depth / _ProjectionParams.z);
                return fixed4(depthValue, depthValue, depthValue, 1.0);
            }
            ENDCG
        }
    }
    Fallback "VertexLit"
}