Shader "Hidden/MaterialIDShader"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _MaterialID;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float id = _MaterialID;
                return fixed4(
                    fmod(id, 256) / 255.0,
                    fmod(id / 256, 256) / 255.0,
                    fmod(id / (256 * 256), 256) / 255.0,
                    1.0);
            }
            ENDCG
        }
    }
}
