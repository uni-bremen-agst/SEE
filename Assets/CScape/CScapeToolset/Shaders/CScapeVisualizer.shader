// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CScapeVisualizer"
{
	Properties
	{
		_Mat1Index("Mat1Index", Range( 0 , 10)) = 0
		_Mat2Index("Mat2Index", Range( 0 , 10)) = 0
		_WindowBorder("WindowBorder", Color) = (0.9852941,0.9852941,0.9852941,0)
		_Scale("Scale", Float) = 0
		_Float0("Float 0", Range( 0 , 10)) = 1.2
		_Blurring("Blurring", Float) = 0
		_ShadowOffset("ShadowOffset", Float) = 0
		_MipBias("MipBias", Float) = -5
		_RefPlane("RefPlane", Float) = 0
		_ShapeTex("ShapeTex", 2D) = "white" {}
		_Surface("Surface", 2DArray ) = "" {}
		_EnterierMap("EnterierMap", 2DArray ) = "" {}
		_TextureSample1("Texture Sample 1", 2D) = "white" {}
		_SurfaceNormalArray("SurfaceNormalArray", 2DArray ) = "" {}
		_Texture1("Texture 1", 2D) = "white" {}
		_Pos1("Pos1", Range( 0 , 1)) = 0
		_pos2("pos2", Range( 0 , 1)) = 0
		_Color0("Color 0", Color) = (0,0,0,0)
		_Color1("Color 1", Color) = (0,0,0,0)
		_Color2("Color 2", Color) = (0,0,0,0)
		_Smooth1("Smooth1", Range( 0 , 0.1)) = 0
		_Smooth2("Smooth2", Range( 0 , 0.1)) = 0
		_MaxSamplesBias("MaxSamplesBias", Float) = 0
		_MinSamplesBias("MinSamplesBias", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[Header(Parallax Occlusion Mapping)]
		_CurvFix("Curvature Bias", Range( 0 , 1)) = 1
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 4.6
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float2 uv_texcoord;
			float3 worldNormal;
			INTERNAL_DATA
			float3 worldPos;
			float3 viewDir;
		};

		uniform sampler2D _ShapeTex;
		uniform float4 _ShapeTex_ST;
		uniform float _MinSamplesBias;
		uniform float _MaxSamplesBias;
		uniform float _Scale;
		uniform float _RefPlane;
		uniform float _CurvFix;
		uniform float _MipBias;
		uniform float4 _Color1;
		uniform float4 _Color2;
		uniform float4 _Color0;
		uniform float _Pos1;
		uniform float _Smooth1;
		uniform float _pos2;
		uniform float _Smooth2;
		uniform UNITY_DECLARE_TEX2DARRAY( _SurfaceNormalArray );
		uniform float _Mat1Index;
		uniform float _Mat2Index;
		uniform UNITY_DECLARE_TEX2DARRAY( _Surface );
		uniform float4 _WindowBorder;
		uniform float _ShadowOffset;
		uniform float _Blurring;
		uniform sampler2D _Texture1;
		uniform UNITY_DECLARE_TEX2DARRAY( _EnterierMap );
		uniform sampler2D _TextureSample1;
		uniform float4 _TextureSample1_ST;
		uniform float _Float0;


		inline float2 POM( sampler2D heightMap, float2 uvs, float2 dx, float2 dy, float3 normalWorld, float3 viewWorld, float3 viewDirTan, int minSamples, int maxSamples, float parallax, float refPlane, float2 tilling, float2 curv, int index )
		{
			float3 result = 0;
			int stepIndex = 0;
			int numSteps = ( int )lerp( (float)maxSamples, (float)minSamples, saturate( dot( normalWorld, viewWorld ) ) );
			float layerHeight = 1.0 / numSteps;
			float2 plane = parallax * ( viewDirTan.xy / viewDirTan.z );
			uvs += refPlane * plane;
			float2 deltaTex = -plane * layerHeight;
			float2 prevTexOffset = 0;
			float prevRayZ = 1.0f;
			float prevHeight = 0.0f;
			float2 currTexOffset = deltaTex;
			float currRayZ = 1.0f - layerHeight;
			float currHeight = 0.0f;
			float intersection = 0;
			float2 finalTexOffset = 0;
			while ( stepIndex < numSteps + 1 )
			{
				result.z = dot( curv, currTexOffset * currTexOffset );
				currHeight = tex2Dgrad( heightMap, uvs + currTexOffset, dx, dy ).a * ( 1 - result.z );
				if ( currHeight > currRayZ )
				{
					stepIndex = numSteps + 1;
				}
				else
				{
					stepIndex++;
					prevTexOffset = currTexOffset;
					prevRayZ = currRayZ;
					prevHeight = currHeight;
					currTexOffset += deltaTex;
					currRayZ -= layerHeight * ( 1 - result.z ) * (1+_CurvFix);
				}
			}
			int sectionSteps = 4;
			int sectionIndex = 0;
			float newZ = 0;
			float newHeight = 0;
			while ( sectionIndex < sectionSteps )
			{
				intersection = ( prevHeight - prevRayZ ) / ( prevHeight - currHeight + currRayZ - prevRayZ );
				finalTexOffset = prevTexOffset + intersection * deltaTex;
				newZ = prevRayZ - intersection * layerHeight;
				newHeight = tex2Dgrad( heightMap, uvs + finalTexOffset, dx, dy ).a;
				if ( newHeight > newZ )
				{
					currTexOffset = finalTexOffset;
					currHeight = newHeight;
					currRayZ = newZ;
					deltaTex = intersection * deltaTex;
					layerHeight = intersection * layerHeight;
				}
				else
				{
					prevTexOffset = finalTexOffset;
					prevHeight = newHeight;
					prevRayZ = newZ;
					deltaTex = ( 1 - intersection ) * deltaTex;
					layerHeight = ( 1 - intersection ) * layerHeight;
				}
				sectionIndex++;
			}
			#ifdef UNITY_PASS_SHADOWCASTER
			if ( unity_LightShadowBias.z == 0.0 )
			{
			#endif
				if ( result.z > 1 )
					clip( -1 );
			#ifdef UNITY_PASS_SHADOWCASTER
			}
			#endif
			return uvs + finalTexOffset;
		}


		inline float3 expr534( float2 X )
		{
			return float3(X * 2 - 1, -1);
		}


		inline float3 expr436( float3 A , float3 B )
		{
			return abs(B) - A * B;
		}


		inline float expr337( float3 C )
		{
			return min(min(C.x, C.y), C.z);
		}


		inline float expr240( float3 pos )
		{
			return pos.z * 0.5 + 0.5;
		}


		inline float Expr142( float depthScale , float interp1 )
		{
			return saturate(interp1) / depthScale + 1;
		}


		inline float expr643( float depthScale , float realZ )
		{
			return (1.0 - (1.0 / realZ)) * (depthScale +1.0);
		}


		inline float2 expr746( float3 pos , float interp2 , float farFrac )
		{
			return pos.xy * lerp(1.0, farFrac, interp2);
		}


		inline float2 expr849( float2 interiorUV )
		{
			return interiorUV * -0.5 - 0.5;
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_ShapeTex = i.uv_texcoord * _ShapeTex_ST.xy + _ShapeTex_ST.zw;
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = Unity_SafeNormalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_worldTangent = WorldNormalVector( i, float3( 1, 0, 0 ) );
			float3 ase_worldBitangent = WorldNormalVector( i, float3( 0, 1, 0 ) );
			float3x3 ase_worldToTangent = float3x3( ase_worldTangent, ase_worldBitangent, ase_worldNormal );
			float3 ase_tanViewDir = mul( ase_worldToTangent, ase_worldViewDir );
			float2 OffsetPOM5 = POM( _ShapeTex, uv_ShapeTex, ddx(uv_ShapeTex), ddy(uv_ShapeTex), ase_worldNormal, ase_worldViewDir, ase_tanViewDir, _MinSamplesBias, _MinSamplesBias, ( ( _MinSamplesBias * 0.0 * _MaxSamplesBias ) + _Scale ), _RefPlane, _ShapeTex_ST.xy, float2(0,0), 0 );
			float2 POMuv138 = OffsetPOM5;
			float4 tex2DNode2 = tex2Dbias( _ShapeTex, float4( POMuv138, 0, _MipBias) );
			float4 appendResult3 = (float4(1.0 , tex2DNode2.g , 0.0 , tex2DNode2.r));
			float temp_output_16_0_g12 = _Pos1;
			float smoothstepResult7_g12 = smoothstep( temp_output_16_0_g12 , ( temp_output_16_0_g12 + _Smooth1 ) , tex2DNode2.b);
			float4 lerpResult11_g12 = lerp( _Color2 , _Color0 , smoothstepResult7_g12);
			float temp_output_19_0_g12 = _pos2;
			float smoothstepResult8_g12 = smoothstep( temp_output_19_0_g12 , ( temp_output_19_0_g12 + _Smooth2 ) , tex2DNode2.b);
			float4 lerpResult9_g12 = lerp( _Color1 , lerpResult11_g12 , smoothstepResult8_g12);
			float2 temp_output_25_0 = ( POMuv138 * float2( 7,7 ) );
			float4 texArray116 = UNITY_SAMPLE_TEX2DARRAY(_SurfaceNormalArray, float3(temp_output_25_0, ( _Mat1Index + 21.0 ))  );
			float4 texArray123 = UNITY_SAMPLE_TEX2DARRAY(_SurfaceNormalArray, float3(temp_output_25_0, ( _Mat2Index + 21.0 ))  );
			float4 layeredBlendVar124 = lerpResult9_g12;
			float4 layeredBlend124 = ( lerp( lerp( lerp( lerp( float4( 1,0.5,1,1 ) , texArray116 , layeredBlendVar124.x ) , texArray123 , layeredBlendVar124.y ) , float4( 1,0.5,1,0.5 ) , layeredBlendVar124.z ) , float4( 1,1,1,0 ) , layeredBlendVar124.w ) );
			float4 break188 = layeredBlend124;
			float4 appendResult189 = (float4(1.0 , break188.g , 1.0 , break188.r));
			float smoothstepResult154 = smoothstep( 0.01 , 0.0 , tex2DNode2.b);
			float GlassMask147 = smoothstepResult154;
			float3 lerpResult120 = lerp( UnpackScaleNormal( appendResult189, 0.2 ) , float3( 0,0,1 ) , GlassMask147);
			float3 temp_output_128_0 = BlendNormals( UnpackNormal( appendResult3 ) , lerpResult120 );
			o.Normal = temp_output_128_0;
			float4 texArray21 = UNITY_SAMPLE_TEX2DARRAY(_Surface, float3(temp_output_25_0, _Mat1Index)  );
			float4 texArray23 = UNITY_SAMPLE_TEX2DARRAY(_Surface, float3(temp_output_25_0, _Mat2Index)  );
			float4 layeredBlendVar20 = lerpResult9_g12;
			float4 layeredBlend20 = ( lerp( lerp( lerp( lerp( float4( 0,0,0,0 ) , texArray21 , layeredBlendVar20.x ) , texArray23 , layeredBlendVar20.y ) , _WindowBorder , layeredBlendVar20.z ) , float4( 0,0,0,0 ) , layeredBlendVar20.w ) );
			float4 lerpResult26 = lerp( ( layeredBlend20 * float4( 1,1,1,0 ) ) , float4( 0.3073097,0.3623703,0.4264706,0 ) , GlassMask147);
			float smoothstepResult100 = smoothstep( 0.005 , 0.1 , tex2DNode2.b);
			float temp_output_93_0 = ( 1.0 - tex2DNode2.b );
			float2 appendResult107 = (float2(0.0 , temp_output_93_0));
			float blendOpSrc96 = tex2Dlod( _ShapeTex, float4( ( POMuv138 + ( appendResult107 * _ShadowOffset ) ), 0, _Blurring) ).a;
			float blendOpDest96 = ( 1.0 - tex2DNode2.a );
			float Occlusion156 = ( ( 1.0 - saturate( pow( ( ( abs( ( tex2DNode2.r - 0.5 ) ) + abs( ( tex2DNode2.g - 0.5 ) ) ) * temp_output_93_0 ) , 0.5 ) ) ) * ( 1.0 - pow( ( saturate( ( blendOpSrc96 + blendOpDest96 - 1.0 ) )) , 0.3 ) ) );
			float4 temp_cast_0 = (tex2D( _Texture1, ( POMuv138 * float2( 8,8 ) ) ).r).xxxx;
			float4 lerpResult144 = lerp( ( lerpResult26 * smoothstepResult100 * Occlusion156 ) , temp_cast_0 , GlassMask147);
			float4 temp_output_127_0 = lerpResult144;
			o.Albedo = temp_output_127_0.rgb;
			float2 X34 = frac( ( uv_ShapeTex * float2( -2,-2 ) ) );
			float3 localexpr534 = expr534( X34 );
			float3 A36 = localexpr534;
			float3 B36 = ( float3( 1,1,1 ) / i.viewDir );
			float3 localexpr436 = expr436( A36 , B36 );
			float3 C37 = localexpr436;
			float localexpr337 = expr337( C37 );
			float3 temp_output_39_0 = ( localexpr534 + ( localexpr337 * i.viewDir ) );
			float3 pos46 = temp_output_39_0;
			float depthScale43 = 1.0;
			float depthScale42 = 1.0;
			float3 pos40 = temp_output_39_0;
			float localexpr240 = expr240( pos40 );
			float interp142 = localexpr240;
			float localExpr142 = Expr142( depthScale42 , interp142 );
			float realZ43 = localExpr142;
			float localexpr643 = expr643( depthScale43 , realZ43 );
			float interp246 = localexpr643;
			float farFrac46 = 0.5;
			float2 localexpr746 = expr746( pos46 , interp246 , farFrac46 );
			float2 interiorUV49 = localexpr746;
			float2 localexpr849 = expr849( interiorUV49 );
			float2 uv_TextureSample1 = i.uv_texcoord * _TextureSample1_ST.xy + _TextureSample1_ST.zw;
			float4 tex2DNode61 = tex2D( _TextureSample1, uv_TextureSample1 );
			float NoiseTex62 = tex2DNode61.r;
			float4 texArray50 = UNITY_SAMPLE_TEX2DARRAY_LOD(_EnterierMap, float3(localexpr849, ( NoiseTex62 * 6.0 )), 0 );
			float4 Enterier51 = texArray50;
			float4 lerpResult58 = lerp( float4( 0,0,0,0 ) , Enterier51 , GlassMask147);
			float Variation63 = tex2DNode61.g;
			float4 temp_output_129_0 = ( lerpResult58 * Variation63 * _Float0 );
			o.Emission = temp_output_129_0.xyz;
			float temp_output_130_0 = ( break188.a - ( 1.0 - Occlusion156 ) );
			o.Metallic = temp_output_130_0;
			o.Smoothness = layeredBlend20.a;
			float temp_output_157_0 = Occlusion156;
			o.Occlusion = temp_output_157_0;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma exclude_renderers d3d9 gles 
		#pragma surface surf Standard keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.6
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.viewDir = IN.tSpace0.xyz * worldViewDir.x + IN.tSpace1.xyz * worldViewDir.y + IN.tSpace2.xyz * worldViewDir.z;
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15800
111;419;2480;850;1761.123;1462.631;1.748372;True;True
Node;AmplifyShaderEditor.RangedFloatNode;194;-2098.498,619.5837;Float;False;Property;_MinSamplesBias;MinSamplesBias;27;0;Create;True;0;0;False;0;0;90.19;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;195;-2092.048,683.991;Float;False;Property;_MaxSamplesBias;MaxSamplesBias;26;0;Create;True;0;0;False;0;0;9.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;200;-1679.635,927.4346;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;6;-2609.206,-185.0442;Float;True;Property;_ShapeTex;ShapeTex;13;0;Create;True;0;0;False;0;None;47c78fe4e0481964ea3fe9715b051558;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-2174.146,320.8826;Float;False;Property;_Scale;Scale;3;0;Create;True;0;0;False;0;0;0.14;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;11;-1969.069,-523.5959;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;201;-1973.409,273.3181;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;206;-2029.735,492.9985;Float;False;Property;_RefPlane;RefPlane;12;0;Create;True;0;0;False;0;0;0.31;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;132;-1953.676,-43.08847;Float;False;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.CommentaryNode;28;-2454.822,-4120.985;Float;False;6082.19;1413.166;Comment;28;56;55;54;53;52;50;49;48;47;46;45;44;43;42;41;40;39;38;37;36;35;34;33;32;31;30;29;51;;1,1,1,1;0;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;7;-2299.107,422.8035;Float;False;Tangent;True;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ParallaxOcclusionMappingNode;5;-1802.307,252.4408;Float;False;3;10;True;194;52;True;195;4;0.02;0.49;False;1,1;True;0,0;Texture2D;7;0;FLOAT2;0,0;False;1;SAMPLER2D;;False;2;FLOAT;0.02;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT2;0,0;False;6;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;29;-2006.169,-3527.779;Float;False;0;6;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;138;-1330.613,293.4731;Float;False;POMuv;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;30;-1994.868,-3230.157;Float;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;31;-1699.507,-3501.93;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;-2,-2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;141;-1314.669,-227.661;Float;False;138;POMuv;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode;33;-1737.467,-3217.456;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FractNode;32;-1488.386,-3491.557;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;190;-1223.318,-69.13556;Float;False;Property;_MipBias;MipBias;8;0;Create;True;0;0;False;0;-5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;34;-1236.771,-3566.956;Float;False;float3(X * 2 - 1, -1);3;False;1;True;X;FLOAT2;0,0;In;;Float;expr5;True;False;0;1;0;FLOAT2;0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;35;-1483.573,-3092.764;Float;False;2;0;FLOAT3;1,1,1;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;2;-1042.923,-159.7489;Float;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;False;0;None;47c78fe4e0481964ea3fe9715b051558;True;0;False;white;Auto;False;Object;-1;MipBias;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RelayNode;126;-489.0309,-91.97888;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;36;-1230.574,-3125.956;Float;False;abs(B) - A * B;3;False;2;True;A;FLOAT3;0,0,0;In;;Float;True;B;FLOAT3;0,0,0;In;;Float;expr4;True;False;0;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;93;-255.8197,599.58;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;37;-918.2714,-3139.858;Float;False;min(min(C.x, C.y), C.z);1;False;1;True;C;FLOAT3;0,0,0;In;;Float;expr3;True;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;84;-521.7095,609.6806;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;83;-523.1858,497.3757;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;106;-1631.428,900.6926;Float;False;Property;_ShadowOffset;ShadowOffset;7;0;Create;True;0;0;False;0;0;0.03;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;107;-1663.523,677.3142;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;-678.6764,-3429.456;Float;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;39;-506.3744,-3451.857;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.AbsOpNode;86;-346.6856,614.6371;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-1372.348,881.3477;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.AbsOpNode;85;-360.0459,512.7529;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;140;-1434.3,735.688;Float;False;138;POMuv;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;94;-1168.132,882.7029;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0.03;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;80;-191.3666,528.8115;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;40;-231.7637,-3319.142;Float;False;pos.z * 0.5 + 0.5;1;False;1;True;pos;FLOAT3;0,0,0;In;;Float;expr2;True;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;82;-1249.664,1024.506;Float;False;Property;_Blurring;Blurring;6;0;Create;True;0;0;False;0;0;3.93;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;41;-104.3586,-3107.743;Float;False;Constant;_Float4;Float 4;2;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;61;1109.476,-1767.775;Float;True;Property;_TextureSample1;Texture Sample 1;16;0;Create;True;0;0;False;0;None;be53a91e4e185d9448d300ead53b5f1f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;22;-962.9245,-813.9846;Float;False;Property;_Mat1Index;Mat1Index;0;0;Create;True;0;0;False;0;0;0.84;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;42;48.53839,-3344.643;Float;False;saturate(interp1) / depthScale + 1;1;False;2;True;depthScale;FLOAT;0;In;;Float;True;interp1;FLOAT;0;In;;Float;Expr1;True;False;0;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;24;-991.2449,-587.4957;Float;False;Property;_Mat2Index;Mat2Index;1;0;Create;True;0;0;False;0;0;4.15;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;97;-566.6407,847.1938;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;88;-152.7834,208.6204;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;6;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;95;-940.8721,912.6491;Float;True;Property;_TextureSample3;Texture Sample 3;0;0;Create;True;0;0;False;0;None;47c78fe4e0481964ea3fe9715b051558;True;0;False;white;Auto;False;Instance;2;MipLevel;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;162;355.1351,-1310.228;Float;False;Property;_Color2;Color 2;23;0;Create;True;0;0;False;0;0,0,0,0;1,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;160;306.3982,-1836.628;Float;False;Property;_Pos1;Pos1;19;0;Create;True;0;0;False;0;0;0.562;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;165;304.222,-1659.747;Float;False;Property;_pos2;pos2;20;0;Create;True;0;0;False;0;0;0.437;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;161;138.7117,-1337.253;Float;False;Property;_Color1;Color 1;22;0;Create;True;0;0;False;0;0,0,0,0;0,0,1,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;163;310.245,-1569.479;Float;False;Property;_Smooth2;Smooth2;25;0;Create;True;0;0;False;0;0;0.0263;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;159;355.7688,-1485.199;Float;False;Property;_Color0;Color 0;21;0;Create;True;0;0;False;0;0,0,0,0;0,1,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;164;306.0778,-1754.823;Float;False;Property;_Smooth1;Smooth1;24;0;Create;True;0;0;False;0;0;0.0016;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;207;940.1113,-1051.764;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;21;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;208;1034.524,-880.4233;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;21;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;62;1627.233,-1717.097;Float;False;NoiseTex;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;43;885.8407,-3217.141;Float;False;(1.0 - (1.0 / realZ)) * (depthScale +1.0);1;False;2;True;depthScale;FLOAT;0;In;;Float;True;realZ;FLOAT;0;In;;Float;expr6;True;False;0;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;96;-341.3443,826.3843;Float;False;LinearBurn;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;92;49.89481,213.8936;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;166;847.0983,-1481.534;Float;False;3ColorGradient;-1;;12;af7134d49ad8c844294e466f39ac2e86;0;8;15;FLOAT;0;False;23;COLOR;0,0,0,0;False;21;COLOR;0,0,0,0;False;22;COLOR;0,0,0,0;False;16;FLOAT;0;False;19;FLOAT;0;False;17;FLOAT;0;False;20;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-783.5156,-380.1265;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;7,7;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;44;1096.733,-2941.843;Float;False;Constant;_Float5;Float 5;3;0;Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;158;497.8739,-867.9783;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TextureArrayNode;116;1418.21,-1139.872;Float;True;Property;_SurfaceNormalArray;SurfaceNormalArray;17;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureArrayNode;123;1370.625,-900.1407;Float;True;Property;_TextureArray1;Texture Array 1;19;0;Create;True;0;0;False;0;None;0;Instance;116;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CustomExpressionNode;46;1491.436,-3130.743;Float;False;pos.xy * lerp(1.0, farFrac, interp2);2;False;3;True;pos;FLOAT3;0,0,0;In;;Float;True;interp2;FLOAT;0;In;;Float;True;farFrac;FLOAT;0;In;;Float;expr7;True;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PowerNode;99;-40.20959,835.5598;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.3;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;45;1954.634,-3482.042;Float;False;62;NoiseTex;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;90;243.0958,196.8031;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;48;2444.936,-3441.439;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;6;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;21;-510.2889,-1273.936;Float;True;Property;_Surface;Surface;14;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureArrayNode;23;-478.9412,-1038.954;Float;True;Property;_TextureArray0;Texture Array 0;3;0;Create;True;0;0;False;0;None;0;Instance;21;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SmoothstepOpNode;154;-137.3085,41.87126;Float;False;3;0;FLOAT;0;False;1;FLOAT;0.01;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;91;700.5237,-482.8893;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LayeredBlendNode;124;2146.048,-672.0236;Float;False;6;0;COLOR;0,0,0,0;False;1;COLOR;1,0.5,1,1;False;2;COLOR;0.5,0.5,1,1;False;3;COLOR;0,0,0,0;False;4;COLOR;1,0.5,1,0.5;False;5;COLOR;1,1,1,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;98;179.8056,823.5486;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;4;-368.2405,-814.8309;Float;False;Property;_WindowBorder;WindowBorder;2;0;Create;True;0;0;False;0;0.9852941,0.9852941,0.9852941,0;0.985294,0.07244808,0.07244808,0.197;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CustomExpressionNode;49;2017.171,-3088.057;Float;False;interiorUV * -0.5 - 0.5;2;False;1;True;interiorUV;FLOAT2;0,0;In;;Float;expr8;True;False;0;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;47;2562.531,-3252.873;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LayeredBlendNode;20;-94.20151,-1240.591;Float;False;6;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.BreakToComponentsNode;188;2368.566,-552.5143;Float;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RegisterLocalVarNode;147;120.7066,76.05547;Float;False;GlassMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;50;2915.635,-3530.575;Float;True;Property;_EnterierMap;EnterierMap;15;0;Create;True;0;0;False;0;None;0;Object;-1;MipLevel;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;155;808.8601,-161.7908;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;156;1014.074,-181.4698;Float;False;Occlusion;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;189;2653.522,-543.3956;Float;False;FLOAT4;4;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;67;379.1805,-564.6974;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;1,1,1,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;145;2338.879,7.021453;Float;False;138;POMuv;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;51;3395.914,-3405.318;Float;False;Enterier;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;153;299.754,-460.953;Float;False;147;GlassMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;146;2584.18,7.02181;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;8,8;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;63;1616.379,-1596.401;Float;False;Variation;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;157;3029.816,966.4309;Float;False;156;Occlusion;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;148;1880.144,-56.846;Float;False;147;GlassMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;26;590.5706,-575.9547;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0.3073097,0.3623703,0.4264706,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.UnpackScaleNormalNode;118;2320.666,-272.7055;Float;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0.2;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TexturePropertyNode;142;2601.664,-213.2125;Float;True;Property;_Texture1;Texture 1;18;0;Create;True;0;0;False;0;None;351a0be17d79558409a4ed61472aa426;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SmoothstepOpNode;100;733.4094,-369.4919;Float;False;3;0;FLOAT;0;False;1;FLOAT;0.005;False;2;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;57;722.8144,17.33135;Float;False;51;Enterier;1;0;OBJECT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;150;716.1785,94.27574;Float;False;147;GlassMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;3;-661.9142,250.295;Float;False;FLOAT4;4;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;58;957.6638,60.48274;Float;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0.3970588,0.3970588,0.3970588,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;76;1104.685,-559.4252;Float;False;3;3;0;COLOR;1,0,0,0;False;1;FLOAT;0.5;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;143;2767.028,81.38976;Float;True;Property;_TextureSample4;Texture Sample 4;15;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.UnpackScaleNormalNode;1;7.565404,-104.6572;Float;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;65;682.1287,211.4594;Float;False;63;Variation;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;66;749.5804,300.4803;Float;False;Property;_Float0;Float 0;5;0;Create;True;0;0;False;0;1.2;10;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;192;2791.122,971.709;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;120;2121.717,-100.023;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,1;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;149;2541.881,390.8065;Float;False;147;GlassMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;181;189.4933,-965.4015;Float;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.LerpOp;144;2887.373,355.1479;Float;False;3;0;COLOR;1,1,1,0;False;1;COLOR;1,1,1,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;64;1135.317,232.3642;Float;False;3;3;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;191;2844.101,703.4075;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendNormalsNode;115;1533.345,57.51233;Float;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FractNode;53;2472.734,-3543.176;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;54;2560.035,-2996.64;Float;False;Property;_Float6;Float 6;4;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCCompareEqual;180;3729.05,228.4395;Float;False;4;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;FLOAT4;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RelayNode;127;3079.26,528.6057;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCCompareEqual;178;3408.669,113.502;Float;False;4;0;FLOAT;0;False;1;FLOAT;1;False;2;COLOR;0,0,0,0;False;3;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ParallaxOffsetHlpNode;197;-2105.869,161.038;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCRemapNode;69;2585.042,944.5467;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0.46;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;129;3043.756,696.4732;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;9;-1558.314,-1050.231;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;52;3082.128,-3218.985;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.DdxOpNode;71;-1409.251,-108.5906;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;55;2301.733,-3544.478;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;3.00456;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;139;-1228.874,495.8593;Float;False;138;POMuv;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;151;2244.51,711.8819;Float;False;147;GlassMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;203;3524.179,1040.143;Float;False;Property;_TessFactor;TessFactor;11;0;Create;True;0;0;False;0;0;69;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;70;2574.533,720.3206;Float;False;5;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;3;FLOAT;0.5;False;4;FLOAT;0.18;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;131;3039.863,871.1954;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;187;2847.571,-541.116;Float;False;FLOAT4;4;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RelayNode;130;3040.679,788.1839;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;81;-982.7628,447.924;Float;True;Property;_TextureSample2;Texture Sample 2;0;0;Create;True;0;0;False;0;None;47c78fe4e0481964ea3fe9715b051558;True;0;False;white;Auto;False;Instance;2;Derivative;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DistanceBasedTessNode;202;3797.779,1050.543;Float;False;3;0;FLOAT;10000;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TFHCCompareEqual;179;3389.241,281.2149;Float;False;4;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT4;0,0,0,0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ParallaxMappingNode;198;-1682.036,-5.53418;Float;False;Normal;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;56;3359.377,-3590.463;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;204;3535.878,1135.043;Float;False;Property;_TessMin;TessMin;10;0;Create;True;0;0;False;0;0;5107.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;128;3055.094,613.3909;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;152;2333.345,938.052;Float;False;147;GlassMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;205;3543.678,1211.743;Float;False;Property;_TessMax;TessMax;9;0;Create;True;0;0;False;0;0;-2406.4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DdyOpNode;186;-1403.988,35.36983;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;4151.046,659.6023;Float;False;True;6;Float;ASEMaterialInspector;0;0;Standard;CScapeVisualizer;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.11;True;True;0;False;Opaque;;Geometry;All;False;True;True;False;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;200;0;194;0
WireConnection;200;2;195;0
WireConnection;11;2;6;0
WireConnection;201;0;200;0
WireConnection;201;1;8;0
WireConnection;132;0;6;0
WireConnection;5;0;11;0
WireConnection;5;1;132;0
WireConnection;5;2;201;0
WireConnection;5;3;7;0
WireConnection;5;4;206;0
WireConnection;138;0;5;0
WireConnection;31;0;29;0
WireConnection;33;0;30;0
WireConnection;32;0;31;0
WireConnection;34;0;32;0
WireConnection;35;1;33;0
WireConnection;2;0;6;0
WireConnection;2;1;141;0
WireConnection;2;2;190;0
WireConnection;126;0;2;3
WireConnection;36;0;34;0
WireConnection;36;1;35;0
WireConnection;93;0;126;0
WireConnection;37;0;36;0
WireConnection;84;0;2;2
WireConnection;83;0;2;1
WireConnection;107;1;93;0
WireConnection;38;0;37;0
WireConnection;38;1;33;0
WireConnection;39;0;34;0
WireConnection;39;1;38;0
WireConnection;86;0;84;0
WireConnection;105;0;107;0
WireConnection;105;1;106;0
WireConnection;85;0;83;0
WireConnection;94;0;140;0
WireConnection;94;1;105;0
WireConnection;80;0;85;0
WireConnection;80;1;86;0
WireConnection;40;0;39;0
WireConnection;42;0;41;0
WireConnection;42;1;40;0
WireConnection;97;0;2;4
WireConnection;88;0;80;0
WireConnection;88;1;93;0
WireConnection;95;0;6;0
WireConnection;95;1;94;0
WireConnection;95;2;82;0
WireConnection;207;0;22;0
WireConnection;208;0;24;0
WireConnection;62;0;61;1
WireConnection;43;0;41;0
WireConnection;43;1;42;0
WireConnection;96;0;95;4
WireConnection;96;1;97;0
WireConnection;92;0;88;0
WireConnection;166;15;2;3
WireConnection;166;23;159;0
WireConnection;166;21;161;0
WireConnection;166;22;162;0
WireConnection;166;16;160;0
WireConnection;166;19;165;0
WireConnection;166;17;164;0
WireConnection;166;20;163;0
WireConnection;25;0;138;0
WireConnection;158;0;166;0
WireConnection;116;0;25;0
WireConnection;116;1;207;0
WireConnection;123;0;25;0
WireConnection;123;1;208;0
WireConnection;46;0;39;0
WireConnection;46;1;43;0
WireConnection;46;2;44;0
WireConnection;99;0;96;0
WireConnection;90;0;92;0
WireConnection;48;0;45;0
WireConnection;21;0;25;0
WireConnection;21;1;22;0
WireConnection;23;0;25;0
WireConnection;23;1;24;0
WireConnection;154;0;126;0
WireConnection;91;0;90;0
WireConnection;124;0;158;0
WireConnection;124;2;116;0
WireConnection;124;3;123;0
WireConnection;98;0;99;0
WireConnection;49;0;46;0
WireConnection;20;0;158;0
WireConnection;20;2;21;0
WireConnection;20;3;23;0
WireConnection;20;4;4;0
WireConnection;188;0;124;0
WireConnection;147;0;154;0
WireConnection;50;0;49;0
WireConnection;50;1;48;0
WireConnection;50;2;47;0
WireConnection;155;0;91;0
WireConnection;155;1;98;0
WireConnection;156;0;155;0
WireConnection;189;1;188;1
WireConnection;189;3;188;0
WireConnection;67;0;20;0
WireConnection;51;0;50;0
WireConnection;146;0;145;0
WireConnection;63;0;61;2
WireConnection;26;0;67;0
WireConnection;26;2;153;0
WireConnection;118;0;189;0
WireConnection;100;0;126;0
WireConnection;3;1;2;2
WireConnection;3;3;2;1
WireConnection;58;1;57;0
WireConnection;58;2;150;0
WireConnection;76;0;26;0
WireConnection;76;1;100;0
WireConnection;76;2;156;0
WireConnection;143;0;142;0
WireConnection;143;1;146;0
WireConnection;1;0;3;0
WireConnection;192;0;157;0
WireConnection;120;0;118;0
WireConnection;120;2;148;0
WireConnection;181;0;20;0
WireConnection;144;0;76;0
WireConnection;144;1;143;1
WireConnection;144;2;149;0
WireConnection;64;0;58;0
WireConnection;64;1;65;0
WireConnection;64;2;66;0
WireConnection;191;0;188;3
WireConnection;191;1;192;0
WireConnection;115;0;1;0
WireConnection;115;1;120;0
WireConnection;53;0;55;0
WireConnection;180;2;178;0
WireConnection;180;3;179;0
WireConnection;127;0;144;0
WireConnection;178;2;127;0
WireConnection;178;3;128;0
WireConnection;69;0;152;0
WireConnection;129;0;64;0
WireConnection;52;0;47;0
WireConnection;71;0;5;0
WireConnection;55;0;45;0
WireConnection;70;0;151;0
WireConnection;131;0;181;3
WireConnection;130;0;191;0
WireConnection;81;0;6;0
WireConnection;81;1;139;0
WireConnection;81;3;71;0
WireConnection;81;4;186;0
WireConnection;202;0;203;0
WireConnection;202;1;204;0
WireConnection;202;2;205;0
WireConnection;179;2;129;0
WireConnection;179;3;130;0
WireConnection;56;0;50;0
WireConnection;56;1;52;0
WireConnection;128;0;115;0
WireConnection;186;0;5;0
WireConnection;0;0;127;0
WireConnection;0;1;128;0
WireConnection;0;2;129;0
WireConnection;0;3;130;0
WireConnection;0;4;131;0
WireConnection;0;5;157;0
ASEEND*/
//CHKSM=2788B57D4D7C072C53043E9C7ABFD953F4CB3F1B