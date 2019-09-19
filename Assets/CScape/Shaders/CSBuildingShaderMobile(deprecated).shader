// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CScape/Deprecated_CSBuildingShaderMobile"
{
	Properties
	{
		_MinSmoothness("MinSmoothness", Range( 0 , 1)) = 0
		_Smoothness("Smoothness", Range( 0 , 1)) = 0
		_MinSpecular("MinSpecular", Range( 0 , 1)) = 0
		_DepthScale("DepthScale", Range( 0 , 1)) = 0.12
		_GlassColor("GlassColor", Color) = (0,0,0,0)
		_Mettalic("Mettalic", Range( 0 , 1)) = 0
		_LightStrenght("LightStrenght", Range( 0 , 100)) = 0
		_LightVariation("LightVariation", Range( 0 , 1)) = 0
		_InteriourBlur("InteriourBlur", Range( 0 , 16)) = 0
		_MinInteriourSmoothnes("MinInteriourSmoothnes", Range( 0 , 10)) = 0
		_BlindsOpen("BlindsOpen", Range( 0 , 2)) = 0
		_SmoothCurtains("SmoothCurtains", Range( 0 , 1)) = 0
		_DirtAmount("DirtAmount", Range( 1 , 5)) = 1
		_Occlusion("Occlusion", Range( 0 , 4)) = 0
		_BuildingLightness("BuildingLightness", Range( 0 , 1)) = 5
		_NormalTextureArray("NormalTextureArray", 2DArray ) = "" {}
		_ScaleTex1("ScaleTex1", Float) = 0
		_DirtTexture("DirtTexture", 2D) = "white" {}
		_NoiseTexture("NoiseTexture", 2D) = "white" {}
		_CurtainsMap("CurtainsMap", 2D) = "white" {}
		_WindowBorderCol1("WindowBorderCol1", Color) = (0.4485294,0.4485294,0.4485294,0)
		_WindowBorderCol2("WindowBorderCol2", Color) = (0.4485294,0.4485294,0.4485294,0)
		_BlindsArray("BlindsArray", 2DArray ) = "" {}
		_Interior2("Interior2", 2DArray ) = "" {}
		_SurfaceNormalArray("SurfaceNormalArray", 2DArray ) = "" {}
		_SurfaceAray("SurfaceAray", 2DArray ) = "" {}
		_Rooftoptex("Rooftoptex", 2D) = "white" {}
		_LightOnThershold("LightOnThershold", Float) = 0
		_WallsArray("WallsArray", 2DArray ) = "" {}
		_GlobalLightsOn("GlobalLightsOn", Range( 0 , 1)) = 0
		_Ambientocclusion("Ambient occlusion", Color) = (1,1,1,0)
		_AOPower("AO Power", Float) = 0
		_dESATURATE("dESATURATE", Float) = 0
		_MaskTexArray("MaskTexArray", 2DArray) = "white" {}
		_roofsCol1("roofsCol1", Color) = (0.2530277,0.5065386,0.5294118,0)
		_RoofsCol2("RoofsCol2", Color) = (0.2530277,0.5065386,0.5294118,0)
		_BuildingLighting("BuildingLighting", Float) = 0
		[HideInInspector] _tex4coord4( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		LOD 100
		Cull Back
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 4.5
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
			float2 uv_texcoord;
			float4 uv4_tex4coord4;
			float4 vertexColor : COLOR;
			float3 worldNormal;
			INTERNAL_DATA
			float3 viewDir;
			float3 worldPos;
		};

		uniform UNITY_DECLARE_TEX2DARRAY( _NormalTextureArray );
		uniform UNITY_DECLARE_TEX2DARRAY( _MaskTexArray );
		uniform float _DepthScale;
		uniform UNITY_DECLARE_TEX2DARRAY( _SurfaceNormalArray );
		uniform float _ScaleTex1;
		uniform sampler2D _NoiseTexture;
		uniform float4 _NoiseTexture_ST;
		uniform sampler2D _Rooftoptex;
		uniform float4 _roofsCol1;
		uniform float4 _RoofsCol2;
		uniform UNITY_DECLARE_TEX2DARRAY( _WallsArray );
		uniform UNITY_DECLARE_TEX2DARRAY( _SurfaceAray );
		uniform float4 _WindowBorderCol1;
		uniform float4 _WindowBorderCol2;
		uniform sampler2D _DirtTexture;
		uniform float _DirtAmount;
		uniform float _BuildingLightness;
		uniform UNITY_DECLARE_TEX2DARRAY( _BlindsArray );
		uniform UNITY_DECLARE_TEX2DARRAY( _Interior2 );
		uniform float _InteriourBlur;
		uniform sampler2D _CurtainsMap;
		uniform float _MinInteriourSmoothnes;
		uniform float _SmoothCurtains;
		uniform float _BlindsOpen;
		uniform float4 _GlassColor;
		uniform float _Occlusion;
		uniform float _dESATURATE;
		uniform float _LightStrenght;
		uniform float _LightVariation;
		uniform float _CSLights;
		uniform float _LightOnThershold;
		uniform float _BuildingLighting;
		uniform float _GlobalLightsOn;
		uniform float _Mettalic;
		uniform float _MinSpecular;
		uniform float _Smoothness;
		uniform float _MinSmoothness;
		uniform float4 _Ambientocclusion;
		uniform float _AOPower;

		#if defined(UNITY_COMPILER_HLSL2GLSL) || defined(SHADER_TARGET_SURFACE_ANALYSIS)
			#define ASE_SAMPLE_TEX2DARRAY_GRAD(tex,coord,dx,dy) UNITY_SAMPLE_TEX2DARRAY (tex,coord)
		#else
			#define ASE_SAMPLE_TEX2DARRAY_GRAD(tex,coord,dx,dy) tex.SampleGrad (sampler##tex,coord,dx,dy)
		#endif


		inline float3 expr5681( float2 X )
		{
			return float3(X * 2 - 1, -1);
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


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 UVcoord637 = i.uv_texcoord;
			float VCoord639 = i.uv_texcoord.y;
			float smoothstepResult432 = smoothstep( 2.0 , 2.0 , VCoord639);
			float maskLowFloor861 = smoothstepResult432;
			float2 appendResult868 = (float2(1.0 , maskLowFloor861));
			float2 appendResult867 = (float2(1.0 , 1.0));
			float2 temp_cast_0 = (maskLowFloor861).xx;
			float2 temp_cast_1 = (maskLowFloor861).xx;
			float4 break925 = i.uv4_tex4coord4;
			float temp_output_831_0 = ( break925.y * 10.0 );
			float temp_output_814_0 = trunc( temp_output_831_0 );
			float temp_output_816_0 = ( ( temp_output_831_0 - temp_output_814_0 ) * 10.0 );
			float temp_output_817_0 = trunc( temp_output_816_0 );
			float LowFloorID876 = ( temp_output_817_0 + 30.0 );
			float temp_output_720_0 = ( break925.x * 10.0 );
			float temp_output_918_0 = floor( temp_output_720_0 );
			float temp_output_723_0 = ( ( temp_output_720_0 - temp_output_918_0 ) * 10.0 );
			float temp_output_919_0 = floor( temp_output_723_0 );
			float faccade_ID746 = ( ( temp_output_918_0 * 10.0 ) + temp_output_919_0 );
			float temp_output_819_0 = ( ( temp_output_816_0 - temp_output_817_0 ) * 10.0 );
			float temp_output_820_0 = trunc( temp_output_819_0 );
			float temp_output_824_0 = ( ( temp_output_819_0 - temp_output_820_0 ) * 10.0 );
			float temp_output_825_0 = trunc( temp_output_824_0 );
			float DivisionID928 = ( ( temp_output_820_0 * 10.0 ) + temp_output_825_0 );
			float temp_output_439_0 = ( break925.z * 10.0 );
			float temp_output_489_0 = trunc( temp_output_439_0 );
			float temp_output_443_0 = ( ( temp_output_439_0 - temp_output_489_0 ) * 10.0 );
			float temp_output_490_0 = trunc( temp_output_443_0 );
			float TileX466 = ( temp_output_490_0 + 2.0 );
			float temp_output_965_0 = max( TileX466 , 0.0001 );
			float temp_output_322_0 = frac( ( VCoord639 / temp_output_965_0 ) );
			float temp_output_325_0 = ( 1.0 / temp_output_965_0 );
			float UCoord638 = i.uv_texcoord.x;
			float temp_output_449_0 = ( ( temp_output_443_0 - temp_output_490_0 ) * 10.0 );
			float temp_output_491_0 = trunc( temp_output_449_0 );
			float TileY467 = ( temp_output_491_0 + 2.0 );
			float temp_output_966_0 = max( TileY467 , 0.0001 );
			float temp_output_329_0 = frac( ( UCoord638 / temp_output_966_0 ) );
			float temp_output_331_0 = ( 1.0 / temp_output_966_0 );
			float clampResult953 = clamp( ( step( temp_output_322_0 , temp_output_325_0 ) + step( temp_output_329_0 , temp_output_331_0 ) ) , 0.0 , 1.0 );
			float temp_output_939_0 = ( clampResult953 - i.vertexColor.r );
			float lerpResult930 = lerp( faccade_ID746 , DivisionID928 , ( 1.0 - step( temp_output_939_0 , 0.2 ) ));
			float lerpResult547 = lerp( lerpResult930 , faccade_ID746 , ( 1.0 - smoothstepResult432 ));
			float temp_output_575_0 = ( lerpResult547 - 1.0 );
			float lerpResult1013 = lerp( LowFloorID876 , temp_output_575_0 , i.vertexColor.r);
			float lerpResult875 = lerp( lerpResult1013 , temp_output_575_0 , maskLowFloor861);
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float temp_output_368_0 = step( ase_worldNormal.y , 0.99 );
			float RoofNormalThreshold577 = temp_output_368_0;
			float lerpResult583 = lerp( 40.0 , lerpResult875 , RoofNormalThreshold577);
			float4 texArray574 = UNITY_SAMPLE_TEX2DARRAY(_MaskTexArray, float3(( ( ( UVcoord637 * ( appendResult868 + appendResult867 ) ) * float2( 0.5,0.5 ) ) - temp_cast_0 ), lerpResult583)  );
			float2 Offset107 = ( ( texArray574.w - 1 ) * ( i.viewDir.xy / i.viewDir.z ) * ( _DepthScale * 0.3 ) ) + ( ( ( UVcoord637 * ( appendResult868 + appendResult867 ) ) * float2( 0.5,0.5 ) ) - temp_cast_0 );
			float2 Parallax584 = Offset107;
			float4 texArray165 = UNITY_SAMPLE_TEX2DARRAY(_NormalTextureArray, float3(Parallax584, lerpResult583)  );
			float4 appendResult1048 = (float4(0.0 , texArray165.y , 0.0 , ( 1.0 - texArray165.x )));
			float2 temp_output_149_0 = ( Parallax584 * ( float2( 2,4 ) * ( ( 1.0 - maskLowFloor861 ) + 1.0 ) * _ScaleTex1 ) );
			float2 temp_cast_2 = (maskLowFloor861).xx;
			float2 temp_cast_3 = (maskLowFloor861).xx;
			float4 texArray75 = ASE_SAMPLE_TEX2DARRAY_GRAD(_MaskTexArray, float3(Parallax584, lerpResult583), ddx( ( ( ( UVcoord637 * ( appendResult868 + appendResult867 ) ) * float2( 0.5,0.5 ) ) - temp_cast_0 ) ), ddy( ( ( ( UVcoord637 * ( appendResult868 + appendResult867 ) ) * float2( 0.5,0.5 ) ) - temp_cast_0 ) ) );
			float4 break182 = texArray75;
			float3 appendResult183 = (float3(break182.x , break182.y , break182.z));
			float temp_output_726_0 = ( ( temp_output_723_0 - temp_output_919_0 ) * 10.0 );
			float temp_output_916_0 = floor( temp_output_726_0 );
			float temp_output_732_0 = ( ( temp_output_726_0 - temp_output_916_0 ) * 10.0 );
			float temp_output_917_0 = floor( temp_output_732_0 );
			float faccadeSuftace_ID2768 = temp_output_917_0;
			float faccadeSuftace_ID748 = temp_output_916_0;
			float3 layeredBlendVar1055 = appendResult183;
			float layeredBlend1055 = ( lerp( lerp( lerp( 0.0 , faccadeSuftace_ID2768 , layeredBlendVar1055.x ) , faccadeSuftace_ID748 , layeredBlendVar1055.y ) , 8.0 , layeredBlendVar1055.z ) );
			float4 texArray1028 = UNITY_SAMPLE_TEX2DARRAY(_SurfaceNormalArray, float3(temp_output_149_0, layeredBlend1055)  );
			float4 appendResult1050 = (float4(0.0 , ( 1.0 - texArray1028.y ) , 0.0 , texArray1028.x));
			float3 surfaceNormal1035 = UnpackScaleNormal( appendResult1050, 0.5 );
			float2 uv_NoiseTexture = i.uv_texcoord * _NoiseTexture_ST.xy + _NoiseTexture_ST.zw;
			float4 tex2DNode91 = tex2D( _NoiseTexture, uv_NoiseTexture );
			float3 lerpResult401 = lerp( float3(-0.04,-0.04,1) , float3(0.04,0.04,1) , tex2DNode91.a);
			float3 normalWindows407 = lerpResult401;
			float smoothstepResult576 = smoothstep( 0.0 , 0.1 , texArray75.w);
			float WindowsMask201 = ( 1.0 - smoothstepResult576 );
			float3 lerpResult412 = lerp( BlendNormals( UnpackNormal( appendResult1048 ) , surfaceNormal1035 ) , normalWindows407 , WindowsMask201);
			float temp_output_755_0 = step( abs( ase_worldNormal.y ) , 0.3 );
			float3 lerpResult945 = lerp( float3( 0,0,1 ) , lerpResult412 , temp_output_755_0);
			o.Normal = lerpResult945;
			float3 ase_worldPos = i.worldPos;
			float temp_output_1008_0 = abs( ( ( frac( ( ( ase_worldPos.y + 1.5 ) * 0.05 ) ) + -0.5 ) * 2.0 ) );
			float3 ase_vertex3Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
			float2 appendResult995 = (float2(ase_vertex3Pos.x , ase_vertex3Pos.z));
			float2 lerpResult994 = lerp( ( ( UVcoord637 + ( temp_output_1008_0 * 8.7 ) ) * 0.07 ) , ( appendResult995 * float2( 0.01,0.01 ) ) , temp_output_368_0);
			float4 tex2DNode367 = tex2D( _Rooftoptex, lerpResult994 );
			float GlassReflection464 = temp_output_489_0;
			float4 lerpResult979 = lerp( _roofsCol1 , _RoofsCol2 , ( GlassReflection464 * 0.2 ));
			float4 lerpResult751 = lerp( ( tex2DNode367 * temp_output_1008_0 ) , lerpResult979 , temp_output_368_0);
			float4 texArray342 = UNITY_SAMPLE_TEX2DARRAY(_WallsArray, float3(temp_output_149_0, faccadeSuftace_ID2768)  );
			float4 texArray712 = UNITY_SAMPLE_TEX2DARRAY(_SurfaceAray, float3(temp_output_149_0, faccadeSuftace_ID748)  );
			float temp_output_827_0 = ( ( temp_output_824_0 - temp_output_825_0 ) * 10.0 );
			float temp_output_836_0 = trunc( temp_output_827_0 );
			float windowColor1000 = ( temp_output_836_0 / 9.0 );
			float4 lerpResult1002 = lerp( _WindowBorderCol1 , _WindowBorderCol2 , windowColor1000);
			float3 layeredBlendVar74 = appendResult183;
			float4 layeredBlend74 = ( lerp( lerp( lerp( float4( 0,0,0,0 ) , texArray342 , layeredBlendVar74.x ) , texArray712 , layeredBlendVar74.y ) , lerpResult1002 , layeredBlendVar74.z ) );
			float temp_output_515_0 = ( break925.w * 10.0 );
			float temp_output_517_0 = trunc( temp_output_515_0 );
			float temp_output_505_0 = ( ( temp_output_515_0 - temp_output_517_0 ) * 10.0 );
			float temp_output_518_0 = trunc( temp_output_505_0 );
			float temp_output_516_0 = ( ( temp_output_505_0 - temp_output_518_0 ) * 10.0 );
			float temp_output_519_0 = trunc( temp_output_516_0 );
			float temp_output_508_0 = ( ( temp_output_516_0 - temp_output_519_0 ) * 10.0 );
			float temp_output_512_0 = trunc( temp_output_508_0 );
			float temp_output_538_0 = ( ( temp_output_508_0 - temp_output_512_0 ) * 10.0 );
			float temp_output_537_0 = trunc( temp_output_538_0 );
			float LightsScale541 = ( floor( temp_output_537_0 ) + 1.0 );
			float offsetDirtMap526 = ( temp_output_517_0 + 3.0 );
			float4 tex2DNode247 = tex2D( _DirtTexture, ( ( ( i.uv_texcoord * float2( 0.01,0.01 ) ) * LightsScale541 ) + ( offsetDirtMap526 * 0.1 ) ) );
			float clampResult252 = clamp( ( tex2DNode247.r * _DirtAmount ) , 0.0 , 1.0 );
			float temp_output_451_0 = ( ( temp_output_449_0 - temp_output_491_0 ) * 10.0 );
			float temp_output_483_0 = trunc( temp_output_451_0 );
			float blin773 = temp_output_483_0;
			float4 texArray874 = UNITY_SAMPLE_TEX2DARRAY(_BlindsArray, float3(( Parallax584 * 2.0 ), blin773)  );
			float4 tex2DNode232 = tex2D( _CurtainsMap, ( Parallax584 * 0.4 ) );
			float smoothstepResult239 = smoothstep( _SmoothCurtains , ( _SmoothCurtains + 0.001 ) , tex2DNode232.g);
			float lerpResult237 = lerp( ( _InteriourBlur + tex2DNode232.r ) , _MinInteriourSmoothnes , smoothstepResult239);
			float mipCurtains646 = lerpResult237;
			float2 X681 = frac( ( i.uv_texcoord * float2( -1,-1 ) ) );
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
			float internVariation843 = ( ( ( tex2DNode91.r + tex2DNode91.g ) + tex2DNode91.b ) * 0.33 );
			float4 texArray757 = UNITY_SAMPLE_TEX2DARRAY_LOD(_Interior2, float3(localexpr8700, ( internVariation843 * 6.0 )), mipCurtains646 );
			float4 enterier632 = ( texArray757 * ( mipCurtains646 * 0.5 ) );
			float temp_output_210_0 = ( ( tex2DNode91.r + tex2DNode91.g ) + tex2DNode91.b );
			float smoothstepResult130 = smoothstep( _BlindsOpen , ( _BlindsOpen + 0.003 ) , ( ( frac( ( 1.0 - VCoord639 ) ) * temp_output_210_0 ) * ( ( blin773 + 1.0 ) * 0.2 ) ));
			float4 lerpResult129 = lerp( ( texArray874 * texArray165.w ) , enterier632 , smoothstepResult130);
			float4 lerpResult269 = lerp( ( lerpResult129 * WindowsMask201 ) , _GlassColor , ( _GlassColor.a * WindowsMask201 ));
			float4 lerpResult158 = lerp( ( ( layeredBlend74 * clampResult252 ) * _BuildingLightness ) , lerpResult269 , WindowsMask201);
			float temp_output_230_0 = ( texArray165.w * _Occlusion );
			float4 temp_output_260_0 = ( lerpResult158 * temp_output_230_0 );
			float4 lerpResult366 = lerp( lerpResult751 , temp_output_260_0 , temp_output_755_0);
			float3 desaturateInitialColor887 = lerpResult366.rgb;
			float desaturateDot887 = dot( desaturateInitialColor887, float3( 0.299, 0.587, 0.114 ));
			float3 desaturateVar887 = lerp( desaturateInitialColor887, desaturateDot887.xxx, _dESATURATE );
			float temp_output_735_0 = ( ( temp_output_732_0 - temp_output_917_0 ) * 10.0 );
			float ColorInterpolate769 = ( floor( temp_output_735_0 ) + -1.0 );
			float3 temp_output_997_0 = ( desaturateVar887 * ( ( ColorInterpolate769 + 3.0 ) * 0.1 ) );
			o.Albedo = temp_output_997_0;
			float4 temp_cast_9 = (0.0).xxxx;
			float4 temp_cast_10 = (0.0).xxxx;
			float clampResult221 = clamp( ( WindowsMask201 - ( 1.0 - smoothstepResult130 ) ) , 0.0 , 1.0 );
			float4 lerpResult216 = lerp( temp_cast_10 , enterier632 , clampResult221);
			float4 temp_cast_11 = (temp_output_210_0).xxxx;
			float4 lerpResult222 = lerp( tex2DNode91 , temp_cast_11 , _LightVariation);
			float smoothstepResult93 = smoothstep( _CSLights , ( _CSLights + 0.001 ) , ( ( tex2DNode91.r + tex2DNode91.g ) * ( ( ( temp_output_814_0 * 10.0 ) * 0.1 ) * _LightOnThershold ) ));
			float3 appendResult524 = (float3(temp_output_518_0 , temp_output_519_0 , temp_output_512_0));
			float3 LightsColor523 = appendResult524;
			float LightShape498 = ( tex2DNode247.g * _BuildingLighting );
			float temp_output_554_0 = ( ( GlassReflection464 * 0.01 ) + _GlobalLightsOn );
			float smoothstepResult550 = smoothstep( temp_output_554_0 , ( temp_output_554_0 + 0.001 ) , _CSLights);
			float GlobalLightsOnOFff556 = smoothstepResult550;
			float4 lerpResult299 = lerp( ( ( ( lerpResult216 * _LightStrenght ) * ( lerpResult222 * smoothstepResult93 ) ) * tex2DNode91.a ) , ( temp_output_260_0 * float4( LightsColor523 , 0.0 ) ) , ( ( ( LightShape498 * texArray165.y ) * RoofNormalThreshold577 ) * ( 1.0 - GlobalLightsOnOFff556 ) ));
			float4 lerpResult940 = lerp( temp_cast_9 , lerpResult299 , temp_output_755_0);
			float4 temp_output_858_0 = lerpResult940;
			o.Emission = temp_output_858_0.rgb;
			float clampResult256 = clamp( ( ( WindowsMask201 * _Mettalic ) * GlassReflection464 ) , _MinSpecular , 1.0 );
			float occlusionPure707 = temp_output_230_0;
			float lerpResult946 = lerp( 0.1 , ( clampResult256 * occlusionPure707 ) , temp_output_755_0);
			float clampResult977 = clamp( lerpResult946 , 0.0 , 1.0 );
			o.Metallic = clampResult977;
			float MetallicVar1056 = texArray1028.w;
			float grayscale706 = Luminance(lerpResult366.rgb);
			float dirthGlass851 = tex2DNode232.b;
			float clampResult258 = clamp( ( WindowsMask201 * _Smoothness ) , ( _MinSmoothness * (0.0 + (( 1.0 - grayscale706 ) - 0.7) * (1.0 - 0.0) / (1.0 - 0.7)) ) , dirthGlass851 );
			float lerpResult1020 = lerp( MetallicVar1056 , clampResult258 , WindowsMask201);
			float4 temp_cast_17 = (lerpResult1020).xxxx;
			float4 lerpResult942 = lerp( tex2DNode367 , temp_cast_17 , temp_output_755_0);
			o.Smoothness = lerpResult942.r;
			float4 temp_cast_19 = (1.0).xxxx;
			float4 transform280 = mul(unity_WorldToObject,float4( ase_worldPos , 0.0 ));
			float temp_output_566_0 = ( ( temp_output_451_0 - temp_output_483_0 ) * 10.0 );
			float temp_output_568_0 = trunc( temp_output_566_0 );
			float AOHeight571 = temp_output_568_0;
			float clampResult382 = clamp( ( transform280.y * ( _Ambientocclusion.a * AOHeight571 ) ) , 0.0 , 1.0 );
			float4 lerpResult380 = lerp( _Ambientocclusion , float4( 1,0,0,0 ) , clampResult382);
			float4 temp_cast_21 = (_AOPower).xxxx;
			float4 temp_output_385_0 = pow( lerpResult380 , temp_cast_21 );
			float4 lerpResult943 = lerp( temp_cast_19 , ( temp_output_230_0 * temp_output_385_0 ) , temp_output_755_0);
			o.Occlusion = lerpResult943.r;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma only_renderers d3d11 glcore gles3 metal xboxone ps4 
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
			#pragma target 4.5
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
				float4 customPack2 : TEXCOORD2;
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
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.customPack2.xyzw = customInputData.uv4_tex4coord4;
				o.customPack2.xyzw = v.texcoord3;
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
				surfIN.uv_texcoord = IN.customPack1.xy;
				surfIN.uv4_tex4coord4 = IN.customPack2.xyzw;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.viewDir = IN.tSpace0.xyz * worldViewDir.x + IN.tSpace1.xyz * worldViewDir.y + IN.tSpace2.xyz * worldViewDir.z;
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				surfIN.vertexColor = IN.color;
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
140;589;2067;627;3254.457;-740.7469;1.3;True;True
Node;AmplifyShaderEditor.TexCoordVertexDataNode;923;-7575.338,299.7655;Float;False;3;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BreakToComponentsNode;925;-7106.466,244.3253;Float;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RelayNode;444;-6776.2,368.4;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;774;-4695.699,953.4991;Float;False;1252.32;1496.207;Comment;25;570;457;483;772;568;771;566;567;453;451;448;456;491;449;447;490;443;445;489;439;460;461;459;441;458;;1,1,1,1;0;0
Node;AmplifyShaderEditor.WireNode;922;-5238.354,549.2917;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;460;-4575.785,1445.191;Float;False;Constant;_Decimals;Decimals;47;0;Create;True;0;0;False;0;10;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;439;-4345.299,1198.899;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;100;False;1;FLOAT;0
Node;AmplifyShaderEditor.TruncOpNode;489;-4113.984,1214.992;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;445;-4228.002,1296.999;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;443;-4350.9,1452.299;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.TruncOpNode;490;-4149.186,1446.991;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;812;-6409.811,601.8184;Float;False;1152.32;1396.207;Uncompress IDS;24;837;836;835;834;832;831;828;827;826;825;824;823;822;821;820;819;818;817;816;815;814;813;878;926;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RelayNode;830;-6770.113,274.8197;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;813;-6335.097,1045.11;Float;False;Constant;_Float12;Float 12;47;0;Create;True;0;0;False;0;10;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;447;-4238.9,1564.299;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;449;-4350.718,1695.158;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;831;-6107.21,776.7184;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;100;False;1;FLOAT;0
Node;AmplifyShaderEditor.TruncOpNode;814;-5873.295,814.9113;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;458;-3908.899,1429.599;Float;False;223.2101;100;TileX;1;455;;1,1,1,1;0;0
Node;AmplifyShaderEditor.TruncOpNode;491;-4120.787,1669.891;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;455;-3874.384,1455.99;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;815;-5987.314,896.9185;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;456;-3857.885,1695.291;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;816;-6110.212,1052.218;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;715;-3378.167,1701.61;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;714;-3445.462,1426.154;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.TruncOpNode;817;-5908.497,1046.91;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;466;-3262.092,1421.531;Float;False;TileX;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;22;-5374.302,-1512.901;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;467;-3154.588,1713.491;Float;False;TileY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;636;-4345.67,-1403.994;Float;False;2153.497;579.15;Comment;22;328;319;322;325;331;488;469;323;330;329;640;641;766;929;931;932;933;939;953;954;965;966;TileGeneration;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;716;-6413.791,-1259.213;Float;False;1152.32;1396.207;Uncompress IDS;25;740;739;735;734;732;731;729;726;725;723;722;720;718;744;738;897;898;916;917;913;918;919;899;982;983;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;638;-4931.27,-1539.495;Float;False;UCoord;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;719;-6777.096,189.388;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;639;-4930.072,-1446.295;Float;False;VCoord;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;718;-6339.077,-815.9209;Float;False;Constant;_Float16;Float 16;47;0;Create;True;0;0;False;0;10;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;488;-4187.794,-987.7393;Float;False;467;TileY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;469;-4150.443,-1259.488;Float;False;466;TileX;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;818;-5998.212,1164.218;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;641;-4280.873,-1346.496;Float;False;639;VCoord;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;819;-6110.03,1295.078;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;720;-6052.39,-1154.913;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;100;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;640;-4290.37,-1091.896;Float;False;638;UCoord;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;965;-3882.206,-1234.065;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.0001;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;966;-3811.009,-975.512;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.0001;False;1;FLOAT;0
Node;AmplifyShaderEditor.TruncOpNode;820;-5823.697,1277.311;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FloorOpNode;918;-5835.271,-1051.908;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;319;-3582.202,-1321.095;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;328;-3595.54,-1101.569;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;322;-3404.303,-1321.395;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;823;-5998.03,1407.078;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;722;-5991.294,-964.1127;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;325;-3468.257,-1228.142;Float;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;331;-3442.566,-984.3918;Float;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;329;-3434.74,-1103.77;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;723;-6054.394,-820.5129;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;824;-6102.829,1530.48;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;933;-3190.256,-933.5396;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;932;-3197.178,-1202.464;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TruncOpNode;825;-5835.303,1480.511;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FloorOpNode;919;-5798.469,-859.9079;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;739;-5566.077,-1042.921;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;822;-5627.397,1223.811;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;931;-3007.756,-1247.418;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;834;-5591.195,1474.309;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;937;-3204.169,-690.2398;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;926;-5405.612,1220.143;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;729;-5583.276,-838.7218;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;744;-5405.762,-1041.389;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;953;-2854.905,-1184.771;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;434;-3345.786,-88.10668;Float;False;Constant;_Float3;Float 3;47;0;Create;True;0;0;False;0;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;433;-3346.99,-197.2069;Float;False;Constant;_Float0;Float 0;47;0;Create;True;0;0;False;0;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;644;-3360.346,-314.2431;Float;False;639;VCoord;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;927;-5207.115,1320.643;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;745;-5144.865,-894.4888;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;522;-4647.312,2552.109;Float;False;1171.07;1583.706;Lighting;26;539;538;537;507;508;512;509;521;519;518;517;516;515;514;513;511;506;505;504;503;502;501;500;544;548;562;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;939;-2643.741,-1112.591;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;520;-6737.112,472.5106;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;432;-3032.484,-250.1069;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;928;-5026.263,1323.993;Float;False;DivisionID;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;954;-2450.877,-1004.1;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;746;-4888.668,-887.0897;Float;False;faccade_ID;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;521;-4568.298,3022.001;Float;False;Constant;_Float8;Float 8;47;0;Create;True;0;0;False;0;10;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;921;-6659.956,2466.692;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;747;-2853.77,-581.6896;Float;False;746;faccade_ID;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;861;-2493.826,13.814;Float;False;maskLowFloor;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;515;-4337.813,2775.709;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;100;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;955;-2325.477,-905.3;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;929;-2989.561,-952.2067;Float;False;928;DivisionID;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;438;-2712.185,-246.9085;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;863;-3073.672,725.5336;Float;False;861;maskLowFloor;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;930;-2431.761,-583.6057;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TruncOpNode;517;-4103.296,2723.002;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;821;-5633.696,1055.91;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;547;-2293.788,-394.5038;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;868;-2834.176,807.0023;Float;False;FLOAT2;4;0;FLOAT;1;False;1;FLOAT;1;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;878;-5387.365,1033.495;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;30;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;637;-4925.668,-1623.495;Float;False;UVcoord;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;867;-2866.477,943.802;Float;False;FLOAT2;4;0;FLOAT;1;False;1;FLOAT;1;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;514;-4220.515,2873.809;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;864;-2619.971,855.6987;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;1,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode;363;-1973.273,-354.3263;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;876;-5181.97,1063.295;Float;False;LowFloorID;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;365;3828.802,1091.687;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;642;-2654.158,697.5988;Float;False;637;UVcoord;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;505;-4343.413,3029.109;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;877;-2580.228,423.7748;Float;False;876;LowFloorID;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;862;-2426.174,786.5004;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;575;-2179.566,69.61225;Float;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TruncOpNode;518;-4141.698,3023.801;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;673;-1087.125,-2167.5;Float;False;6082.19;1413.166;Comment;27;701;695;690;682;679;677;676;675;674;703;757;759;761;764;765;848;849;693;691;685;683;681;696;698;700;688;687;;1,1,1,1;0;0
Node;AmplifyShaderEditor.StepOpNode;368;4274.248,839.2598;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.99;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;577;4492.745,856.1744;Float;False;RoofNormalThreshold;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1013;-2287.25,247.4318;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;674;-534.4719,-1554.794;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;866;-2261.279,789.8323;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;504;-4231.413,3141.109;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;875;-2002.049,278.3176;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;872;-2119.605,837.9273;Float;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;578;-3048.07,1364.334;Float;False;577;RoofNormalThreshold;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;676;-627.1709,-1276.672;Float;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;675;-251.58,-1539.172;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;-1,-1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;516;-4343.231,3271.968;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;583;-2710.747,1386.214;Float;False;3;0;FLOAT;40;False;1;FLOAT;39;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;116;-2762.374,1226.716;Float;False;Property;_DepthScale;DepthScale;5;0;Create;True;0;0;False;0;0.12;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;873;-1930.871,854.5496;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode;679;-369.7694,-1263.971;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TruncOpNode;519;-4151.299,3324.601;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;677;-83.68896,-1535.072;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;108;-2617.019,1538.695;Float;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TextureArrayNode;574;-2597.447,960.6594;Float;True;Property;_TextureArray1;Texture Array 1;21;0;Create;True;0;0;False;0;None;0;Instance;75;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;990;-2385.384,1156.082;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.3;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;681;130.926,-1613.471;Float;False;float3(X * 2 - 1, -1);3;False;1;True;X;FLOAT2;0,0;In;;Float;expr5;True;False;0;1;0;FLOAT2;0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;682;-115.8752,-1139.279;Float;False;2;0;FLOAT3;1,1,1;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;506;-4231.231,3383.968;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;508;-4344.83,3537.569;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.ParallaxMappingNode;107;-2172.956,1078.775;Float;False;Planar;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0.1;False;3;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CustomExpressionNode;683;137.123,-1172.471;Float;False;abs(B) - A * B;3;False;2;True;A;FLOAT3;0,0,0;In;;Float;True;B;FLOAT3;0,0,0;In;;Float;expr4;True;False;0;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;656;-4616.882,4820.606;Float;False;1893.389;661.2908;Comment;14;146;242;240;241;234;238;239;237;646;232;243;244;653;851;Curtains;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;584;-1577.416,1035.834;Float;False;Parallax;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CustomExpressionNode;685;449.425,-1186.373;Float;False;min(min(C.x, C.y), C.z);1;False;1;True;C;FLOAT3;0,0,0;In;;Float;expr3;True;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TruncOpNode;512;-4096,3489.6;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;91;-3133.699,3169.697;Float;True;Property;_NoiseTexture;NoiseTexture;21;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;244;-4469.917,5064.293;Float;False;Constant;_Float9;Float 9;31;0;Create;True;0;0;False;0;0.4;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;653;-4498.982,4963.904;Float;False;584;Parallax;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;507;-4232.83,3649.569;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;687;689.02,-1475.971;Float;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;240;-3949.411,5223.006;Float;False;Property;_SmoothCurtains;SmoothCurtains;13;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;840;-2924.07,2976.506;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;242;-3885.815,5335.5;Float;False;Constant;_Float10;Float 10;30;0;Create;True;0;0;False;0;0.001;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;243;-4278.216,5043.194;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;688;861.322,-1498.372;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;538;-4336.971,3776.353;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;448;-4238.718,1807.158;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;690;1310.338,-1123.258;Float;False;Constant;_Float6;Float 6;2;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;232;-4107.505,5014.904;Float;True;Property;_CurtainsMap;CurtainsMap;22;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;241;-3653.115,5326.903;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;146;-4001.307,4902.899;Float;False;Property;_InteriourBlur;InteriourBlur;10;0;Create;True;0;0;False;0;0;0;0;16;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;841;-2750.07,2965.506;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TruncOpNode;537;-4088.141,3728.384;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;691;1277.933,-1458.657;Float;False;pos.z * 0.5 + 0.5;1;False;1;True;pos;FLOAT3;0,0,0;In;;Float;expr2;True;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;725;-6002.192,-696.813;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;451;-4343.517,1930.56;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.TruncOpNode;483;-4059.991,1906.191;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;239;-3479.816,5224.599;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;842;-2591.07,2959.506;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.33;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;238;-3601.513,5145.999;Float;False;Property;_MinInteriourSmoothnes;MinInteriourSmoothnes;11;0;Create;True;0;0;False;0;0;0;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;234;-3569.807,5015.005;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;693;1636.235,-1374.158;Float;False;saturate(interp1) / depthScale + 1;1;False;2;True;depthScale;FLOAT;0;In;;Float;True;interp1;FLOAT;0;In;;Float;Expr1;True;False;0;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;826;-5979.831,1596.279;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;726;-6114.01,-565.9536;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.FloorOpNode;544;-3825.686,3730.096;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;513;-3868.896,2807.801;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;1011;2990.931,808.4325;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;695;2415.43,-855.358;Float;False;Constant;_Float15;Float 15;3;0;Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;843;-2428.07,2987.506;Float;False;internVariation;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;237;-3242.913,5130.501;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;457;-3839.885,1874.39;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;548;-3629.987,2703.097;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1009;3287.28,924.9284;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;827;-6111.915,1741.701;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;562;-3647.285,3745.297;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FloorOpNode;916;-5576.07,-693.5078;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;696;2253.537,-1263.656;Float;False;(1.0 - (1.0 / realZ)) * (depthScale +1.0);1;False;2;True;depthScale;FLOAT;0;In;;Float;True;realZ;FLOAT;0;In;;Float;expr6;True;False;0;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;698;2859.133,-1177.258;Float;False;pos.xy * lerp(1.0, farFrac, interp2);2;False;3;True;pos;FLOAT3;0,0,0;In;;Float;True;interp2;FLOAT;0;In;;Float;True;farFrac;FLOAT;0;In;;Float;expr7;True;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;759;3322.331,-1527.292;Float;False;843;internVariation;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;526;-3398.589,2700.393;Float;False;offsetDirtMap;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;646;-3003.576,5122.106;Float;False;mipCurtains;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;773;-3238.844,1858.022;Float;False;blin;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;731;-5973.51,-452.0536;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;645;-2779.273,1884.005;Float;False;639;VCoord;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;541;-3281.888,3672.199;Float;False;LightsScale;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;987;3445.455,936.959;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.05;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;527;-1310.219,1343.63;Float;False;Constant;_Vector3;Vector 3;46;0;Create;True;0;0;False;0;0.01,0.01;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;531;-1430.392,1176.894;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TruncOpNode;836;-5748.391,1702.932;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;700;3333.73,-992.959;Float;False;interiorUV * -0.5 - 0.5;2;False;1;True;interiorUV;FLOAT2;0,0;In;;Float;expr8;True;False;0;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode;837;-5579.682,1702.732;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;703;3930.228,-1299.388;Float;False;646;mipCurtains;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;732;-6106.809,-330.5517;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;765;4010.13,-1577.99;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;6;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;985;3608.4,937.4099;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;561;-1311.086,1523.094;Float;False;541;LightsScale;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;532;-1049.792,1323.294;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;1026;-2297.848,671.4249;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;529;-1067.992,1574.294;Float;False;526;offsetDirtMap;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;209;-2214.508,2884.297;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;138;-2397.625,1874.211;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;775;-2775.118,1998.345;Float;False;773;blin;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;652;-2281.921,1519.459;Float;False;584;Parallax;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;150;-1741.051,628.1891;Float;False;Property;_ScaleTex1;ScaleTex1;19;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FloorOpNode;917;-5758.472,-400.7078;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;849;4449.825,-1265.5;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;1004;-5144.908,1859.975;Float;False;2;0;FLOAT;0;False;1;FLOAT;9;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;210;-1941.107,2889.394;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;951;-1788.375,305.8029;Float;True;Property;_MaskTexArray;MaskTexArray;38;0;Create;True;0;0;False;0;None;None;False;white;LockedToTexture2DArray;Texture2DArray;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.TextureArrayNode;757;4283.332,-1577.09;Float;True;Property;_Interior2;Interior2;27;0;Create;True;0;0;False;0;None;0;Object;-1;MipLevel;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;1005;3743.087,943.3226;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;-0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.DdyOpNode;957;-1556.549,1368.063;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;560;-928.887,1428.195;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DdxOpNode;956;-1595.544,1280.22;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;1024;-1818.44,720.6919;Float;False;Constant;_Vector4;Vector 4;43;0;Create;True;0;0;False;0;2,4;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;1025;-2097.194,706.9227;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;777;-2379.237,2016.681;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;121;-2223.533,1874.615;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;533;-758.3459,1560.648;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;901;-1681.813,177.1965;Float;False;584;Parallax;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;711;-2278.679,1590.508;Float;False;Constant;_Float4;Float 4;38;0;Create;True;0;0;False;0;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;135;-2079.606,1715.997;Float;False;Property;_BlindsOpen;BlindsOpen;12;0;Create;True;0;0;False;0;0;0;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;710;-2054.086,1527.477;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;848;4686.624,-1599.9;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1000;-5058.68,1675.818;Float;False;windowColor;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;748;-4929.174,-670.3892;Float;False;faccadeSuftace_ID;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;768;-4917.869,-440.8899;Float;False;faccadeSuftace_ID2;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;136;-1966.395,2079.622;Float;False;Constant;_BlindsAperture;BlindsAperture;11;0;Create;True;0;0;False;0;0.003;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1007;3884.064,926.9722;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;528;-725.392,1406.993;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;651;158.5969,1435.177;Float;False;584;Parallax;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1023;-1653.337,839.7322;Float;False;3;3;0;FLOAT2;1,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;776;-2199.762,2016.511;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;131;-2085.002,1875.999;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;75;-1415.156,212.491;Float;True;Property;_tt;tt;17;0;Create;True;0;0;False;0;None;0;Object;-1;MipBias;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureArrayNode;874;-1837.238,1517.953;Float;True;Property;_BlindsArray;BlindsArray;26;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BreakToComponentsNode;182;-1000.154,272.8771;Float;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SmoothstepOpNode;576;-1043.606,792.1154;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;632;5387.532,-1711.8;Float;False;enterier;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.AbsOpNode;1008;3992.385,832.958;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;247;-575.1188,1213.396;Float;True;Property;_DirtTexture;DirtTexture;20;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureArrayNode;165;621.9036,1479.735;Float;True;Property;_NormalTextureArray;NormalTextureArray;18;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;436;-1891.784,1859.293;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;268;-868.0551,463.4648;Float;False;Property;_WindowBorderCol1;WindowBorderCol1;24;0;Create;True;0;0;False;0;0.4485294,0.4485294,0.4485294,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;1001;-865.7043,654.5868;Float;False;Property;_WindowBorderCol2;WindowBorderCol2;25;0;Create;True;0;0;False;0;0.4485294,0.4485294,0.4485294,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;149;-1547.796,548.8627;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;770;-1270.985,1047.23;Float;False;768;faccadeSuftace_ID2;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1003;-258.1211,773.2332;Float;False;1000;windowColor;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;137;-1656.003,1945.897;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;767;-2056.105,527.9297;Float;False;748;faccadeSuftace_ID;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;250;-568.3206,1419.693;Float;False;Property;_DirtAmount;DirtAmount;14;0;Create;True;0;0;False;0;1;0;1;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;342;-944.4855,1008.56;Float;True;Property;_WallsArray;WallsArray;33;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;634;-1399.345,1956.433;Float;False;632;enterier;1;0;OBJECT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;1002;-587.4786,654.9333;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SmoothstepOpNode;130;-1586.035,1793.593;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;90;-792.4456,855.8799;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;441;-3905.9,1188.299;Float;False;219;119;GlassGloss;1;454;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;453;-4220.519,1996.359;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;969;2974.705,641.1418;Float;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureArrayNode;712;-1343.192,556.1448;Float;True;Property;_SurfaceAray;SurfaceAray;30;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;967;3211.589,522.6123;Float;False;637;UVcoord;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;251;-185.8198,1145.094;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;423;-1474.983,1577.892;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;973;3334.825,662.7032;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;8.7;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;183;-652.5665,283.6556;Float;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;971;3528.642,640.7516;Float;False;Constant;_Float19;Float 19;39;0;Create;True;0;0;False;0;0.07;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;201;-581.9291,815.9923;Float;False;WindowsMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;566;-4352.603,2141.781;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;454;-3876.385,1230.991;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;968;3454.123,530.3875;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ClampOpNode;252;36.78006,1145.594;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;129;-1141.165,1890.906;Float;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LayeredBlendNode;74;-326.6104,430.6297;Float;False;6;0;FLOAT3;0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT4;0,0,0,0;False;3;FLOAT4;0,0,0,0;False;4;FLOAT4;0,0,0,0;False;5;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DynamicAppendNode;995;3450.636,779.0972;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;996;3625.226,754.8566;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.01,0.01;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;657;-427.0775,3331.807;Float;False;1729.959;920.7582;Comment;22;556;224;222;94;95;96;225;551;552;555;554;553;550;93;164;214;221;217;655;216;215;871;WindowLightsManager;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;464;-3386.588,1234.891;Float;False;GlassReflection;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;459;-3891.943,2035.299;Float;False;172.5995;124.2;Uncompress B;1;569;;1,1,1,1;0;0
Node;AmplifyShaderEditor.TruncOpNode;568;-4084.078,2091.813;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;246;-10.91939,465.6945;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;649;748.8198,796.1052;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;633;-379.3555,1883.497;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;970;3608.159,534.0342;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;650;1391.47,902.8267;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;204;816.3448,604.7408;Float;False;Property;_GlassColor;GlassColor;6;0;Create;True;0;0;False;0;0,0,0,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1022;984.9767,1083.538;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;994;3753.362,626.6553;Float;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode;569;-3856.571,2079.412;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;551;-314.1838,3456.792;Float;False;464;GlassReflection;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;980;3963.554,455.5535;Float;False;464;GlassReflection;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;198;286.591,579.2902;Float;False;Property;_BuildingLightness;BuildingLightness;16;0;Create;True;0;0;False;0;5;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;828;-5635.697,830.9103;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;270;1126.341,740.3283;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;120;282.1949,470.6982;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;552;-33.3843,3455.993;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;752;4012.48,83.46993;Float;False;Property;_roofsCol1;roofsCol1;40;0;Create;True;0;0;False;0;0.2530277,0.5065386,0.5294118,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;231;2078.894,855.4017;Float;False;Property;_Occlusion;Occlusion;15;0;Create;True;0;0;False;0;0;0;0;4;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;555;-148.604,3600.953;Float;False;Property;_GlobalLightsOn;GlobalLightsOn;34;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LayeredBlendNode;1055;-895.6509,-203.0186;Float;False;6;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;8;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;571;-3302.829,2095.04;Float;False;AOHeight;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;832;-5401.782,819.642;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;367;4009.427,579.665;Float;True;Property;_Rooftoptex;Rooftoptex;31;0;Create;True;0;0;False;0;None;None;True;0;False;white;LockedToTexture2D;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;984;4231.692,428.5943;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;269;1399.379,656.1971;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;197;660.3914,474.2902;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ColorNode;978;4018.724,264.44;Float;False;Property;_RoofsCol2;RoofsCol2;42;0;Create;True;0;0;False;0;0.2530277,0.5065386,0.5294118,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldPosInputsNode;271;2139.581,-131.7048;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.LerpOp;158;1814.48,645.992;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;340;-2287.792,3093.103;Float;False;Property;_LightOnThershold;LightOnThershold;32;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;734;-5913.409,-255.1528;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;1028;-599.1924,-140.3779;Float;True;Property;_SurfaceNormalArray;SurfaceNormalArray;29;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;975;-257.1328,1423.15;Float;False;Property;_BuildingLighting;BuildingLighting;44;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;554;152.416,3448.793;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;1012;4142.1,1138.459;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;220;-1243.201,1762.498;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;838;-2567.963,2818.505;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;384;2060.912,10.70453;Float;False;Property;_Ambientocclusion;Ambient occlusion;35;0;Create;True;0;0;False;0;1,1,1,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;979;4362.724,276.397;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;573;2117.63,196.5033;Float;False;571;AOHeight;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;986;4363.417,676.6525;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;661;-1079.018,1721.564;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;230;2533.593,683.0029;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldToObjectTransfNode;280;2375.881,-130.905;Float;False;1;0;FLOAT4;0,0,0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;341;-1991.695,3182.903;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;500;-3885.014,3481.309;Float;False;215.6;111.5597;BB;1;510;;1,1,1,1;0;0
Node;AmplifyShaderEditor.OneMinusNode;1054;-294.3293,191.6986;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;735;-6115.895,-119.3307;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;553;246.7159,3617.791;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.001;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;974;-125.1328,1278.15;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;572;2392.229,137.3015;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;95;170.8992,3874.997;Float;False;Constant;_Float14;Float 14;9;0;Create;True;0;0;False;0;0.001;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;94;-110.9004,3742.198;Float;False;Global;_CSLights;_CSLights;7;0;Create;True;0;0;False;0;0;0.06685537;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;751;4620.713,649.3967;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StepOpNode;755;4320.393,1081.766;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;339;-1793.394,3101.105;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;219;-827.278,1733.958;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;260;2732.188,847.6556;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SmoothstepOpNode;550;466.0161,3649.69;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;381;2603.11,107.7053;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;510;-3828.095,3519.8;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;511;-3850.396,3272.101;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;509;-3866.895,3032.8;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FloorOpNode;982;-5591.389,-162.7133;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;1050;-38.14064,-97.8457;Float;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;498;57.9026,1277.29;Float;False;LightShape;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;96;499.699,3859.898;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;224;378.3981,4028.197;Float;False;Property;_LightVariation;LightVariation;9;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;221;325.9379,3430.998;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;157;4116.309,2578.006;Float;False;Property;_Mettalic;Mettalic;7;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;337;-1592.499,3198.303;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;662;4241.686,2492.775;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;217;535.8955,3373.595;Float;False;Constant;_Float7;Float 7;26;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;366;4927.846,901.8322;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;655;537.416,3448.003;Float;False;632;enterier;1;0;OBJECT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RelayNode;738;-5537.25,-53.98537;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1053;1020.547,1703.271;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;406;-2212.881,3525.204;Float;False;Constant;_Vector2;Vector 2;50;0;Create;True;0;0;False;0;0.04,0.04,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;556;719.5157,3660.491;Float;False;GlobalLightsOnOFff;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;404;-2218.381,3352.005;Float;False;Constant;_Vector0;Vector 0;50;0;Create;True;0;0;False;0;-0.04,-0.04,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;524;-3423.092,2893.995;Float;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.UnpackScaleNormalNode;1049;301.6064,-63.40991;Float;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0.5;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SmoothstepOpNode;93;717.1996,3794.299;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;465;4327.836,2802.307;Float;False;464;GlassReflection;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;154;4542.922,2528.146;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;222;801.9993,3941.298;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCGrayscale;706;5018.717,778.2473;Float;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;382;2786.81,76.50539;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;499;3356.606,1288.793;Float;False;498;LightShape;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;216;791.0973,3377.296;Float;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;164;605.1806,3531.287;Float;False;Property;_LightStrenght;LightStrenght;8;0;Create;True;0;0;False;0;0;0;0;100;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;658;3397.125,1474.707;Float;False;577;RoofNormalThreshold;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;257;4805.55,2801.015;Float;False;Property;_MinSpecular;MinSpecular;2;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;983;-5394.132,-69.50378;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;523;-3205.289,2892.696;Float;False;LightsColor;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1035;685.6901,-16.58398;Float;False;surfaceNormal;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;401;-1933.483,3372.605;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;558;3573.615,1563.193;Float;False;556;GlobalLightsOnOFff;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;214;999.3965,3466.596;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;386;3006.211,138.507;Float;False;Property;_AOPower;AO Power;36;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;1048;1250.934,1608.291;Float;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;462;4827.247,2597.997;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;225;1060.197,3793.798;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;380;3025.412,-15.69453;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;592;5258.73,775.3729;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;534;3624.096,1329.341;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;559;3876.917,1563.793;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;259;5331.181,660.2937;Float;False;Property;_MinSmoothness;MinSmoothness;0;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;407;-1738.786,3365.006;Float;False;normalWindows;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;851;-4131.479,5358.301;Float;False;dirthGlass;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;769;-5209.795,-93.68449;Float;False;ColorInterpolate;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;707;2718.366,541.0514;Float;False;occlusionPure;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;385;3243.713,110.6086;Float;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;525;2655.506,1083.372;Float;False;523;LightsColor;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;660;5013.324,455.4084;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;256;5127.201,2588.599;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;215;1152.2,3650.395;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;549;3905.417,1449.196;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.UnpackScaleNormalNode;1041;1700.815,1799.375;Float;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TFHCRemapNode;704;5438.737,792.8091;Float;False;5;0;FLOAT;0;False;1;FLOAT;0.7;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1037;1975.421,1994.817;Float;False;1035;surfaceNormal;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;156;4917.98,572.5897;Float;False;Property;_Smoothness;Smoothness;1;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;852;5729.823,885.3014;Float;False;851;dirthGlass;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;413;2713.824,2111.978;Float;False;201;WindowsMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1056;-248.4639,88.16066;Float;False;MetallicVar;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;591;5650.233,710.9031;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;839;2464.936,3121.907;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;421;3722.811,192.3901;Float;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;302;3076.082,1071.175;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;991;6883.369,1344.033;Float;False;769;ColorInterpolate;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;155;5375.199,537.4963;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;557;4088.518,1463.094;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;665;5357.296,2620.077;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;414;2702.175,1995.657;Float;False;407;normalWindows;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.BlendNormalsNode;1036;2373.8,1844.777;Float;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;709;5201.25,2748.505;Float;False;707;occlusionPure;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1057;5975.881,535.8007;Float;False;1056;MetallicVar;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;941;5063.442,1280.493;Float;False;Constant;_Float11;Float 11;37;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;299;4250.673,1476.198;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;412;3043.687,1912.965;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WireNode;663;6081.225,412.5096;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;888;5768.432,1124.195;Float;False;Property;_dESATURATE;dESATURATE;37;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;258;5973.277,696.8956;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;999;7166.417,1376.35;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;708;5503.874,2595.235;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DesaturateOpNode;887;6088.432,1001.795;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;1020;6410.149,574.7805;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;946;7117.148,1857.143;Float;False;3;0;FLOAT;0.1;False;1;FLOAT;111;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;940;5453.379,1132.555;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;998;7347.21,1384.581;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;859;6135.095,1905.467;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;944;6602.944,1741.893;Float;False;Constant;_Float13;Float 13;37;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;857;6558.432,1495.401;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;913;-5431.767,-470.2082;Float;False;Constant;_Truncation;Truncation;37;0;Create;True;0;0;False;0;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;989;-1934.437,1003.878;Float;False;Property;_Float20;Float 20;46;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TruncOpNode;772;-4095.261,2305.109;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;1014;-7650.234,670.0007;Float;False;3;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;1018;379.8162,938.8357;Float;False;Smoothness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;323;-3161.532,-1357.486;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;740;-5766.396,-116.0307;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;758;-2605.071,3426.61;Float;False;noise;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TruncOpNode;898;-5655.96,-311.0054;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendNormalsNode;588;3398.232,1882.301;Float;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SurfaceDepthNode;963;-2499.79,1339.389;Float;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;766;-2433.174,-1139.993;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1019;5792.042,399.4981;Float;False;1018;Smoothness;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;1071;7177.382,2348.431;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;152;-3215.914,-433.1069;Float;False;Property;_flooroffset;flooroffset;4;0;Create;True;0;0;False;0;0;0;0;16;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;870;-2692.178,-25.49701;Float;False;3;0;FLOAT;1;False;1;FLOAT;2;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCGrayscale;749;-1294.852,782.6667;Float;False;2;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;1016;-7557.75,33.54362;Float;False;Constant;_Vector1;Vector 1;43;0;Create;True;0;0;False;0;1,1,1,1;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;960;-2101.445,1234.72;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TruncOpNode;899;-5927.962,-4.805155;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;855;1612.428,435.1572;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;977;7394.846,1741.158;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;589;2813.705,1738.727;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.03,0.03;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;567;-4145.604,2191.581;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;854;1193.212,382.9323;Float;False;646;mipCurtains;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;330;-3221.043,-1079.332;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;964;-2285.727,1344.739;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;902;-6734.653,-887.1046;Float;False;Constant;_Float5;Float 5;37;0;Create;True;0;0;False;0;1E-05;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;590;2596.51,1738.384;Float;False;584;Parallax;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;905;-5139.565,-282.7068;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;950;-2091.797,1376.52;Float;False;Property;_RefFrame;RefFrame;39;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;422;3521.914,327.0915;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;835;-5937.715,1733.901;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TruncOpNode;897;-5784.364,-675.005;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1021;6132.799,452.0343;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexToFragmentNode;924;-7282.76,497.0913;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DistanceOpNode;1065;8742.145,2192.609;Float;True;2;0;FLOAT3;0,0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;1073;7904.354,2174.04;Float;False;Property;_Color1;Color 1;41;0;Create;True;0;0;False;0;0.8676471,0.7320442,0.4402033,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;945;7023.357,1716.604;Float;False;3;0;FLOAT3;0,0,1;False;1;FLOAT3;1,1,1;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1064;8668.818,2482.245;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FractNode;764;3840.431,-1589.691;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1063;8381.088,2321.42;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RelayNode;858;7531.084,1592.982;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;1066;8384.378,2042.568;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;1079;7412.186,1873.66;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleAddOpNode;435;-2546.886,-377.8068;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;997;7562.936,1360.648;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1077;7843.773,2754.265;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1072;8165.251,2109.303;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1074;7772.361,1941.596;Float;False;5;5;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1069;7849.662,2635.053;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1070;7558.951,2666.421;Float;False;Property;_Float21;Float 21;45;0;Create;True;0;0;False;0;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;1067;7528.785,2438.152;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FractNode;1058;8001.391,2457.133;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1061;8178.366,2566.9;Float;False;Property;_Float2;Float 2;43;0;Create;True;0;0;False;0;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;1060;8443.989,2533.595;Float;False;Constant;_Vector6;Vector 6;42;0;Create;True;0;0;False;0;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.DynamicAppendNode;1062;8496.049,2667.918;Float;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FractNode;1059;8010.901,2351.053;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;1078;8050.207,2693.098;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;988;-1784.252,974.7399;Float;False;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;771;-4315.461,2285.109;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;856;1623.175,353.6504;Float;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;879;1156.14,1860.986;Float;False;pureAO;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;871;-154.3749,4064.899;Float;False;Property;_Float35534543;Float35534543;23;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;539;-4224.971,3888.353;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;1015;-7225.118,666.7723;Float;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.ComponentMaskNode;1017;140.3387,741.852;Float;False;False;False;False;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1068;7804.233,2476.333;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;942;7421.522,2103.316;Float;False;3;0;COLOR;1,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;992;1028.224,2978.457;Float;False;TransparencyWindows;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;943;6862.042,1610.593;Float;False;3;0;COLOR;1,1,1,0;False;1;COLOR;1,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;903;-5170.354,-535.3055;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;647;-2514.575,558.7059;Float;False;584;Parallax;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;1076;7610.566,2163.733;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1075;7768.494,1728.786;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;587;2974.455,1706.966;Float;True;Property;_NoiseNormal;NoiseNormal;28;0;Create;True;0;0;False;0;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;0.1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RelayNode;1027;-539.9283,466.708;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;761;3669.43,-1590.993;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;3.00456;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;853;1424.721,347.3433;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;701;3927.733,-1043.155;Float;False;Property;_Float17;Float 17;3;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ParallaxOcclusionMappingNode;947;-1892.479,1242.505;Float;False;3;4;False;-1;12;False;-1;3;0.02;0;False;1,1;False;-1000,-100;TextureArray;7;0;FLOAT2;0,0;False;1;SAMPLER2D;;False;2;FLOAT;0.02;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT2;0,0;False;6;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;7992.113,1529.229;Float;False;True;5;Float;ASEMaterialInspector;100;0;Standard;CScape/Deprecated_CSBuildingShaderMobile;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;False;True;True;False;True;True;False;False;True;True;False;False;False;True;True;True;True;0;False;-1;False;1;False;-1;255;False;-1;255;False;-1;0;False;-1;3;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;4;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;1;False;-1;1;False;-1;0;False;0.17;0,0,0,0;VertexScale;True;False;Cylindrical;False;Relative;100;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.CommentaryNode;570;-3874.5,1851.499;Float;False;166.5601;117.9199;Blin;0;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;503;-3898.198,3216.901;Float;False;217.0601;132.4097;G;0;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;502;-3898.411,2765.109;Float;False;219;119;heightLights;0;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;501;-3901.41,3006.409;Float;False;223.2101;100;R;0;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;461;-3926.488,1640.091;Float;False;217.0601;132.4097;TileY;0;;1,1,1,1;0;0
WireConnection;925;0;923;0
WireConnection;444;0;925;2
WireConnection;922;0;444;0
WireConnection;439;0;922;0
WireConnection;439;1;460;0
WireConnection;489;0;439;0
WireConnection;445;0;439;0
WireConnection;445;1;489;0
WireConnection;443;0;445;0
WireConnection;443;1;460;0
WireConnection;490;0;443;0
WireConnection;830;0;925;1
WireConnection;447;0;443;0
WireConnection;447;1;490;0
WireConnection;449;0;447;0
WireConnection;449;1;460;0
WireConnection;831;0;830;0
WireConnection;831;1;813;0
WireConnection;814;0;831;0
WireConnection;491;0;449;0
WireConnection;455;0;490;0
WireConnection;815;0;831;0
WireConnection;815;1;814;0
WireConnection;456;0;491;0
WireConnection;816;0;815;0
WireConnection;816;1;813;0
WireConnection;715;0;456;0
WireConnection;714;0;455;0
WireConnection;817;0;816;0
WireConnection;466;0;714;0
WireConnection;467;0;715;0
WireConnection;638;0;22;1
WireConnection;719;0;925;0
WireConnection;639;0;22;2
WireConnection;818;0;816;0
WireConnection;818;1;817;0
WireConnection;819;0;818;0
WireConnection;819;1;813;0
WireConnection;720;0;719;0
WireConnection;720;1;718;0
WireConnection;965;0;469;0
WireConnection;966;0;488;0
WireConnection;820;0;819;0
WireConnection;918;0;720;0
WireConnection;319;0;641;0
WireConnection;319;1;965;0
WireConnection;328;0;640;0
WireConnection;328;1;966;0
WireConnection;322;0;319;0
WireConnection;823;0;819;0
WireConnection;823;1;820;0
WireConnection;722;0;720;0
WireConnection;722;1;918;0
WireConnection;325;1;965;0
WireConnection;331;1;966;0
WireConnection;329;0;328;0
WireConnection;723;0;722;0
WireConnection;723;1;718;0
WireConnection;824;0;823;0
WireConnection;824;1;813;0
WireConnection;933;0;329;0
WireConnection;933;1;331;0
WireConnection;932;0;322;0
WireConnection;932;1;325;0
WireConnection;825;0;824;0
WireConnection;919;0;723;0
WireConnection;739;0;918;0
WireConnection;822;0;820;0
WireConnection;931;0;932;0
WireConnection;931;1;933;0
WireConnection;834;0;825;0
WireConnection;926;0;822;0
WireConnection;729;0;919;0
WireConnection;744;0;739;0
WireConnection;953;0;931;0
WireConnection;927;0;926;0
WireConnection;927;1;834;0
WireConnection;745;0;744;0
WireConnection;745;1;729;0
WireConnection;939;0;953;0
WireConnection;939;1;937;1
WireConnection;520;0;925;3
WireConnection;432;0;644;0
WireConnection;432;1;433;0
WireConnection;432;2;434;0
WireConnection;928;0;927;0
WireConnection;954;0;939;0
WireConnection;746;0;745;0
WireConnection;921;0;520;0
WireConnection;861;0;432;0
WireConnection;515;0;921;0
WireConnection;515;1;521;0
WireConnection;955;0;954;0
WireConnection;438;0;432;0
WireConnection;930;0;747;0
WireConnection;930;1;929;0
WireConnection;930;2;955;0
WireConnection;517;0;515;0
WireConnection;821;0;817;0
WireConnection;547;0;930;0
WireConnection;547;1;747;0
WireConnection;547;2;438;0
WireConnection;868;1;863;0
WireConnection;878;0;821;0
WireConnection;637;0;22;0
WireConnection;514;0;515;0
WireConnection;514;1;517;0
WireConnection;864;0;868;0
WireConnection;864;1;867;0
WireConnection;363;0;547;0
WireConnection;876;0;878;0
WireConnection;505;0;514;0
WireConnection;505;1;521;0
WireConnection;862;0;642;0
WireConnection;862;1;864;0
WireConnection;575;0;363;0
WireConnection;518;0;505;0
WireConnection;368;0;365;2
WireConnection;577;0;368;0
WireConnection;1013;0;877;0
WireConnection;1013;1;575;0
WireConnection;1013;2;937;1
WireConnection;866;0;862;0
WireConnection;504;0;505;0
WireConnection;504;1;518;0
WireConnection;875;0;1013;0
WireConnection;875;1;575;0
WireConnection;875;2;861;0
WireConnection;872;0;866;0
WireConnection;872;1;863;0
WireConnection;675;0;674;0
WireConnection;516;0;504;0
WireConnection;516;1;521;0
WireConnection;583;1;875;0
WireConnection;583;2;578;0
WireConnection;873;0;872;0
WireConnection;679;0;676;0
WireConnection;519;0;516;0
WireConnection;677;0;675;0
WireConnection;574;0;873;0
WireConnection;574;1;583;0
WireConnection;990;0;116;0
WireConnection;681;0;677;0
WireConnection;682;1;679;0
WireConnection;506;0;516;0
WireConnection;506;1;519;0
WireConnection;508;0;506;0
WireConnection;508;1;521;0
WireConnection;107;0;873;0
WireConnection;107;1;574;4
WireConnection;107;2;990;0
WireConnection;107;3;108;0
WireConnection;683;0;681;0
WireConnection;683;1;682;0
WireConnection;584;0;107;0
WireConnection;685;0;683;0
WireConnection;512;0;508;0
WireConnection;507;0;508;0
WireConnection;507;1;512;0
WireConnection;687;0;685;0
WireConnection;687;1;679;0
WireConnection;840;0;91;1
WireConnection;840;1;91;2
WireConnection;243;0;653;0
WireConnection;243;1;244;0
WireConnection;688;0;681;0
WireConnection;688;1;687;0
WireConnection;538;0;507;0
WireConnection;538;1;521;0
WireConnection;448;0;449;0
WireConnection;448;1;491;0
WireConnection;232;1;243;0
WireConnection;241;0;240;0
WireConnection;241;1;242;0
WireConnection;841;0;840;0
WireConnection;841;1;91;3
WireConnection;537;0;538;0
WireConnection;691;0;688;0
WireConnection;725;0;723;0
WireConnection;725;1;919;0
WireConnection;451;0;448;0
WireConnection;451;1;460;0
WireConnection;483;0;451;0
WireConnection;239;0;232;2
WireConnection;239;1;240;0
WireConnection;239;2;241;0
WireConnection;842;0;841;0
WireConnection;234;0;146;0
WireConnection;234;1;232;1
WireConnection;693;0;690;0
WireConnection;693;1;691;0
WireConnection;826;0;824;0
WireConnection;826;1;825;0
WireConnection;726;0;725;0
WireConnection;726;1;718;0
WireConnection;544;0;537;0
WireConnection;513;0;517;0
WireConnection;843;0;842;0
WireConnection;237;0;234;0
WireConnection;237;1;238;0
WireConnection;237;2;239;0
WireConnection;457;0;483;0
WireConnection;548;0;513;0
WireConnection;1009;0;1011;2
WireConnection;827;0;826;0
WireConnection;827;1;813;0
WireConnection;562;0;544;0
WireConnection;916;0;726;0
WireConnection;696;0;690;0
WireConnection;696;1;693;0
WireConnection;698;0;688;0
WireConnection;698;1;696;0
WireConnection;698;2;695;0
WireConnection;526;0;548;0
WireConnection;646;0;237;0
WireConnection;773;0;457;0
WireConnection;731;0;726;0
WireConnection;731;1;916;0
WireConnection;541;0;562;0
WireConnection;987;0;1009;0
WireConnection;836;0;827;0
WireConnection;700;0;698;0
WireConnection;837;0;836;0
WireConnection;732;0;731;0
WireConnection;732;1;718;0
WireConnection;765;0;759;0
WireConnection;985;0;987;0
WireConnection;532;0;531;0
WireConnection;532;1;527;0
WireConnection;1026;0;863;0
WireConnection;209;0;91;1
WireConnection;209;1;91;2
WireConnection;138;0;645;0
WireConnection;917;0;732;0
WireConnection;849;0;703;0
WireConnection;1004;0;837;0
WireConnection;210;0;209;0
WireConnection;210;1;91;3
WireConnection;757;0;700;0
WireConnection;757;1;765;0
WireConnection;757;2;703;0
WireConnection;1005;0;985;0
WireConnection;957;0;873;0
WireConnection;560;0;532;0
WireConnection;560;1;561;0
WireConnection;956;0;873;0
WireConnection;1025;0;1026;0
WireConnection;777;0;775;0
WireConnection;121;0;138;0
WireConnection;533;0;529;0
WireConnection;710;0;652;0
WireConnection;710;1;711;0
WireConnection;848;0;757;0
WireConnection;848;1;849;0
WireConnection;1000;0;1004;0
WireConnection;748;0;916;0
WireConnection;768;0;917;0
WireConnection;1007;0;1005;0
WireConnection;528;0;560;0
WireConnection;528;1;533;0
WireConnection;1023;0;1024;0
WireConnection;1023;1;1025;0
WireConnection;1023;2;150;0
WireConnection;776;0;777;0
WireConnection;131;0;121;0
WireConnection;131;1;210;0
WireConnection;75;6;951;0
WireConnection;75;0;901;0
WireConnection;75;1;583;0
WireConnection;75;4;956;0
WireConnection;75;5;957;0
WireConnection;874;0;710;0
WireConnection;874;1;773;0
WireConnection;182;0;75;0
WireConnection;576;0;75;4
WireConnection;632;0;848;0
WireConnection;1008;0;1007;0
WireConnection;247;1;528;0
WireConnection;165;0;651;0
WireConnection;165;1;583;0
WireConnection;436;0;131;0
WireConnection;436;1;776;0
WireConnection;149;0;584;0
WireConnection;149;1;1023;0
WireConnection;137;0;135;0
WireConnection;137;1;136;0
WireConnection;342;0;149;0
WireConnection;342;1;770;0
WireConnection;1002;0;268;0
WireConnection;1002;1;1001;0
WireConnection;1002;2;1003;0
WireConnection;130;0;436;0
WireConnection;130;1;135;0
WireConnection;130;2;137;0
WireConnection;90;0;576;0
WireConnection;453;0;451;0
WireConnection;453;1;483;0
WireConnection;712;0;149;0
WireConnection;712;1;767;0
WireConnection;251;0;247;1
WireConnection;251;1;250;0
WireConnection;423;0;874;0
WireConnection;423;1;165;4
WireConnection;973;0;1008;0
WireConnection;183;0;182;0
WireConnection;183;1;182;1
WireConnection;183;2;182;2
WireConnection;201;0;90;0
WireConnection;566;0;453;0
WireConnection;566;1;460;0
WireConnection;454;0;489;0
WireConnection;968;0;967;0
WireConnection;968;1;973;0
WireConnection;252;0;251;0
WireConnection;129;0;423;0
WireConnection;129;1;634;0
WireConnection;129;2;130;0
WireConnection;74;0;183;0
WireConnection;74;2;342;0
WireConnection;74;3;712;0
WireConnection;74;4;1002;0
WireConnection;995;0;969;1
WireConnection;995;1;969;3
WireConnection;996;0;995;0
WireConnection;464;0;454;0
WireConnection;568;0;566;0
WireConnection;246;0;74;0
WireConnection;246;1;252;0
WireConnection;633;0;129;0
WireConnection;970;0;968;0
WireConnection;970;1;971;0
WireConnection;1022;0;633;0
WireConnection;1022;1;650;0
WireConnection;994;0;970;0
WireConnection;994;1;996;0
WireConnection;994;2;368;0
WireConnection;569;0;568;0
WireConnection;828;0;814;0
WireConnection;270;0;204;4
WireConnection;270;1;649;0
WireConnection;120;0;246;0
WireConnection;552;0;551;0
WireConnection;1055;0;183;0
WireConnection;1055;2;770;0
WireConnection;1055;3;767;0
WireConnection;571;0;569;0
WireConnection;832;0;828;0
WireConnection;367;1;994;0
WireConnection;984;0;980;0
WireConnection;269;0;1022;0
WireConnection;269;1;204;0
WireConnection;269;2;270;0
WireConnection;197;0;120;0
WireConnection;197;1;198;0
WireConnection;158;0;197;0
WireConnection;158;1;269;0
WireConnection;158;2;650;0
WireConnection;734;0;732;0
WireConnection;734;1;917;0
WireConnection;1028;0;149;0
WireConnection;1028;1;1055;0
WireConnection;554;0;552;0
WireConnection;554;1;555;0
WireConnection;1012;0;365;2
WireConnection;220;0;130;0
WireConnection;838;0;832;0
WireConnection;979;0;752;0
WireConnection;979;1;978;0
WireConnection;979;2;984;0
WireConnection;986;0;367;0
WireConnection;986;1;1008;0
WireConnection;230;0;165;4
WireConnection;230;1;231;0
WireConnection;280;0;271;0
WireConnection;341;0;91;1
WireConnection;341;1;91;2
WireConnection;1054;0;1028;2
WireConnection;735;0;734;0
WireConnection;735;1;718;0
WireConnection;553;0;554;0
WireConnection;974;0;247;2
WireConnection;974;1;975;0
WireConnection;572;0;384;4
WireConnection;572;1;573;0
WireConnection;751;0;986;0
WireConnection;751;1;979;0
WireConnection;751;2;368;0
WireConnection;755;0;1012;0
WireConnection;339;0;838;0
WireConnection;339;1;340;0
WireConnection;219;0;661;0
WireConnection;219;1;220;0
WireConnection;260;0;158;0
WireConnection;260;1;230;0
WireConnection;550;0;94;0
WireConnection;550;1;554;0
WireConnection;550;2;553;0
WireConnection;381;0;280;2
WireConnection;381;1;572;0
WireConnection;510;0;512;0
WireConnection;511;0;519;0
WireConnection;509;0;518;0
WireConnection;982;0;735;0
WireConnection;1050;1;1054;0
WireConnection;1050;3;1028;1
WireConnection;498;0;974;0
WireConnection;96;0;94;0
WireConnection;96;1;95;0
WireConnection;221;0;219;0
WireConnection;337;0;341;0
WireConnection;337;1;339;0
WireConnection;366;0;751;0
WireConnection;366;1;260;0
WireConnection;366;2;755;0
WireConnection;738;0;982;0
WireConnection;1053;0;165;1
WireConnection;556;0;550;0
WireConnection;524;0;509;0
WireConnection;524;1;511;0
WireConnection;524;2;510;0
WireConnection;1049;0;1050;0
WireConnection;93;0;337;0
WireConnection;93;1;94;0
WireConnection;93;2;96;0
WireConnection;154;0;662;0
WireConnection;154;1;157;0
WireConnection;222;0;91;0
WireConnection;222;1;210;0
WireConnection;222;2;224;0
WireConnection;706;0;366;0
WireConnection;382;0;381;0
WireConnection;216;0;217;0
WireConnection;216;1;655;0
WireConnection;216;2;221;0
WireConnection;983;0;738;0
WireConnection;523;0;524;0
WireConnection;1035;0;1049;0
WireConnection;401;0;404;0
WireConnection;401;1;406;0
WireConnection;401;2;91;4
WireConnection;214;0;216;0
WireConnection;214;1;164;0
WireConnection;1048;1;165;2
WireConnection;1048;3;1053;0
WireConnection;462;0;154;0
WireConnection;462;1;465;0
WireConnection;225;0;222;0
WireConnection;225;1;93;0
WireConnection;380;0;384;0
WireConnection;380;2;382;0
WireConnection;592;0;706;0
WireConnection;534;0;499;0
WireConnection;534;1;165;2
WireConnection;559;0;558;0
WireConnection;407;0;401;0
WireConnection;851;0;232;3
WireConnection;769;0;983;0
WireConnection;707;0;230;0
WireConnection;385;0;380;0
WireConnection;385;1;386;0
WireConnection;256;0;462;0
WireConnection;256;1;257;0
WireConnection;215;0;214;0
WireConnection;215;1;225;0
WireConnection;549;0;534;0
WireConnection;549;1;658;0
WireConnection;1041;0;1048;0
WireConnection;704;0;592;0
WireConnection;1056;0;1028;4
WireConnection;591;0;259;0
WireConnection;591;1;704;0
WireConnection;839;0;215;0
WireConnection;839;1;91;4
WireConnection;421;0;230;0
WireConnection;421;1;385;0
WireConnection;302;0;260;0
WireConnection;302;1;525;0
WireConnection;155;0;660;0
WireConnection;155;1;156;0
WireConnection;557;0;549;0
WireConnection;557;1;559;0
WireConnection;665;0;256;0
WireConnection;1036;0;1041;0
WireConnection;1036;1;1037;0
WireConnection;299;0;839;0
WireConnection;299;1;302;0
WireConnection;299;2;557;0
WireConnection;412;0;1036;0
WireConnection;412;1;414;0
WireConnection;412;2;413;0
WireConnection;663;0;421;0
WireConnection;258;0;155;0
WireConnection;258;1;591;0
WireConnection;258;2;852;0
WireConnection;999;0;991;0
WireConnection;708;0;665;0
WireConnection;708;1;709;0
WireConnection;887;0;366;0
WireConnection;887;1;888;0
WireConnection;1020;0;1057;0
WireConnection;1020;1;258;0
WireConnection;1020;2;660;0
WireConnection;946;1;708;0
WireConnection;946;2;755;0
WireConnection;940;0;941;0
WireConnection;940;1;299;0
WireConnection;940;2;755;0
WireConnection;998;0;999;0
WireConnection;859;0;412;0
WireConnection;857;0;663;0
WireConnection;772;0;771;0
WireConnection;1018;0;1017;0
WireConnection;323;0;322;0
WireConnection;323;1;325;0
WireConnection;323;2;325;0
WireConnection;740;0;735;0
WireConnection;740;1;899;0
WireConnection;758;0;91;1
WireConnection;898;0;732;0
WireConnection;588;0;587;0
WireConnection;588;1;412;0
WireConnection;766;0;939;0
WireConnection;1071;0;945;0
WireConnection;870;2;432;0
WireConnection;749;0;75;0
WireConnection;960;0;116;0
WireConnection;960;1;964;0
WireConnection;899;0;735;0
WireConnection;855;0;269;0
WireConnection;855;1;854;0
WireConnection;977;0;946;0
WireConnection;589;0;590;0
WireConnection;567;0;566;0
WireConnection;567;1;568;0
WireConnection;330;0;329;0
WireConnection;330;1;331;0
WireConnection;330;2;331;0
WireConnection;964;0;963;0
WireConnection;905;0;917;0
WireConnection;905;1;913;0
WireConnection;422;0;385;0
WireConnection;835;0;827;0
WireConnection;835;1;836;0
WireConnection;897;0;726;0
WireConnection;1021;0;1057;0
WireConnection;924;0;923;0
WireConnection;1065;0;1063;0
WireConnection;1065;1;1064;0
WireConnection;945;1;859;0
WireConnection;945;2;755;0
WireConnection;1064;0;1060;0
WireConnection;1064;1;1061;0
WireConnection;764;0;761;0
WireConnection;1063;0;1062;0
WireConnection;1063;1;1061;0
WireConnection;858;0;940;0
WireConnection;1066;0;1065;0
WireConnection;1079;0;945;0
WireConnection;435;0;747;0
WireConnection;997;0;887;0
WireConnection;997;1;998;0
WireConnection;1077;0;1067;2
WireConnection;1077;1;1070;0
WireConnection;1072;0;1073;4
WireConnection;1074;0;997;0
WireConnection;1074;1;1073;0
WireConnection;1074;2;1079;2
WireConnection;1074;3;1066;0
WireConnection;1074;4;1072;0
WireConnection;1069;0;1067;3
WireConnection;1069;1;1070;0
WireConnection;1058;0;1069;0
WireConnection;1062;0;1059;0
WireConnection;1062;1;1058;0
WireConnection;1062;2;1078;0
WireConnection;1059;0;1068;0
WireConnection;1078;0;1077;0
WireConnection;988;0;989;0
WireConnection;988;2;107;0
WireConnection;988;3;947;0
WireConnection;771;0;567;0
WireConnection;771;1;460;0
WireConnection;856;0;269;0
WireConnection;856;1;854;0
WireConnection;879;0;165;4
WireConnection;539;0;538;0
WireConnection;539;1;537;0
WireConnection;1015;0;1014;0
WireConnection;1017;0;74;0
WireConnection;1068;0;1067;1
WireConnection;1068;1;1070;0
WireConnection;942;0;367;0
WireConnection;942;1;1020;0
WireConnection;942;2;755;0
WireConnection;992;0;221;0
WireConnection;943;0;944;0
WireConnection;943;1;857;0
WireConnection;943;2;755;0
WireConnection;903;0;916;0
WireConnection;903;1;913;0
WireConnection;1076;0;945;0
WireConnection;1075;0;858;0
WireConnection;1075;1;1074;0
WireConnection;587;1;589;0
WireConnection;1027;0;633;0
WireConnection;761;0;759;0
WireConnection;853;0;269;0
WireConnection;853;1;854;0
WireConnection;947;0;873;0
WireConnection;947;1;951;0
WireConnection;947;2;116;0
WireConnection;947;3;108;0
WireConnection;947;4;950;0
WireConnection;947;6;583;0
WireConnection;0;0;997;0
WireConnection;0;1;945;0
WireConnection;0;2;858;0
WireConnection;0;3;977;0
WireConnection;0;4;942;0
WireConnection;0;5;943;0
ASEEND*/
//CHKSM=A843C0EE75A006B5BB70B7D15E35DC21077E9259