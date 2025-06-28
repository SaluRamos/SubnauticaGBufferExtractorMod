Shader "UWE/Terrain/Triplanar" {
	Properties {
		_Color ("Main Color (RGB)", Vector) = (1,1,1,1)
		[TextureFeature(null, null, null, false)] _MainTex ("Base (RGB) Splotch(A)", 2D) = "green" {}
		[TextureFeature(null, null, null, false)] _BumpMap ("Normalmap", 2D) = "bump" {}
		[MarmoToggle(UWE_SIG,null,BoldLabel)] _EnableSIG ("SIG", Float) = 0
		[TextureFeature(null, UWE_SIG, null, false)] _SIGMap ("Spec(R) Illum(G) map *Gloss/B ignored*", 2D) = "black" {}
		[HDR] _SpecColor ("Specular Color", Vector) = (1,1,1,1)
		_EmissionScale ("Emission Scale", Range(0, 2)) = 1
		_TriplanarScale ("TriplanarScale", Float) = 0.1
		_TriplanarBlendRange ("Triplanar Blend Sharpness", Range(0.1, 80)) = 2
		_InnerBorderBlendRange ("Inner Border Softness", Range(0, 1)) = 0.5
		_InnerBorderBlendOffset ("Inner Border Offset", Range(0, 1)) = 1
		_BorderTint ("Border Tint (RGB)", Vector) = (1,1,1,1)
		_BorderBlendRange ("Border Softness", Range(0, 1)) = 0.5
		_BorderBlendOffset ("Border Offset", Range(0, 1)) = 0
		_Gloss ("Gloss", Range(0, 1)) = 0.5
		[Enum(Off,0,On,1)] _ZWrite ("DO NOT EDIT", Float) = 0
		[Enum(Blue,2,Green,4,Red,8,RGB,14,All,255)] _ColorMask ("DO NOT EDIT ColorMask", Float) = 14
		[Enum(Zero,0,One,1,SrcAlpha,5,OneMinusSrcAlpha,10)] _BlendSrcFactor ("Blend Src", Float) = 5
		[Enum(Zero,0,One,1,SrcAlpha,5,OneMinusSrcAlpha,10)] _BlendDstFactor ("Blend Dst", Float) = 10
		_IsOpaque ("DO NOT EDIT IsOpaque", Float) = 0
		_AlphaTestValue ("DO NOT EDIT AlphaTestValue", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_MatrixMVP;

			struct Vertex_Stage_Input
			{
				float3 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixMVP, float4(input.pos, 1.0));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, float2(input.uv.x, input.uv.y)) * float4(0, 1, 0, 1);
			}

			ENDHLSL
		}
	}
	Fallback "Bumped Specular"
}