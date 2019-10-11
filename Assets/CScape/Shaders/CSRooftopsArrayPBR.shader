// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CScape/CSRooftopsArrayPBR"
{
	Properties
	{
		_IlluminationStrenght("IlluminationStrenght", Float) = 0
		_Smoothness("Smoothness", Float) = 0
		_Float0("Float 0", Float) = 0
		_Float6("Float 6", Float) = 0.1
		_Float10("Float 10", Range( 0 , 1)) = 0.32
		_Float4("Float 4", Float) = 0.1
		_SpecularInfluenceReLighten("SpecularInfluenceReLighten", Range( 0 , 1)) = 0
		_TextureSample1("Texture Sample 1", 2D) = "white" {}
		_Float3("Float 3", Float) = 0
		_Float1("Float 1", Float) = 0
		_Blur("Blur", Float) = 0
		_AlbedoArray("AlbedoArray", 2DArray ) = "" {}
		_NormalArray("NormalArray", 2DArray ) = "" {}
		_Size("Size", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] _texcoord4( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
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
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float2 uv_texcoord;
			float2 uv4_texcoord4;
			float4 vertexColor : COLOR;
			float3 worldPos;
			float3 viewDir;
			INTERNAL_DATA
		};

		uniform UNITY_DECLARE_TEX2DARRAY( _NormalArray );
		uniform float4 _NormalArray_ST;
		uniform UNITY_DECLARE_TEX2DARRAY( _AlbedoArray );
		uniform float4 _AlbedoArray_ST;
		uniform float _CSReLight;
		uniform float _Float10;
		uniform float _IlluminationStrenght;
		uniform float _Size;
		uniform float _SpecularInfluenceReLighten;
		uniform float _Float4;
		uniform float _Float6;
		uniform float4 _reLightColor;
		uniform float _CSReLightDistance;
		uniform sampler2D _TextureSample1;
		uniform float _Float1;
		uniform float _Float3;
		uniform float _Blur;
		uniform float _Float0;
		uniform float _Smoothness;

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float2 uv_NormalArray = i.uv_texcoord * _NormalArray_ST.xy + _NormalArray_ST.zw;
			float4 texArray96 = UNITY_SAMPLE_TEX2DARRAY(_NormalArray, float3(uv_NormalArray, i.uv4_texcoord4.x)  );
			float4 appendResult98 = (float4(1.0 , texArray96.g , 1.0 , texArray96.r));
			o.Normal = UnpackNormal( appendResult98 );
			float2 uv_AlbedoArray = i.uv_texcoord * _AlbedoArray_ST.xy + _AlbedoArray_ST.zw;
			float4 texArray89 = UNITY_SAMPLE_TEX2DARRAY(_AlbedoArray, float3(uv_AlbedoArray, i.uv4_texcoord4.x)  );
			o.Albedo = ( texArray89 * 0.5 ).rgb;
			float4 texArray93 = UNITY_SAMPLE_TEX2DARRAY(_AlbedoArray, float3(uv_AlbedoArray, ( i.uv4_texcoord4.x + _Size ))  );
			float3 break54 = UnpackNormal( appendResult98 );
			float3 ase_worldPos = i.worldPos;
			float clampResult32 = clamp( ( ( ase_worldPos.y * 0.6 ) * _Float4 ) , 0.0 , 1.0 );
			float3 appendResult36 = (float3(frac( ( ase_worldPos.x * _Float4 ) ) , frac( ( ase_worldPos.z * _Float4 ) ) , clampResult32));
			float clampResult58 = clamp( (( ( ( texArray93 + ( texArray93 * _SpecularInfluenceReLighten ) ) * float4( 0.5,0.5,0.5,0 ) ) * ( break54.y + break54.z ) * ( 1.0 - distance( ( appendResult36 * _Float6 ) , float3( ( float2( 0.5,0.5 ) * _Float6 ) ,  0.0 ) ) ) )).r , 0.0 , 1.0 );
			float clampResult48 = clamp( ( distance( ase_worldPos , _WorldSpaceCameraPos ) * _CSReLightDistance ) , 0.0 , 1.0 );
			float4 ifLocalVar53 = 0;
			UNITY_BRANCH 
			if( _CSReLight > _Float10 )
				ifLocalVar53 = ( ( 1.0 - i.vertexColor.r ) * ( float4(1,0,0,0) * _IlluminationStrenght ) );
			else if( _CSReLight < _Float10 )
				ifLocalVar53 = ( ( ( 1.0 - i.vertexColor.r ) * ( float4(1,0,0,0) * _IlluminationStrenght ) ) + ( ( pow( clampResult58 , 1.5 ) * ( _reLightColor.a * 10.0 ) ) * _reLightColor * clampResult48 ) );
			float2 appendResult83 = (float2(ase_worldPos.z , ase_worldPos.x));
			float2 Offset80 = ( ( _Float1 - 1 ) * i.viewDir.xy * _Float3 ) + ( appendResult83 * float2( 0.02,0.02 ) );
			float4 lerpResult76 = lerp( ifLocalVar53 , tex2Dlod( _TextureSample1, float4( Offset80, 0, _Blur) ) , texArray96.b);
			o.Emission = lerpResult76.rgb;
			o.Specular = ( texArray93 * _Float0 ).rgb;
			o.Smoothness = ( texArray93.a * _Smoothness );
			o.Occlusion = texArray89.a;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma exclude_renderers d3d9 gles d3d11_9x 
		#pragma surface surf StandardSpecular keepalpha fullforwardshadows dithercrossfade 

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
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.customPack1.zw = customInputData.uv4_texcoord4;
				o.customPack1.zw = v.texcoord3;
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
				surfIN.uv4_texcoord4 = IN.customPack1.zw;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.viewDir = IN.tSpace0.xyz * worldViewDir.x + IN.tSpace1.xyz * worldViewDir.y + IN.tSpace2.xyz * worldViewDir.z;
				surfIN.worldPos = worldPos;
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
414;557;2067;627;5485.879;2368.451;7.609225;True;True
Node;AmplifyShaderEditor.CommentaryNode;64;-1604.311,1072.044;Float;False;3451.494;1554.365;Comment;39;53;51;50;52;49;63;48;60;59;46;47;58;44;45;43;57;42;56;55;41;61;40;54;38;62;39;37;36;35;32;33;34;29;30;31;27;28;26;65;;1,1,1,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;26;-952.4962,2151.998;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;28;-873.3766,2397.88;Float;False;Property;_Float4;Float 4;7;0;Create;True;0;0;False;0;0.1;0.11;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;95;-1150.365,406.2612;Float;False;Property;_Size;Size;16;0;Create;True;0;0;False;0;0;6;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;91;-1647.189,74.69535;Float;False;3;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-571.949,2440.775;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.6;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;29;-335.007,2179.227;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;-295.467,2457.159;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;94;-941.1603,301.1678;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;96;-2149.732,273.9259;Float;True;Property;_NormalArray;NormalArray;15;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;31;-299.7116,2325.279;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;32;-63.26392,2477.381;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;33;-128.339,2053.947;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;93;6.15816,160.6014;Float;True;Property;_TextureArray2;Texture Array 2;17;0;Create;True;0;0;False;0;None;0;Instance;89;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;73;-262.2271,705.24;Float;False;Property;_SpecularInfluenceReLighten;SpecularInfluenceReLighten;8;0;Create;True;0;0;False;0;0;0.047;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;34;-109.9806,2167.628;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;98;-1634.537,288.8052;Float;False;FLOAT4;4;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DynamicAppendNode;36;180.7176,2431.655;Float;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.UnpackScaleNormalNode;97;-1390.14,276.8568;Float;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector2Node;35;105.3279,2135.093;Float;False;Constant;_Vector1;Vector 1;42;0;Create;True;0;0;False;0;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;72;174.9628,611.2174;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;37;127.9167,2297.281;Float;False;Property;_Float6;Float 6;5;0;Create;True;0;0;False;0;0.1;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;411.1336,2344.228;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;70;289.5724,453.8329;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;39;386.0633,2132.788;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode;62;-1349.03,1614.085;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DistanceOpNode;40;637.6531,2329.826;Float;True;2;0;FLOAT3;0,0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;54;-942.6663,1638.43;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;71;519.7689,590.5866;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0.5,0.5,0.5,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;55;-615.4851,1618.132;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;41;888.2917,2248.779;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;61;-1353.03,1487.261;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;56;-458.7384,1603.813;Float;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;42;-48.13304,1957.303;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ComponentMaskNode;57;-303.7028,1661.342;Float;False;True;False;False;False;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;3;-718.8996,254.7002;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;9;-969.9001,440.1001;Float;False;Constant;_Color0;Color 0;3;0;Create;True;0;0;False;0;1,0,0,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ClampOpNode;58;-68.85272,1637.93;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;44;-590.8051,1824.007;Float;False;Global;_reLightColor;_reLightColor;5;0;Create;True;0;0;False;0;0.8676471,0.7320442,0.4402033,0;0.9338235,0.7337997,0.4806445,0.703;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DistanceOpNode;45;283.1121,1890.664;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;10;-689.6003,585.1359;Float;False;Property;_IlluminationStrenght;IlluminationStrenght;2;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;43;328.1692,2075.577;Float;False;Global;_CSReLightDistance;_CSReLightDistance;48;0;Create;True;0;0;False;0;0;0.007220217;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;59;114.781,1649.214;Float;False;2;0;FLOAT;0;False;1;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;506.3768,1901.186;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;6;-479.6997,238.2;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-477.1003,393.6999;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;-235.2209,1898.088;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;-248.3002,254.7001;Float;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;48;770.4482,2004.234;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;82;-688.4682,-889.3461;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;60;307.034,1667.671;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;63;-1355.973,1746.103;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;49;1010.674,1894.783;Float;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;83;-419.837,-837.6754;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;86;-270.3932,-475.9956;Float;False;Property;_Float3;Float 3;11;0;Create;True;0;0;False;0;0;0.13;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;85;-581.6981,-695.3549;Float;False;Property;_Float1;Float 1;12;0;Create;True;0;0;False;0;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;84;-301.471,-932.5007;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.02,0.02;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;51;1232.872,1921.421;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;50;1181.313,1822.075;Float;False;Property;_Float10;Float 10;6;0;Create;True;0;0;False;0;0.32;0.32;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;52;1215.096,1731.933;Float;False;Global;_CSReLight;_CSReLight;45;0;Create;True;0;0;False;0;2;0.3217259;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;81;-488.9153,-612.7151;Float;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;87;79.37885,-608.8434;Float;False;Property;_Blur;Blur;13;0;Create;True;0;0;False;0;0;3.19;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;53;1630.937,1712.695;Float;False;True;5;0;FLOAT;0;False;1;FLOAT;1;False;2;COLOR;0,0,0,0;False;3;FLOAT;0;False;4;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ParallaxMappingNode;80;94.99406,-942.3582;Float;False;Normal;4;0;FLOAT2;0,0;False;1;FLOAT;1.98;False;2;FLOAT;1;False;3;FLOAT3;1,0,1.33;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;78;623.2325,-910.9245;Float;True;Property;_TextureSample1;Texture Sample 1;10;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;MipLevel;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;12;-245.9001,13.00006;Float;False;Property;_Float0;Float 0;4;0;Create;True;0;0;False;0;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;25;-188.814,-106.17;Float;False;Constant;_Float2;Float 2;5;0;Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;65;1670.775,1373.869;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TextureArrayNode;89;-828.6412,25.26486;Float;True;Property;_AlbedoArray;AlbedoArray;14;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;13;513.314,118.537;Float;False;Property;_Smoothness;Smoothness;3;0;Create;True;0;0;False;0;0;0.77;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;76;808.7909,-275.0771;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0.6397059,0.6397059,0.6397059,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;101;759.9666,129.2599;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;67;335.9389,-63.3044;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;24;98.48602,-212.7701;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SinTimeNode;7;-1177.311,703.9168;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.MMatrixNode;23;-1139.401,-326.2995;Float;False;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.TransformVariables;20;-1182.401,-269.0995;Float;False;_Object2World;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.WorldToObjectMatrix;21;-1055.001,-135.1996;Float;False;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.SamplerNode;1;-634.7,-252.8;Float;True;Property;_diffuse;diffuse;0;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;17;-368.0002,-392.5998;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;75;473.9639,-476.7771;Float;True;Property;_TextureSample0;Texture Sample 0;9;0;Create;True;0;0;False;0;None;None;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;100;-1801.304,252.0766;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosFromTransformMatrix;14;-902.9998,-412.0997;Float;False;1;0;FLOAT4x4;0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;-233.4004,-306.7998;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1234;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;99;-1790.33,426.1035;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;74;-1528.68,697.9962;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TextureCoordinatesNode;92;-1243.1,-47.88408;Float;False;3;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FractNode;18;-104.7004,-378.2997;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosFromTransformMatrix;22;-1334.001,-482.3994;Float;False;1;0;FLOAT4x4;0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ClampOpNode;11;-843.782,698.1002;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;107;1754.924,140.5334;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;106;1753.924,71.53339;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;16;-644.9002,-405.5996;Float;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RelayNode;103;1752.924,-135.4666;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;104.4997,-322.3998;Float;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RelayNode;102;1751.924,-204.4666;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;2;-2226.317,-169.8589;Float;True;Property;_normal;normal;1;0;Create;True;0;0;False;0;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RelayNode;104;1756.924,-67.46661;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RelayNode;105;1754.924,0.5333862;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldSpaceViewDirHlpNode;88;-207.4418,-718.9479;Float;False;1;0;FLOAT4;0,0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;2074.754,-184.8693;Float;False;True;3;Float;ASEMaterialInspector;0;0;StandardSpecular;CScape/CSRooftopsArrayPBR;False;False;False;False;False;False;False;False;False;False;False;False;True;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;False;True;True;False;True;True;False;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;4;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;1;False;-1;1;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;27;0;26;2
WireConnection;29;0;26;1
WireConnection;29;1;28;0
WireConnection;30;0;27;0
WireConnection;30;1;28;0
WireConnection;94;0;91;1
WireConnection;94;1;95;0
WireConnection;96;1;91;1
WireConnection;31;0;26;3
WireConnection;31;1;28;0
WireConnection;32;0;30;0
WireConnection;33;0;29;0
WireConnection;93;1;94;0
WireConnection;34;0;31;0
WireConnection;98;1;96;2
WireConnection;98;3;96;1
WireConnection;36;0;33;0
WireConnection;36;1;34;0
WireConnection;36;2;32;0
WireConnection;97;0;98;0
WireConnection;72;0;93;0
WireConnection;72;1;73;0
WireConnection;38;0;36;0
WireConnection;38;1;37;0
WireConnection;70;0;93;0
WireConnection;70;1;72;0
WireConnection;39;0;35;0
WireConnection;39;1;37;0
WireConnection;62;0;97;0
WireConnection;40;0;38;0
WireConnection;40;1;39;0
WireConnection;54;0;62;0
WireConnection;71;0;70;0
WireConnection;55;0;54;1
WireConnection;55;1;54;2
WireConnection;41;0;40;0
WireConnection;61;0;71;0
WireConnection;56;0;61;0
WireConnection;56;1;55;0
WireConnection;56;2;41;0
WireConnection;57;0;56;0
WireConnection;58;0;57;0
WireConnection;45;0;26;0
WireConnection;45;1;42;0
WireConnection;59;0;58;0
WireConnection;46;0;45;0
WireConnection;46;1;43;0
WireConnection;6;0;3;1
WireConnection;8;0;9;0
WireConnection;8;1;10;0
WireConnection;47;0;44;4
WireConnection;4;0;6;0
WireConnection;4;1;8;0
WireConnection;48;0;46;0
WireConnection;60;0;59;0
WireConnection;60;1;47;0
WireConnection;63;0;4;0
WireConnection;49;0;60;0
WireConnection;49;1;44;0
WireConnection;49;2;48;0
WireConnection;83;0;82;3
WireConnection;83;1;82;1
WireConnection;84;0;83;0
WireConnection;51;0;63;0
WireConnection;51;1;49;0
WireConnection;53;0;52;0
WireConnection;53;1;50;0
WireConnection;53;2;63;0
WireConnection;53;4;51;0
WireConnection;80;0;84;0
WireConnection;80;1;85;0
WireConnection;80;2;86;0
WireConnection;80;3;81;0
WireConnection;78;1;80;0
WireConnection;78;2;87;0
WireConnection;65;0;53;0
WireConnection;89;1;91;1
WireConnection;76;0;65;0
WireConnection;76;1;78;0
WireConnection;76;2;96;3
WireConnection;101;0;93;4
WireConnection;101;1;13;0
WireConnection;67;0;93;0
WireConnection;67;1;12;0
WireConnection;24;0;89;0
WireConnection;24;1;25;0
WireConnection;17;0;16;0
WireConnection;17;1;16;2
WireConnection;100;0;96;2
WireConnection;14;0;23;0
WireConnection;19;0;17;0
WireConnection;99;0;96;1
WireConnection;18;0;19;0
WireConnection;11;0;7;3
WireConnection;107;0;89;4
WireConnection;106;0;101;0
WireConnection;16;0;14;0
WireConnection;103;0;97;0
WireConnection;15;0;18;0
WireConnection;15;1;89;0
WireConnection;102;0;24;0
WireConnection;104;0;76;0
WireConnection;105;0;67;0
WireConnection;88;0;81;0
WireConnection;0;0;102;0
WireConnection;0;1;103;0
WireConnection;0;2;104;0
WireConnection;0;3;105;0
WireConnection;0;4;106;0
WireConnection;0;5;107;0
ASEEND*/
//CHKSM=56D8A6109CC6966A90CFD1EBE15C0DF8FA16E48D