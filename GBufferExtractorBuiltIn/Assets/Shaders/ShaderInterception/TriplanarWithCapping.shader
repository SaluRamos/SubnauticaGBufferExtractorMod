Shader "UWE/Terrain/Triplanar with Capping" {
	Properties {
		_Color ("Main Color (RGB)", Vector) = (1,1,1,1)
		[HDR] _SpecColor ("Specular Color", Vector) = (1,1,1,1)
		_CapColor ("Cap Color (RGB)", Vector) = (1,1,1,1)
		[HDR] _CapSpecColor ("Cap Specular Color", Vector) = (1,1,1,1)
		[TextureFeature(null, null, null, false)] _CapTexture ("Cap Base (RGB) Splotch(A)", 2D) = "green" {}
		[TextureFeature(null, null, null, false)] _CapBumpMap ("Cap Normal map", 2D) = "bump" {}
		_CapEmissionScale ("Cap Emission Scale", Range(0, 2)) = 1
		[TextureFeature(null, null, null, false)] _SideTexture ("Side Base (RGB) Splotch(A)", 2D) = "yellow" {}
		[TextureFeature(null, null, null, false)] _SideBumpMap ("Side Normal map", 2D) = "bump" {}
		_SideEmissionScale ("Side Emission Scale", Range(0, 2)) = 1
		[MarmoToggle(UWE_SIG,null,BoldLabel)] _EnableSIG ("SIG", Float) = 0
		[TextureFeature(null,UWE_SIG,null,false)] _CapSIGMap ("Cap Spec(R) Illum(G) map *Gloss/B ignored*", 2D) = "black" {}
		[TextureFeature(null,UWE_SIG,null,false)] _SideSIGMap ("Side Spec(R) Illum(G) map *Gloss/B ignored*", 2D) = "black" {}
		_CapScale ("CapScale", Float) = 0.1
		_SideScale ("SideScale", Float) = 0.1
		_TriplanarBlendRange ("Triplanar Blend Sharpness", Range(0.1, 80)) = 2
		_CapBorderBlendRange ("Cap Border Softness", Range(0, 1)) = 0.1
		_CapBorderBlendOffset ("Cap Border Offset", Range(-1, 0)) = 0
		_CapBorderBlendAngle ("Cap Border Angle", Range(0.5, 5)) = 1
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

			float4 _Color;

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return _Color; // RGBA
			}

			ENDHLSL
		}
	}
	Fallback "Hidden/UWE/Capped"
}