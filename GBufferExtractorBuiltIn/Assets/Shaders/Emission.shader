Shader "Hidden/Capture/EmissionMapApproximation"
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





// Shader "Custom/TextureDifference"
// {
//     Properties
//     {
//         _MainTex("After Everything Frame", 2D) = "white" {}
//         _BaseTex ("Base Tex", 2D) = "gray" {}
//         _BeforeLightTex("Before Light Frame", 2D) = "black" {}
//         _HueMinBrightness ("Hue Min Brightness", Range(-1, 1)) = 0.2
//         _PureWhiteDefinition ("Pure White Definition", Range(0, 1)) = 0.01176
//         _BrightnessThreshold ("Brightness Threshold", Range(-1, 1)) = 0.2
//         _MinBrightness ("Min Brightness", Range(0, 1)) = 0.2
//     }
//     SubShader
//     {
//         Tags { "RenderType" = "Opaque" }
//         Pass
//         {
//             ZTest Always Cull Off ZWrite Off

//             CGPROGRAM
//             #pragma vertex vert
//             #pragma fragment frag

//             sampler2D _MainTex;
//             sampler2D _BaseTex;
//             sampler2D _BeforeLightTex;
//             float _HueMinBrightness;
//             float _PureWhiteDefinition;
//             float _BrightnessThreshold;
//             float _MinBrightness;

//             struct appdata
//             {
//                 float4 vertex : POSITION;
//                 float2 uv : TEXCOORD0;
//             };

//             struct v2f
//             {
//                 float2 uv : TEXCOORD0;
//                 float4 vertex : SV_POSITION;
//             };

//             float3 rgb2hsv(float3 c) 
//             {
//                 float4 K = float4(0., -1./3., 2./3., -1.);
//                 float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
//                 float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
//                 float d = q.x - min(q.w, q.y);
//                 float e = 1e-10;
//                 return float3(abs(q.z + (q.w - q.y) / (6.*d + e)), d / (q.x + e), q.x);
//             }

//             v2f vert(appdata v)
//             {
//                 v2f o;
//                 o.vertex = UnityObjectToClipPos(v.vertex);
//                 o.uv = v.uv;
//                 return o;
//             }

//             fixed4 frag(v2f i) : SV_Target
//             {
//                 float _HueThresholds[256] = { 0.246473, 0.246484, 0.246496, 0.246509, 0.246524, 0.24654, 0.246557, 0.246574, 0.246593, 0.246613, 0.246633, 0.246653, 0.246675, 0.246696, 0.246718, 0.24674, 0.246763, 0.246785, 0.246807, 0.246829, 0.246851, 0.246873, 0.246894, 0.246915, 0.246935, 0.246954, 0.246973, 0.246991, 0.247007, 0.247023, 0.247038, 0.247051, 0.247064, 0.247074, 0.247084, 0.247091, 0.247097, 0.247101, 0.247104, 0.247104, 0.247103, 0.247099, 0.247093, 0.247085, 0.247074, 0.247061, 0.247045, 0.247027, 0.247006, 0.246982, 0.246955, 0.246925, 0.246892, 0.246855, 0.246816, 0.246773, 0.246726, 0.246676, 0.246623, 0.246565, 0.246504, 0.246594, 0.290457, 0.395898, 0.539002, 0.695855, 0.842542, 0.95515, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0.966708, 0.906913, 0.829126, 0.739358, 0.643622, 0.547928, 0.458287, 0.380711, 0.321211, 0.285799, 0.280416, 0.306607, 0.358966, 0.431472, 0.518104, 0.612842, 0.709666, 0.802554, 0.885488, 0.952445, 0.997406, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0.876494, 0.578916, 0.311579, 0.246752, 0.246805, 0.246852, 0.246892, 0.246926, 0.246954, 0.246976, 0.246994, 0.247006, 0.247013, 0.247016, 0.247015, 0.24701, 0.247001, 0.246989, 0.246974, 0.246956, 0.246935, 0.246913, 0.246888, 0.246862, 0.246835, 0.246806, 0.246777, 0.246747, 0.246717, 0.246687, 0.246658, 0.246629, 0.246601, 0.246574, 0.246549, 0.246525, 0.246504, 0.246485, 0.246469, 0.246455, 0.246445, 0.246443, 0.246443, 0.246443, 0.246443, 0.246443, 0.246443 };

//                 fixed4 final = tex2D(_MainTex, i.uv);
//                 fixed4 before = tex2D(_BeforeLightTex, i.uv);
//                 fixed3 base = tex2D(_BaseTex, i.uv);

//                 fixed3 finalHSV = rgb2hsv(final.rgb);
//                 fixed3 beforeHSV = rgb2hsv(before.rgb);
//                 fixed3 baseHSV = rgb2hsv(base.rgb);

//                 float hueDiff = abs(finalHSV.x - beforeHSV.x);
//                 hueDiff = min(hueDiff, 1.0 - hueDiff); // ajuste circular
//                 int hueIndex = clamp(int(finalHSV.x * 255.0), 0, 255);
//                 if (hueDiff > _HueThresholds[hueIndex] + _HueMinBrightness && finalHSV.y > _PureWhiteDefinition)
//                 {
//                     return final;
//                 }
                
//                 if (beforeHSV.z < _MinBrightness ||
//                     beforeHSV.z <= finalHSV.z) //_BrightnessThreshold
//                 {
//                     return fixed4(0.0, 0.0, 0.0, 1.0);
//                 }

//                 return before;
//             }
//             ENDCG
//         }
//     }
// }
