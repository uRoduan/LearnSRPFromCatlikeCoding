Shader "Custom RP/Unlit" {
	
	Properties 
	{
		[HDR]  _BaseMap("Texture", 2D) = "white" {}
		_BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		[Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)]_DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
		_Cutoff ("Alpha Cutoff", range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
	}
	
	CustomEditor "CustomShaderGUI"
	
	SubShader {
		HLSLINCLUDE
		#include "../ShaderLibrary/Common.hlsl"
		#include  "UnlitInput.hlsl"
		ENDHLSL
		
		Pass 
		{
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			
			HLSLPROGRAM
			#pragma shader_feature _CLIPPING
			#pragma multi_compile_instancing
			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment
			#include "UnlitPass.hlsl"
			ENDHLSL
		}
		
		Pass
		{
			Tags {
				"LightMode" = "ShadowCaster"
			}
			
			ColorMask 0
			
			HLSLPROGRAM
			#pragma enable_d3d11_debug_symbols
			#pragma target 3.5
			#pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
			#pragma multi_complie_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "ShadowCasterPass.hlsl"
			ENDHLSL
		}
	}
}