Shader "o3n/Skin FFS Variant" {
    Properties {
        _MainTex ("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        _MaskTex ("Spec (R) Gloss (G) Occlusion (B)", 2D) = "white" {}
		_SkinTex ("Skin Mask", 2D) = "white" {}
        _NormalMap ("Normalmap", 2D) = "bump" {}                      
        _BRDFTex ("Brdf Map", 2D) = "gray" {}
        _BeckmannTex ("BeckmannTex", 2D) = "gray" {}                    
        _SpecPow ("Specular", Range(0, 1)) = 0.03
        _GlossPow ("Smoothness", Range(0, 1)) = 0.28           
        _AmbientContribution ("Ambience", Range(0, 1)) = 1 
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags
		{
			"Queue" = "AlphaTest"
			"RenderType" = "TransparentCutout"
		}
        LOD 300

		Cull Back
            
        CGPROGRAM
        #pragma surface surf StandardSkin fullforwardshadows

        #pragma target 3.0
		#include "UnityCG.cginc"


		struct SurfaceOutputStandardSkin {
	   		fixed3 Albedo;
	    	half Specular;
	    	fixed3 Normal;
	    	half3 Emission;
	    	half Smoothness;
	    	half Occlusion;
	    	fixed Alpha;
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
        };
            
            
        sampler2D _MainTex;
        sampler2D _MaskTex;
        sampler2D _NormalMap;
		sampler2D _SkinTex;

        float _SpecPow;
        float _GlossPow;
        float _AmbientContribution;
		float _Cutoff;
            
        sampler2D _BRDFTex;
        sampler2D _BeckmannTex;
            
        float Fresnel(float3 _half, float3 view, float f0) {
			float base = 1.0 - dot(view, _half);
			float exponential = pow(base, 5.0);
			return exponential + f0 * (1.0 - exponential);
		}

		half SpecularKSK(sampler2D beckmannTex, float3 normal, float3 light, float3 view, float roughness) {
				
			const float _specularFresnel = 1.08;
					
			half3 _half = view + light;
			half3 halfn = normalize(_half);

			half ndotl = max(dot(normal, light), 0.0);
			half ndoth = max(dot(normal, halfn), 0.0);

			half ph = pow(2.0 * tex2D(beckmannTex, float2(ndoth, roughness)).r, 10.0);
			half f = lerp(0.25, Fresnel(halfn, view, 0.028), _specularFresnel);
			half ksk = max(ph * f / dot(_half, _half), 0.0);
				
			return ndotl * ksk;   
		}

		half3 Skin_BRDF_PBS (SurfaceOutputStandardSkin s, float oneMinusReflectivity, half3 viewDir, UnityLight light, UnityIndirect gi)
		{
			half3 normalizedLightDir = normalize(light.dir);
            viewDir = normalize(viewDir);

            float3 occl = light.color.rgb * s.Occlusion;
            half specular = (s.Specular * SpecularKSK(_BeckmannTex, s.Normal, normalizedLightDir, viewDir , s.Smoothness) );

            float dotNL = dot(s.Normal, normalizedLightDir);
            float2 brdfUV = float2(dotNL * 0.5 + 0.5, 0.7 * dot(light.color, fixed3(0.2126, 0.7152, 0.0722)));
            half3 brdf = tex2D( _BRDFTex, brdfUV ).rgb;

            half nv = DotClamped (s.Normal, viewDir);
            half grazingTerm = saturate(1-s.Smoothness + (1-oneMinusReflectivity));
            
            half3 color = s.Albedo * (_AmbientContribution * gi.diffuse + occl * brdf) 
                        + specular * light.color
                        + gi.specular * FresnelLerp (specular, grazingTerm, nv) * _AmbientContribution * 0.1; // reduced this effect to 10% to get rid of bright rim effect
            // reflections
            color += BRDF3_Indirect(0, s.Specular, gi, grazingTerm, 0);
            return color;
		}

		half4 BRDF3_Unity_PBS__ (half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness, half3 normal, half3 viewDir, UnityLight light, UnityIndirect gi)
		{
			half3 reflDir = reflect (viewDir, normal);

			half nl = saturate(dot(normal, light.dir));
			half nv = saturate(dot(normal, viewDir));

			// Vectorize Pow4 to save instructions
			half2 rlPow4AndFresnelTerm = Pow4 (half2(dot(reflDir, light.dir), 1-nv));  // use R.L instead of N.H to save couple of instructions
			half rlPow4 = rlPow4AndFresnelTerm.x; // power exponent must match kHorizontalWarpExp in NHxRoughness() function in GeneratedTextures.cpp
			half fresnelTerm = rlPow4AndFresnelTerm.y;

			half grazingTerm = saturate(smoothness + (1-oneMinusReflectivity));

			half3 color = BRDF3_Direct(diffColor, specColor, rlPow4, smoothness);
			color *= light.color * nl;
			color += BRDF3_Indirect(diffColor, specColor, gi, grazingTerm, fresnelTerm);

			return half4(color, 1);
		}

		inline half4 LightingStandardSkin (SurfaceOutputStandardSkin s, half3 viewDir, UnityGI gi)
		{
			s.Normal = normalize(s.Normal);

			half oneMinusReflectivity;
			half3 specColor;
			s.Albedo = EnergyConservationBetweenDiffuseAndSpecular (s.Albedo, s.Specular, oneMinusReflectivity);

			half outputAlpha;
			s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, outputAlpha);
				
			half4 color = half4(0.0, 0.0, 0.0, 1.0);
			if (s.Skin > 0.5) {
				color.rgb = Skin_BRDF_PBS(s, oneMinusReflectivity, viewDir, gi.light, gi.indirect);
			} else {
				color.rgb = BRDF3_Unity_PBS__ (s.Albedo, s.Specular, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
			}
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
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			
			// Normal
            o.Normal = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));
                                
            // SPECULAR / GLOSS / Occlusion
			half4 maskColor = tex2D (_MaskTex, IN.uv_MainTex);
            o.Specular = maskColor.r * _SpecPow;    
            o.Smoothness = maskColor.g * _GlossPow;
            o.Occlusion = maskColor.b;

			// SET SKIN MASK
			half4 skinFilter = tex2D(_SkinTex, IN.uv_MainTex);
			o.Skin = skinFilter.r;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
