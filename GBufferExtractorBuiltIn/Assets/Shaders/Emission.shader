Shader "Hidden/Capture/DifferenceEmission"
{
    Properties
    {
        _MainTex ("Final Render", 2D) = "white" {}
        _BaseTex ("Base Tex", 2D) = "gray" {}
        _BrightnessThreshold ("Brightness Threshold", Range(0, 1)) = 0.1
        _EmissionMultiplier ("Emission Multiplier", Range(1, 10)) = 1.0
        _MaskThreshold ("Mask Cutoff", Range(0, 1)) = 0.2
        _AlbedoPenalty ("Albedo Penalty Power", Range(0, 4)) = 1.0
        _PureWhiteDefinition ("Pure White Definition", Range(0, 1)) = 0.01176
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            sampler2D _BaseTex;
            float _BrightnessThreshold;
            float _EmissionMultiplier;
            float _MaskThreshold;
            float _AlbedoPenalty;
            float _PureWhiteDefinition;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 finalColor = tex2D(_MainTex, i.uv).rgb;
                fixed3 baseColor = tex2D(_BaseTex, i.uv).rgb;
                fixed3 difference = finalColor - baseColor - _BrightnessThreshold;
                fixed3 emissionMask  = max(fixed3(0,0,0), difference) * _EmissionMultiplier;

                float albedoBrightness = max(baseColor.r, max(baseColor.g, baseColor.b));
                float albedoDarkness = min(baseColor.r, min(baseColor.g, baseColor.b));
                float albedoAmplitude = albedoBrightness - albedoDarkness;
                // retorna 1 se amplitude < limiar, e 0 se amplitude >= limiar.
                float applyPenality = 1 - step(albedoAmplitude, _PureWhiteDefinition);

                float penaltyFactor = 1.0 - (albedoBrightness*applyPenality);
                emissionMask *= pow(penaltyFactor, _AlbedoPenalty);

                float maskBrightness = max(emissionMask.r, max(emissionMask.g, emissionMask.b));
                // retorna 0 se threshold < limiar, e 1 se threshold >= limiar.
                float keepColor = step(_MaskThreshold, maskBrightness);
                return fixed4(finalColor * keepColor, 1.0);
            }
            ENDCG
        }
    }
}