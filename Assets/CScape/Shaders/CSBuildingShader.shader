// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CScape/CSBuildingShader"
{
	Properties
	{
		_Smoothness("Smoothness", Range( 0 , 1)) = 0
		_MinSpecular("MinSpecular", Range( 0 , 1)) = 0
		_DepthScale("DepthScale", Range( 0 , 1)) = 0.12
		_Mettalic("Mettalic", Range( 0 , 1)) = 0
		_LightStrenght("LightStrenght", Range( 0 , 100)) = 0
		_LightVariation("LightVariation", Range( 0 , 1)) = 0
		_BlindsOpen("BlindsOpen", Range( 0 , 2)) = 0
		_DirtAmount("DirtAmount", Range( 1 , 5)) = 1
		_Occlusion("Occlusion", Range( 0 , 4)) = 0
		_BuildingLightness("BuildingLightness", Range( 0 , 2)) = 5
		_ScaleTex1("ScaleTex1", Float) = 0
		_NoiseTexture("NoiseTexture", 2D) = "white" {}
		_BlindsArray("BlindsArray", 2DArray ) = "" {}
		_Interior2("Interior2", 2DArray ) = "" {}
		_LightOnThershold("LightOnThershold", Float) = 0
		_GlobalLightsOn("GlobalLightsOn", Range( 0 , 1)) = 0
		_TVAO_Bias("TVAO_Bias", Float) = 0
		[Gamma]_MaskTexArray("MaskTexArray", 2DArray) = "white" {}
		_RefFrame("RefFrame", Float) = 0
		_Float23("Float 23", Range( 0 , 1)) = 0.32
		_BuildingLighting("BuildingLighting", Float) = 0
		_Dirt("Dirt", 2DArray ) = "" {}
		_IlluminationArraySize("IlluminationArraySize", Float) = 0
		_NormalWindow("NormalWindow", Range( 0 , 1)) = 2
		_Snowmoss("Snow/moss", Color) = (1,1,1,0)
		_MaxSnow("MaxSnow", Range( 0 , 1)) = 0
		_MinSnow("MinSnow", Range( 0 , 1)) = 0
		[Toggle(_USE_SNOW_MOSS_DIRT_ON)] _Use_Snow_Moss_Dirt("Use_Snow_Moss_Dirt", Float) = 0
		_AttenuateBuildingHeight("AttenuateBuildingHeight", Float) = 0
		_graffiti("graffiti", 2D) = "white" {}
		_Int0("Int 0", Int) = 0
		_SurfaceArray("SurfaceArray", 2DArray) = "white" {}
		_ParallaxSteps("ParallaxSteps", Int) = 0
		_TVAO_High_Value("TVAO_High_Value", Float) = 0
		_TVAO_LowValue("TVAO_LowValue", Float) = 0
		_ShopSigns("ShopSigns", 2DArray) = "white" {}
		_ShopSignsLight("ShopSignsLight", Float) = 0
		_DistanceSmoothness("DistanceSmoothness", Range( 0 , 10)) = 1000
		_roofPlaneTex("roofPlaneTex", Float) = 0
		_test("test", Float) = 0
		_mipCurtains("mipCurtains", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] _tex4coord4( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		LOD 500
		Cull Back
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "UnityCG.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.5
		#pragma shader_feature _CSCAPE_DESKTOP_ON
		#pragma shader_feature _USE_SNOW_MOSS_DIRT_ON
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float4 uv4_tex4coord4;
			float3 worldNormal;
			INTERNAL_DATA
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
			float3 viewDir;
			float3 worldPos;
		};

		uniform int _ParallaxSteps;
		uniform UNITY_DECLARE_TEX2DARRAY( _MaskTexArray );
		uniform int _Int0;
		uniform float _DepthScale;
		uniform float _RefFrame;
		uniform float4 _MaskTexArray_ST;
		uniform UNITY_DECLARE_TEX2DARRAY( _SurfaceArray );
		uniform float _ScaleTex1;
		uniform float _roofPlaneTex;
		uniform float _textureScales[21];
		uniform float _DistanceSmoothness;
		uniform sampler2D _NoiseTexture;
		uniform float _NormalWindow;
		uniform UNITY_DECLARE_TEX2DARRAY( _ShopSigns );
		uniform float4 _shopColors[22];
		uniform float _test;
		uniform float4 _borderArray[11];
		uniform float4 _concreteColors[11];
		uniform float4 _faccadeColors[41];
		uniform UNITY_DECLARE_TEX2DARRAY( _Dirt );
		uniform float _IlluminationArraySize;
		uniform float _DirtAmount;
		uniform float _BuildingLightness;
		uniform sampler2D _graffiti;
		uniform UNITY_DECLARE_TEX2DARRAY( _BlindsArray );
		uniform UNITY_DECLARE_TEX2DARRAY( _Interior2 );
		uniform float _mipCurtains;
		uniform float _BlindsOpen;
		uniform float4 _glassColors[10];
		uniform float _Occlusion;
		uniform float4 _Snowmoss;
		uniform float _MinSnow;
		uniform float _MaxSnow;
		uniform sampler2D _ReLightingControlTex;
		uniform float4 _ReLightingProjection;
		uniform float _TVAO_Bias;
		uniform float _TVAO_LowValue;
		uniform float _TVAO_High_Value;
		uniform float _ShopSignsLight;
		uniform float _CSReLight;
		uniform float _Float23;
		uniform float _LightStrenght;
		uniform float _LightVariation;
		uniform float _CSLights;
		uniform float _LightOnThershold;
		uniform float4 _buildingLightsColors[10];
		uniform float _BuildingLighting;
		uniform float _AttenuateBuildingHeight;
		uniform float _GlobalLightsOn;
		uniform float _LightsDistance;
		uniform float _lightsContour;
		uniform float4 _reLightColor;
		uniform float _CSReLightDistance;
		uniform float _Mettalic;
		uniform float _MinSpecular;
		uniform float _Smoothness;

		#if defined(UNITY_COMPILER_HLSL2GLSL) || defined(SHADER_TARGET_SURFACE_ANALYSIS)
			#define ASE_SAMPLE_TEX2DARRAY_GRAD(tex,coord,dx,dy) UNITY_SAMPLE_TEX2DARRAY (tex,coord)
		#else
			#define ASE_SAMPLE_TEX2DARRAY_GRAD(tex,coord,dx,dy) tex.SampleGrad (sampler##tex,coord,dx,dy)
		#endif


		inline float2 POM( UNITY_ARGS_TEX2DARRAY(heightMap), float2 uvs, float2 dx, float2 dy, float3 normalWorld, float3 viewWorld, float3 viewDirTan, int minSamples, int maxSamples, float parallax, float refPlane, float2 tilling, float2 curv, int index )
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
				currHeight = ASE_SAMPLE_TEX2DARRAY_GRAD( heightMap,  float3(uvs + currTexOffset,index), dx, dy ).a;
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
					currRayZ -= layerHeight;
				}
			}
			int sectionSteps = 3;
			int sectionIndex = 0;
			float newZ = 0;
			float newHeight = 0;
			while ( sectionIndex < sectionSteps )
			{
				intersection = ( prevHeight - prevRayZ ) / ( prevHeight - currHeight + currRayZ - prevRayZ );
				finalTexOffset = prevTexOffset + intersection * deltaTex;
				newZ = prevRayZ - intersection * layerHeight;
				newHeight = ASE_SAMPLE_TEX2DARRAY_GRAD( heightMap, float3(uvs + finalTexOffset,index), dx, dy ).a;
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
			return uvs + finalTexOffset;
		}


		float3 HSVToRGB( float3 c )
		{
			float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
			float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
			return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
		}


		inline float3 expr5681( float2 X )
		{
			return float3( X * 2 - 1, -1);
		}


		inline float3 expr4683( float3 A , float3 B )
		{
			return abs(B) - A * B;
		}


		inline float expr3685( float3 C )
		{
			return min(min(C.x, C.y), C.z);
		}


		inline float expr2691( float3 pos )
		{
			return pos.z * 0.5 + 0.5;
		}


		inline float Expr1693( float depthScale , float interp1 )
		{
			return saturate(interp1) / depthScale + 1;
		}


		inline float expr6696( float depthScale , float realZ )
		{
			return (1.0 - (1.0 / realZ)) * (depthScale +1.0);
		}


		inline float2 expr7698( float3 pos , float interp2 , float farFrac )
		{
			return pos.xy * lerp(1.0, farFrac, interp2);
		}


		inline float2 expr8700( float2 interiorUV )
		{
			return interiorUV * -0.5 - 0.5;
		}


		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float4 break1_g1 = i.uv4_tex4coord4;
			float temp_output_44_0_g1 = ( break1_g1.y * 10.0 );
			float temp_output_45_0_g1 = trunc( temp_output_44_0_g1 );
			float temp_output_47_0_g1 = ( ( temp_output_44_0_g1 - temp_output_45_0_g1 ) * 10.0 );
			float temp_output_48_0_g1 = trunc( temp_output_47_0_g1 );
			float LowFloorID876 = ( temp_output_48_0_g1 + 30.0 );
			float temp_output_9_0_g1 = ( break1_g1.x * 10.0 );
			float temp_output_10_0_g1 = floor( temp_output_9_0_g1 );
			float temp_output_12_0_g1 = ( ( temp_output_9_0_g1 - temp_output_10_0_g1 ) * 10.0 );
			float temp_output_14_0_g1 = floor( temp_output_12_0_g1 );
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_vertexNormal = mul( unity_WorldToObject, float4( ase_worldNormal, 0 ) );
			float temp_output_1449_0 = step( ase_vertexNormal.y , -0.4 );
			float lerpResult1448 = lerp( ( 1.0 + ( ( temp_output_10_0_g1 * 10.0 ) + temp_output_14_0_g1 ) ) , 29.0 , temp_output_1449_0);
			float lerpResult1641 = lerp( lerpResult1448 , 30.0 , step( ase_vertexNormal.y , -0.98 ));
			half faccade_ID746 = lerpResult1641;
			float temp_output_50_0_g1 = ( ( temp_output_47_0_g1 - temp_output_48_0_g1 ) * 10.0 );
			float temp_output_51_0_g1 = trunc( temp_output_50_0_g1 );
			float temp_output_53_0_g1 = ( ( temp_output_50_0_g1 - temp_output_51_0_g1 ) * 10.0 );
			float temp_output_55_0_g1 = trunc( temp_output_53_0_g1 );
			float DivisionID928 = ( ( temp_output_51_0_g1 * 10.0 ) + temp_output_55_0_g1 );
			float VCoord639 = i.uv_texcoord.y;
			float temp_output_77_0_g1 = ( break1_g1.z * 10.0 );
			float temp_output_78_0_g1 = trunc( temp_output_77_0_g1 );
			float temp_output_80_0_g1 = ( ( temp_output_77_0_g1 - temp_output_78_0_g1 ) * 10.0 );
			float temp_output_81_0_g1 = trunc( temp_output_80_0_g1 );
			float TileX466 = ( temp_output_81_0_g1 + 3.0 );
			float temp_output_965_0 = max( TileX466 , 0.0001 );
			float UCoord638 = i.uv_texcoord.x;
			float temp_output_83_0_g1 = ( ( temp_output_80_0_g1 - temp_output_81_0_g1 ) * 10.0 );
			float temp_output_84_0_g1 = trunc( temp_output_83_0_g1 );
			float TileY467 = ( temp_output_84_0_g1 + 3.0 );
			float temp_output_966_0 = max( TileY467 , 0.0001 );
			float clampResult953 = clamp( ( step( frac( ( ( VCoord639 * 1.0 ) / temp_output_965_0 ) ) , ( 1.0 / temp_output_965_0 ) ) + step( frac( ( ( UCoord638 * 1.0 ) / temp_output_966_0 ) ) , ( 1.0 / temp_output_966_0 ) ) ) , 0.0 , 1.0 );
			float lerpResult930 = lerp( faccade_ID746 , DivisionID928 , ( 1.0 - step( ( clampResult953 - i.vertexColor.r ) , 0.2 ) ));
			float smoothstepResult432 = smoothstep( 2.0 , 2.0 , VCoord639);
			float lerpResult547 = lerp( lerpResult930 , faccade_ID746 , ( 1.0 - smoothstepResult432 ));
			float temp_output_575_0 = ( lerpResult547 - 1.0 );
			float lerpResult1013 = lerp( LowFloorID876 , temp_output_575_0 , ( i.vertexColor.r + 0.0 ));
			float maskLowFloor861 = smoothstepResult432;
			float lerpResult875 = lerp( lerpResult1013 , temp_output_575_0 , maskLowFloor861);
			float RoofNormalThreshold577 = step( ase_worldNormal.y , 0.99 );
			float lerpResult583 = lerp( 40.0 , lerpResult875 , RoofNormalThreshold577);
			float lerpResult1289 = lerp( 20.0 , lerpResult583 , step( ase_vertexNormal.y , 0.2 ));
			float lerpResult1285 = lerp( lerpResult1289 , 20.0 , step( VCoord639 , 0.0 ));
			float clampResult1276 = clamp( ( lerpResult1285 + _Int0 ) , (float)_Int0 , (float)( _Int0 + _Int0 ) );
			float2 UVcoord637 = i.uv_texcoord;
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float2 OffsetPOM947 = POM( UNITY_PASS_TEX2DARRAY(_MaskTexArray), ( UVcoord637 * float2( 0.5,0.5 ) ), ddx(( UVcoord637 * float2( 0.5,0.5 ) )), ddy(( UVcoord637 * float2( 0.5,0.5 ) )), ase_worldNormal, ase_worldViewDir, i.viewDir, 42, 42, _DepthScale, _RefFrame, _MaskTexArray_ST.xy, float2(-1000,-100), clampResult1276 );
			float4 texArray574 = UNITY_SAMPLE_TEX2DARRAY(_MaskTexArray, float3(( UVcoord637 * float2( 0.5,0.5 ) ), clampResult1276)  );
			float2 Offset107 = ( ( texArray574.w - 1 ) * ( i.viewDir.xy / i.viewDir.z ) * ( _DepthScale * 0.3 ) ) + ( UVcoord637 * float2( 0.5,0.5 ) );
			#ifdef _CSCAPE_DESKTOP_ON
				float2 staticSwitch1117 = Offset107;
			#else
				float2 staticSwitch1117 = OffsetPOM947;
			#endif
			float2 Parallax584 = staticSwitch1117;
			float clampResult1278 = clamp( lerpResult1285 , 0.0 , (float)( _Int0 - 1 ) );
			float4 texArray165 = UNITY_SAMPLE_TEX2DARRAY(_MaskTexArray, float3(Parallax584, clampResult1278)  );
			float4 appendResult1048 = (float4(1.0 , texArray165.y , 1.0 , texArray165.x));
			float2 temp_output_149_0 = ( Parallax584 * _ScaleTex1 );
			float temp_output_1520_0 = ( i.vertexColor.b * 10.0 );
			float RooftopID1313 = ( ( frac( temp_output_1520_0 ) * 10.0 ) + 10.1 );
			float temp_output_20_0_g1 = ( ( temp_output_12_0_g1 - temp_output_14_0_g1 ) * 10.0 );
			float temp_output_21_0_g1 = floor( temp_output_20_0_g1 );
			float temp_output_23_0_g1 = ( ( temp_output_20_0_g1 - temp_output_21_0_g1 ) * 10.0 );
			float temp_output_24_0_g1 = floor( temp_output_23_0_g1 );
			float lerpResult1297 = lerp( RooftopID1313 , temp_output_24_0_g1 , step( ase_vertexNormal.y , 0.2 ));
			float temp_output_1469_0 = step( ase_vertexNormal.y , 0.99 );
			float lerpResult1470 = lerp( _roofPlaneTex , lerpResult1297 , temp_output_1469_0);
			float faccadeSuftace_ID2768 = lerpResult1470;
			float4 texArray1487 = UNITY_SAMPLE_TEX2DARRAY(_SurfaceArray, float3(( temp_output_149_0 * _textureScales[clamp((int)faccadeSuftace_ID2768,0,(21 - 1))] ), ( faccadeSuftace_ID2768 + 21.0 ))  );
			float4 appendResult1493 = (float4(1.0 , texArray1487.y , 1.0 , texArray1487.x));
			float faccadeSuftace_ID748 = temp_output_21_0_g1;
			float4 texArray1028 = UNITY_SAMPLE_TEX2DARRAY(_SurfaceArray, float3(( temp_output_149_0 * _textureScales[(int)faccadeSuftace_ID748] ), ( faccadeSuftace_ID748 + 21.0 ))  );
			float4 appendResult1050 = (float4(1.0 , texArray1028.y , 1.0 , texArray1028.x));
			float4 texArray75 = ASE_SAMPLE_TEX2DARRAY_GRAD(_MaskTexArray, float3(Parallax584, clampResult1276), ddx( ( UVcoord637 * float2( 0.5,0.5 ) ) ), ddy( ( UVcoord637 * float2( 0.5,0.5 ) ) ) );
			float3 hsvTorgb3_g34 = HSVToRGB( float3(( texArray75.x + 0.3333333 ),1.0,1.0) );
			float3 temp_output_1397_6 = hsvTorgb3_g34;
			float3 break1648 = temp_output_1397_6;
			float2 break3_g33 = fwidth( ( i.uv_texcoord * _DistanceSmoothness ) );
			float UVDistanceValue1672 = saturate( ( 1.0 - ( ( break3_g33.x + break3_g33.y ) + -0.4 ) ) );
			float3 appendResult1649 = (float3(break1648.x , break1648.y , ( break1648.z * UVDistanceValue1672 )));
			float3 break1372 = appendResult1649;
			float4 lerpResult1488 = lerp( appendResult1493 , appendResult1050 , break1372.y);
			float3 surfaceNormal1035 = UnpackScaleNormal( lerpResult1488, 0.5 );
			float BorderMask1159 = break1372.z;
			float3 lerpResult1715 = lerp( surfaceNormal1035 , UnpackNormal( appendResult1048 ) , BorderMask1159);
			float WindowsMask201 = ( 1.0 - saturate( ( texArray75.w * 20.0 ) ) );
			float lerpResult1680 = lerp( ( 1.0 - WindowsMask201 ) , 1.0 , UVDistanceValue1672);
			float NormalDistanceBlendWindows1682 = lerpResult1680;
			float3 lerpResult1677 = lerp( float3( 0,0,1 ) , BlendNormals( UnpackNormal( appendResult1048 ) , lerpResult1715 ) , NormalDistanceBlendWindows1682);
			float2 lerpResult1471 = lerp( ( Parallax584 * float2( 0.0078125,0.0078125 ) ) , ( Parallax584 * float2( 0.0078125,0.015625 ) ) , maskLowFloor861);
			float4 tex2DNode91 = tex2D( _NoiseTexture, lerpResult1471 );
			float3 lerpResult401 = lerp( float3(-0.05,-0.05,1) , float3(0.05,0.05,1) , tex2DNode91.a);
			float3 NormalShape1142 = UnpackNormal( appendResult1048 );
			float3 lerpResult1148 = lerp( NormalShape1142 , float3( 0,0,1 ) , _NormalWindow);
			float3 normalWindows407 = BlendNormals( lerpResult401 , lerpResult1148 );
			float3 lerpResult412 = lerp( lerpResult1677 , normalWindows407 , WindowsMask201);
			float2 Offset1553 = ( ( 0.1 - 1 ) * i.viewDir.xy * -0.1 ) + ( ( i.uv_texcoord + float2( 0,-0.5 ) ) * float2( 1,2 ) );
			float2 advertParralax1577 = Offset1553;
			float4 colorNoise1549 = tex2D( _NoiseTexture, ( advertParralax1577 * float2( 0.00390625,0.00390625 ) ) );
			float4 break1572 = colorNoise1549;
			float4 texArray1569 = UNITY_SAMPLE_TEX2DARRAY(_ShopSigns, float3(Offset1553, ( floor( ( frac( ( break1572.g + break1572.a ) ) * 11.0 ) ) * 2.0 ))  );
			float4 appendResult1586 = (float4(0.0 , 0.0 , 0.0 , ( 1.0 - texArray1569.a )));
			float smoothstepResult1632 = smoothstep( 0.9 , 0.98 , texArray1569.a);
			float temp_output_1628_0 = ( break1572.r + break1572.g + break1572.b );
			float temp_output_1623_0 = ( floor( ( frac( temp_output_1628_0 ) * 11.0 ) ) * 2.0 );
			float4 lerpResult1570 = lerp( _shopColors[(int)temp_output_1623_0] , _shopColors[(int)( temp_output_1623_0 + 1.0 )] , texArray1569.r);
			float4 lerpResult1555 = lerp( appendResult1586 , ( smoothstepResult1632 * lerpResult1570 ) , texArray1569.a);
			float4 advertising1536 = lerpResult1555;
			float lerpResult1591 = lerp( (( i.uv_texcoord.y >= 1.5 && i.uv_texcoord.y <= 1.98 ) ? 0.0 :  1.0 ) , 1.0 , (advertising1536).a);
			float NoiseBParralax1606 = ( temp_output_1628_0 * 0.333333 );
			float temp_output_92_0_g1 = ( ( temp_output_83_0_g1 - temp_output_84_0_g1 ) * 10.0 );
			float temp_output_93_0_g1 = trunc( temp_output_92_0_g1 );
			float temp_output_95_0_g1 = ( ( temp_output_92_0_g1 - temp_output_93_0_g1 ) * 10.0 );
			float temp_output_98_0_g1 = trunc( temp_output_95_0_g1 );
			float AOHeight571 = temp_output_98_0_g1;
			float ShopDensity1652 = AOHeight571;
			float lerpResult1620 = lerp( 1.0 , lerpResult1591 , saturate( step( NoiseBParralax1606 , ( ShopDensity1652 * 0.1 ) ) ));
			float VNormalMask1741 = temp_output_1449_0;
			float lerpResult1740 = lerp( 1.0 , lerpResult1620 , step( VNormalMask1741 , _test ));
			float3 lerpResult1576 = lerp( float3( 0,0,1 ) , lerpResult412 , lerpResult1740);
			o.Normal = lerpResult1576;
			float4 texArray342 = UNITY_SAMPLE_TEX2DARRAY(_SurfaceArray, float3(( temp_output_149_0 * _textureScales[(int)faccadeSuftace_ID2768] ), faccadeSuftace_ID2768)  );
			float4 texArray712 = UNITY_SAMPLE_TEX2DARRAY(_SurfaceArray, float3(( temp_output_149_0 * _textureScales[(int)faccadeSuftace_ID748] ), faccadeSuftace_ID748)  );
			float temp_output_64_0_g1 = ( ( temp_output_53_0_g1 - temp_output_55_0_g1 ) * 10.0 );
			float temp_output_65_0_g1 = trunc( temp_output_64_0_g1 );
			float windowColor1000 = ( temp_output_65_0_g1 / 9.0 );
			float3 layeredBlendVar74 = (appendResult1649).xyz;
			float4 layeredBlend74 = ( lerp( lerp( lerp( float4( 0,0,0,0 ) , texArray342 , layeredBlendVar74.x ) , texArray712 , layeredBlendVar74.y ) , _borderArray[(int)( windowColor1000 * 10.0 )] , layeredBlendVar74.z ) );
			float temp_output_119_0_g1 = ( break1_g1.w * 10.0 );
			float temp_output_120_0_g1 = trunc( temp_output_119_0_g1 );
			float temp_output_122_0_g1 = ( ( temp_output_119_0_g1 - temp_output_120_0_g1 ) * 10.0 );
			float temp_output_123_0_g1 = trunc( temp_output_122_0_g1 );
			float temp_output_125_0_g1 = ( ( temp_output_122_0_g1 - temp_output_123_0_g1 ) * 10.0 );
			float temp_output_126_0_g1 = trunc( temp_output_125_0_g1 );
			float temp_output_128_0_g1 = ( ( temp_output_125_0_g1 - temp_output_126_0_g1 ) * 10.0 );
			float temp_output_129_0_g1 = trunc( temp_output_128_0_g1 );
			float3 appendResult147_g1 = (float3(temp_output_123_0_g1 , temp_output_126_0_g1 , temp_output_129_0_g1));
			float3 LightsColor523 = appendResult147_g1;
			float3 break1436 = LightsColor523;
			float FloorColorize1438 = break1436.y;
			float temp_output_1719_0 = ( FloorColorize1438 * 2.0 );
			float4 lerpResult1720 = lerp( _faccadeColors[(int)temp_output_1719_0] , _faccadeColors[(int)( temp_output_1719_0 + 1.0 )] , break1648.x);
			float4 lerpResult1744 = lerp( ( lerpResult1720 * layeredBlend74 ) , lerpResult1720 , (lerpResult1720).a);
			float temp_output_1704_0 = saturate( ( break1372.z + maskLowFloor861 ) );
			float4 lerpResult1699 = lerp( lerpResult1744 , layeredBlend74 , temp_output_1704_0);
			float4 lerpResult1733 = lerp( _faccadeColors[(int)( temp_output_1719_0 + 20.0 )] , _faccadeColors[(int)( temp_output_1719_0 + 21.0 )] , break1648.x);
			float4 lerpResult1729 = lerp( ( lerpResult1733 * layeredBlend74 ) , layeredBlend74 , break1372.z);
			float4 lerpResult1728 = lerp( lerpResult1699 , lerpResult1729 , maskLowFloor861);
			float RoofMask1509 = temp_output_1469_0;
			float4 lerpResult1513 = lerp( ( layeredBlend74 * _concreteColors[(int)faccadeSuftace_ID748] ) , lerpResult1728 , RoofMask1509);
			float temp_output_131_0_g1 = ( ( temp_output_128_0_g1 - temp_output_129_0_g1 ) * 10.0 );
			float temp_output_132_0_g1 = trunc( temp_output_131_0_g1 );
			float LightsScale541 = ( floor( temp_output_132_0_g1 ) + 1.0 );
			float offsetDirtMap526 = ( temp_output_120_0_g1 + 3.0 );
			float IllumArray1132 = (0.0 + (faccade_ID746 - 0.0) * (_IlluminationArraySize - 0.0) / (20.0 - 0.0));
			float4 texArray1123 = UNITY_SAMPLE_TEX2DARRAY(_Dirt, float3(( ( ( i.uv_texcoord * float2( 0.01,0.01 ) ) * LightsScale541 ) + ( offsetDirtMap526 * 0.1 ) ), IllumArray1132)  );
			float clampResult252 = clamp( ( texArray1123.r * _DirtAmount ) , 0.0 , 1.0 );
			float4 temp_output_197_0 = ( ( lerpResult1513 * clampResult252 ) * _BuildingLightness );
			float4 tex2DNode1219 = tex2D( _graffiti, ( Parallax584 * float2( 0.2,1 ) ) );
			float GraffitiMask1705 = temp_output_1704_0;
			float negativeNormal1763 = step( ase_vertexNormal.y , -0.1 );
			float lerpResult1711 = lerp( ( 1.0 - tex2DNode1219.a ) , 1.0 , saturate( ( GraffitiMask1705 + negativeNormal1763 ) ));
			float4 lerpResult1224 = lerp( tex2DNode1219 , temp_output_197_0 , lerpResult1711);
			half UseGraffitiColor1236 = i.vertexColor.a;
			float4 lerpResult1258 = lerp( temp_output_197_0 , lerpResult1224 , UseGraffitiColor1236);
			float4 texArray874 = UNITY_SAMPLE_TEX2DARRAY(_BlindsArray, float3(( Parallax584 * 2.0 ), 0.0)  );
			float4 texArray1758 = UNITY_SAMPLE_TEX2DARRAY(_BlindsArray, float3(( Parallax584 * 0.4 ), 1.0)  );
			float mipCurtains646 = texArray1758.r;
			float2 uv_TexCoord674 = i.uv_texcoord * float2( 0.5,0.5 );
			float2 lerpResult1476 = lerp( ( uv_TexCoord674 * float2( -1,-1 ) ) , ( uv_TexCoord674 * float2( -2,-2 ) ) , maskLowFloor861);
			float2 X681 = frac( lerpResult1476 );
			float3 localexpr5681 = expr5681( X681 );
			float3 A683 = localexpr5681;
			float3 B683 = ( float3( 1,1,1 ) / i.viewDir );
			float3 localexpr4683 = expr4683( A683 , B683 );
			float3 C685 = localexpr4683;
			float localexpr3685 = expr3685( C685 );
			float3 temp_output_688_0 = ( localexpr5681 + ( localexpr3685 * i.viewDir ) );
			float3 pos698 = temp_output_688_0;
			float depthScale696 = 1.0;
			float depthScale693 = 1.0;
			float3 pos691 = temp_output_688_0;
			float localexpr2691 = expr2691( pos691 );
			float interp1693 = localexpr2691;
			float localExpr1693 = Expr1693( depthScale693 , interp1693 );
			float realZ696 = localExpr1693;
			float localexpr6696 = expr6696( depthScale696 , realZ696 );
			float interp2698 = localexpr6696;
			float farFrac698 = 0.5;
			float2 localexpr7698 = expr7698( pos698 , interp2698 , farFrac698 );
			float2 interiorUV700 = localexpr7698;
			float2 localexpr8700 = expr8700( interiorUV700 );
			float internVariation843 = ( tex2DNode91.r + tex2DNode91.g + tex2DNode91.b + tex2DNode91.a );
			float4 texArray757 = UNITY_SAMPLE_TEX2DARRAY_LOD(_Interior2, float3(localexpr8700, ( frac( internVariation843 ) * 10.0 )), ( mipCurtains646 * NormalDistanceBlendWindows1682 * _mipCurtains ) );
			float3 gammaToLinear1688 = GammaToLinearSpace( texArray757.xyz );
			float3 enterier632 = gammaToLinear1688;
			float temp_output_209_0 = ( tex2DNode91.r + tex2DNode91.g );
			float temp_output_210_0 = ( temp_output_209_0 + tex2DNode91.b );
			float blin773 = temp_output_93_0_g1;
			float smoothstepResult130 = smoothstep( _BlindsOpen , ( _BlindsOpen + 0.003 ) , ( ( frac( ( 1.0 - (( Parallax584 * float2( 2,2 ) )).y ) ) * temp_output_210_0 ) * ( ( blin773 + 1.0 ) * 0.2 ) ));
			float4 lerpResult129 = lerp( ( texArray874 * texArray165.w ) , float4( enterier632 , 0.0 ) , smoothstepResult130);
			float4 temp_output_1022_0 = ( lerpResult129 * WindowsMask201 );
			float GlassReflection464 = ( temp_output_78_0_g1 * 0.5 );
			float4 break1498 = _glassColors[(int)GlassReflection464];
			float3 appendResult1499 = (float3(break1498.r , break1498.g , break1498.b));
			float4 lerpResult158 = lerp( lerpResult1258 , ( temp_output_1022_0 * float4( appendResult1499 , 0.0 ) ) , WindowsMask201);
			float temp_output_230_0 = ( texArray165.w * _Occlusion );
			float4 temp_output_260_0 = ( lerpResult158 * temp_output_230_0 );
			float occlusionPure707 = temp_output_230_0;
			float dirtSnow1193 = clampResult252;
			float smoothstepResult1303 = smoothstep( _MinSnow , _MaxSnow , ( (WorldNormalVector( i , NormalShape1142 )).y + ( 1.0 - occlusionPure707 ) + ( 1.0 - dirtSnow1193 ) ));
			float4 lerpResult1301 = lerp( temp_output_260_0 , _Snowmoss , saturate( smoothstepResult1303 ));
			#ifdef _USE_SNOW_MOSS_DIRT_ON
				float4 staticSwitch1187 = lerpResult1301;
			#else
				float4 staticSwitch1187 = temp_output_260_0;
			#endif
			float4 OutAfterSnow1584 = staticSwitch1187;
			float2 appendResult3_g35 = (float2(ase_worldPos.x , ase_worldPos.z));
			float4 break1420 = tex2D( _ReLightingControlTex, ( ( appendResult3_g35 / (_ReLightingProjection).xy ) + (_ReLightingProjection).zw ) );
			float AODepthData1421 = break1420.g;
			float temp_output_1424_0 = saturate( (_TVAO_LowValue + (( ( ase_worldPos.y * 0.05 ) * pow( AODepthData1421 , ( ase_worldPos.y * _TVAO_Bias ) ) ) - 0.0) * (_TVAO_High_Value - _TVAO_LowValue) / (10.0 - 0.0)) );
			float4 lerpResult1581 = lerp( float4( (advertising1536).rgb , 0.0 ) , ( OutAfterSnow1584 * temp_output_1424_0 ) , lerpResult1740);
			o.Albedo = lerpResult1581.rgb;
			float temp_output_1637_0 = step( _CSReLight , _Float23 );
			float triggerLights1634 = temp_output_1637_0;
			float4 temp_cast_29 = (0.0).xxxx;
			float3 temp_cast_30 = (0.0).xxx;
			float clampResult221 = clamp( ( WindowsMask201 - ( 1.0 - smoothstepResult130 ) ) , 0.0 , 1.0 );
			float3 lerpResult216 = lerp( temp_cast_30 , enterier632 , clampResult221);
			float4 temp_cast_32 = (temp_output_210_0).xxxx;
			float4 lerpResult222 = lerp( tex2DNode91 , temp_cast_32 , _LightVariation);
			float clampResult1141 = clamp( ( 1.0 - ( i.uv_texcoord.y * 0.5 ) ) , 0.0 , 1.0 );
			float smoothstepResult93 = smoothstep( _CSLights , ( _CSLights + 0.001 ) , ( temp_output_209_0 * ( ( ( temp_output_45_0_g1 * 10.0 ) * 0.1 ) + ( clampResult1141 * 100.0 ) ) * _LightOnThershold ));
			float buildingLightsColorsID1437 = break1436.x;
			float clampResult1217 = clamp( ( _AttenuateBuildingHeight * i.uv_texcoord.y ) , 0.0 , 1.0 );
			float LightShape498 = ( texArray1123.g * _BuildingLighting * clampResult1217 );
			float temp_output_554_0 = ( ( GlassReflection464 * 0.0025 ) + _GlobalLightsOn );
			float smoothstepResult550 = smoothstep( temp_output_554_0 , ( temp_output_554_0 + 0.001 ) , _CSLights);
			float GlobalLightsOnOFff556 = smoothstepResult550;
			float4 lerpResult299 = lerp( ( ( float4( ( lerpResult216 * _LightStrenght ) , 0.0 ) * ( lerpResult222 * smoothstepResult93 ) ) * tex2DNode91.a ) , ( temp_output_260_0 * _buildingLightsColors[(int)buildingLightsColorsID1437] ) , ( ( ( LightShape498 * texArray165.y ) * RoofNormalThreshold577 ) * ( 1.0 - GlobalLightsOnOFff556 ) ));
			float temp_output_755_0 = step( abs( ase_worldNormal.y ) , 0.3 );
			float4 lerpResult940 = lerp( temp_cast_29 , lerpResult299 , temp_output_755_0);
			float clampResult1081 = clamp( ( ( ase_worldPos.y * 0.6 ) * _LightsDistance ) , 0.0 , 1.0 );
			float3 appendResult1062 = (float3(frac( ( ase_worldPos.x * _LightsDistance ) ) , frac( ( ase_worldPos.z * _LightsDistance ) ) , clampResult1081));
			float lightingNormal1157 = temp_output_755_0;
			float clampResult1083 = clamp( ( ( 1.0 - distance( ( appendResult1062 * _lightsContour ) , float3( ( float2( 0.5,0.5 ) * _lightsContour ) ,  0.0 ) ) ) * lightingNormal1157 ) , 0.0 , 1.0 );
			float clampResult1096 = clamp( ( distance( ase_worldPos , _WorldSpaceCameraPos ) * _CSReLightDistance ) , 0.0 , 1.0 );
			float4 lerpResult1635 = lerp( lerpResult940 , ( lerpResult940 + ( ( pow( clampResult1083 , 1.5 ) * ( _reLightColor.a * 10.0 ) * lerpResult1581 ) * _reLightColor * clampResult1096 * break1420.r ) ) , temp_output_1637_0);
			float4 lerpResult1542 = lerp( ( advertising1536 * _ShopSignsLight * triggerLights1634 ) , lerpResult1635 , lerpResult1740);
			o.Emission = ( lerpResult1542 * temp_output_1424_0 ).rgb;
			float blindsFront1521 = ceil( temp_output_1520_0 );
			float clampResult256 = clamp( ( ( WindowsMask201 * _Mettalic ) * ( GlassReflection464 * 0.4 ) ) , _MinSpecular , 1.0 );
			float lerpResult946 = lerp( 0.1 , ( clampResult256 * offsetDirtMap526 ) , temp_output_755_0);
			float glossFeed1752 = break1498.a;
			float lerpResult1754 = lerp( lerpResult946 , glossFeed1752 , WindowsMask201);
			float temp_output_1494_0 = saturate( lerpResult1754 );
			float lerpResult1501 = lerp( 1.0 , 0.0 , smoothstepResult130);
			float frontBlind1502 = lerpResult1501;
			float lerpResult1503 = lerp( temp_output_1494_0 , 0.0 , frontBlind1502);
			float ifLocalVar1523 = 0;
			if( 1.0 == blindsFront1521 )
				ifLocalVar1523 = temp_output_1494_0;
			else if( 1.0 < blindsFront1521 )
				ifLocalVar1523 = lerpResult1503;
			float lerpResult1587 = lerp( 0.0 , ifLocalVar1523 , lerpResult1740);
			float noise758 = tex2DNode91.r;
			float temp_output_1246_0 = saturate( ( lerpResult1587 + ( 0.05 * noise758 ) ) );
			float3 temp_cast_36 = (temp_output_1246_0).xxx;
			o.Specular = temp_cast_36;
			float lerpResult1505 = lerp( texArray1487.w , texArray1028.w , break1372.y);
			float MetallicVar1056 = lerpResult1505;
			float lerpResult1020 = lerp( MetallicVar1056 , _Smoothness , WindowsMask201);
			float lerpResult1589 = lerp( 0.0 , lerpResult1020 , lerpResult1740);
			o.Smoothness = saturate( lerpResult1589 );
			float lerpResult943 = lerp( 1.0 , occlusionPure707 , temp_output_755_0);
			float lerpResult1588 = lerp( 1.0 , lerpResult943 , lerpResult1740);
			o.Occlusion = lerpResult1588;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma exclude_renderers d3d9 gles d3d11_9x xbox360 psp2 n3ds 
		#pragma surface surf StandardSpecular keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
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
				float4 customPack1 : TEXCOORD1;
				float2 customPack2 : TEXCOORD2;
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				half4 color : COLOR0;
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
				o.customPack1.xyzw = customInputData.uv4_tex4coord4;
				o.customPack1.xyzw = v.texcoord3;
				o.customPack2.xy = customInputData.uv_texcoord;
				o.customPack2.xy = v.texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				o.color = v.color;
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
				surfIN.uv4_tex4coord4 = IN.customPack1.xyzw;
				surfIN.uv_texcoord = IN.customPack2.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.viewDir = IN.tSpace0.xyz * worldViewDir.x + IN.tSpace1.xyz * worldViewDir.y + IN.tSpace2.xyz * worldViewDir.z;
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				surfIN.vertexColor = IN.color;
				SurfaceOutputStandardSpecular o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandardSpecular, o )
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
Version=16100
438;177;1983;1198;140.4831;525.6274;1.244667;True;True
Node;AmplifyShaderEditor.TexCoordVertexDataNode;923;-7296.489,-638.1862;Float;False;3;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;1280;-7005.059,-635.2876;Float;False;DecodeMeshInformation;-1;;1;dd70cb26047247348ae4c75a354e137b;0;1;6;FLOAT4;0,0,0,0;False;16;FLOAT;37;FLOAT;38;FLOAT;39;FLOAT;40;FLOAT;0;FLOAT;72;FLOAT;73;FLOAT;74;FLOAT;112;FLOAT;113;FLOAT;114;FLOAT;115;FLOAT;116;FLOAT;155;FLOAT3;156;FLOAT;157
Node;AmplifyShaderEditor.TextureCoordinatesNode;22;-5338.274,-1551.812;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;467;-6021.86,-145.4947;Float;False;TileY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;636;-4345.67,-1403.994;Float;False;2153.497;579.15;Comment;21;328;319;322;325;488;469;329;640;641;932;933;939;965;966;331;931;953;955;954;1365;1367;TileGeneration;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;639;-4930.072,-1446.295;Float;False;VCoord;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;466;-6027.848,-226.5602;Float;False;TileX;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;638;-4931.27,-1539.495;Float;False;UCoord;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;469;-4150.443,-1259.488;Float;False;466;TileX;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;488;-4187.794,-987.7393;Float;False;467;TileY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;641;-4280.873,-1346.496;Float;False;639;VCoord;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;640;-4290.37,-1091.896;Float;False;638;UCoord;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;966;-3760,-1024;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.0001;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;965;-3882.206,-1234.065;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.0001;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1365;-3931.486,-1340.623;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1367;-3969.071,-1099.23;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;319;-3582.202,-1321.095;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;328;-3595.54,-1101.569;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;322;-3404.303,-1321.395;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;329;-3434.74,-1103.77;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;331;-3561.266,-959.2918;Float;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;325;-3468.257,-1228.142;Float;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalVertexDataNode;1299;-6803.792,-895.6021;Float;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StepOpNode;932;-3197.178,-1202.464;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;933;-3190.256,-933.5396;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;1449;-6601.779,-962.6035;Float;False;2;0;FLOAT;0;False;1;FLOAT;-0.4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1640;-6573.003,-816.0228;Float;False;2;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;931;-2983.186,-1213.018;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;937;-4325.269,-761.6693;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ClampOpNode;953;-2827.876,-1192.142;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;1642;-6394.703,-1004.985;Float;False;2;0;FLOAT;0;False;1;FLOAT;-0.98;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1448;-6401.072,-883.2058;Float;False;3;0;FLOAT;11;False;1;FLOAT;29;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1641;-6196.703,-873.9855;Float;False;3;0;FLOAT;11;False;1;FLOAT;30;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;939;-2673.226,-1112.591;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;644;-3336.853,-332.4904;Float;False;639;VCoord;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;954;-2528.523,-992.7961;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;746;-6027.691,-876.1716;Half;False;faccade_ID;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;928;-6041.347,-456.8891;Float;False;DivisionID;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;434;-3345.786,-88.10668;Float;False;Constant;_Float3;Float 3;47;0;Create;True;0;0;False;0;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;433;-3346.99,-197.2069;Float;False;Constant;_Float0;Float 0;47;0;Create;True;0;0;False;0;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;929;-2900.499,-712.1909;Float;False;928;DivisionID;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;432;-3032.484,-250.1069;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;747;-2853.77,-581.6896;Float;False;746;faccade_ID;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;955;-2378.867,-956.9001;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;930;-2431.761,-583.6057;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;438;-2712.185,-246.9085;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;547;-2293.788,-394.5038;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;363;-2081.591,-361.787;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;365;3828.802,1091.687;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;876;-6040.849,-533.2155;Float;False;LowFloorID;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;368;4213.083,985.7267;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.99;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;877;-2929.945,122.0783;Float;False;876;LowFloorID;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1213;-2951.505,246.0846;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;575;-2683.043,6.929386;Float;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;577;4432.105,970.7241;Float;False;RoofNormalThreshold;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;861;-2781.805,330.5308;Float;False;maskLowFloor;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1013;-2679.284,173.8204;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalVertexDataNode;1290;-3521.187,1366.572;Float;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;578;-3613.867,1236.105;Float;False;577;RoofNormalThreshold;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;875;-2463.91,203.0601;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;583;-3316.48,1205.233;Float;False;3;0;FLOAT;40;False;1;FLOAT;39;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;1293;-3267.587,1392.572;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1286;-3377.624,1575.767;Float;False;639;VCoord;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;1287;-3121.875,1636.448;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1289;-3067.917,1335.844;Float;False;3;0;FLOAT;20;False;1;FLOAT;21;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;637;-4925.668,-1623.495;Float;False;UVcoord;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;642;-2697.765,816.3844;Float;False;637;UVcoord;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.IntNode;1274;-2232.315,311.272;Float;False;Property;_Int0;Int 0;45;0;Create;True;0;0;False;0;0;40;0;1;INT;0
Node;AmplifyShaderEditor.LerpOp;1285;-2847.369,1443.751;Float;False;3;0;FLOAT;0;False;1;FLOAT;20;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;866;-2324.852,765.3058;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1277;-1976.665,239.2446;Float;False;2;2;0;FLOAT;0;False;1;INT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1273;-1961.785,359.4542;Float;False;2;2;0;INT;0;False;1;INT;0;False;1;INT;0
Node;AmplifyShaderEditor.RelayNode;873;-2005.005,829.8384;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;116;-2879.816,1229.019;Float;False;Property;_DepthScale;DepthScale;6;0;Create;True;0;0;False;0;0.12;0.078;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;1276;-1798.614,261.4091;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1520;-4211.063,-297.567;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;574;-1751.164,2312.916;Float;True;Property;_TextureArray1;Texture Array 1;21;0;Create;True;0;0;False;0;None;0;Instance;75;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;990;-2429.31,1161.073;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.3;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;950;-2153.487,1364.9;Float;False;Property;_RefFrame;RefFrame;29;0;Create;True;0;0;False;0;0;0.4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;1519;-4038.063,-367.567;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;951;-2274.954,62.65939;Float;True;Property;_MaskTexArray;MaskTexArray;28;1;[Gamma];Create;True;0;0;False;0;None;9f1f6844d95968a49bec5d3d63630afb;False;white;LockedToTexture2DArray;Texture2DArray;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;108;-2583.445,1495.049;Float;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;673;-1087.125,-2167.5;Float;False;6082.19;1413.166;Comment;34;701;695;690;682;679;677;676;675;674;703;757;759;765;693;691;685;683;681;696;698;700;688;687;1252;1253;1254;1255;1256;1477;1476;1478;1690;1691;1756;;1,1,1,1;0;0
Node;AmplifyShaderEditor.ParallaxOcclusionMappingNode;947;-1963.775,1287.216;Float;False;3;42;False;1311;72;False;-1;3;0.02;0;False;1,1;False;-1000,-100;TextureArray;7;0;FLOAT2;0,0;False;1;SAMPLER2D;;False;2;FLOAT;0.02;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT2;0,0;False;6;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ParallaxMappingNode;107;-2205.424,1050.656;Float;False;Planar;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0.1;False;3;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1316;-3919.638,-454.5587;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;674;-926.4719,-1541.294;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;0.5,0.5;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;1117;-1749.336,1118.132;Float;False;Property;_CSCAPE_DESKTOP;CSCAPE_DESKTOP;47;0;Create;True;0;0;False;0;0;0;0;False;;Toggle;2;Key0;Key1;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1317;-3770.771,-443.4061;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;584;-1919.413,1030.796;Float;False;Parallax;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1313;-3617.669,-434.5284;Float;False;RooftopID;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;1656;-2554.982,-213.5876;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;1645;-2666.908,-92.34633;Float;False;Property;_DistanceSmoothness;DistanceSmoothness;56;0;Create;True;0;0;False;0;1000;8.71;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1477;-535.1287,-1386.657;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;-2,-2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;675;-519.7347,-1517.563;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;-1,-1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;1478;-824.2283,-1347.149;Float;False;861;maskLowFloor;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;1644;-2344.4,-156.1144;Float;False;UVDistanceFade;-1;;33;0ca3e21c3344b4340bc4f48c703d2836;0;2;7;FLOAT2;0,0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;1534;2229.181,-363.2421;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StepOpNode;1298;-6543.479,-512.8619;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1476;-343.2283,-1443.149;Float;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;1318;-6576.483,-588.0972;Float;False;1313;RooftopID;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DdyOpNode;957;-1794.881,813.8491;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DdxOpNode;956;-1809.301,732.2277;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;676;-706.509,-1143.185;Float;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;901;-1798.261,121.8877;Float;False;584;Parallax;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1546;2487.17,-342.6535;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,-0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StepOpNode;1469;-6563.54,-710.6581;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.99;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;75;-1519.597,96.84297;Float;True;Property;_tt;tt;17;0;Create;True;0;0;False;0;None;0;Object;-1;MipBias;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;1655;-2072.309,-116.4515;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;-0.4;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;679;-377.8378,-1260.744;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;1730;-6377.454,-688.6642;Float;False;Property;_roofPlaneTex;roofPlaneTex;57;0;Create;True;0;0;False;0;0;2.38;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;677;-83.68896,-1535.072;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;523;-6013.029,174.488;Float;False;LightsColor;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;1297;-6365.046,-619.2312;Float;False;3;0;FLOAT;11;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;1436;-5695.215,154.7925;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.LerpOp;1470;-6194.088,-727.3513;Float;False;3;0;FLOAT;3;False;1;FLOAT;10;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;681;130.926,-1613.471;Float;False;float3( X * 2 - 1, -1);3;False;1;True;X;FLOAT2;0,0;In;;Float;expr5;True;False;0;1;0;FLOAT2;0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;682;-114.5752,-1151.979;Float;False;2;0;FLOAT3;1,1,1;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1396;-1224.018,-102.8579;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.3333333;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1399;-1217.172,832.4964;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;20;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1651;-1938.964,-76.97735;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;1554;2436.867,-181.8067;Float;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1535;2643.148,-358.8348;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;1,2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;1400;-1036.335,877.4937;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ParallaxMappingNode;1553;2813.318,-369.5732;Float;False;Normal;4;0;FLOAT2;0,0;False;1;FLOAT;0.1;False;2;FLOAT;-0.1;False;3;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;150;-2379.042,393.9478;Float;False;Property;_ScaleTex1;ScaleTex1;19;0;Create;True;0;0;False;0;0;2.66;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;1646;-1760.857,-97.64864;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;683;135.123,-1182.471;Float;False;abs(B) - A * B;3;False;2;True;A;FLOAT3;0,0,0;In;;Float;True;B;FLOAT3;0,0,0;In;;Float;expr4;True;False;0;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1438;-5384.097,162.3332;Float;False;FloorColorize;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1150;-3713.273,3335.384;Float;False;584;Parallax;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;768;-6037.085,-683.5455;Float;False;faccadeSuftace_ID2;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;1397;-1381.855,-574.2473;Float;False;Simple HUE;-1;;34;32abb5f0db087604486c2db83a2e817a;0;1;1;FLOAT;0;False;4;FLOAT3;6;FLOAT;7;FLOAT;5;FLOAT;8
Node;AmplifyShaderEditor.RegisterLocalVarNode;748;-6039.395,-761.9308;Float;False;faccadeSuftace_ID;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1000;-6037.425,-380.6249;Float;False;windowColor;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;90;-821.4456,875.8799;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1577;3142.626,-382.1708;Float;False;advertParralax;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;770;-1717.578,956.0438;Float;False;768;faccadeSuftace_ID2;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1475;-3528.786,3512.057;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.0078125,0.0078125;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CustomExpressionNode;685;319.425,-1178.373;Float;False;min(min(C.x, C.y), C.z);1;False;1;True;C;FLOAT3;0,0,0;In;;Float;expr3;True;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;1455;-1893.219,457.2065;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1714;-687.0109,79.8485;Float;False;1438;FloorColorize;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;1648;-1818.607,-245.6592;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RegisterLocalVarNode;1672;-1638.077,-3.516846;Float;False;UVDistanceValue;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;767;-2828.877,618.4956;Float;False;748;faccadeSuftace_ID;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1474;-3482.073,3381.381;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.0078125,0.015625;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;1472;-3805.687,3640.758;Float;False;861;maskLowFloor;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1719;-475.1001,87.32824;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1003;-1037.353,769.2726;Float;False;1000;windowColor;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1578;-3832.549,3018.212;Float;False;1577;advertParralax;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1650;-1567.454,-132.5182;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;656;-4616.882,4820.606;Float;False;1893.389;661.2908;Comment;14;146;242;240;241;234;238;239;237;646;243;244;653;1170;1758;Curtains;1,1,1,1;0;0
Node;AmplifyShaderEditor.GlobalArrayNode;1695;-1518.515,566.4357;Float;False;_textureScales;0;21;0;False;False;0;1;False;Instance;1460;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GlobalArrayNode;1697;-1362.711,932.0371;Float;False;_textureScales;0;21;0;False;False;0;1;False;Instance;1460;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1471;-3330.206,3551.357;Float;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;201;-611.4291,864.9924;Float;False;WindowsMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;687;480.02,-1320.971;Float;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;149;-1549.838,674.7468;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;1284;-1479.704,310.7308;Float;True;Property;_SurfaceArray;SurfaceArray;47;0;Create;True;0;0;False;0;None;55240eb16b3a21b4b8b4177c9b8ac2b8;False;white;LockedToTexture2DArray;Texture2DArray;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode;1679;2514.198,1687.824;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1579;-3569.467,3050.989;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.00390625,0.00390625;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1698;-1049.212,985.71;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;653;-4599.508,4918.428;Float;False;584;Parallax;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;688;861.322,-1498.372;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1696;-1244.275,542.1279;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;244;-4565.656,5061.899;Float;False;Constant;_Float9;Float 9;31;0;Create;True;0;0;False;0;0.4;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1409;-786.8411,761.3177;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1724;-303.6003,186.8282;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;91;-3168.044,3205.603;Float;True;Property;_NoiseTexture;NoiseTexture;20;0;Create;True;0;0;False;0;None;be53a91e4e185d9448d300ead53b5f1f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;1649;-1408.74,-154.3836;Float;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1735;-570.7277,230.1492;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;21;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;1369;-1213.675,-200.1594;Float;False;True;True;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;1575;-3352.614,3002.542;Float;True;Property;_TextureSample0;Texture Sample 0;20;0;Create;True;0;0;False;0;None;be53a91e4e185d9448d300ead53b5f1f;True;0;False;white;Auto;False;Instance;91;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;1734;-438.7277,208.1492;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;20;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;243;-4383.236,4999.094;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;1681;2791.008,1672.293;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;690;1310.338,-1124.258;Float;False;Constant;_Float6;Float 6;2;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GlobalArrayNode;1713;-329.9572,46.56048;Float;False;_faccadeColors;0;41;1;False;False;0;1;False;Object;-1;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CustomExpressionNode;691;1280.933,-1477.657;Float;False;pos.z * 0.5 + 0.5;1;False;1;True;pos;FLOAT3;0,0,0;In;;Float;expr2;True;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GlobalArrayNode;1410;-613.6187,735.1622;Float;False;_borderArray;0;11;2;False;False;0;1;False;Object;-1;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;1678;2224.194,1966.2;Float;True;1672;UVDistanceValue;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GlobalArrayNode;1721;-157.1202,200.9819;Float;False;_faccadeColors;0;41;1;False;False;0;1;False;Instance;1713;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TextureArrayNode;712;-953.4354,317.6141;Float;True;Property;_SurfaceAray;SurfaceAray;24;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;840;-2543.634,3163.48;Float;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;342;-868.1193,995.7475;Float;True;Property;_WallsArray;WallsArray;31;0;Create;True;0;0;False;0;None;0;Instance;712;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;531;-1430.392,1176.894;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;1720;63.94449,150.4845;Float;False;3;0;COLOR;0.5,0,0,1;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1700;-479.6503,493.8819;Float;False;861;maskLowFloor;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1549;-3020.479,2976.866;Float;False;colorNoise;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TextureArrayNode;1758;-3932.804,5027.667;Float;True;Property;_TextureArray3;Texture Array 3;60;0;Create;True;0;0;False;0;None;0;Instance;874;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;541;-6017.968,264.4967;Float;False;LightsScale;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;527;-1310.219,1343.63;Float;False;Constant;_Vector3;Vector 3;46;0;Create;True;0;0;False;0;0.01,0.01;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.CustomExpressionNode;693;1636.235,-1374.158;Float;False;saturate(interp1) / depthScale + 1;1;False;2;True;depthScale;FLOAT;0;In;;Float;True;interp1;FLOAT;0;In;;Float;Expr1;True;False;0;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1403;-3088.206,1983.091;Float;False;584;Parallax;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GlobalArrayNode;1732;-359.2891,403.8651;Float;False;_faccadeColors;0;41;1;False;False;0;1;False;Instance;1713;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;526;-6019.712,100.9051;Float;False;offsetDirtMap;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GlobalArrayNode;1731;-295.2891,289.8651;Float;False;_faccadeColors;0;41;1;False;False;0;1;False;Instance;1713;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.BreakToComponentsNode;1372;-1356.003,-12.1778;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RangedFloatNode;1131;-4396.977,218.1721;Float;False;Property;_IlluminationArraySize;IlluminationArraySize;37;0;Create;True;0;0;False;0;0;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;843;-2403.721,3158.73;Float;False;internVariation;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1680;2510.866,1922.283;Float;False;3;0;FLOAT;1;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LayeredBlendNode;74;-575.2806,328.4363;Float;False;6;0;FLOAT3;0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT4;0,0,0,0;False;3;FLOAT4;0,0,0,0;False;4;FLOAT4;0,0,0,0;False;5;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;561;-1311.086,1523.094;Float;False;541;LightsScale;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1703;-137.8635,560.4533;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1550;2560.094,21.9291;Float;False;1549;colorNoise;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1745;279.6758,76.93851;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;646;-3006.576,4938.106;Float;False;mipCurtains;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1405;-2820.856,1952.006;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;2,2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;1733;-187.2891,351.8651;Float;False;3;0;COLOR;0.5,0,0,1;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ComponentMaskNode;1747;199.6758,234.9385;Float;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;759;3356.139,-1591.349;Float;False;843;internVariation;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;696;2253.537,-1263.656;Float;False;(1.0 - (1.0 / realZ)) * (depthScale +1.0);1;False;2;True;depthScale;FLOAT;0;In;;Float;True;realZ;FLOAT;0;In;;Float;expr6;True;False;0;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;695;2464.43,-988.358;Float;False;Constant;_Float15;Float 15;3;0;Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;532;-1049.792,1323.294;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1682;2653.466,1954.003;Float;False;NormalDistanceBlendWindows;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;1129;-4033.837,132.8488;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;20;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;529;-1067.992,1574.294;Float;False;526;offsetDirtMap;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;560;-928.887,1428.195;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;533;-821.1201,1566.626;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;1572;2771.123,-107.7576;Float;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.FractNode;1690;3887.336,-1553.525;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;773;-6019.93,-62.84596;Float;False;blin;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1701;14.187,427.197;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0.5,0,0,2;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1518;-872.4042,519.6249;Float;False;748;faccadeSuftace_ID;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1757;3890.062,-556.4115;Float;False;Property;_mipCurtains;mipCurtains;59;0;Create;True;0;0;False;0;0;16.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1756;3951.062,-1119.411;Float;False;1682;NormalDistanceBlendWindows;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;1404;-2821.669,2120.443;Float;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1132;-3712.27,130.9882;Float;False;IllumArray;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;1704;32.68146,548.5005;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;698;2859.133,-1177.258;Float;False;pos.xy * lerp(1.0, farFrac, interp2);2;False;3;True;pos;FLOAT3;0,0,0;In;;Float;True;interp2;FLOAT;0;In;;Float;True;farFrac;FLOAT;0;In;;Float;expr7;True;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;703;4077.367,-1314.875;Float;False;646;mipCurtains;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1744;427.6758,147.9385;Float;False;3;0;COLOR;0.5,0,0,1;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CustomExpressionNode;700;3384.868,-1134.572;Float;False;interiorUV * -0.5 - 0.5;2;False;1;True;interiorUV;FLOAT2;0,0;In;;Float;expr8;True;False;0;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1628;3081.415,-15.25606;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1133;-833.2213,1267.745;Float;False;1132;IllumArray;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1509;-6426.067,-747.9205;Float;False;RoofMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;765;4333.485,-1242.543;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0.25;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;528;-686.5317,1353.187;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1691;4103.588,-1528.573;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1699;586.1472,222.0281;Float;False;3;0;FLOAT4;0.5,0,0,1;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;775;-2537.626,2181.672;Float;False;773;blin;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GlobalArrayNode;1517;-506.562,583.7329;Float;False;_concreteColors;0;11;1;False;False;0;1;False;Object;-1;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;1729;221.7575,352.9593;Float;False;3;0;FLOAT4;0.5,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.OneMinusNode;138;-2492.284,1906.281;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;209;-2214.508,2884.297;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;250;-366.1748,960.31;Float;False;Property;_DirtAmount;DirtAmount;14;0;Create;True;0;0;False;0;1;2.13;1;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1662;2986.11,132.9542;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;1639;3240.205,26.29489;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;210;-1941.107,2889.394;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1728;411.8346,396.0341;Float;False;3;0;FLOAT4;0.5,0,0,1;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleAddOpNode;777;-2379.237,2016.681;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;1123;-492.7086,1053.272;Float;True;Property;_Dirt;Dirt;36;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;3;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;1279;-347.3536,1712.132;Float;False;2;0;INT;0;False;1;INT;1;False;1;INT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1516;650.972,440.3309;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TextureArrayNode;757;4473.578,-1456.143;Float;True;Property;_Interior2;Interior2;23;0;Create;True;0;0;False;0;None;0;Object;-1;MipLevel;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StepOpNode;1762;-6648.24,-1111.945;Float;False;2;0;FLOAT;0;False;1;FLOAT;-0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;711;-2262.344,1629.984;Float;False;Constant;_Float4;Float 4;38;0;Create;True;0;0;False;0;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;121;-2287.321,1870.97;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1511;-330.4873,675.8339;Float;False;1509;RoofMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;652;-2281.921,1519.459;Float;False;584;Parallax;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1705;365.2328,536.0032;Float;False;GraffitiMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GammaToLinearNode;1688;4835.951,-1443.985;Float;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;710;-2054.086,1527.477;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;251;-107.2575,1074.92;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1220;841.9641,-101.5947;Float;False;584;Parallax;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;776;-2199.762,2016.511;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;1658;2974.105,309.6471;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1513;833.7648,506.053;Float;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;136;-1980.704,2083.915;Float;False;Constant;_BlindsAperture;BlindsAperture;11;0;Create;True;0;0;False;0;0.003;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;131;-2085.002,1875.999;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1763;-6492.24,-1202.945;Float;False;negativeNormal;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;1278;-601.338,2029.108;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;135;-2079.606,1715.997;Float;False;Property;_BlindsOpen;BlindsOpen;12;0;Create;True;0;0;False;0;0;0.24;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;651;-492.7318,2144.635;Float;False;584;Parallax;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1630;3352.915,-139.2879;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;11;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;874;-1819.165,1514.023;Float;True;Property;_BlindsArray;BlindsArray;22;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;137;-1711.81,1950.19;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;1510;-125.5344,806.8549;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1657;3109.214,307.2644;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;11;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1764;803.4639,368.0435;Float;False;1763;negativeNormal;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1222;1040.553,-104.0103;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.2,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;464;-6031.731,-304.4449;Float;False;GlassReflection;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1626;3075.092,-297.3631;Float;False;Constant;_Float16;Float 16;62;0;Create;True;0;0;False;0;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;165;-187.7757,1978.575;Float;True;Property;_NormalTextureArray;NormalTextureArray;18;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FloorOpNode;1627;3158.82,-200.6863;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1231;824.7397,269.084;Float;False;1705;GraffitiMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;632;5238.458,-1454.394;Float;False;enterier;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ClampOpNode;252;83.42058,1073.513;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;436;-1891.784,1859.293;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1495;186.1701,823.3497;Float;False;464;GlassReflection;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1623;3250.019,-308.9477;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1219;1235.808,-143.5036;Float;True;Property;_graffiti;graffiti;44;0;Create;True;0;0;False;0;None;28ca7a79df19d54458a0fe0a2d01bf6b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;1766;1068.088,304.5654;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;634;-1494.565,2029.314;Float;False;632;enterier;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;246;58.0863,730.9882;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;423;-1474.983,1577.892;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.FloorOpNode;1659;3275.121,306.666;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;130;-1551.478,1834.696;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1661;3234.593,182.7891;Float;False;Constant;_Float18;Float 18;62;0;Create;True;0;0;False;0;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;1530;2670.697,-584.705;Float;True;Property;_ShopSigns;ShopSigns;52;0;Create;True;0;0;False;0;None;4e0938b5ecfcdd5428b6144578944ea7;False;white;LockedToTexture2DArray;Texture2DArray;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;198;298.291,631.2901;Float;False;Property;_BuildingLightness;BuildingLightness;16;0;Create;True;0;0;False;0;5;1;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;129;-826.9814,1875.851;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;231;2095.894,885.4017;Float;False;Property;_Occlusion;Occlusion;15;0;Create;True;0;0;False;0;0;1;0;4;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;1760;1234.472,324.284;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;1048;1135.934,1426.291;Float;False;FLOAT4;4;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.OneMinusNode;1710;946.0817,191.939;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GlobalArrayNode;1497;428.1456,804.7944;Float;False;_glassColors;0;10;1;False;False;0;1;False;Object;-1;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1625;3444.64,-235.9649;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;120;252.1238,724.9296;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1660;3467.119,334.4045;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;1067;7200.644,3077.427;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TextureCoordinatesNode;1137;-3313.014,2483.444;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;650;978.9679,1107.652;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;1569;3301.302,-705.7487;Float;True;Property;_TextureArray2;Texture Array 2;54;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;230;2467.87,859.9496;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1711;1341.177,141.7057;Float;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GlobalArrayNode;1621;3421.802,-428.2353;Float;False;_shopColors;0;22;1;False;False;0;1;False;Object;-1;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.UnpackScaleNormalNode;1041;1626.715,1755.175;Float;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;1236;-2947.201,12.05792;Half;False;UseGraffitiColor;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;197;650.7915,667.8901;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RelayNode;633;-379.3555,1883.497;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GlobalArrayNode;1622;3595.595,-237.6052;Float;False;_shopColors;0;22;1;False;False;0;1;False;Object;-1;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.BreakToComponentsNode;1498;759.187,863.8551;Float;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1080;7559.485,3254.48;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.6;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1224;1563.124,222.4853;Float;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;707;2621.469,731.8885;Float;False;occlusionPure;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1142;1932.874,1624.542;Float;False;NormalShape;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;657;-427.0775,3331.807;Float;False;1729.959;920.7582;Comment;23;556;224;222;94;95;96;225;551;552;555;554;553;550;93;164;214;221;217;655;216;215;871;1500;WindowLightsManager;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1193;364.6432,1066.878;Float;False;dirtSnow;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1601;3860.335,-303.4503;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1022;1294.582,1123.865;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;1070;7109.63,3311.511;Float;False;Global;_LightsDistance;_LightsDistance;43;0;Create;True;0;0;False;0;0.1;0.06;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1570;3760.719,-576.1404;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1237;954.7259,633.9678;Float;False;1236;UseGraffitiColor;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;1632;3848.145,-171.0973;Float;False;3;0;FLOAT;0;False;1;FLOAT;0.9;False;2;FLOAT;0.98;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;1499;1090.111,786.6219;Float;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1140;-3015.158,2511.467;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1490;-1079.84,-52.6546;Float;False;748;faccadeSuftace_ID;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1304;7906.262,896.0208;Float;False;707;occlusionPure;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;1432;8754.904,3022.362;Float;False;CScapeReLighting;0;;35;dc5e3dd0bdebf8d43be648b95d13eb58;0;0;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;1586;4269.509,-387.0719;Float;False;COLOR;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1077;7857.673,3382.588;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1258;1245.92,539.5068;Float;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1068;7818.133,3104.656;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1603;4056.781,-17.50327;Float;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;571;-6014.496,18.74004;Float;False;AOHeight;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1164;7228.06,601.2011;Float;False;1142;NormalShape;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;1216;-443.9641,1307.049;Float;False;Property;_AttenuateBuildingHeight;AttenuateBuildingHeight;43;0;Create;True;0;0;False;0;0;0.06;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1751;1657.936,842.615;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1489;-967.6979,-367.2324;Float;False;768;faccadeSuftace_ID2;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1138;-2830.298,2513.028;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;551;-399.9838,3415.991;Float;False;464;GlassReflection;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1194;7682.103,787.5749;Float;False;1193;dirtSnow;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1069;7853.428,3250.708;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;1059;8021.669,3098.382;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;1169;7526.822,610.8283;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.OneMinusNode;1307;8295.683,736.8292;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1652;-5503.814,-42.41604;Float;False;ShopDensity;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GlobalArrayNode;1693;-922.8706,-283.0658;Float;False;_textureScales;0;21;0;True;False;0;1;False;Object;-1;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1215;-151.9948,1339.319;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;555;-148.604,3600.953;Float;False;Property;_GlobalLightsOn;GlobalLightsOn;26;0;Create;True;0;0;False;0;0;0.178;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;158;1899.921,685.8647;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;1081;8089.876,3402.81;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1195;7915.712,784.999;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1629;3386.718,-22.80848;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.333333;False;1;FLOAT;0
Node;AmplifyShaderEditor.GlobalArrayNode;1468;-794.0026,161.4612;Float;False;_textureScales;0;21;0;False;False;0;1;False;Object;-1;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;1141;-2597.333,2474.055;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1555;4481.619,-195.673;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.FractNode;1058;8038.461,3221.458;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;552;-33.3843,3455.993;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.0025;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;1420;9038.67,3018.219;Float;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;838;-2929.76,2347.283;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;465;4197.836,2792.307;Float;False;464;GlassReflection;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1173;8110.604,1010.947;Float;False;Property;_MaxSnow;MaxSnow;40;0;Create;True;0;0;False;0;0;0.566;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;554;152.416,3448.793;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;157;4116.309,2578.006;Float;False;Property;_Mettalic;Mettalic;7;0;Create;True;0;0;False;0;0;0.184;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;1062;8333.857,3357.084;Float;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;1172;8117.865,931.4737;Float;False;Property;_MinSnow;MinSnow;41;0;Create;True;0;0;False;0;0;0.083;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;260;2732.188,847.6556;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1694;-676.8723,-234.3004;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;1653;10941.59,2670.783;Float;False;1652;ShopDensity;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1536;4718.407,-170.6478;Float;False;advertising;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1692;-642.4027,-35.09101;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ClampOpNode;1217;-1.522747,1335.474;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;1012;4142.1,1138.459;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;661;-1079.018,1721.564;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;662;4241.686,2492.775;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;1060;8282.297,3080.686;Float;False;Constant;_Vector6;Vector 6;42;0;Create;True;0;0;False;0;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.WorldPosInputsNode;1442;11857.49,2313.494;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;1426;11675.37,2452.835;Float;False;Property;_TVAO_Bias;TVAO_Bias;27;0;Create;True;0;0;False;0;0;-0.06;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1492;-773.6031,-99.71544;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;21;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1139;-2415.514,2698.744;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;100;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1061;8281.057,3222.71;Float;False;Global;_lightsContour;_lightsContour;40;0;Create;True;0;0;False;0;0.1;-0.88;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1302;8522.792,708.0932;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1606;3523.814,73.74505;Float;False;NoiseBParralax;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1421;9336.68,3046.48;Float;False;AODepthData;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;220;-1243.201,1762.498;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1491;-662.0945,-364.5508;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;21;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;975;20.64196,1232.298;Float;False;Property;_BuildingLighting;BuildingLighting;31;0;Create;True;0;0;False;0;0;8.03;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;1585;5346.525,881.3101;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;974;254.6166,1222.269;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1136;-2314.969,2365.698;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;219;-827.278,1733.958;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;95;170.8992,3874.997;Float;False;Constant;_Float14;Float 14;9;0;Create;True;0;0;False;0;0.001;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1427;11924.73,2557.237;Float;False;1421;AODepthData;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1539;11492.98,1516.303;Float;False;1536;advertising;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TextureArrayNode;1028;-446.25,-172.3034;Float;True;Property;_SurfaceNormalArray;SurfaceNormalArray;27;0;Create;True;0;0;False;0;None;0;Instance;712;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;29;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1453;4553.774,2803.138;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.4;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;755;4320.393,1081.766;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;154;4542.922,2528.146;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1613;10943.99,2421.345;Float;False;1606;NoiseBParralax;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;94;-110.9004,3742.198;Float;False;Global;_CSLights;_CSLights;7;0;Create;True;0;0;False;0;0;0.3217129;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1063;8564.273,3269.657;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TextureArrayNode;1487;-479.968,-409.1671;Float;True;Property;_TextureArray0;Texture Array 0;28;0;Create;True;0;0;False;0;None;0;Instance;712;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;29;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1430;12015.72,2432.26;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;340;-2092.792,3213.103;Float;False;Property;_LightOnThershold;LightOnThershold;25;0;Create;True;0;0;False;0;0;0.04;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1654;11188,2637.656;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;1557;10772.96,2214.476;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1064;8539.203,3058.217;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SmoothstepOpNode;1303;8678.104,999.629;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;553;246.7159,3617.791;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.001;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;655;537.416,3448.003;Float;False;632;enterier;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1157;4492.496,1235.874;Float;False;lightingNormal;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;337;-1592.499,3198.303;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;1493;-140.8806,-406.1241;Float;False;FLOAT4;4;0;FLOAT;1;False;1;FLOAT;1;False;2;FLOAT;1;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;498;442.2127,1196.055;Float;False;LightShape;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;1596;11784.91,1545.997;Float;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1741;-6462.977,-1088.552;Float;False;VNormalMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DistanceOpNode;1065;8785.793,3190.255;Float;True;2;0;FLOAT3;0,0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;217;535.8955,3373.595;Float;False;Constant;_Float7;Float 7;26;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;1050;-98.89773,-164.0623;Float;False;FLOAT4;4;0;FLOAT;1;False;1;FLOAT;1;False;2;FLOAT;1;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleAddOpNode;96;499.699,3859.898;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;1583;7384.21,894.8878;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;1165;8557.053,481.1123;Float;False;Property;_Snowmoss;Snow/moss;39;0;Create;True;0;0;False;0;1,1,1,0;0.8014706,0.8014706,0.8014706,0.097;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;257;4805.55,2801.015;Float;False;Property;_MinSpecular;MinSpecular;3;0;Create;True;0;0;False;0;0;0.009;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCCompareWithRange;1568;11038.78,2213.013;Float;False;5;0;FLOAT;0;False;1;FLOAT;1.5;False;2;FLOAT;1.98;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;221;325.9379,3430.998;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;1425;12226.69,2447.076;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;1611;11234.28,2451.118;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;1306;8535.13,850.0058;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;224;378.3981,4028.197;Float;False;Property;_LightVariation;LightVariation;9;0;Create;True;0;0;False;0;0;0.924;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;550;466.0161,3649.69;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;462;4827.247,2597.997;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1433;12243.48,2321.831;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.05;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;164;605.1806,3531.287;Float;False;Property;_LightStrenght;LightStrenght;8;0;Create;True;0;0;False;0;0;3.3;0;100;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1158;7429.279,2486.724;Float;False;1157;lightingNormal;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1429;12440.77,2382.251;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;1618;11400.67,2329.235;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1301;8853.666,790.5362;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;499;3356.606,1288.793;Float;False;498;LightShape;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;556;719.5157,3660.491;Float;False;GlobalLightsOnOFff;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1591;11369.9,2170.483;Float;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1437;-5434.443,72.32832;Float;False;buildingLightsColorsID;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1066;9041.432,3174.208;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;93;717.1996,3794.299;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1743;11476.18,2391.271;Float;False;Property;_test;test;58;0;Create;True;0;0;False;0;0;0.01;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;256;5127.201,2588.599;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1488;76.89852,-234.9406;Float;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;1443;12489.97,2663.18;Float;False;Property;_TVAO_LowValue;TVAO_LowValue;51;0;Create;True;0;0;False;0;0;0.35;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1742;11433.88,2465.371;Float;False;1741;VNormalMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1444;12356.2,2565.872;Float;False;Property;_TVAO_High_Value;TVAO_High_Value;50;0;Create;True;0;0;False;0;0;3.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;216;791.0973,3377.296;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;222;801.9993,3941.298;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;1620;11564.03,2175.028;Float;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;1187;9110.755,912.8214;Float;False;Property;_Use_Snow_Moss_Dirt;Use_Snow_Moss_Dirt;42;0;Create;True;0;0;False;0;0;0;0;True;;Toggle;2;Key0;Key1;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WireNode;665;5357.296,2620.077;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;1435;12647.8,2243.264;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;10;False;3;FLOAT;0.2;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;558;3573.615,1563.193;Float;False;556;GlobalLightsOnOFff;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1074;7650.825,2572.13;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1454;5232.625,2470.763;Float;False;526;offsetDirtMap;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;214;999.3965,3466.596;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StepOpNode;1739;11634.38,2308.979;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.UnpackScaleNormalNode;1049;358.2076,-245.8726;Float;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0.5;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;525;2386.013,1153.675;Float;False;1437;buildingLightsColorsID;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;225;1060.197,3793.798;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;658;3397.125,1474.707;Float;False;577;RoofNormalThreshold;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;534;3624.096,1329.341;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;404;-2418.095,3376.342;Float;False;Constant;_Vector0;Vector 0;50;0;Create;True;0;0;False;0;-0.05,-0.05,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ComponentMaskNode;1089;7809.679,2568.88;Float;False;True;False;False;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1035;683.0795,-252.7763;Float;False;surfaceNormal;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;559;3866.554,1594.882;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;1424;11701.9,1998.42;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;406;-2507.181,3588.604;Float;False;Constant;_Vector2;Vector 2;50;0;Create;True;0;0;False;0;0.05,0.05,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;708;5503.874,2595.235;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;215;1152.2,3650.395;Float;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;549;3891.168,1491.944;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1584;11013.7,1232.067;Float;False;OutAfterSnow;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1159;-1063.467,71.79207;Float;False;BorderMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1147;-1942.833,3901.199;Float;False;Property;_NormalWindow;NormalWindow;38;0;Create;True;0;0;False;0;2;0.99;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1752;1059.33,931.7979;Float;False;glossFeed;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1143;-2199.632,3834.699;Float;False;1142;NormalShape;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GlobalArrayNode;1439;2721.04,1136.836;Float;False;_buildingLightsColors;0;10;1;False;False;0;1;False;Object;-1;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;1094;8105.007,2940.937;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.LerpOp;1740;11742.09,2174.542;Float;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1101;8446.913,2962.543;Float;False;Global;_CSReLightDistance;_CSReLightDistance;48;0;Create;True;0;0;False;0;0;0.006396316;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;1073;7562.335,2749.436;Float;False;Global;_reLightColor;_reLightColor;40;0;Create;True;0;0;False;0;0.8676471,0.7320442,0.4402033,0;0.9338235,0.7337997,0.4806445,0.703;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RelayNode;1590;11852.12,2087.874;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;401;-1948.37,3353.798;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;1037;1783.884,2003.643;Float;False;1035;surfaceNormal;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;1753;8550.55,1440.793;Float;False;1752;glossFeed;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DistanceOpNode;1092;8436.252,2872.291;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;302;3076.082,1071.175;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;1083;8084.287,2563.359;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1422;11564.75,1347.277;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1755;8596.43,1545.027;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1716;1863.994,2149.033;Float;False;1159;BorderMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;557;4074.269,1505.841;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1148;-1790.633,3684.601;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,1;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;946;7195.294,1527.435;Float;False;3;0;FLOAT;0.1;False;1;FLOAT;111;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;839;2464.936,3121.907;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;1501;-1142.643,2124.452;Float;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;1616;11776.6,1421.156;Float;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1097;8703.672,2884.82;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1100;9532.188,2591.822;Float;False;Property;_Float23;Float 23;30;0;Create;True;0;0;False;0;0.32;0.131;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1502;-912.6541,2101.392;Float;False;frontBlind;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendNormalsNode;1144;-1640.932,3517.399;Float;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;1754;8859.558,1489.082;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1581;12115,1494.498;Float;False;3;0;COLOR;1,1,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;1715;2109.694,1840.932;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;941;5126.775,1190.637;Float;False;Constant;_Float11;Float 11;37;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CeilOpNode;1526;-3896.885,-333.3603;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;1082;8267.921,2574.643;Float;False;2;0;FLOAT;0;False;1;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;299;4250.673,1476.198;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;1099;9555.172,2491.354;Float;False;Global;_CSReLight;_CSReLight;45;0;Create;True;0;0;False;0;2;0.3217129;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1072;7917.919,2823.517;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendNormalsNode;1036;2288.588,1779.12;Float;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;1494;9164.787,1513.452;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;1096;8940.262,2885.64;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1084;8460.174,2593.1;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1521;-3654.145,-321.4653;Float;False;blindsFront;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;407;-1360.44,3425.236;Float;False;normalWindows;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;940;5497.949,1266.69;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;1505;142.7777,-65.4841;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;1637;9899.184,2535.24;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1504;10978.3,2030.731;Float;False;1502;frontBlind;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1160;2606.679,2349.501;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1087;9191.04,2769.498;Float;False;4;4;0;COLOR;1,1,1,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;1503;11289.5,1975.969;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;414;2665.471,2101.585;Float;False;407;normalWindows;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RelayNode;858;8503.2,1622.882;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1524;11057.46,1871.614;Float;False;1521;blindsFront;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1056;389.3644,-54.58792;Float;False;MetallicVar;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1634;10164.73,2503.5;Float;False;triggerLights;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;758;-2731.471,3448.41;Float;False;noise;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1677;3055.145,1831.995;Float;False;3;0;FLOAT3;0,0,1;False;1;FLOAT3;0,0,1;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;1638;11642.26,1799.047;Float;False;1634;triggerLights;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1548;11270.28,1618.699;Float;False;Property;_ShopSignsLight;ShopSignsLight;53;0;Create;True;0;0;False;0;0;6;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;660;6048.324,661.4084;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;1523;11506.76,1854.937;Float;False;False;5;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1598;12271.6,2207.509;Float;False;758;noise;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1528;6527.326,1635.318;Float;False;707;occlusionPure;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;156;5571.585,604.2471;Float;False;Property;_Smoothness;Smoothness;2;0;Create;True;0;0;False;0;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;944;6633.423,1787.972;Float;False;Constant;_Float13;Float 13;37;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;412;3297.547,1918.938;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,1;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;1057;6060.857,503.9041;Float;False;1056;MetallicVar;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1075;9460.936,2723.519;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;943;6869.398,1611.629;Float;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;859;6135.095,1905.467;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;1020;6405.149,561.7805;Float;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1582;11886.75,1742.36;Float;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;1587;12126.89,1871.995;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1597;12404.94,2035.792;Float;False;2;2;0;FLOAT;0.05;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1635;10130.45,2235.182;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RelayNode;1300;7117.482,1901.402;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RelayNode;1176;7425.659,1661.835;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1599;12600.28,1978.943;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1589;12123.4,1993.236;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1542;12121.16,1757.954;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;1736;12808.15,1954.723;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1576;12122.76,1619.548;Float;False;3;0;FLOAT3;0,0,1;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;1588;12133.49,2115.366;Float;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;1737;12806.64,2037.562;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1434;12617.44,1877.611;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1670;12908.36,3264.153;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1508;-1469.367,2137.143;Float;False;1506;GlassColors;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1401;-1566.933,-371.4192;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;1,1,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;242;-3885.815,5335.5;Float;False;Constant;_Float10;Float 10;30;0;Create;True;0;0;False;0;0.001;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;1402;-1983.483,-297.5916;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;1663;7170.308,1648.764;Float;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1025;-2097.194,706.9227;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;152;-3215.914,-433.1069;Float;False;Property;_flooroffset;flooroffset;5;0;Create;True;0;0;False;0;0;0;0;16;0;1;FLOAT;0
Node;AmplifyShaderEditor.GlobalArrayNode;1456;-2279.212,515.9717;Float;False;_textureScales;0;11;0;False;False;0;1;False;Object;-1;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DdyOpNode;1261;-1248.404,729.3022;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1463;-1142.451,1082.39;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;769;-6042.129,-607.3271;Float;False;ColorInterpolate;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;1243;12981.58,1716.727;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;992;1054.553,3112.874;Float;False;TransparencyWindows;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;239;-3394.016,5310.398;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;991;6830.545,1350.87;Float;False;769;ColorInterpolate;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;237;-3161.013,5148.701;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DdxOpNode;1262;-1259.801,651.1655;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1018;379.8162,938.8357;Float;False;Smoothness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;238;-3526.113,5177.198;Float;False;Property;_MinInteriourSmoothnes;MinInteriourSmoothnes;11;0;Create;True;0;0;False;0;0;0;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1109;9697.399,1689.073;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;146;-3721.807,4923.699;Float;False;Property;_InteriourBlur;InteriourBlur;10;0;Create;True;0;0;False;0;0;0.09;0;16;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1112;8722.47,1780.38;Float;False;Property;_FresnelPow;FresnelPow;33;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1605;3909.446,-684.0372;Float;False;Property;_ShopDensity;ShopDensity;55;0;Create;True;0;0;False;0;0;0.974;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;997;7584.674,2046.989;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;1,1,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;240;-3960.411,5263.006;Float;False;Property;_SmoothCurtains;SmoothCurtains;13;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1272;-2597.25,1140.462;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;269;1430.03,683.4422;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1473;-3708.186,3475.658;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.0078125,0.015625;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;1111;8806.145,1866.828;Float;False;Property;_FresnelScale;FresnelScale;32;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1256;3575.872,-1020.336;Float;False;Property;_Windowrefraction;Windowrefraction;46;0;Create;True;0;0;False;0;0.15;0.54;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1312;-7055.389,-759.2324;Float;False;Property;_Float8;Float 8;49;0;Create;True;0;0;False;0;10;13.04;10;20;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1110;8900.733,1740.888;Float;False;Property;_FresnelBias;FresnelBias;34;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;1604;4051.847,-580.0377;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;1170;-4407.787,5200.426;Float;False;Constant;_Color1;Color 1;53;0;Create;True;0;0;False;0;1,1,1,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;1017;-8.449407,901.1985;Float;False;False;False;False;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;1311;-4796.22,79.12381;Float;False;Property;_ParallaxSteps;ParallaxSteps;48;0;Create;True;0;0;True;0;0;23;0;1;INT;0
Node;AmplifyShaderEditor.StaticSwitch;1118;9740.632,3048.974;Float;False;Property;_CSCAPE_RE_LIGHT;CSCAPE_RE_LIGHT;35;0;Create;True;0;0;False;0;0;0;0;True;;Toggle;2;Key0;Key1;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1748;13392.76,2163.507;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;1712;9537.337,1735.829;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1507;-1211.453,1921.466;Float;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FresnelNode;1108;9104.102,1680.755;Float;False;Standard;WorldNormal;ViewDir;False;5;0;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;1;FLOAT;5.8;False;2;FLOAT;1.68;False;3;FLOAT;1.22;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;649;667.7453,1081.621;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;999;7162.417,1398.35;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;701;3927.733,-1043.155;Float;False;Property;_Float17;Float 17;4;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1255;3885.527,-1270.388;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1252;4062.055,-1215.258;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1151;-3544.099,3274.95;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.0078125,0.015625;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;1600;4138.958,-133.1194;Float;False;FLOAT4;4;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;1153;-2549.516,1800.199;Float;False;1152;blindsOpenSource;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1366;-2961.095,-1375.561;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1683;12373.86,1794.021;Float;False;1682;NormalDistanceBlendWindows;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1152;807.2097,1785.729;Float;False;blindsOpenSource;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;902;-6935.653,-952.1046;Float;False;Constant;_Float5;Float 5;37;0;Create;True;0;0;False;0;1E-05;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;960;-2087.045,1209.12;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1717;-5370.831,272.3365;Float;False;UpperFloorsColorize;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;1248;12984.89,2014.44;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;964;-2332.927,1336.939;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;998;7300.663,1377.585;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1668;12771.49,1769.711;Float;False;3;0;FLOAT3;0,0,1;False;1;FLOAT3;0,0,1;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;1412;-2754.931,1005.062;Float;False;Property;_surfaceArraySize;surfaceArraySize;60;0;Create;True;0;0;False;0;0;20;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;709;5201.25,2748.505;Float;False;707;occlusionPure;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;1413;-2463.315,960.8622;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;863;-3073.672,725.5336;Float;False;861;maskLowFloor;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;1254;3646.956,-1302.682;Float;False;True;True;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode;1247;12982.43,2096.168;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1305;8127.734,848.6059;Float;False;879;pureAO;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;1244;12979.92,1801.351;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RelayNode;1246;12983.4,1939.438;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1506;721.9152,770.9485;Float;False;GlassColors;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;1026;-2348.948,672.425;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1500;-193.3252,3512.484;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;871;-154.3749,4064.899;Float;False;Property;_Float35534543;Float35534543;21;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;1071;7479.396,1740.436;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;879;1122.275,1888.078;Float;False;pureAO;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1167;4039.572,1273.139;Float;False;XWorldNormal;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;234;-3426.806,5070.903;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GlobalArrayNode;1460;-2267.114,928.1288;Float;False;_textureScales;0;11;0;False;False;0;1;False;Object;-1;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;1245;12983.08,1869.579;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SurfaceDepthNode;963;-2653.791,1317.389;Float;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;645;-3059.453,1885.407;Float;False;639;VCoord;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;1636;9998.39,2706.44;Float;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1749;13055.76,2257.507;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1750;12502.76,2099.507;Float;False;1506;GlassColors;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GlobalArrayNode;1462;-1485.029,1030.99;Float;False;_textureScales;0;11;0;False;False;0;1;False;Instance;1460;4;0;INT;0;False;2;INT;0;False;1;INT;0;False;3;INT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1253;3361.406,-1329.862;Float;False;407;normalWindows;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;1664;6826.651,1787.755;Float;False;992;TransparencyWindows;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1631;3640.847,-68.50219;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;20;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;241;-3653.115,5326.903;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;1113;9375.933,1686.666;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;13967.79,1900.415;Float;False;True;3;Float;ASEMaterialInspector;500;0;StandardSpecular;CScape/CSBuildingShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;False;True;True;False;True;True;False;False;True;True;False;False;True;True;True;True;True;0;False;-1;False;1;False;-1;201;False;-1;255;False;-1;3;False;-1;2;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;4;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;1;False;-1;1;False;-1;0;False;0.17;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;500;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;1280;6;923;0
WireConnection;467;0;1280;114
WireConnection;639;0;22;2
WireConnection;466;0;1280;113
WireConnection;638;0;22;1
WireConnection;966;0;488;0
WireConnection;965;0;469;0
WireConnection;1365;0;641;0
WireConnection;1367;0;640;0
WireConnection;319;0;1365;0
WireConnection;319;1;965;0
WireConnection;328;0;1367;0
WireConnection;328;1;966;0
WireConnection;322;0;319;0
WireConnection;329;0;328;0
WireConnection;331;1;966;0
WireConnection;325;1;965;0
WireConnection;932;0;322;0
WireConnection;932;1;325;0
WireConnection;933;0;329;0
WireConnection;933;1;331;0
WireConnection;1449;0;1299;2
WireConnection;1640;1;1280;37
WireConnection;931;0;932;0
WireConnection;931;1;933;0
WireConnection;953;0;931;0
WireConnection;1642;0;1299;2
WireConnection;1448;0;1640;0
WireConnection;1448;2;1449;0
WireConnection;1641;0;1448;0
WireConnection;1641;2;1642;0
WireConnection;939;0;953;0
WireConnection;939;1;937;1
WireConnection;954;0;939;0
WireConnection;746;0;1641;0
WireConnection;928;0;1280;73
WireConnection;432;0;644;0
WireConnection;432;1;433;0
WireConnection;432;2;434;0
WireConnection;955;0;954;0
WireConnection;930;0;747;0
WireConnection;930;1;929;0
WireConnection;930;2;955;0
WireConnection;438;0;432;0
WireConnection;547;0;930;0
WireConnection;547;1;747;0
WireConnection;547;2;438;0
WireConnection;363;0;547;0
WireConnection;876;0;1280;72
WireConnection;368;0;365;2
WireConnection;1213;0;937;1
WireConnection;575;0;363;0
WireConnection;577;0;368;0
WireConnection;861;0;432;0
WireConnection;1013;0;877;0
WireConnection;1013;1;575;0
WireConnection;1013;2;1213;0
WireConnection;875;0;1013;0
WireConnection;875;1;575;0
WireConnection;875;2;861;0
WireConnection;583;1;875;0
WireConnection;583;2;578;0
WireConnection;1293;0;1290;2
WireConnection;1287;0;1286;0
WireConnection;1289;1;583;0
WireConnection;1289;2;1293;0
WireConnection;637;0;22;0
WireConnection;1285;0;1289;0
WireConnection;1285;2;1287;0
WireConnection;866;0;642;0
WireConnection;1277;0;1285;0
WireConnection;1277;1;1274;0
WireConnection;1273;0;1274;0
WireConnection;1273;1;1274;0
WireConnection;873;0;866;0
WireConnection;1276;0;1277;0
WireConnection;1276;1;1274;0
WireConnection;1276;2;1273;0
WireConnection;1520;0;937;3
WireConnection;574;0;873;0
WireConnection;574;1;1276;0
WireConnection;990;0;116;0
WireConnection;1519;0;1520;0
WireConnection;947;0;873;0
WireConnection;947;1;951;0
WireConnection;947;2;116;0
WireConnection;947;3;108;0
WireConnection;947;4;950;0
WireConnection;947;6;1276;0
WireConnection;107;0;873;0
WireConnection;107;1;574;4
WireConnection;107;2;990;0
WireConnection;107;3;108;0
WireConnection;1316;0;1519;0
WireConnection;1117;1;947;0
WireConnection;1117;0;107;0
WireConnection;1317;0;1316;0
WireConnection;584;0;1117;0
WireConnection;1313;0;1317;0
WireConnection;1477;0;674;0
WireConnection;675;0;674;0
WireConnection;1644;7;1656;0
WireConnection;1644;8;1645;0
WireConnection;1298;0;1299;2
WireConnection;1476;0;675;0
WireConnection;1476;1;1477;0
WireConnection;1476;2;1478;0
WireConnection;957;0;873;0
WireConnection;956;0;873;0
WireConnection;1546;0;1534;0
WireConnection;1469;0;1299;2
WireConnection;75;6;951;0
WireConnection;75;0;901;0
WireConnection;75;1;1276;0
WireConnection;75;4;956;0
WireConnection;75;5;957;0
WireConnection;1655;0;1644;0
WireConnection;679;0;676;0
WireConnection;677;0;1476;0
WireConnection;523;0;1280;156
WireConnection;1297;0;1318;0
WireConnection;1297;1;1280;39
WireConnection;1297;2;1298;0
WireConnection;1436;0;523;0
WireConnection;1470;0;1730;0
WireConnection;1470;1;1297;0
WireConnection;1470;2;1469;0
WireConnection;681;0;677;0
WireConnection;682;1;679;0
WireConnection;1396;0;75;1
WireConnection;1399;0;75;4
WireConnection;1651;0;1655;0
WireConnection;1535;0;1546;0
WireConnection;1400;0;1399;0
WireConnection;1553;0;1535;0
WireConnection;1553;3;1554;0
WireConnection;1646;0;1651;0
WireConnection;683;0;681;0
WireConnection;683;1;682;0
WireConnection;1438;0;1436;1
WireConnection;768;0;1470;0
WireConnection;1397;1;1396;0
WireConnection;748;0;1280;38
WireConnection;1000;0;1280;74
WireConnection;90;0;1400;0
WireConnection;1577;0;1553;0
WireConnection;1475;0;1150;0
WireConnection;685;0;683;0
WireConnection;1455;0;150;0
WireConnection;1648;0;1397;6
WireConnection;1672;0;1646;0
WireConnection;1474;0;1150;0
WireConnection;1719;0;1714;0
WireConnection;1650;0;1648;2
WireConnection;1650;1;1672;0
WireConnection;1695;0;767;0
WireConnection;1697;0;770;0
WireConnection;1471;0;1475;0
WireConnection;1471;1;1474;0
WireConnection;1471;2;1472;0
WireConnection;201;0;90;0
WireConnection;687;0;685;0
WireConnection;687;1;679;0
WireConnection;149;0;584;0
WireConnection;149;1;1455;0
WireConnection;1579;0;1578;0
WireConnection;1698;0;149;0
WireConnection;1698;1;1697;0
WireConnection;688;0;681;0
WireConnection;688;1;687;0
WireConnection;1696;0;149;0
WireConnection;1696;1;1695;0
WireConnection;1409;0;1003;0
WireConnection;1724;0;1719;0
WireConnection;91;1;1471;0
WireConnection;1649;0;1648;0
WireConnection;1649;1;1648;1
WireConnection;1649;2;1650;0
WireConnection;1735;0;1719;0
WireConnection;1369;0;1649;0
WireConnection;1575;1;1579;0
WireConnection;1734;0;1719;0
WireConnection;243;0;653;0
WireConnection;243;1;244;0
WireConnection;1681;0;1679;0
WireConnection;1713;0;1719;0
WireConnection;691;0;688;0
WireConnection;1410;0;1409;0
WireConnection;1721;0;1724;0
WireConnection;712;6;1284;0
WireConnection;712;0;1696;0
WireConnection;712;1;767;0
WireConnection;840;0;91;1
WireConnection;840;1;91;2
WireConnection;840;2;91;3
WireConnection;840;3;91;4
WireConnection;342;6;1284;0
WireConnection;342;0;1698;0
WireConnection;342;1;770;0
WireConnection;1720;0;1713;0
WireConnection;1720;1;1721;0
WireConnection;1720;2;1648;0
WireConnection;1549;0;1575;0
WireConnection;1758;0;243;0
WireConnection;541;0;1280;157
WireConnection;693;0;690;0
WireConnection;693;1;691;0
WireConnection;1732;0;1735;0
WireConnection;526;0;1280;155
WireConnection;1731;0;1734;0
WireConnection;1372;0;1649;0
WireConnection;843;0;840;0
WireConnection;1680;0;1681;0
WireConnection;1680;2;1678;0
WireConnection;74;0;1369;0
WireConnection;74;2;342;0
WireConnection;74;3;712;0
WireConnection;74;4;1410;0
WireConnection;1703;0;1372;2
WireConnection;1703;1;1700;0
WireConnection;1745;0;1720;0
WireConnection;1745;1;74;0
WireConnection;646;0;1758;1
WireConnection;1405;0;1403;0
WireConnection;1733;0;1731;0
WireConnection;1733;1;1732;0
WireConnection;1733;2;1648;0
WireConnection;1747;0;1720;0
WireConnection;696;0;690;0
WireConnection;696;1;693;0
WireConnection;532;0;531;0
WireConnection;532;1;527;0
WireConnection;1682;0;1680;0
WireConnection;1129;0;746;0
WireConnection;1129;4;1131;0
WireConnection;560;0;532;0
WireConnection;560;1;561;0
WireConnection;533;0;529;0
WireConnection;1572;0;1550;0
WireConnection;1690;0;759;0
WireConnection;773;0;1280;115
WireConnection;1701;0;1733;0
WireConnection;1701;1;74;0
WireConnection;1404;0;1405;0
WireConnection;1132;0;1129;0
WireConnection;1704;0;1703;0
WireConnection;698;0;688;0
WireConnection;698;1;696;0
WireConnection;698;2;695;0
WireConnection;1744;0;1745;0
WireConnection;1744;1;1720;0
WireConnection;1744;2;1747;0
WireConnection;700;0;698;0
WireConnection;1628;0;1572;0
WireConnection;1628;1;1572;1
WireConnection;1628;2;1572;2
WireConnection;1509;0;1469;0
WireConnection;765;0;703;0
WireConnection;765;1;1756;0
WireConnection;765;2;1757;0
WireConnection;528;0;560;0
WireConnection;528;1;533;0
WireConnection;1691;0;1690;0
WireConnection;1699;0;1744;0
WireConnection;1699;1;74;0
WireConnection;1699;2;1704;0
WireConnection;1517;0;1518;0
WireConnection;1729;0;1701;0
WireConnection;1729;1;74;0
WireConnection;1729;2;1372;2
WireConnection;138;0;1404;0
WireConnection;209;0;91;1
WireConnection;209;1;91;2
WireConnection;1662;0;1572;1
WireConnection;1662;1;1572;3
WireConnection;1639;0;1628;0
WireConnection;210;0;209;0
WireConnection;210;1;91;3
WireConnection;1728;0;1699;0
WireConnection;1728;1;1729;0
WireConnection;1728;2;1700;0
WireConnection;777;0;775;0
WireConnection;1123;0;528;0
WireConnection;1123;1;1133;0
WireConnection;1279;0;1274;0
WireConnection;1516;0;74;0
WireConnection;1516;1;1517;0
WireConnection;757;0;700;0
WireConnection;757;1;1691;0
WireConnection;757;2;765;0
WireConnection;1762;0;1299;2
WireConnection;121;0;138;0
WireConnection;1705;0;1704;0
WireConnection;1688;0;757;0
WireConnection;710;0;652;0
WireConnection;710;1;711;0
WireConnection;251;0;1123;1
WireConnection;251;1;250;0
WireConnection;776;0;777;0
WireConnection;1658;0;1662;0
WireConnection;1513;0;1516;0
WireConnection;1513;1;1728;0
WireConnection;1513;2;1511;0
WireConnection;131;0;121;0
WireConnection;131;1;210;0
WireConnection;1763;0;1762;0
WireConnection;1278;0;1285;0
WireConnection;1278;2;1279;0
WireConnection;1630;0;1639;0
WireConnection;874;0;710;0
WireConnection;137;0;135;0
WireConnection;137;1;136;0
WireConnection;1510;0;1513;0
WireConnection;1657;0;1658;0
WireConnection;1222;0;1220;0
WireConnection;464;0;1280;112
WireConnection;165;6;951;0
WireConnection;165;0;651;0
WireConnection;165;1;1278;0
WireConnection;1627;0;1630;0
WireConnection;632;0;1688;0
WireConnection;252;0;251;0
WireConnection;436;0;131;0
WireConnection;436;1;776;0
WireConnection;1623;0;1627;0
WireConnection;1623;1;1626;0
WireConnection;1219;1;1222;0
WireConnection;1766;0;1231;0
WireConnection;1766;1;1764;0
WireConnection;246;0;1510;0
WireConnection;246;1;252;0
WireConnection;423;0;874;0
WireConnection;423;1;165;4
WireConnection;1659;0;1657;0
WireConnection;130;0;436;0
WireConnection;130;1;135;0
WireConnection;130;2;137;0
WireConnection;129;0;423;0
WireConnection;129;1;634;0
WireConnection;129;2;130;0
WireConnection;1760;0;1766;0
WireConnection;1048;1;165;2
WireConnection;1048;3;165;1
WireConnection;1710;0;1219;4
WireConnection;1497;0;1495;0
WireConnection;1625;0;1623;0
WireConnection;120;0;246;0
WireConnection;1660;0;1659;0
WireConnection;1660;1;1661;0
WireConnection;1569;6;1530;0
WireConnection;1569;0;1553;0
WireConnection;1569;1;1660;0
WireConnection;230;0;165;4
WireConnection;230;1;231;0
WireConnection;1711;0;1710;0
WireConnection;1711;2;1760;0
WireConnection;1621;0;1623;0
WireConnection;1041;0;1048;0
WireConnection;1236;0;937;4
WireConnection;197;0;120;0
WireConnection;197;1;198;0
WireConnection;633;0;129;0
WireConnection;1622;0;1625;0
WireConnection;1498;0;1497;0
WireConnection;1080;0;1067;2
WireConnection;1224;0;1219;0
WireConnection;1224;1;197;0
WireConnection;1224;2;1711;0
WireConnection;707;0;230;0
WireConnection;1142;0;1041;0
WireConnection;1193;0;252;0
WireConnection;1601;0;1569;4
WireConnection;1022;0;633;0
WireConnection;1022;1;650;0
WireConnection;1570;0;1621;0
WireConnection;1570;1;1622;0
WireConnection;1570;2;1569;1
WireConnection;1632;0;1569;4
WireConnection;1499;0;1498;0
WireConnection;1499;1;1498;1
WireConnection;1499;2;1498;2
WireConnection;1140;0;1137;2
WireConnection;1586;3;1601;0
WireConnection;1077;0;1080;0
WireConnection;1077;1;1070;0
WireConnection;1258;0;197;0
WireConnection;1258;1;1224;0
WireConnection;1258;2;1237;0
WireConnection;1068;0;1067;1
WireConnection;1068;1;1070;0
WireConnection;1603;0;1632;0
WireConnection;1603;1;1570;0
WireConnection;571;0;1280;116
WireConnection;1751;0;1022;0
WireConnection;1751;1;1499;0
WireConnection;1138;0;1140;0
WireConnection;1069;0;1067;3
WireConnection;1069;1;1070;0
WireConnection;1059;0;1068;0
WireConnection;1169;0;1164;0
WireConnection;1307;0;1304;0
WireConnection;1652;0;571;0
WireConnection;1693;0;1489;0
WireConnection;1215;0;1216;0
WireConnection;1215;1;531;2
WireConnection;158;0;1258;0
WireConnection;158;1;1751;0
WireConnection;158;2;650;0
WireConnection;1081;0;1077;0
WireConnection;1195;0;1194;0
WireConnection;1629;0;1628;0
WireConnection;1468;0;1490;0
WireConnection;1141;0;1138;0
WireConnection;1555;0;1586;0
WireConnection;1555;1;1603;0
WireConnection;1555;2;1569;4
WireConnection;1058;0;1069;0
WireConnection;552;0;551;0
WireConnection;1420;0;1432;0
WireConnection;838;0;1280;0
WireConnection;554;0;552;0
WireConnection;554;1;555;0
WireConnection;1062;0;1059;0
WireConnection;1062;1;1058;0
WireConnection;1062;2;1081;0
WireConnection;260;0;158;0
WireConnection;260;1;230;0
WireConnection;1694;0;149;0
WireConnection;1694;1;1693;0
WireConnection;1536;0;1555;0
WireConnection;1692;0;149;0
WireConnection;1692;1;1468;0
WireConnection;1217;0;1215;0
WireConnection;1012;0;365;2
WireConnection;1492;0;1490;0
WireConnection;1139;0;1141;0
WireConnection;1302;0;1169;2
WireConnection;1302;1;1307;0
WireConnection;1302;2;1195;0
WireConnection;1606;0;1629;0
WireConnection;1421;0;1420;1
WireConnection;220;0;130;0
WireConnection;1491;0;1489;0
WireConnection;1585;0;260;0
WireConnection;974;0;1123;2
WireConnection;974;1;975;0
WireConnection;974;2;1217;0
WireConnection;1136;0;838;0
WireConnection;1136;1;1139;0
WireConnection;219;0;661;0
WireConnection;219;1;220;0
WireConnection;1028;6;1284;0
WireConnection;1028;0;1692;0
WireConnection;1028;1;1492;0
WireConnection;1453;0;465;0
WireConnection;755;0;1012;0
WireConnection;154;0;662;0
WireConnection;154;1;157;0
WireConnection;1063;0;1062;0
WireConnection;1063;1;1061;0
WireConnection;1487;6;1284;0
WireConnection;1487;0;1694;0
WireConnection;1487;1;1491;0
WireConnection;1430;0;1442;2
WireConnection;1430;1;1426;0
WireConnection;1654;0;1653;0
WireConnection;1064;0;1060;0
WireConnection;1064;1;1061;0
WireConnection;1303;0;1302;0
WireConnection;1303;1;1172;0
WireConnection;1303;2;1173;0
WireConnection;553;0;554;0
WireConnection;1157;0;755;0
WireConnection;337;0;209;0
WireConnection;337;1;1136;0
WireConnection;337;2;340;0
WireConnection;1493;1;1487;2
WireConnection;1493;3;1487;1
WireConnection;498;0;974;0
WireConnection;1596;0;1539;0
WireConnection;1741;0;1449;0
WireConnection;1065;0;1063;0
WireConnection;1065;1;1064;0
WireConnection;1050;1;1028;2
WireConnection;1050;3;1028;1
WireConnection;96;0;94;0
WireConnection;96;1;95;0
WireConnection;1583;0;1585;0
WireConnection;1568;0;1557;2
WireConnection;221;0;219;0
WireConnection;1425;0;1427;0
WireConnection;1425;1;1430;0
WireConnection;1611;0;1613;0
WireConnection;1611;1;1654;0
WireConnection;1306;0;1303;0
WireConnection;550;0;94;0
WireConnection;550;1;554;0
WireConnection;550;2;553;0
WireConnection;462;0;154;0
WireConnection;462;1;1453;0
WireConnection;1433;0;1442;2
WireConnection;1429;0;1433;0
WireConnection;1429;1;1425;0
WireConnection;1618;0;1611;0
WireConnection;1301;0;1583;0
WireConnection;1301;1;1165;0
WireConnection;1301;2;1306;0
WireConnection;556;0;550;0
WireConnection;1591;0;1568;0
WireConnection;1591;2;1596;0
WireConnection;1437;0;1436;0
WireConnection;1066;0;1065;0
WireConnection;93;0;337;0
WireConnection;93;1;94;0
WireConnection;93;2;96;0
WireConnection;256;0;462;0
WireConnection;256;1;257;0
WireConnection;1488;0;1493;0
WireConnection;1488;1;1050;0
WireConnection;1488;2;1372;1
WireConnection;216;0;217;0
WireConnection;216;1;655;0
WireConnection;216;2;221;0
WireConnection;222;0;91;0
WireConnection;222;1;210;0
WireConnection;222;2;224;0
WireConnection;1620;1;1591;0
WireConnection;1620;2;1618;0
WireConnection;1187;1;1583;0
WireConnection;1187;0;1301;0
WireConnection;665;0;256;0
WireConnection;1435;0;1429;0
WireConnection;1435;3;1443;0
WireConnection;1435;4;1444;0
WireConnection;1074;0;1066;0
WireConnection;1074;1;1158;0
WireConnection;214;0;216;0
WireConnection;214;1;164;0
WireConnection;1739;0;1742;0
WireConnection;1739;1;1743;0
WireConnection;1049;0;1488;0
WireConnection;225;0;222;0
WireConnection;225;1;93;0
WireConnection;534;0;499;0
WireConnection;534;1;165;2
WireConnection;1089;0;1074;0
WireConnection;1035;0;1049;0
WireConnection;559;0;558;0
WireConnection;1424;0;1435;0
WireConnection;708;0;665;0
WireConnection;708;1;1454;0
WireConnection;215;0;214;0
WireConnection;215;1;225;0
WireConnection;549;0;534;0
WireConnection;549;1;658;0
WireConnection;1584;0;1187;0
WireConnection;1159;0;1372;2
WireConnection;1752;0;1498;3
WireConnection;1439;0;525;0
WireConnection;1740;1;1620;0
WireConnection;1740;2;1739;0
WireConnection;1590;0;1740;0
WireConnection;401;0;404;0
WireConnection;401;1;406;0
WireConnection;401;2;91;4
WireConnection;1092;0;1067;0
WireConnection;1092;1;1094;0
WireConnection;302;0;260;0
WireConnection;302;1;1439;0
WireConnection;1083;0;1089;0
WireConnection;1422;0;1584;0
WireConnection;1422;1;1424;0
WireConnection;557;0;549;0
WireConnection;557;1;559;0
WireConnection;1148;0;1143;0
WireConnection;1148;2;1147;0
WireConnection;946;1;708;0
WireConnection;946;2;755;0
WireConnection;839;0;215;0
WireConnection;839;1;91;4
WireConnection;1501;2;130;0
WireConnection;1616;0;1539;0
WireConnection;1097;0;1092;0
WireConnection;1097;1;1101;0
WireConnection;1502;0;1501;0
WireConnection;1144;0;401;0
WireConnection;1144;1;1148;0
WireConnection;1754;0;946;0
WireConnection;1754;1;1753;0
WireConnection;1754;2;1755;0
WireConnection;1581;0;1616;0
WireConnection;1581;1;1422;0
WireConnection;1581;2;1590;0
WireConnection;1715;0;1037;0
WireConnection;1715;1;1041;0
WireConnection;1715;2;1716;0
WireConnection;1526;0;1520;0
WireConnection;1082;0;1083;0
WireConnection;299;0;839;0
WireConnection;299;1;302;0
WireConnection;299;2;557;0
WireConnection;1072;0;1073;4
WireConnection;1036;0;1041;0
WireConnection;1036;1;1715;0
WireConnection;1494;0;1754;0
WireConnection;1096;0;1097;0
WireConnection;1084;0;1082;0
WireConnection;1084;1;1072;0
WireConnection;1084;2;1581;0
WireConnection;1521;0;1526;0
WireConnection;407;0;1144;0
WireConnection;940;0;941;0
WireConnection;940;1;299;0
WireConnection;940;2;755;0
WireConnection;1505;0;1487;4
WireConnection;1505;1;1028;4
WireConnection;1505;2;1372;1
WireConnection;1637;0;1099;0
WireConnection;1637;1;1100;0
WireConnection;1087;0;1084;0
WireConnection;1087;1;1073;0
WireConnection;1087;2;1096;0
WireConnection;1087;3;1420;0
WireConnection;1503;0;1494;0
WireConnection;1503;2;1504;0
WireConnection;858;0;940;0
WireConnection;1056;0;1505;0
WireConnection;1634;0;1637;0
WireConnection;758;0;91;1
WireConnection;1677;1;1036;0
WireConnection;1677;2;1682;0
WireConnection;1523;1;1524;0
WireConnection;1523;3;1494;0
WireConnection;1523;4;1503;0
WireConnection;412;0;1677;0
WireConnection;412;1;414;0
WireConnection;412;2;1160;0
WireConnection;1075;0;858;0
WireConnection;1075;1;1087;0
WireConnection;943;0;944;0
WireConnection;943;1;1528;0
WireConnection;943;2;755;0
WireConnection;859;0;412;0
WireConnection;1020;0;1057;0
WireConnection;1020;1;156;0
WireConnection;1020;2;660;0
WireConnection;1582;0;1539;0
WireConnection;1582;1;1548;0
WireConnection;1582;2;1638;0
WireConnection;1587;1;1523;0
WireConnection;1587;2;1590;0
WireConnection;1597;1;1598;0
WireConnection;1635;0;858;0
WireConnection;1635;1;1075;0
WireConnection;1635;2;1637;0
WireConnection;1300;0;859;0
WireConnection;1176;0;943;0
WireConnection;1599;0;1587;0
WireConnection;1599;1;1597;0
WireConnection;1589;1;1020;0
WireConnection;1589;2;1590;0
WireConnection;1542;0;1582;0
WireConnection;1542;1;1635;0
WireConnection;1542;2;1590;0
WireConnection;1736;0;1599;0
WireConnection;1576;1;1300;0
WireConnection;1576;2;1590;0
WireConnection;1588;1;1176;0
WireConnection;1588;2;1590;0
WireConnection;1737;0;1589;0
WireConnection;1434;0;1542;0
WireConnection;1434;1;1424;0
WireConnection;1401;0;1397;6
WireConnection;1402;0;1397;6
WireConnection;1663;0;943;0
WireConnection;1663;2;1664;0
WireConnection;1025;0;1026;0
WireConnection;1456;0;767;0
WireConnection;1261;0;149;0
WireConnection;1463;1;1462;0
WireConnection;769;0;1280;40
WireConnection;1243;0;1581;0
WireConnection;992;0;221;0
WireConnection;239;0;1170;2
WireConnection;239;1;240;0
WireConnection;239;2;241;0
WireConnection;237;0;234;0
WireConnection;237;1;238;0
WireConnection;237;2;239;0
WireConnection;1262;0;149;0
WireConnection;1018;0;1017;0
WireConnection;1109;0;1494;0
WireConnection;1109;1;1712;0
WireConnection;269;0;1022;0
WireConnection;269;1;1499;0
WireConnection;269;2;649;0
WireConnection;1604;0;1572;2
WireConnection;1604;1;1605;0
WireConnection;1017;0;1510;0
WireConnection;1748;0;1246;0
WireConnection;1748;1;1750;0
WireConnection;1748;2;1749;0
WireConnection;1712;0;1113;0
WireConnection;1507;0;634;0
WireConnection;1507;1;1508;0
WireConnection;1108;0;1071;0
WireConnection;1108;1;1110;0
WireConnection;1108;2;1111;0
WireConnection;1108;3;1112;0
WireConnection;999;0;991;0
WireConnection;1255;0;1254;0
WireConnection;1255;1;1256;0
WireConnection;1252;0;700;0
WireConnection;1252;1;1255;0
WireConnection;1151;0;1150;0
WireConnection;1600;0;1570;0
WireConnection;1152;0;165;3
WireConnection;960;0;116;0
WireConnection;960;1;964;0
WireConnection;1717;0;1436;2
WireConnection;1248;0;1737;0
WireConnection;964;0;963;0
WireConnection;998;0;999;0
WireConnection;1668;1;1576;0
WireConnection;1668;2;1683;0
WireConnection;1413;0;767;0
WireConnection;1413;2;1412;0
WireConnection;1254;0;1253;0
WireConnection;1247;0;1588;0
WireConnection;1244;0;1576;0
WireConnection;1246;0;1736;0
WireConnection;1506;0;1497;0
WireConnection;1026;0;863;0
WireConnection;1500;0;551;0
WireConnection;1071;0;1300;0
WireConnection;879;0;165;4
WireConnection;1167;0;365;2
WireConnection;234;0;146;0
WireConnection;234;1;1170;1
WireConnection;1460;0;1413;0
WireConnection;1245;0;1434;0
WireConnection;1462;0;770;0
WireConnection;1631;0;1572;0
WireConnection;241;0;240;0
WireConnection;241;1;242;0
WireConnection;1113;0;1108;0
WireConnection;0;0;1243;0
WireConnection;0;1;1244;0
WireConnection;0;2;1245;0
WireConnection;0;3;1246;0
WireConnection;0;4;1248;0
WireConnection;0;5;1247;0
ASEEND*/
//CHKSM=244A2A30070046706D0534BF719214C195AD7E94