// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CScape/CSStreetShader"
{
	Properties
	{
		_Normal1("Normal 1", 2D) = "bump" {}
		_Specular("Specular", 2D) = "white" {}
		[Gamma]_Smoothness("Smoothness", 2D) = "white" {}
		_Puddles("Puddles", Float) = 0
		_Float3("Float 3", Float) = 1
		_Float2("Float 2", Float) = 0
		_Float6("Float 6", Float) = 0
		_grate("grate", 2D) = "white" {}
		_Noise("Noise", 2D) = "white" {}
		_GrateFrequency("GrateFrequency", Float) = 0
		_faloff("faloff", Float) = 0
		_grate1("grate 1", 2D) = "bump" {}
		_GrateSpecular("GrateSpecular", Float) = 0
		_NormalStrenght("NormalStrenght", Float) = 0
		_Mettalic("Mettalic", Float) = 0
		_Float7("Float 7", Float) = 0
		_TextureSample0("Texture Sample 0", 2D) = "white" {}
		_StreetDecalls("StreetDecalls", 2DArray ) = "" {}
		_Float9("Float 9", Float) = 0
		_SidewalkAlbedoCol("SidewalkAlbedoCol", Color) = (0,0,0,0)
		_Street_Albedo("Street_Albedo", Color) = (0,0,0,0)
		_StreetsArray("StreetsArray", 2DArray ) = "" {}
		_TireShineless("TireShineless", Float) = 0
		_ScaleNoise2("ScaleNoise2", Float) = 0
		_ScaleNoise1("ScaleNoise1", Float) = 0
		_ReLightTreshold("ReLightTreshold", Range( 0 , 1)) = 0.32
		_ErodeSigns("ErodeSigns", Range( 0 , 1)) = 0
		_Patches("Patches", Float) = 0
		_patchesLightness("patchesLightness", Float) = 0
		_Saturate("Saturate", Float) = 0
		_TilingLines("TilingLines", Float) = 1
		_TVAO_High_Value("TVAO_High_Value", Float) = 0
		_TVAO_LowValue("TVAO_LowValue", Float) = 0
		[Toggle(_USE_SNOW_MOSS_DIRT_ON)] _Use_Snow_Moss_Dirt("Use_Snow_Moss_Dirt", Float) = 0
		[HideInInspector] _texcoord4( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 4.6
		#pragma shader_feature _USE_SNOW_MOSS_DIRT_ON
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
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
			float2 uv4_texcoord4;
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
		};

		uniform float _NormalStrenght;
		uniform float _Patches;
		uniform sampler2D _Smoothness;
		uniform float _ScaleNoise1;
		uniform float _ScaleNoise2;
		uniform float _patchesLightness;
		uniform sampler2D _Normal1;
		uniform float _Puddles;
		uniform float _Float2;
		uniform sampler2D _grate1;
		uniform float _faloff;
		uniform sampler2D _Noise;
		uniform float _GrateFrequency;
		uniform sampler2D _grate;
		uniform UNITY_DECLARE_TEX2DARRAY( _StreetsArray );
		uniform UNITY_DECLARE_TEX2DARRAY( _StreetDecalls );
		uniform float4 _StreetDecalls_ST;
		uniform float _TilingLines;
		uniform float _ErodeSigns;
		uniform sampler2D _TextureSample0;
		uniform float4 _SidewalkAlbedoCol;
		uniform float4 _Street_Albedo;
		uniform sampler2D _ReLightingControlTex;
		uniform float4 _ReLightingProjection;
		uniform float _Saturate;
		uniform float _TVAO_LowValue;
		uniform float _TVAO_High_Value;
		uniform float _CSReLight;
		uniform float _ReLightTreshold;
		uniform float4 _reLightColor;
		uniform float _CSReLightDistance;
		uniform float _Float3;
		uniform float _Float7;
		uniform sampler2D _Specular;
		uniform float _Float6;
		uniform float _GrateSpecular;
		uniform float _Mettalic;
		uniform float _Float9;
		uniform float _TireShineless;

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float3 ase_worldPos = i.worldPos;
			float2 appendResult427 = (float2(ase_worldPos.y , ase_worldPos.z));
			float2 appendResult423 = (float2(ase_worldPos.y , ase_worldPos.x));
			float2 appendResult376 = (float2(ase_worldPos.x , ase_worldPos.z));
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_vertexNormal = mul( unity_WorldToObject, float4( ase_worldNormal, 0 ) );
			float2 lerpResult422 = lerp( appendResult423 , appendResult376 , step( abs( ase_vertexNormal.z ) , 0.5 ));
			float2 lerpResult426 = lerp( appendResult427 , lerpResult422 , step( abs( ase_vertexNormal.x ) , 0.5 ));
			float2 worldUV414 = lerpResult426;
			float4 tex2DNode325 = tex2D( _Smoothness, ( worldUV414 * _ScaleNoise1 ) );
			float4 tex2DNode337 = tex2D( _Smoothness, ( worldUV414 * _ScaleNoise2 ) );
			float blendOpSrc522 = tex2DNode325.g;
			float blendOpDest522 = tex2DNode337.g;
			float smoothstepResult532 = smoothstep( _Patches , ( _Patches + 0.2 ) , ( saturate( ( blendOpDest522 / blendOpSrc522 ) )));
			float clampResult538 = clamp( ( smoothstepResult532 + _patchesLightness ) , 0.0 , 1.0 );
			float smoothstepResult327 = smoothstep( _Puddles , ( _Puddles + 0.7 ) , ( ( tex2DNode337.r * _Float2 ) + tex2DNode325.r ));
			float3 lerpResult331 = lerp( float3(0,0,1) , UnpackScaleNormal( tex2D( _Normal1, worldUV414 ), ( _NormalStrenght * clampResult538 ) ) , smoothstepResult327);
			float3 ase_vertex3Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
			float2 appendResult370 = (float2(ase_vertex3Pos.x , ase_vertex3Pos.z));
			float2 temp_output_373_0 = ( appendResult370 * float2( 1,1 ) );
			float smoothstepResult358 = smoothstep( _faloff , ( _faloff + 0.01 ) , ( tex2D( _Noise, ( temp_output_373_0 / float2( 64,64 ) ) ).r * _GrateFrequency ));
			float4 tex2DNode353 = tex2D( _grate, temp_output_373_0 );
			float temp_output_367_0 = ( smoothstepResult358 * tex2DNode353.a );
			float3 lerpResult362 = lerp( lerpResult331 , UnpackNormal( tex2D( _grate1, temp_output_373_0 ) ) , temp_output_367_0);
			o.Normal = lerpResult362;
			float4 texArray400 = UNITY_SAMPLE_TEX2DARRAY(_StreetsArray, float3(worldUV414, i.uv4_texcoord4.x)  );
			float lerpResult517 = lerp( 0.94 , 1.0 , smoothstepResult327);
			float2 uv_StreetDecalls = i.uv_texcoord * _StreetDecalls_ST.xy + _StreetDecalls_ST.zw;
			float4 texArray384 = UNITY_SAMPLE_TEX2DARRAY(_StreetDecalls, float3(uv_StreetDecalls, ( i.vertexColor.r * 10.0 ))  );
			float2 break508 = step( frac( ( ( ( 1.0 - i.uv_texcoord ) + float2( 0,0 ) ) * float2( 0.5,0.5 ) * _TilingLines ) ) , float2( 0.03,0.03 ) );
			float clampResult507 = clamp( ( break508.x + break508.y ) , 0.0 , 1.0 );
			float temp_output_490_0 = ( 0.5 + 0.04 );
			float smoothstepResult488 = smoothstep( 0.5 , temp_output_490_0 , frac( ( i.uv_texcoord.y * 2.0 * _TilingLines ) ));
			float smoothstepResult494 = smoothstep( 0.5 , temp_output_490_0 , frac( ( i.uv_texcoord.x * 2.0 * _TilingLines ) ));
			float SplitLineMask511 = ( clampResult507 * abs( ( smoothstepResult488 - smoothstepResult494 ) ) );
			float lerpResult513 = lerp( texArray384.x , 0.0 , SplitLineMask511);
			float4 temp_cast_0 = (lerpResult513).xxxx;
			float DiffuseCol475 = texArray400.r;
			float smoothstepResult481 = smoothstep( _ErodeSigns , ( _ErodeSigns + 0.1 ) , DiffuseCol475);
			float clampResult478 = clamp( ( lerpResult513 * smoothstepResult481 ) , 0.0 , 1.0 );
			float4 lerpResult352 = lerp( ( texArray400 * lerpResult517 ) , temp_cast_0 , clampResult478);
			float4 lerpResult357 = lerp( lerpResult352 , tex2DNode353 , temp_output_367_0);
			float4 lerpResult382 = lerp( lerpResult357 , tex2D( _TextureSample0, worldUV414 ) , i.vertexColor.g);
			float OldMask523 = clampResult538;
			float clampResult521 = clamp( i.uv4_texcoord4.x , 0.0 , 1.0 );
			float4 lerpResult519 = lerp( _SidewalkAlbedoCol , ( _Street_Albedo * OldMask523 ) , step( clampResult521 , 0.9 ));
			float4 temp_output_398_0 = ( lerpResult382 * lerpResult519 );
			float2 appendResult3_g5 = (float2(ase_worldPos.x , ase_worldPos.z));
			float4 break559 = tex2D( _ReLightingControlTex, ( ( appendResult3_g5 / (_ReLightingProjection).xy ) + (_ReLightingProjection).zw ) );
			float AODepthData560 = break559.g;
			float temp_output_573_0 = (_TVAO_LowValue + (pow( AODepthData560 , _Saturate ) - 0.0) * (_TVAO_High_Value - _TVAO_LowValue) / (10.0 - 0.0));
			float temp_output_567_0 = saturate( temp_output_573_0 );
			float temp_output_580_0 = pow( temp_output_567_0 , 10.0 );
			float temp_output_585_0 = ( 1.0 - saturate( ase_worldNormal.y ) );
			float lerpResult587 = lerp( temp_output_580_0 , 1.0 , temp_output_585_0);
			float4 lerpResult578 = lerp( float4(0.7867647,0.7867647,0.7867647,1) , ( temp_output_398_0 * temp_output_567_0 ) , lerpResult587);
			#ifdef _USE_SNOW_MOSS_DIRT_ON
				float4 staticSwitch577 = lerpResult578;
			#else
				float4 staticSwitch577 = ( temp_output_398_0 * temp_output_567_0 );
			#endif
			o.Albedo = ( staticSwitch577 * temp_output_567_0 ).rgb;
			float clampResult470 = clamp( ( distance( ase_worldPos , _WorldSpaceCameraPos ) * _CSReLightDistance ) , 0.0 , 1.0 );
			float4 ifLocalVar464 = 0;
			UNITY_BRANCH 
			if( _CSReLight < _ReLightTreshold )
				ifLocalVar464 = ( ( staticSwitch577 * _reLightColor * ase_worldNormal.y * break559.r * ( _reLightColor.a * 10.0 ) ) * clampResult470 );
			o.Emission = ( ifLocalVar464 * temp_output_567_0 ).rgb;
			float clampResult381 = clamp( smoothstepResult327 , 0.1 , 1.0 );
			float lerpResult326 = lerp( _Float3 , smoothstepResult327 , clampResult381);
			float4 temp_cast_3 = (( lerpResult326 + _Float7 )).xxxx;
			float4 lerpResult335 = lerp( temp_cast_3 , ( tex2D( _Specular, worldUV414 ) * _Float6 ) , clampResult381);
			float4 temp_cast_4 = (( tex2DNode353.r * _GrateSpecular )).xxxx;
			float4 lerpResult363 = lerp( lerpResult335 , temp_cast_4 , temp_output_367_0);
			float4 clampResult395 = clamp( lerpResult363 , float4( 0,0,0,0 ) , float4( 1,1,1,1 ) );
			o.Specular = ( clampResult395 * _Mettalic ).rgb;
			o.Smoothness = ( ( lerpResult363 * _Float9 ) + ( texArray384.y * _TireShineless * temp_output_398_0 ) ).r;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma exclude_renderers d3d9 gles d3d11_9x ps4 psp2 n3ds 
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
				float4 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
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
				o.customPack1.xy = customInputData.uv4_texcoord4;
				o.customPack1.xy = v.texcoord3;
				o.customPack1.zw = customInputData.uv_texcoord;
				o.customPack1.zw = v.texcoord;
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
				surfIN.uv4_texcoord4 = IN.customPack1.xy;
				surfIN.uv_texcoord = IN.customPack1.zw;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
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
Version=15800
31;361;2480;850;-5140.332;-1353.994;1.678336;True;True
Node;AmplifyShaderEditor.TextureCoordinatesNode;485;6352.017,1234.112;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;514;6667.599,1413.659;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;570;6491.761,1535.436;Float;False;Property;_TilingLines;TilingLines;34;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;505;6830.217,1472.637;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.NormalVertexDataNode;417;1437.886,1806.935;Float;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;501;6721.132,1658.117;Float;False;3;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.AbsOpNode;415;1735.746,1777.943;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;375;1370.284,1482.556;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;486;6987.135,1364.234;Float;True;3;3;0;FLOAT;0;False;1;FLOAT;2;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;492;7182.447,1275.692;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;2;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;489;7448.146,1376.432;Float;False;Constant;_tiling;tiling;37;0;Create;True;0;0;False;0;0.5;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;503;6873.357,1668.688;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.AbsOpNode;424;1715.017,1894.96;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;376;1678.686,1486.139;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StepOpNode;421;1950.157,1899.866;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;423;1779.877,1586.398;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FractNode;487;7386.444,1528.709;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;490;7217.219,1600.836;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.04;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;504;7034.039,1681.374;Float;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.03,0.03;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FractNode;493;7458.44,1210.388;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;427;1912.469,1715.265;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StepOpNode;425;1929.428,2016.883;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;422;1954.981,1534.94;Float;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SmoothstepOpNode;488;7630.511,1499.175;Float;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;494;7710.382,1326.508;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;426;2176.269,1663.703;Float;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.BreakToComponentsNode;508;7212.088,1746.914;Float;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleAddOpNode;506;7500.078,1753.257;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;497;7971.398,1519.986;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;412;2534.803,1202.865;Float;False;Property;_ScaleNoise1;ScaleNoise1;28;0;Create;True;0;0;False;0;0;0.01;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;414;2256.868,1471.077;Float;False;worldUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;411;2749.718,1465.756;Float;False;Property;_ScaleNoise2;ScaleNoise2;27;0;Create;True;0;0;False;0;0;0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;403;1972.555,1075.675;Float;False;3;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;409;2816.709,1086.019;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.AbsOpNode;499;7960.862,1650.576;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;507;7658.645,1763.585;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;369;2713.759,3700.51;Float;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;410;2908.001,1338.53;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;533;4007.01,1007.99;Float;False;Property;_Patches;Patches;31;0;Create;True;0;0;False;0;0;0.73;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;370;2969.458,3741.114;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureArrayNode;400;2363.365,934.4376;Float;True;Property;_StreetsArray;StreetsArray;25;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;347;3329.174,2715.827;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;509;7885.868,1747.298;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;337;3222.232,1160.101;Float;True;Property;_TextureSample6;Texture Sample 6;5;0;Create;True;0;0;False;0;None;3ce46f687cf9deb40aa0d90c50165748;True;0;False;white;Auto;False;Instance;325;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;342;3628.602,1073.813;Float;False;Property;_Float2;Float 2;8;0;Create;True;0;0;False;0;0;0.92;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;325;3222.06,895.1548;Float;True;Property;_Smoothness;Smoothness;5;1;[Gamma];Create;True;0;0;False;0;None;3ce46f687cf9deb40aa0d90c50165748;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BlendOpsNode;522;3839,844.3472;Float;False;Divide;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;482;3949.927,3016.88;Float;False;Property;_ErodeSigns;ErodeSigns;30;0;Create;True;0;0;False;0;0;0.177;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;534;4196.192,1050.982;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;511;8192.818,1733.139;Float;False;SplitLineMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;475;2739.727,925.8188;Float;False;DiffuseCol;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;396;3669.854,2815.812;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;328;3464.001,1527.112;Float;False;Property;_Puddles;Puddles;6;0;Create;True;0;0;False;0;0;0.11;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;341;3845.802,1021.012;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;373;3214.359,3801.212;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;1,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;512;4189.99,2611.286;Float;False;511;SplitLineMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;532;4442.536,916.3994;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;476;4097.69,2926.389;Float;False;475;DiffuseCol;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;483;4189.466,3112.799;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;536;4637.575,1102.486;Float;False;Property;_patchesLightness;patchesLightness;32;0;Create;True;0;0;False;0;0;0.84;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;329;3723.805,1580.812;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.7;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;340;3690.803,1279.713;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;372;3298.258,3513.614;Float;False;2;0;FLOAT2;0,0;False;1;FLOAT2;64,64;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureArrayNode;384;3915.119,2730.321;Float;True;Property;_StreetDecalls;StreetDecalls;20;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;356;3408.656,3308.21;Float;False;Property;_GrateFrequency;GrateFrequency;12;0;Create;True;0;0;False;0;0;9;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;481;4324.682,3022.621;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;537;4824.104,944.4852;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;354;3351.658,3011.811;Float;True;Property;_Noise;Noise;11;0;Create;True;0;0;False;0;6f9a0e55e3ffc6044944be5e3035e195;ba5087a48b3d70c4098ef50deb41a792;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;569;5451.796,2832.332;Float;False;CScapeReLighting;1;;5;dc5e3dd0bdebf8d43be648b95d13eb58;0;0;1;COLOR;0
Node;AmplifyShaderEditor.SmoothstepOpNode;327;3912.277,1393.881;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;360;3479.756,3490.91;Float;False;Property;_faloff;faloff;13;0;Create;True;0;0;False;0;0;1.54;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;513;4419.681,2686.24;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;538;4975.521,887.4293;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;355;3670.857,3051.71;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;359;3760.65,3216.975;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;517;4259.965,1645.856;Float;False;3;0;FLOAT;0.94;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;477;4511.087,2870.742;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;559;5711.139,2573.728;Float;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SamplerNode;353;3478.058,3685.009;Float;True;Property;_grate;grate;10;0;Create;True;0;0;False;0;230db1c933d400a45ae6f454858ef2b9;230db1c933d400a45ae6f454858ef2b9;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;523;5202.465,838.2023;Float;False;OldMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;560;5975.139,2589.728;Float;False;AODepthData;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;358;3886.121,3074.563;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;478;4673.135,2835.784;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;336;4208.763,2322.016;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;521;5035,1786.646;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;518;4747.153,1395.319;Float;False;Property;_Street_Albedo;Street_Albedo;24;0;Create;True;0;0;False;0;0,0,0,0;0.5514706,0.5352507,0.5352507,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;352;4776.195,2647.963;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;367;4313.387,3162.19;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;525;4729.797,1693.537;Float;False;523;OldMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;568;7954.571,2689.685;Float;False;Property;_Saturate;Saturate;33;0;Create;True;0;0;False;0;0;1.81;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;561;8072.176,2621.789;Float;False;560;AODepthData;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;571;8133.678,2877.836;Float;False;Property;_TVAO_LowValue;TVAO_LowValue;37;0;Create;True;0;0;False;0;0;0.26;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;524;5398.189,1628.553;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;383;4496.195,1855.496;Float;True;Property;_TextureSample0;Texture Sample 0;19;0;Create;True;0;0;False;0;None;84508b93f15f2b64386ec07486afc7a3;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;572;8129.843,3010.453;Float;False;Property;_TVAO_High_Value;TVAO_High_Value;36;0;Create;True;0;0;False;0;0;8.23;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;357;4982.109,2826.04;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.PowerNode;566;8344.669,2637.733;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;397;4972.601,1597.381;Float;False;Property;_SidewalkAlbedoCol;SidewalkAlbedoCol;23;0;Create;True;0;0;False;0;0,0,0,0;0.5073529,0.5073529,0.5073529,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StepOpNode;520;5211.859,1805.473;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.9;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;445;5639.576,2191.587;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.LerpOp;519;5457.874,1829.31;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode;573;8433.643,2943.94;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;10;False;3;FLOAT;0.2;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;382;4995.229,1957.146;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;567;8134.096,2409.219;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;398;5518.367,1965.153;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;586;7125.084,2510.757;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;580;7543.14,2484.583;Float;False;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;563;5770.573,1879.243;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;10;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;585;7290.409,2503.617;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;330;3630.047,1837.824;Float;False;Property;_Float3;Float 3;7;0;Create;True;0;0;False;0;1;1.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;381;3996.953,1778.71;Float;False;3;0;FLOAT;0;False;1;FLOAT;0.1;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;321;3173.82,2255.97;Float;True;Property;_Specular;Specular;4;0;Create;True;0;0;False;0;None;6f2663be96abdbf40bd995f7b189a8f6;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;326;3909.6,1993.014;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;473;5984.603,2392.891;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RelayNode;491;6910.44,1865.082;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;587;7453.602,2447.601;Float;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;579;6929,2023.009;Float;False;Constant;_Color0;Color 0;38;0;Create;True;0;0;False;0;0.7867647,0.7867647,0.7867647,1;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;380;4026.859,2126.812;Float;False;Property;_Float7;Float 7;18;0;Create;True;0;0;False;0;0;-0.46;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;350;3644.947,2343.99;Float;False;Property;_Float6;Float 6;9;0;Create;True;0;0;False;0;0;0.33;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;447;5383.348,2961.312;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;366;4474.028,2575.442;Float;False;Property;_GrateSpecular;GrateSpecular;15;0;Create;True;0;0;False;0;0;0.53;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;578;7318.463,1986.303;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;474;6388.313,2526.811;Float;False;Global;_CSReLightDistance;_CSReLightDistance;45;0;Create;True;0;0;False;0;0;0.007084662;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;444;5453.009,2290.635;Float;False;Global;_reLightColor;_reLightColor;41;0;Create;True;0;0;False;0;0.8676471,0.7320442,0.4402033,0;0.9338235,0.7337997,0.4806445,0.703;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;349;3833.441,2175.983;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;379;4215.56,2031.412;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DistanceOpNode;472;6372.493,2365.226;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;577;7579.179,1889.176;Float;False;Property;_Use_Snow_Moss_Dirt;Use_Snow_Moss_Dirt;39;0;Create;True;0;0;False;0;0;0;1;True;;Toggle;2;Key0;Key1;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;374;2494.358,1681.812;Float;False;Property;_NormalStrenght;NormalStrenght;16;0;Create;True;0;0;False;0;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;365;4743.316,2476.463;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;335;4439.099,2217.215;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;463;5717.26,2398.197;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;471;6591.831,2373.939;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;394;5220.456,2646.79;Float;False;Property;_Float9;Float 9;22;0;Create;True;0;0;False;0;0;2.85;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;470;6782.502,2316.713;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;539;2832.578,1936.378;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;407;5183.982,2829.319;Float;False;Property;_TireShineless;TireShineless;26;0;Create;True;0;0;False;0;0;37.07;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;443;5965.004,2194.982;Float;False;5;5;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;363;4997.356,2467.609;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;469;6332.272,2187.33;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;466;6322.142,2016.222;Float;False;Global;_CSReLight;_CSReLight;44;0;Create;True;0;0;False;0;2;0.06671931;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;406;5408.436,2689.744;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;393;5354.853,2382.08;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.Vector3Node;333;3424.403,1855.112;Float;False;Constant;_Vector0;Vector 0;17;0;Create;True;0;0;False;0;0,0,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;378;4863.66,2212.411;Float;False;Property;_Mettalic;Mettalic;17;0;Create;True;0;0;False;0;0;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;318;2909.003,1653.811;Float;True;Property;_Normal1;Normal 1;0;0;Create;True;0;0;False;0;None;8ec80db84d0480045abd3ef9031d4939;True;0;True;bump;LockedToTexture2D;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ClampOpNode;395;5056.756,2304.212;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,1,1,1;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;467;5968.244,2064.123;Float;False;Property;_ReLightTreshold;ReLightTreshold;29;0;Create;True;0;0;False;0;0.32;0.131;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;464;6596.646,2024.523;Float;False;True;5;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT4;0,0,0,0;False;3;FLOAT;0;False;4;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;377;5195.261,2203.711;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;331;3551.24,2144.968;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;405;5557.326,2542.627;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;361;4283.355,3543.71;Float;True;Property;_grate1;grate 1;14;0;Create;True;0;0;False;0;ff6cb15a58c04d443ab54b3e1fae6cfc;ff6cb15a58c04d443ab54b3e1fae6cfc;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;576;7959.506,2086.061;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RelayNode;515;7615.254,2789.635;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;362;4930.456,3029.31;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RelayNode;516;7826.219,2219.619;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;575;7994.437,1850.963;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RelayNode;540;8456.872,1943.361;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FractNode;448;5854.411,2717.779;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;420;5401.044,2094.303;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;545;6849.753,3394.631;Float;True;Global;_ReLightingControlTex;_ReLightingControlTex;35;0;Create;True;0;0;False;0;None;92dbb2a403ac83f45a6f7973909742fa;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;461;5651.866,3139.184;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;449;5844.901,2823.859;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;450;6020.785,2697.479;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;364;4294.758,3338.708;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;582;7692.163,2593.037;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0.3602941,0.3602941,0.3602941,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;581;7866.158,2503.44;Float;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ComponentMaskNode;556;6513.405,3713.79;Float;False;False;False;True;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode;543;8461.827,2165.136;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;416;1561.034,2085.881;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;293;3074.418,2483.097;Float;False;Constant;_Float5;Float 5;38;0;Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;593;8678.594,2380.615;Float;False;Property;_tesselation;tesselation;38;0;Create;True;0;0;False;0;0;115.7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;583;7807.351,2381.611;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;456;6030.383,2857.062;Float;False;Global;_lightsContour;_lightsContour;33;0;Create;True;0;0;False;0;0.1;-1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;549;6528.945,3495.059;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode;542;8459.896,2090.203;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RelayNode;544;8458.895,2242.045;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;390;4022.454,2615.409;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.DistanceOpNode;451;6480.055,2861.309;Float;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;462;5325.678,3190.934;Float;False;Global;_LightsDistance;_LightsDistance;34;0;Create;True;0;0;False;0;0.1;0.06;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;454;6799.073,2798.248;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;591;7922.357,2785.184;Float;False;588;tiresMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;320;4223.002,1421.512;Float;False;Property;_Float1;Float 1;3;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;584;7411.431,2358.033;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;528;3059.916,1135.137;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;283;2885.483,3396.796;Float;False;Property;_Lightsheight;Lightsheight;21;0;Create;True;0;0;False;0;0.05;0;0;0.05;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;588;4303.441,2823.493;Float;False;tiresMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;547;6258.112,3473.792;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector2Node;458;6046.463,2978.304;Float;False;Constant;_Vector2;Vector 2;42;0;Create;True;0;0;False;0;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SamplerNode;546;7191.253,3531.442;Float;True;Property;_TextureSample1;Texture Sample 1;32;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;452;5781.417,3329.09;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;314;2504.5,1861.113;Float;False;Constant;_Color2;Color 2;7;0;Create;True;0;0;False;0;0,0,0,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;551;6932.875,3714.398;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode;541;8465.052,2017.671;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector4Node;554;6180.881,3698.505;Float;False;Global;_ReLightingProjection;_ReLightingProjection;32;0;Create;True;0;0;False;0;0,0,0,0;1991,1991,0.06504269,0.1283275;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;460;5669.444,3009.551;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;459;6312.41,2848.97;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;574;8746.49,2646.905;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;589;8213.241,2780.06;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;555;6503.958,3617.692;Float;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;455;6224.597,2688.146;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;553;6764.372,3585.917;Float;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;8952.744,1950.581;Float;False;True;6;Float;ASEMaterialInspector;0;0;StandardSpecular;CScape/CSStreetShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;False;True;True;False;True;True;False;True;True;False;False;False;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;9.3;10;25;True;0.764;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;1;False;-1;1;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;514;0;485;0
WireConnection;505;0;514;0
WireConnection;501;0;505;0
WireConnection;501;2;570;0
WireConnection;415;0;417;3
WireConnection;486;0;485;2
WireConnection;486;2;570;0
WireConnection;492;0;485;1
WireConnection;492;2;570;0
WireConnection;503;0;501;0
WireConnection;424;0;417;1
WireConnection;376;0;375;1
WireConnection;376;1;375;3
WireConnection;421;0;415;0
WireConnection;423;0;375;2
WireConnection;423;1;375;1
WireConnection;487;0;486;0
WireConnection;490;0;489;0
WireConnection;504;0;503;0
WireConnection;493;0;492;0
WireConnection;427;0;375;2
WireConnection;427;1;375;3
WireConnection;425;0;424;0
WireConnection;422;0;423;0
WireConnection;422;1;376;0
WireConnection;422;2;421;0
WireConnection;488;0;487;0
WireConnection;488;1;489;0
WireConnection;488;2;490;0
WireConnection;494;0;493;0
WireConnection;494;1;489;0
WireConnection;494;2;490;0
WireConnection;426;0;427;0
WireConnection;426;1;422;0
WireConnection;426;2;425;0
WireConnection;508;0;504;0
WireConnection;506;0;508;0
WireConnection;506;1;508;1
WireConnection;497;0;488;0
WireConnection;497;1;494;0
WireConnection;414;0;426;0
WireConnection;409;0;414;0
WireConnection;409;1;412;0
WireConnection;499;0;497;0
WireConnection;507;0;506;0
WireConnection;410;0;414;0
WireConnection;410;1;411;0
WireConnection;370;0;369;1
WireConnection;370;1;369;3
WireConnection;400;0;414;0
WireConnection;400;1;403;1
WireConnection;509;0;507;0
WireConnection;509;1;499;0
WireConnection;337;1;410;0
WireConnection;325;1;409;0
WireConnection;522;0;325;2
WireConnection;522;1;337;2
WireConnection;534;0;533;0
WireConnection;511;0;509;0
WireConnection;475;0;400;1
WireConnection;396;0;347;1
WireConnection;341;0;337;1
WireConnection;341;1;342;0
WireConnection;373;0;370;0
WireConnection;532;0;522;0
WireConnection;532;1;533;0
WireConnection;532;2;534;0
WireConnection;483;0;482;0
WireConnection;329;0;328;0
WireConnection;340;0;341;0
WireConnection;340;1;325;1
WireConnection;372;0;373;0
WireConnection;384;1;396;0
WireConnection;481;0;476;0
WireConnection;481;1;482;0
WireConnection;481;2;483;0
WireConnection;537;0;532;0
WireConnection;537;1;536;0
WireConnection;354;1;372;0
WireConnection;327;0;340;0
WireConnection;327;1;328;0
WireConnection;327;2;329;0
WireConnection;513;0;384;1
WireConnection;513;2;512;0
WireConnection;538;0;537;0
WireConnection;355;0;354;1
WireConnection;355;1;356;0
WireConnection;359;0;360;0
WireConnection;517;2;327;0
WireConnection;477;0;513;0
WireConnection;477;1;481;0
WireConnection;559;0;569;0
WireConnection;353;1;373;0
WireConnection;523;0;538;0
WireConnection;560;0;559;1
WireConnection;358;0;355;0
WireConnection;358;1;360;0
WireConnection;358;2;359;0
WireConnection;478;0;477;0
WireConnection;336;0;400;0
WireConnection;336;1;517;0
WireConnection;521;0;403;1
WireConnection;352;0;336;0
WireConnection;352;1;513;0
WireConnection;352;2;478;0
WireConnection;367;0;358;0
WireConnection;367;1;353;4
WireConnection;524;0;518;0
WireConnection;524;1;525;0
WireConnection;383;1;414;0
WireConnection;357;0;352;0
WireConnection;357;1;353;0
WireConnection;357;2;367;0
WireConnection;566;0;561;0
WireConnection;566;1;568;0
WireConnection;520;0;521;0
WireConnection;519;0;397;0
WireConnection;519;1;524;0
WireConnection;519;2;520;0
WireConnection;573;0;566;0
WireConnection;573;3;571;0
WireConnection;573;4;572;0
WireConnection;382;0;357;0
WireConnection;382;1;383;0
WireConnection;382;2;347;2
WireConnection;567;0;573;0
WireConnection;398;0;382;0
WireConnection;398;1;519;0
WireConnection;586;0;445;2
WireConnection;580;0;567;0
WireConnection;563;0;398;0
WireConnection;563;1;567;0
WireConnection;585;0;586;0
WireConnection;381;0;327;0
WireConnection;321;1;414;0
WireConnection;326;0;330;0
WireConnection;326;1;327;0
WireConnection;326;2;381;0
WireConnection;491;0;563;0
WireConnection;587;0;580;0
WireConnection;587;2;585;0
WireConnection;578;0;579;0
WireConnection;578;1;491;0
WireConnection;578;2;587;0
WireConnection;349;0;321;0
WireConnection;349;1;350;0
WireConnection;379;0;326;0
WireConnection;379;1;380;0
WireConnection;472;0;447;0
WireConnection;472;1;473;0
WireConnection;577;1;491;0
WireConnection;577;0;578;0
WireConnection;365;0;353;1
WireConnection;365;1;366;0
WireConnection;335;0;379;0
WireConnection;335;1;349;0
WireConnection;335;2;381;0
WireConnection;463;0;444;4
WireConnection;471;0;472;0
WireConnection;471;1;474;0
WireConnection;470;0;471;0
WireConnection;539;0;374;0
WireConnection;539;1;538;0
WireConnection;443;0;577;0
WireConnection;443;1;444;0
WireConnection;443;2;445;2
WireConnection;443;3;559;0
WireConnection;443;4;463;0
WireConnection;363;0;335;0
WireConnection;363;1;365;0
WireConnection;363;2;367;0
WireConnection;469;0;443;0
WireConnection;469;1;470;0
WireConnection;406;0;384;2
WireConnection;406;1;407;0
WireConnection;406;2;398;0
WireConnection;393;0;363;0
WireConnection;393;1;394;0
WireConnection;318;1;414;0
WireConnection;318;5;539;0
WireConnection;395;0;363;0
WireConnection;464;0;466;0
WireConnection;464;1;467;0
WireConnection;464;4;469;0
WireConnection;377;0;395;0
WireConnection;377;1;378;0
WireConnection;331;0;333;0
WireConnection;331;1;318;0
WireConnection;331;2;327;0
WireConnection;405;0;393;0
WireConnection;405;1;406;0
WireConnection;361;1;373;0
WireConnection;576;0;464;0
WireConnection;576;1;567;0
WireConnection;515;0;377;0
WireConnection;362;0;331;0
WireConnection;362;1;361;0
WireConnection;362;2;367;0
WireConnection;516;0;405;0
WireConnection;575;0;577;0
WireConnection;575;1;567;0
WireConnection;540;0;575;0
WireConnection;448;0;460;0
WireConnection;461;0;447;3
WireConnection;461;1;462;0
WireConnection;449;0;461;0
WireConnection;450;0;448;0
WireConnection;450;1;449;0
WireConnection;364;0;353;1
WireConnection;582;0;515;0
WireConnection;581;0;567;0
WireConnection;581;1;582;0
WireConnection;556;0;554;0
WireConnection;543;0;515;0
WireConnection;583;0;515;0
WireConnection;583;1;567;0
WireConnection;549;0;547;1
WireConnection;549;1;547;3
WireConnection;542;0;576;0
WireConnection;544;0;516;0
WireConnection;390;0;347;1
WireConnection;451;0;455;0
WireConnection;451;1;459;0
WireConnection;454;0;451;0
WireConnection;584;0;580;0
WireConnection;584;1;585;0
WireConnection;588;0;384;2
WireConnection;546;0;545;0
WireConnection;546;1;551;0
WireConnection;551;0;553;0
WireConnection;551;1;556;0
WireConnection;541;0;362;0
WireConnection;460;0;447;1
WireConnection;460;1;462;0
WireConnection;459;0;458;0
WireConnection;459;1;456;0
WireConnection;574;0;573;0
WireConnection;555;0;554;0
WireConnection;455;0;450;0
WireConnection;455;1;456;0
WireConnection;553;0;549;0
WireConnection;553;1;555;0
WireConnection;0;0;540;0
WireConnection;0;1;541;0
WireConnection;0;2;542;0
WireConnection;0;3;543;0
WireConnection;0;4;544;0
ASEEND*/
//CHKSM=59B5F3A94DCBF44BA64ADBB3BD51ABD680854449