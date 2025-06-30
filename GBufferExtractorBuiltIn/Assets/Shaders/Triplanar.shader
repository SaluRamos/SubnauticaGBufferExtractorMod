Shader "UWE/Terrain/Triplanar" {
	Properties {
		_Color ("Main Color (RGB)", Color) = (1,1,1,1)
		[TextureFeature(null, null, null, false)] _MainTex ("Base (RGB) Splotch(A)", 2D) = "green" {}
		[TextureFeature(null, null, null, false)] _BumpMap ("Normalmap", 2D) = "bump" {}
		[MarmoToggle(UWE_SIG,null,BoldLabel)] _EnableSIG ("SIG", Float) = 0
		[TextureFeature(null, UWE_SIG, null, false)] _SIGMap ("Spec(R) Illum(G) map *Gloss/B ignored*", 2D) = "black" {}
		[HDR] _SpecColor ("Specular Color", Color) = (1,1,1,1)
		_EmissionScale ("Emission Scale", Range(0, 2)) = 1
		_TriplanarScale ("TriplanarScale", Float) = 0.1
		_TriplanarBlendRange ("Triplanar Blend Sharpness", Range(0.1, 80)) = 2
		_InnerBorderBlendRange ("Inner Border Softness", Range(0, 1)) = 0.5
		_InnerBorderBlendOffset ("Inner Border Offset", Range(0, 1)) = 1
		_BorderTint ("Border Tint (RGB)", Color) = (1,1,1,1)
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
	SubShader {
		Tags { "QUEUE" = "Geometry+1" "RenderType" = "Opaque" }
		Pass {
			Name "DEFERRED"
			Tags { "LIGHTMODE" = "DEFERRED" "QUEUE" = "Geometry+1" "RenderType" = "Opaque" }
			Blend Zero Zero, Zero Zero
			ColorMask 0
			ZWrite Off
			Fog {
				Mode 0
			}
			GpuProgramID 61004
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			struct v2f
			{
				float4 position : SV_POSITION0;
				float3 texcoord : TEXCOORD0;
				float3 texcoord1 : TEXCOORD1;
				float2 texcoord2 : TEXCOORD2;
			};
			struct fout
			{
				float4 sv_target : SV_Target0;
				float4 sv_target1 : SV_Target1;
				float4 sv_target2 : SV_Target2;
				float4 sv_target3 : SV_Target3;
			};
			// $Globals ConstantBuffers for Vertex Shader
			// $Globals ConstantBuffers for Fragment Shader
			float4 _SpecColor;
			float _BorderBlendRange;
			float _BorderBlendOffset;
			float _IsOpaque;
			float4 _Color;
			float _TriplanarScale;
			float _TriplanarBlendRange;
			float _InnerBorderBlendRange;
			float _InnerBorderBlendOffset;
			float4 _BorderTint;
			// Custom ConstantBuffers for Vertex Shader
			// Custom ConstantBuffers for Fragment Shader
			// Texture params for Vertex Shader
			// Texture params for Fragment Shader
			sampler2D _MainTex;
			sampler2D _BumpMap;
			
			// Keywords: 
			v2f vert(appdata_full v)
			{
                v2f o;
                float4 tmp0;
                float4 tmp1;
                tmp0 = v.vertex.yyyy * unity_ObjectToWorld._m01_m11_m21_m31;
                tmp0 = unity_ObjectToWorld._m00_m10_m20_m30 * v.vertex.xxxx + tmp0;
                tmp0 = unity_ObjectToWorld._m02_m12_m22_m32 * v.vertex.zzzz + tmp0;
                tmp1 = tmp0 + unity_ObjectToWorld._m03_m13_m23_m33;
                o.texcoord.xyz = unity_ObjectToWorld._m03_m13_m23 * v.vertex.www + tmp0.xyz;
                tmp0 = tmp1.yyyy * unity_MatrixVP._m01_m11_m21_m31;
                tmp0 = unity_MatrixVP._m00_m10_m20_m30 * tmp1.xxxx + tmp0;
                tmp0 = unity_MatrixVP._m02_m12_m22_m32 * tmp1.zzzz + tmp0;
                o.position = unity_MatrixVP._m03_m13_m23_m33 * tmp1.wwww + tmp0;
                tmp0.x = dot(v.normal.xyz, unity_WorldToObject._m00_m10_m20);
                tmp0.y = dot(v.normal.xyz, unity_WorldToObject._m01_m11_m21);
                tmp0.z = dot(v.normal.xyz, unity_WorldToObject._m02_m12_m22);
                tmp0.w = dot(tmp0.xyz, tmp0.xyz);
                tmp0.w = rsqrt(tmp0.w);
                o.texcoord1.xyz = tmp0.www * tmp0.xyz;
                o.texcoord2.xy = v.texcoord.xy;
                return o;
			}
			// Keywords: 
			fout frag(v2f inp)
			{
                fout o;
                float4 tmp0;
                float4 tmp1;
                float4 tmp2;
                float4 tmp3;
                float4 tmp4;
                float4 tmp5;
                tmp0.x = _InnerBorderBlendRange + 1.0;
                tmp0.y = 1.01 - _InnerBorderBlendOffset;
                tmp0.x = tmp0.x / tmp0.y;
                tmp0.y = 1.0 - inp.texcoord2.x;
                tmp0.z = tmp0.y - _InnerBorderBlendOffset;
                tmp0.y = tmp0.y - _BorderBlendOffset;
                tmp0.x = tmp0.x * tmp0.z + -_InnerBorderBlendRange;
                tmp1.xyz = inp.texcoord1.xyz * inp.texcoord1.xyz;
                tmp1.xyz = tmp1.xyz * float3(1.96, 1.96, 1.96);
                tmp1.xyz = log(tmp1.xyz);
                tmp1.xyz = tmp1.xyz * _TriplanarBlendRange.xxx;
                tmp1.xyz = exp(tmp1.xyz);
                tmp1.xyz = tmp1.xyz * _TriplanarBlendRange.xxx;
                tmp0.z = dot(tmp1.xyz, float3(1.0, 1.0, 1.0));
                tmp1.xyz = tmp1.xyz / tmp0.zzz;
                tmp2.xyz = inp.texcoord.xyz * _TriplanarScale.xxx;
                tmp3 = tex2D(_MainTex, tmp2.xz);
                tmp3 = tmp1.yyyy * tmp3;
                tmp4 = tex2D(_MainTex, tmp2.yz);
                tmp3 = tmp1.xxxx * tmp4 + tmp3;
                tmp4 = tex2D(_MainTex, tmp2.yx);
                tmp3 = tmp1.zzzz * tmp4 + tmp3;
                tmp0.x = tmp3.w - tmp0.x;
                tmp0.x = saturate(tmp0.x / _InnerBorderBlendRange);
                tmp4.xyz = _Color.xyz * tmp3.xyz + -_BorderTint.xyz;
                o.sv_target.xyz = tmp0.xxx * tmp4.xyz + _BorderTint.xyz;
                tmp0.x = _BorderBlendRange + 1.0;
                tmp0.z = 1.01 - _BorderBlendOffset;
                tmp0.x = tmp0.x / tmp0.z;
                tmp0.x = tmp0.x * tmp0.y + -_BorderBlendRange;
                tmp0.x = tmp3.w - tmp0.x;
                o.sv_target1.xyz = tmp3.xxx * _SpecColor.xyz;
                tmp0.x = saturate(tmp0.x / _BorderBlendRange);
                tmp0.y = _IsOpaque > 0.0;
                tmp3.x = 0.0;
                tmp3.y = inp.texcoord2.y;
                tmp0.xw = tmp0.yy ? tmp3.xy : tmp0.xx;
                o.sv_target2.w = tmp0.x;
                o.sv_target.w = tmp0.w;
                o.sv_target1.w = tmp0.w;
                o.sv_target3.w = tmp0.w;
                tmp0.xyz = inp.texcoord1.xzy * float3(1.0, -1.0, 1.0);
                tmp3.xyz = inp.texcoord1.xyz > float3(0.0, 0.0, 0.0);
                tmp4.xyz = inp.texcoord1.xyz < float3(0.0, 0.0, 0.0);
                tmp3.xyz = tmp4.xyz - tmp3.xyz;
                tmp3.xyz = floor(tmp3.xyz);
                tmp0.xyz = tmp0.xyz * tmp3.yyy;
                tmp4 = tex2D(_BumpMap, tmp2.xz);
                tmp4.x = tmp4.w * tmp4.x;
                tmp4.xy = tmp4.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
                tmp0.xyz = tmp0.xyz * tmp4.yyy;
                tmp5.xyz = inp.texcoord1.yxz * float3(1.0, -1.0, 1.0);
                tmp5.xyz = tmp3.yyy * tmp5.xyz;
                tmp0.xyz = tmp4.xxx * tmp5.xyz + tmp0.xyz;
                tmp0.w = dot(tmp4.xy, tmp4.xy);
                tmp0.w = min(tmp0.w, 1.0);
                tmp0.w = 1.0 - tmp0.w;
                tmp0.w = sqrt(tmp0.w);
                tmp0.xyz = tmp0.www * inp.texcoord1.xyz + tmp0.xyz;
                tmp0.xyz = tmp1.yyy * tmp0.xyz;
                tmp4.xyz = inp.texcoord1.zyx * float3(-1.0, 1.0, 1.0);
                tmp3.xyw = tmp3.xxx * tmp4.xyz;
                tmp4.xyz = tmp3.zzz * inp.texcoord1.zyx;
                tmp5 = tex2D(_BumpMap, tmp2.yz);
                tmp2 = tex2D(_BumpMap, tmp2.yx);
                tmp5.x = tmp5.w * tmp5.x;
                tmp1.yw = tmp5.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
                tmp5.xyz = tmp3.xyw * tmp1.www;
                tmp3.xyz = tmp1.yyy * tmp3.ywx + tmp5.xyz;
                tmp0.w = dot(tmp1.xy, tmp1.xy);
                tmp0.w = min(tmp0.w, 1.0);
                tmp0.w = 1.0 - tmp0.w;
                tmp0.w = sqrt(tmp0.w);
                tmp3.xyz = tmp0.www * inp.texcoord1.xyz + tmp3.xyz;
                tmp0.xyz = tmp3.xyz * tmp1.xxx + tmp0.xyz;
                tmp2.x = tmp2.w * tmp2.x;
                tmp1.xy = tmp2.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
                tmp2.xyz = tmp4.xyz * tmp1.yyy;
                tmp2.xyz = tmp1.xxx * tmp4.zxy + tmp2.xyz;
                tmp0.w = dot(tmp1.xy, tmp1.xy);
                tmp0.w = min(tmp0.w, 1.0);
                tmp0.w = 1.0 - tmp0.w;
                tmp0.w = sqrt(tmp0.w);
                tmp1.xyw = tmp0.www * inp.texcoord1.xyz + tmp2.xyz;
                tmp0.xyz = tmp1.xyw * tmp1.zzz + tmp0.xyz;
                tmp0.w = dot(tmp0.xyz, tmp0.xyz);
                tmp0.w = rsqrt(tmp0.w);
                tmp0.xyz = tmp0.www * tmp0.xyz;
                o.sv_target2.xyz = tmp0.xyz * float3(0.5, 0.5, 0.5) + float3(0.5, 0.5, 0.5);
                o.sv_target3.xyz = float3(1.0, 1.0, 1.0);
                return o;
			}
			ENDCG
		}
	}
	Fallback "Bumped Specular"
}