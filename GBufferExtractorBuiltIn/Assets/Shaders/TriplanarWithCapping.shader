Shader "UWE/Terrain/Triplanar with Capping" {
	Properties {
		_Color ("Main Color (RGB)", Color) = (1,1,1,1)
		[HDR] _SpecColor ("Specular Color", Color) = (1,1,1,1)
		_CapColor ("Cap Color (RGB)", Color) = (1,1,1,1)
		[HDR] _CapSpecColor ("Cap Specular Color", Color) = (1,1,1,1)
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
			GpuProgramID 64919
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
			float _CapScale;
			float _SideScale;
			float _TriplanarBlendRange;
			float4 _CapColor;
			float4 _CapSpecColor;
			float _CapBorderBlendRange;
			float _CapBorderBlendOffset;
			float _CapBorderBlendAngle;
			float _InnerBorderBlendRange;
			float _InnerBorderBlendOffset;
			float4 _BorderTint;
			// Custom ConstantBuffers for Vertex Shader
			// Custom ConstantBuffers for Fragment Shader
			// Texture params for Vertex Shader
			// Texture params for Fragment Shader
			sampler2D _CapTexture;
			sampler2D _SideTexture;
			sampler2D _SideBumpMap;
			sampler2D _CapBumpMap;
			
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
                float4 tmp6;
                float4 tmp7;
                float4 tmp8;
                float4 tmp9;
                tmp0.xy = inp.texcoord.xz * _CapScale.xx;
                tmp1 = tex2D(_CapTexture, tmp0.xy);
                tmp0.z = inp.texcoord1.y + _CapBorderBlendOffset;
                tmp0.z = -tmp0.z * _CapBorderBlendAngle + 1.0;
                tmp0.z = tmp0.z - tmp1.w;
                tmp0.z = max(tmp0.z, -1.0);
                tmp0.z = min(tmp0.z, 1.0);
                tmp0.z = tmp0.z + _CapBorderBlendRange;
                tmp0.w = _CapBorderBlendRange + _CapBorderBlendRange;
                tmp0.z = saturate(tmp0.z / tmp0.w);
                tmp0.w = tmp0.z > 0.0;
                if (tmp0.w) {
                    tmp2.xyz = inp.texcoord1.xyz * inp.texcoord1.xyz;
                    tmp2.xyz = tmp2.xyz * float3(1.96, 1.96, 1.96);
                    tmp2.xyz = log(tmp2.xyz);
                    tmp2.xyz = tmp2.xyz * _TriplanarBlendRange.xxx;
                    tmp2.xyz = exp(tmp2.xyz);
                    tmp2.xyz = tmp2.xyz * _TriplanarBlendRange.xxx;
                    tmp0.w = dot(tmp2.xyz, float3(1.0, 1.0, 1.0));
                    tmp2.xyz = tmp2.xyz / tmp0.www;
                    tmp3.xyz = inp.texcoord.xyz * _SideScale.xxx;
                    tmp4 = tex2D(_SideTexture, tmp3.yz);
                    tmp5 = tex2D(_SideTexture, tmp3.xz);
                    tmp5 = tmp2.yyyy * tmp5;
                    tmp4 = tmp2.xxxx * tmp4 + tmp5;
                    tmp5 = tex2D(_SideTexture, tmp3.yx);
                    tmp4 = tmp2.zzzz * tmp5 + tmp4;
                    tmp5.xyz = inp.texcoord1.xyz > float3(0.0, 0.0, 0.0);
                    tmp6.xyz = inp.texcoord1.xyz < float3(0.0, 0.0, 0.0);
                    tmp5.xyz = tmp6.xyz - tmp5.xyz;
                    tmp5.xyz = floor(tmp5.xyz);
                    tmp6 = tex2D(_SideBumpMap, tmp3.yz);
                    tmp6.x = tmp6.w * tmp6.x;
                    tmp6.xy = tmp6.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
                    tmp0.w = dot(tmp6.xy, tmp6.xy);
                    tmp0.w = min(tmp0.w, 1.0);
                    tmp0.w = 1.0 - tmp0.w;
                    tmp0.w = sqrt(tmp0.w);
                    tmp7.xyz = inp.texcoord1.zyx * float3(-1.0, 1.0, 1.0);
                    tmp7.xyz = tmp5.xxx * tmp7.xyz;
                    tmp6.yzw = tmp6.yyy * tmp7.xyz;
                    tmp6.xyz = tmp6.xxx * tmp7.yzx + tmp6.yzw;
                    tmp6.xyz = tmp0.www * inp.texcoord1.xyz + tmp6.xyz;
                    tmp7 = tex2D(_SideBumpMap, tmp3.xz);
                    tmp7.x = tmp7.w * tmp7.x;
                    tmp3.zw = tmp7.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
                    tmp0.w = dot(tmp3.xy, tmp3.xy);
                    tmp0.w = min(tmp0.w, 1.0);
                    tmp0.w = 1.0 - tmp0.w;
                    tmp0.w = sqrt(tmp0.w);
                    tmp7.xyz = inp.texcoord1.yxz * float3(1.0, -1.0, 1.0);
                    tmp7.xyz = tmp5.yyy * tmp7.xyz;
                    tmp8.xyz = inp.texcoord1.xzy * float3(1.0, -1.0, 1.0);
                    tmp5.xyw = tmp5.yyy * tmp8.xyz;
                    tmp8.xyz = tmp3.www * tmp5.xyw;
                    tmp8.xyz = tmp3.zzz * tmp7.xyz + tmp8.xyz;
                    tmp8.xyz = tmp0.www * inp.texcoord1.xyz + tmp8.xyz;
                    tmp3 = tex2D(_SideBumpMap, tmp3.yx);
                    tmp3.x = tmp3.w * tmp3.x;
                    tmp3.xy = tmp3.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
                    tmp0.w = dot(tmp3.xy, tmp3.xy);
                    tmp0.w = min(tmp0.w, 1.0);
                    tmp0.w = 1.0 - tmp0.w;
                    tmp0.w = sqrt(tmp0.w);
                    tmp9.xyz = tmp5.zzz * inp.texcoord1.zyx;
                    tmp3.yzw = tmp3.yyy * tmp9.xyz;
                    tmp3.xyz = tmp3.xxx * tmp9.zxy + tmp3.yzw;
                    tmp3.xyz = tmp0.www * inp.texcoord1.xyz + tmp3.xyz;
                    tmp8.xyz = tmp2.yyy * tmp8.xyz;
                    tmp2.xyw = tmp6.xyz * tmp2.xxx + tmp8.xyz;
                    tmp2.xyz = tmp3.xyz * tmp2.zzz + tmp2.xyw;
                    tmp0.w = dot(tmp2.xyz, tmp2.xyz);
                    tmp0.w = rsqrt(tmp0.w);
                    tmp3 = tmp1 * _CapColor;
                    tmp4 = tmp4 * _Color + -tmp3;
                    tmp3 = tmp0.zzzz * tmp4 + tmp3;
                    tmp4 = tex2D(_CapBumpMap, tmp0.xy);
                    tmp4.x = tmp4.w * tmp4.x;
                    tmp4.xy = tmp4.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
                    tmp2.w = dot(tmp4.xy, tmp4.xy);
                    tmp2.w = min(tmp2.w, 1.0);
                    tmp2.w = 1.0 - tmp2.w;
                    tmp2.w = sqrt(tmp2.w);
                    tmp4.yzw = tmp5.xyw * tmp4.yyy;
                    tmp4.xyz = tmp4.xxx * tmp7.xyz + tmp4.yzw;
                    tmp4.xyz = tmp2.www * inp.texcoord1.xyz + tmp4.xyz;
                    tmp2.w = dot(tmp4.xyz, tmp4.xyz);
                    tmp2.w = rsqrt(tmp2.w);
                    tmp4.xyz = tmp2.www * tmp4.xyz;
                    tmp2.xyz = tmp2.xyz * tmp0.www + -tmp4.xyz;
                    tmp2.xyz = tmp0.zzz * tmp2.xyz + tmp4.xyz;
                    tmp0.w = dot(tmp2.xyz, tmp2.xyz);
                    tmp0.w = rsqrt(tmp0.w);
                    tmp2.xyz = tmp0.www * tmp2.xyz;
                    tmp4.xyz = _SpecColor.xyz - _CapSpecColor.xyz;
                    tmp4.xyz = tmp0.zzz * tmp4.xyz + _CapSpecColor.xyz;
                } else {
                    tmp3 = tmp1 * _CapColor;
                    tmp0 = tex2D(_CapBumpMap, tmp0.xy);
                    tmp0.x = tmp0.w * tmp0.x;
                    tmp0.xy = tmp0.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
                    tmp0.z = dot(tmp0.xy, tmp0.xy);
                    tmp0.z = min(tmp0.z, 1.0);
                    tmp0.z = 1.0 - tmp0.z;
                    tmp0.z = sqrt(tmp0.z);
                    tmp0.w = inp.texcoord1.y > 0.0;
                    tmp1.x = inp.texcoord1.y < 0.0;
                    tmp0.w = tmp1.x - tmp0.w;
                    tmp0.w = floor(tmp0.w);
                    tmp1.xyz = inp.texcoord1.yxz * float3(1.0, -1.0, 1.0);
                    tmp1.xyz = tmp0.www * tmp1.xyz;
                    tmp5.xyz = inp.texcoord1.xzy * float3(1.0, -1.0, 1.0);
                    tmp5.xyz = tmp0.www * tmp5.xyz;
                    tmp5.xyz = tmp0.yyy * tmp5.xyz;
                    tmp0.xyw = tmp0.xxx * tmp1.xyz + tmp5.xyz;
                    tmp0.xyz = tmp0.zzz * inp.texcoord1.xyz + tmp0.xyw;
                    tmp0.w = dot(tmp0.xyz, tmp0.xyz);
                    tmp0.w = rsqrt(tmp0.w);
                    tmp2.xyz = tmp0.www * tmp0.xyz;
                    tmp4.xyz = _SpecColor.xyz;
                }
                tmp0.x = 1.0 - inp.texcoord2.x;
                tmp0.y = _InnerBorderBlendRange + 1.0;
                tmp0.z = 1.01 - _InnerBorderBlendOffset;
                tmp0.y = tmp0.y / tmp0.z;
                tmp0.z = tmp0.x - _InnerBorderBlendOffset;
                tmp0.y = tmp0.y * tmp0.z + -_InnerBorderBlendRange;
                tmp0.y = tmp3.w - tmp0.y;
                tmp0.y = saturate(tmp0.y / _InnerBorderBlendRange);
                tmp1.xyz = tmp3.xyz - _BorderTint.xyz;
                tmp1.xyz = tmp0.yyy * tmp1.xyz + _BorderTint.xyz;
                o.sv_target1.xyz = tmp1.xxx * tmp4.xyz;
                o.sv_target2.xyz = tmp2.xyz * float3(0.5, 0.5, 0.5) + float3(0.5, 0.5, 0.5);
                tmp0.y = _IsOpaque > 0.0;
                tmp0.z = _BorderBlendRange + 1.0;
                tmp0.w = 1.01 - _BorderBlendOffset;
                tmp0.z = tmp0.z / tmp0.w;
                tmp0.x = tmp0.x - _BorderBlendOffset;
                tmp0.x = tmp0.z * tmp0.x + -_BorderBlendRange;
                tmp0.x = tmp3.w - tmp0.x;
                tmp0.x = saturate(tmp0.x / _BorderBlendRange);
                tmp2.x = 0.0;
                tmp2.y = inp.texcoord2.y;
                tmp0.xy = tmp0.yy ? tmp2.xy : tmp0.xx;
                tmp1.w = tmp0.y;
                tmp1 = float4(tmp1.x, tmp1.y, 0.0, 1.0);
                o.sv_target = tmp1;
                o.sv_target1.w = tmp1.w;
                o.sv_target2.w = tmp0.x;
                o.sv_target3.xyz = float3(1.0, 1.0, 1.0);
                o.sv_target3.w = tmp1.w;
                return o;
			}
			ENDCG
		}
	}
	Fallback "Hidden/UWE/Capped"
}