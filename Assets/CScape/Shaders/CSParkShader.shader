// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CScape/CSParkShader"
{
	Properties
	{
		_lightsContour("_lightsContour", Float) = 0.1
		_LightsDistance("_LightsDistance", Float) = 0.1
		_ReLightTreshold("ReLightTreshold", Range( 0 , 1)) = 0.32
		_Grass_Albedo("Grass_Albedo", 2D) = "white" {}
		_Grass_Normal("Grass_Normal", 2D) = "bump" {}
		_Texture0("Texture 0", 2D) = "white" {}
		_AlbedoCol("AlbedoCol", Color) = (0.6838235,0.6838235,0.6838235,0)
		_SpecularMultiply("SpecularMultiply", Float) = 0
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.5
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) fixed3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
		};

		uniform sampler2D _Grass_Normal;
		uniform sampler2D _Grass_Albedo;
		uniform float4 _AlbedoCol;
		uniform float _CSReLight;
		uniform float _ReLightTreshold;
		uniform float4 _reLightColor;
		uniform float _LightsDistance;
		uniform float _lightsContour;
		uniform float _CSReLightDistance;
		uniform sampler2D _Texture0;
		uniform float _SpecularMultiply;

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float3 ase_worldPos = i.worldPos;
			float2 appendResult549 = (float2(ase_worldPos.x , ase_worldPos.z));
			float2 temp_output_548_0 = ( appendResult549 * float2( 0.2,0.2 ) );
			o.Normal = UnpackNormal( tex2D( _Grass_Normal, temp_output_548_0 ) );
			float4 DiffuseInput540 = ( tex2D( _Grass_Albedo, temp_output_548_0 ) * _AlbedoCol );
			o.Albedo = DiffuseInput540.rgb;
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float2 appendResult450 = (float2(frac( ( ase_worldPos.x * _LightsDistance ) ) , frac( ( ase_worldPos.z * _LightsDistance ) )));
			float clampResult470 = clamp( ( distance( ase_worldPos , _WorldSpaceCameraPos ) * _CSReLightDistance ) , 0.0 , 1.0 );
			float4 ifLocalVar464 = 0;
			UNITY_BRANCH 
			if( _CSReLight < _ReLightTreshold )
				ifLocalVar464 = ( ( DiffuseInput540 * _reLightColor * ase_worldNormal.y * ( 1.0 - distance( ( appendResult450 * _lightsContour ) , ( float2( 0.5,0.5 ) * _lightsContour ) ) ) * ( _reLightColor.a * 10.0 ) ) * clampResult470 );
			o.Emission = ifLocalVar464.rgb;
			float4 tex2DNode552 = tex2D( _Texture0, temp_output_548_0 );
			float3 temp_cast_2 = (( tex2DNode552.r * _SpecularMultiply )).xxx;
			o.Specular = temp_cast_2;
			o.Smoothness = ( 1.0 - tex2DNode552.g );
			o.Occlusion = tex2DNode552.a;
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
				float4 tSpace0 : TEXCOORD1;
				float4 tSpace1 : TEXCOORD2;
				float4 tSpace2 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				fixed3 worldNormal = UnityObjectToWorldNormal( v.normal );
				fixed3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				fixed3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			fixed4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				fixed3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
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
Version=15302
42;279;1697;764;-5418.012;-2835.534;1;True;True
Node;AmplifyShaderEditor.WorldPosInputsNode;447;5383.348,2961.312;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;462;5416.572,3241.287;Float;False;Property;_LightsDistance;_LightsDistance;1;0;Create;True;0;0;False;0;0.1;0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;460;5669.444,3009.551;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;461;5651.866,3139.184;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;547;3847.815,1451.993;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FractNode;449;5897.003,3086.744;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;448;5911.251,3009.085;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;549;4072.815,1486.993;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;458;6467.492,3087.42;Float;False;Constant;_Vector2;Vector 2;42;0;Create;True;0;0;False;0;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;456;6168.712,3144.408;Float;False;Property;_lightsContour;_lightsContour;0;0;Create;True;0;0;False;0;0.1;1.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;548;4260.815,1546.993;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.2,0.2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;450;6089.466,2984.048;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;459;6713.637,3059.752;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;455;6390.38,2993.661;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;555;5357.713,1572.486;Float;False;Property;_AlbedoCol;AlbedoCol;6;0;Create;True;0;0;False;0;0.6838235,0.6838235,0.6838235,0;0.6102941,0.6102941,0.6102941,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;543;4848.984,1536.75;Float;True;Property;_Grass_Albedo;Grass_Albedo;3;0;Create;True;0;0;False;0;1d38b4c7072fa1b4e91e3ba755d32b7e;1d38b4c7072fa1b4e91e3ba755d32b7e;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldSpaceCameraPos;473;5740.683,2856.219;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ColorNode;444;5337.667,2413.973;Float;False;Global;_reLightColor;_reLightColor;41;0;Create;True;0;0;False;0;0.8676471,0.7320442,0.4402033,0;0.9513185,1,0.7794118,0.409;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DistanceOpNode;451;6923.709,2919.802;Float;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;474;6647.284,2616.95;Float;False;Global;_CSReLightDistance;_CSReLightDistance;45;0;Create;True;0;0;False;0;0;0.02771618;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;556;5821.806,1455.37;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DistanceOpNode;472;6265.919,2883.892;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;445;5979.502,2242.913;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;542;6371.306,2307.922;Float;False;540;0;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;471;6894.978,2622.615;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;540;6331.064,1615.82;Float;False;DiffuseInput;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;454;6442.242,2617.465;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;463;5740.201,2479.74;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;546;3723.519,1851.618;Float;True;Property;_Texture0;Texture 0;5;0;Create;True;0;0;False;0;85c7302b886d61d46a3dd335a3a5ed94;85c7302b886d61d46a3dd335a3a5ed94;False;white;Auto;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;443;6775.861,2389.679;Float;False;5;5;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;470;7090.385,2624.597;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;469;6981.456,2466.157;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;467;5968.244,2064.123;Float;False;Property;_ReLightTreshold;ReLightTreshold;2;0;Create;True;0;0;False;0;0.32;0.131;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;552;4794.947,1997.983;Float;True;Property;_TextureSample0;Texture Sample 0;4;0;Create;True;0;0;False;0;85c7302b886d61d46a3dd335a3a5ed94;85c7302b886d61d46a3dd335a3a5ed94;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;559;7394.299,2365.922;Float;False;Property;_SpecularMultiply;SpecularMultiply;7;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;466;6322.142,2016.222;Float;False;Global;_CSReLight;_CSReLight;44;0;Create;True;0;0;False;0;2;0.07063781;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;544;7453.811,1805.764;Float;False;540;0;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;553;5241.756,1820.033;Float;True;Property;_Grass_Normal;Grass_Normal;4;0;Create;True;0;0;False;0;407964dbb4552fa41bd5edd8d919d1ff;407964dbb4552fa41bd5edd8d919d1ff;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ConditionalIfNode;464;7281.341,2163.605;Float;False;True;5;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT4;0,0,0,0;False;3;FLOAT;0;False;4;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;554;7630.936,2401.312;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;558;7707.287,2298.543;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;564;8218.078,2454.628;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;565;8223.078,2530.628;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;563;8220.078,2379.628;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;560;8214.078,2169.628;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RelayNode;561;8217.078,2240.628;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RelayNode;562;8219.078,2308.628;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;8455.572,2183.624;Float;False;True;3;Float;ASEMaterialInspector;0;0;StandardSpecular;CScape/CSParkShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;0;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;False;True;True;False;True;True;False;True;True;False;False;False;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;4;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;0;0;False;0;0;0;False;-1;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;460;0;447;1
WireConnection;460;1;462;0
WireConnection;461;0;447;3
WireConnection;461;1;462;0
WireConnection;449;0;461;0
WireConnection;448;0;460;0
WireConnection;549;0;547;1
WireConnection;549;1;547;3
WireConnection;548;0;549;0
WireConnection;450;0;448;0
WireConnection;450;1;449;0
WireConnection;459;0;458;0
WireConnection;459;1;456;0
WireConnection;455;0;450;0
WireConnection;455;1;456;0
WireConnection;543;1;548;0
WireConnection;451;0;455;0
WireConnection;451;1;459;0
WireConnection;556;0;543;0
WireConnection;556;1;555;0
WireConnection;472;0;447;0
WireConnection;472;1;473;0
WireConnection;471;0;472;0
WireConnection;471;1;474;0
WireConnection;540;0;556;0
WireConnection;454;0;451;0
WireConnection;463;0;444;4
WireConnection;443;0;542;0
WireConnection;443;1;444;0
WireConnection;443;2;445;2
WireConnection;443;3;454;0
WireConnection;443;4;463;0
WireConnection;470;0;471;0
WireConnection;469;0;443;0
WireConnection;469;1;470;0
WireConnection;552;0;546;0
WireConnection;552;1;548;0
WireConnection;553;1;548;0
WireConnection;464;0;466;0
WireConnection;464;1;467;0
WireConnection;464;4;469;0
WireConnection;554;0;552;2
WireConnection;558;0;552;1
WireConnection;558;1;559;0
WireConnection;564;0;554;0
WireConnection;565;0;552;4
WireConnection;563;0;558;0
WireConnection;560;0;544;0
WireConnection;561;0;553;0
WireConnection;562;0;464;0
WireConnection;0;0;560;0
WireConnection;0;1;561;0
WireConnection;0;2;562;0
WireConnection;0;3;563;0
WireConnection;0;4;564;0
WireConnection;0;5;565;0
ASEEND*/
//CHKSM=C9DA9CB4704457992B883575BAA09F7562001B88