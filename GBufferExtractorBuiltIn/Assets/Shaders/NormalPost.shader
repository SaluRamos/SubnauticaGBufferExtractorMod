// Upgrade NOTE: commented out 'float3 _WorldSpaceCameraPos', a built-in variable

Shader "Hidden/DepthPost"
{
    Properties 
    {
        _MainTex ("Texture", 2D) = "white" {} 
    }
    SubShader
    {
        Cull Off 
        ZWrite Off 
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _CameraDepthTexture;
            sampler2D _MainTex;

            float4x4 _CameraProj;
            float4x4 CameraToWorld;
            float _DepthCutoff;
            float _WaterLevel;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv); //0 a 1
                float invRawDepth = 1 - rawDepth;

                const float2 p11_22 = float2(_CameraProj._11, _CameraProj._22);
                const float2 p13_31 = float2(_CameraProj._13, _CameraProj._23);
                const float isOrtho = 0.0;
                const float near = _ProjectionParams.y;
                const float far = _ProjectionParams.z;

                float zOrtho = lerp(near, far, invRawDepth);
                float zPers = near * far / lerp(far, near, invRawDepth);
                float vz = lerp(zPers, zOrtho, isOrtho);

                float3 vpos = float3((i.uv * 2 - 1 - p13_31) / p11_22 * lerp(vz, 1, isOrtho), -vz);
                float4 wpos = mul(CameraToWorld, float4(vpos, 1));

                float depth = LinearEyeDepth(rawDepth);
                float mask;
                if (wpos.y > _WaterLevel)
                {
                    mask = 1.0 - step(1000.0, depth);
                }
                else
                {
                    mask = 1.0 - step(_DepthCutoff, depth);
                }
                float3 normalColor = tex2D(_MainTex, i.uv).rgb;
                return float4(normalColor * mask, 1.0);
            }
            ENDCG
        }
    }
}
