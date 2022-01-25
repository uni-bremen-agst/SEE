Shader "o3n/Skin Standard FFS Variant" {
    Properties {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB) Alpha (A)", 2D) = "white" {}
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
		_MetallicGlossMap("Metallic", 2D) = "white" {}
		_BumpMap("Normal Map", 2D) = "bump" {}
        _MetallicPow ("Metallic Power", Range(0, 1)) = 0.1
		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}
		_MaskTex ("Mask Texture (R - Skin)", 2D) = "white" {}
        _BRDFTex ("Brdf Map", 2D) = "gray" {}
        _GlossPow ("Smoothness", Range(0, 1)) = 1.0
        _AmbientContribution ("Ambience", Range(0, 1)) = 1
    }
    SubShader {
        Tags
		{
			"Queue" = "AlphaTest"
			"RenderType" = "TransparentCutout"
		}
        LOD 300

		Cull Back
        
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0

			// -------------------------------------


			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP

			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog


			#pragma vertex vertAdd
			#pragma fragment fragAdd
			#include "UnityStandardCoreForward.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 3.0

			// -------------------------------------


			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature _PARALLAXMAP
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Deferred pass
		Pass
		{
			Name "DEFERRED"
			Tags { "LightMode" = "Deferred" }

			CGPROGRAM
			#pragma target 3.0
			#pragma exclude_renderers nomrt


			// -------------------------------------

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP

			#pragma multi_compile ___ UNITY_HDR_ON
			#pragma multi_compile ___ LIGHTMAP_ON
			#pragma multi_compile ___ DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
			#pragma multi_compile ___ DYNAMICLIGHTMAP_ON

			#pragma vertex vertDeferred
			#pragma fragment fragDeferred

			#include "UnityStandardCore.cginc"

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature ___ _DETAIL_MULX2

			#include "UnityStandardMeta.cginc"
			ENDCG
		}
        CGPROGRAM
        #pragma surface surf StandardSkin fullforwardshadows vertex:vert
        #pragma target 3.0
		#include "UnityCG.cginc"
		#include "UnityStandardUtils.cginc"
		#include "UnityPBSLighting.cginc"

		struct SurfaceOutputStandardSkin {
		    fixed3 Albedo;		// base (diffuse or specular) color
            fixed3 Normal;		// tangent space normal, if written
            half3 Emission;
            half Metallic;		// 0=non-metal, 1=metal
            // Smoothness is the user facing name, it should be perceptual smoothness but user should not have to deal with it.
            // Everywhere in the code you meet smoothness it is perceptual smoothness
            half Smoothness;	// 0=rough, 1=smooth
            half Occlusion;		// occlusion (default 1)
            fixed Alpha;		// alpha for transparencies
			fixed Skin;
		};

        struct appdata {
            float4 vertex : POSITION;
            float4 tangent : TANGENT;
            float3 normal : NORMAL;
            float2 texcoord : TEXCOORD0;
            float2 texcoord1 : TEXCOORD1;
            float2 texcoord2 : TEXCOORD2;  
        };
            
        struct Input {
            float2 uv_MainTex;
            float3 viewDir;
            float3 coords0;
            float3 coords1;
        };
            
        sampler2D _MainTex;
        sampler2D _BumpMap;
		sampler2D _MetallicGlossMap;
        sampler2D _OcclusionMap;
		sampler2D _MaskTex;

        float _MetallicPow;
        float _GlossPow;
        float _AmbientContribution;
		float _Cutoff;
		float4 _Color;
            
        sampler2D _BRDFTex;
        
        void vert (inout appdata v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);	
            TANGENT_SPACE_ROTATION;
            o.coords0 = mul(rotation, UNITY_MATRIX_IT_MV[0].xyz);
            o.coords1 = mul(rotation, UNITY_MATRIX_IT_MV[1].xyz);
        }
        
        half3 BRDF3_Direct_Skin(half3 diffColor, half3 specColor, half rlPow4, half smoothness)
        {
            half LUT_RANGE = 16.0; // must match range in NHxRoughness() function in GeneratedTextures.cpp
            // Lookup texture to save instructions
            half specular = tex2D(unity_NHxRoughness, half2(rlPow4, SmoothnessToPerceptualRoughness(smoothness))).UNITY_ATTEN_CHANNEL * LUT_RANGE;
        #if defined(_SPECULARHIGHLIGHTS_OFF)
            specular = 0.0;
        #endif
            return diffColor *.3 + specular * specColor;
        }
        
        half3 BRDF3_Indirect_Skin (half3 diffColor, half3 specColor, UnityIndirect indirect, half grazingTerm, half3 occl, half3 brdf, half nv)
        {
            half3 c = (indirect.diffuse + occl * brdf ) * diffColor;
            c += indirect.specular * FresnelLerp (specColor, grazingTerm, nv) * _AmbientContribution;
            return c;
        }

		half4 Skin_BRDF_PBS (half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness, half3 normal, half3 viewDir, UnityLight light, UnityIndirect gi, float occlusion)
		{
			half3 normalizedLightDir = normalize(light.dir);
            float dotNL = dot(normal, normalizedLightDir);
            float2 brdfUV = float2(dotNL * 0.5 + 0.5, 0.7 * dot(light.color, fixed3(0.2126, 0.7152, 0.0722)));
            half3 brdf = tex2D( _BRDFTex, brdfUV ).rgb;

            half nv = saturate(dot(normal, viewDir));
            half grazingTerm = saturate(smoothness + (1-oneMinusReflectivity));
            
            half3 reflDir = reflect (viewDir, normal);
            half2 rlPow4AndFresnelTerm = Pow4 (half2(dot(reflDir, light.dir), 1-nv));  // use R.L instead of N.H to save couple of instructions
            half rlPow4 = rlPow4AndFresnelTerm.x;
            
            half nl = saturate(dot(normal, light.dir));
            half3 color = BRDF3_Direct_Skin(diffColor, specColor, rlPow4, smoothness);
            color *= light.color * nl;
            float3 occl = light.color.rgb * occlusion;
            color += BRDF3_Indirect_Skin (diffColor, specColor, gi, grazingTerm, occl, brdf, nv);
            return half4(color, 1);
		}

		inline half4 LightingStandardSkin (SurfaceOutputStandardSkin s, half3 viewDir, UnityGI gi)
		{
			s.Normal = normalize(s.Normal);

			half oneMinusReflectivity;
            half3 specColor;
            s.Albedo = DiffuseAndSpecularFromMetallic (s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);
			// shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
            // this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
            half outputAlpha;
            s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);
				
			half4 color = half4(0.0, 0.0, 0.0, 1.0);
			if (s.Skin > 0.5) {
				color = Skin_BRDF_PBS(s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect, s.Occlusion);
			} else {
				color = BRDF3_Unity_PBS (s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
			}
            color.rgb += UNITY_BRDF_GI (s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, s.Occlusion, gi);
			color.a = outputAlpha;			
			return color;
		}

		inline void LightingStandardSkin_GI (SurfaceOutputStandardSkin s, UnityGIInput data, inout UnityGI gi)
		{
			gi = UnityGlobalIllumination (data, s.Occlusion, s.Smoothness, s.Normal);
		}

        void surf (Input IN, inout SurfaceOutputStandardSkin o) {
            
			half4 c = tex2D (_MainTex, IN.uv_MainTex);
            if (c.a < _Cutoff) {
				discard;
			}
			// Albedo
			o.Albedo = c.rgb * _Color;
			o.Alpha = c.a;
			
			// Normal
            float3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
			o.Normal = normal;
                                
            // Metallic / Smoothness / Occlusion
			half4 occlusion = tex2D (_OcclusionMap, IN.uv_MainTex);
			half4 metallic = tex2D (_MetallicGlossMap, IN.uv_MainTex);
            o.Metallic = metallic.rgb * _MetallicPow;
            o.Smoothness = metallic.a * _GlossPow;
            o.Occlusion = occlusion.rgb;

			// SET SKIN MASK
			half4 skinFilter = tex2D(_MaskTex, IN.uv_MainTex);
			o.Skin = skinFilter.r;
        }
        ENDCG
    }
    
    SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 150

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma target 2.0
			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION 
			#pragma shader_feature _METALLICGLOSSMAP 
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature _ _GLOSSYREFLECTIONS_OFF
			// SM2.0: NOT SUPPORTED shader_feature ___ _DETAIL_MULX2
			// SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP

			#pragma skip_variants SHADOWS_SOFT DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#pragma vertex vertBase
			#pragma fragment fragBase
			#include "UnityStandardCoreForward.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual
			
			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature ___ _DETAIL_MULX2
			// SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP
			#pragma skip_variants SHADOWS_SOFT
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#pragma vertex vertAdd
			#pragma fragment fragAdd
			#include "UnityStandardCoreForward.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma skip_variants SHADOWS_SOFT
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature ___ _DETAIL_MULX2

			#include "UnityStandardMeta.cginc"
			ENDCG
		}
	}
    
    FallBack "VertexLit"
}
