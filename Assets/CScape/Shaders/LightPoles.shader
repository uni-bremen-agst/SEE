// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CScape/LightPoles"
{
	Properties
	{
		_Lamp_albedo("Lamp_albedo", 2D) = "white" {}
		_Lamp_normal("Lamp_normal", 2D) = "bump" {}
		_LampIllumination("LampIllumination", 2D) = "white" {}
		_Float11("Float 11", Float) = 0.1
		_Float17("Float 17", Range( 0 , 1)) = 0.32
		_Float7("Float 7", Float) = 0.1
		_Float0("Float 0", Range( 0 , 1)) = 0
		_Smooth("Smooth", Range( 0 , 1)) = 0
		_Spec("Spec", Range( 0 , 1)) = 0
		_Illumination("Illumination", Range( 0 , 1)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
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
			float2 uv_texcoord;
			float3 worldNormal;
			INTERNAL_DATA
			float3 worldPos;
		};

		uniform sampler2D _Lamp_normal;
		uniform float4 _Lamp_normal_ST;
		uniform sampler2D _Lamp_albedo;
		uniform float4 _Lamp_albedo_ST;
		uniform float _Float0;
		uniform float _CSReLight;
		uniform float _Float17;
		uniform sampler2D _LampIllumination;
		uniform float4 _LampIllumination_ST;
		uniform float4 _reLightColor;
		uniform float _Illumination;
		uniform float _Float7;
		uniform float _Float11;
		uniform float _CSReLightDistance;
		uniform float _Spec;
		uniform float _Smooth;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_Lamp_normal = i.uv_texcoord * _Lamp_normal_ST.xy + _Lamp_normal_ST.zw;
			o.Normal = UnpackNormal( tex2D( _Lamp_normal, uv_Lamp_normal ) );
			float2 uv_Lamp_albedo = i.uv_texcoord * _Lamp_albedo_ST.xy + _Lamp_albedo_ST.zw;
			o.Albedo = ( tex2D( _Lamp_albedo, uv_Lamp_albedo ) * _Float0 ).rgb;
			float2 uv_LampIllumination = i.uv_texcoord * _LampIllumination_ST.xy + _LampIllumination_ST.zw;
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_worldPos = i.worldPos;
			float clampResult11 = clamp( ( ( ase_worldPos.y * 0.6 ) * _Float7 ) , 0 , 1 );
			float3 appendResult16 = (float3(frac( ( ase_worldPos.x * _Float7 ) ) , frac( ( ase_worldPos.z * _Float7 ) ) , clampResult11));
			float clampResult29 = clamp( (( ( tex2D( _Lamp_albedo, uv_Lamp_albedo ) * _Float0 ) * abs( ( ase_worldNormal.y + ase_worldNormal.z + ase_worldNormal.x ) ) * ( 1.0 - distance( ( appendResult16 * _Float11 ) , float3( ( float2( 0.5,0.5 ) * _Float11 ) ,  0.0 ) ) ) )).r , 0 , 1 );
			float clampResult35 = clamp( ( distance( ase_worldPos , _WorldSpaceCameraPos ) * _CSReLightDistance ) , 0 , 1 );
			float4 ifLocalVar42 = 0;
			UNITY_BRANCH 
			if( _CSReLight > _Float17 )
				ifLocalVar42 = float4(0,0,0,0);
			else if( _CSReLight < _Float17 )
				ifLocalVar42 = ( ( tex2D( _LampIllumination, uv_LampIllumination ) * _reLightColor * ( _Illumination * 10 ) ) + ( ( pow( clampResult29 , 1.5 ) * ( _reLightColor.a * 10 ) ) * _reLightColor * clampResult35 ) );
			o.Emission = ifLocalVar42.rgb;
			o.Metallic = _Spec;
			o.Smoothness = _Smooth;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma exclude_renderers d3d9 
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
				fixed3 worldNormal = UnityObjectToWorldNormal( v.normal );
				fixed3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				fixed3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
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
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				fixed3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
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
Version=14501
121;210;1697;764;654.8022;-686.8966;1;True;True
Node;AmplifyShaderEditor.CommentaryNode;4;-3607.087,714.0151;Float;False;3451.494;1554.365;Comment;41;43;42;41;40;38;39;37;36;35;32;33;34;29;30;31;27;28;26;25;24;23;22;21;20;19;18;17;16;15;14;13;12;11;10;9;8;7;6;5;48;54;;1,1,1,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;5;-2990.838,1601.915;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;6;-2610.291,1890.692;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.6;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;7;-2911.719,1847.797;Float;False;Property;_Float7;Float 7;5;0;Create;True;0;0.1;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-2373.349,1629.144;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;-2333.809,1907.076;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;-2338.053,1775.196;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;11;-2101.606,1927.298;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;47;-4054.303,1232.173;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FractNode;13;-2148.323,1617.545;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;12;-2166.681,1503.864;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;19;-3380.258,1135.133;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector2Node;15;-1933.014,1585.01;Float;False;Constant;_Vector3;Vector 3;42;0;Create;True;0;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.DynamicAppendNode;16;-1857.625,1881.572;Float;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;14;-1910.426,1747.198;Float;False;Property;_Float11;Float 11;3;0;Create;True;0;0.1;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;21;-3155.891,1196.018;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;18;-1663.979,1597.005;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-1627.209,1794.145;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;1;-4647.545,631.2172;Float;True;Property;_Lamp_albedo;Lamp_albedo;0;0;Create;True;0;48036792b6c216449a96b55b89bd9286;48036792b6c216449a96b55b89bd9286;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;50;-4397.705,884.7571;Float;False;Property;_Float0;Float 0;7;0;Create;True;0;0;0.436;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;22;-2834.264,1166.86;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DistanceOpNode;20;-1400.69,1779.743;Float;True;2;0;FLOAT3;0,0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;49;-4014.184,786.8943;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;23;-1142.776,1798.129;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;24;-3391.372,937.178;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;48;-2657.027,1223.219;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-2497.081,1053.73;Float;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ComponentMaskNode;26;-2306.479,1303.313;Float;False;True;False;False;False;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;27;-2050.91,1599.274;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;53;-4864.331,2034.169;Float;False;Property;_Illumination;Illumination;10;0;Create;True;0;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;28;-3391.355,1538.823;Float;False;Global;_reLightColor;_reLightColor;5;0;Create;True;0;0.8676471,0.7320442,0.4402033,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DistanceOpNode;30;-1719.665,1532.635;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;29;-2071.63,1279.901;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;31;-1674.608,1717.548;Float;False;Global;_CSReLightDistance;_CSReLightDistance;48;0;Create;True;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;-1496.4,1543.157;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;-4436.207,1827.515;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;3;-4878.307,1373.414;Float;True;Property;_LampIllumination;LampIllumination;2;0;Create;True;0;d036c8d72ba3c24448637745ab558fb9;d036c8d72ba3c24448637745ab558fb9;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;-2237.998,1540.059;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;33;-1887.996,1291.185;Float;False;2;0;FLOAT;0;False;1;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;35;-1267.429,1611.105;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;-4205.263,1462.746;Float;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;36;-1695.743,1309.642;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;37;-992.1032,1536.754;Float;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RelayNode;38;-3358.749,1388.074;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;40;-781.6813,1361.904;Float;False;Global;_CSReLight;_CSReLight;45;0;Create;True;0;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;41;-821.4642,1464.046;Float;False;Property;_Float17;Float 17;4;0;Create;True;0;0.32;0.415;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;39;-769.9054,1563.392;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;54;-1236.582,1494.532;Float;False;Constant;_Color0;Color 0;11;0;Create;True;0;0,0,0,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ConditionalIfNode;42;-371.8397,1354.666;Float;False;True;5;0;FLOAT;0;False;1;FLOAT;1;False;2;COLOR;0,0,0,0;False;3;FLOAT;0;False;4;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RelayNode;43;-332.0016,1015.84;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;2;-4480.274,1065.702;Float;True;Property;_Lamp_normal;Lamp_normal;1;0;Create;True;0;0a44b5e47ce0aa243a11f79f31744971;0a44b5e47ce0aa243a11f79f31744971;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;52;-133.3962,1230.46;Float;False;Property;_Smooth;Smooth;8;0;Create;True;0;0;0.56;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;51;-120.3962,1075.46;Float;False;Property;_Spec;Spec;9;0;Create;True;0;0;0.574;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;57;322.1978,1005.897;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RelayNode;58;320.1978,1071.897;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;59;319.1978,1141.897;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;56;321.1978,938.8966;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;45;-4889.153,1727.749;Float;False;Property;_Color2;Color 2;6;0;Create;True;0;1,1,1,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RelayNode;55;327.1978,878.8966;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;539.6684,881.0275;Float;False;True;3;Float;ASEMaterialInspector;0;0;Standard;CScape/LightPoles;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;0;False;0;0;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;False;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;False;0;255;255;0;0;0;0;0;0;0;0;False;0;4;10;25;False;0.5;True;0;Zero;Zero;0;Zero;Zero;Add;Add;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;0;0;False;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;6;0;5;2
WireConnection;8;0;5;1
WireConnection;8;1;7;0
WireConnection;9;0;6;0
WireConnection;9;1;7;0
WireConnection;10;0;5;3
WireConnection;10;1;7;0
WireConnection;11;0;9;0
WireConnection;13;0;10;0
WireConnection;12;0;8;0
WireConnection;19;0;47;0
WireConnection;16;0;12;0
WireConnection;16;1;13;0
WireConnection;16;2;11;0
WireConnection;21;0;19;0
WireConnection;18;0;15;0
WireConnection;18;1;14;0
WireConnection;17;0;16;0
WireConnection;17;1;14;0
WireConnection;22;0;21;1
WireConnection;22;1;21;2
WireConnection;22;2;21;0
WireConnection;20;0;17;0
WireConnection;20;1;18;0
WireConnection;49;0;1;0
WireConnection;49;1;50;0
WireConnection;23;0;20;0
WireConnection;24;0;49;0
WireConnection;48;0;22;0
WireConnection;25;0;24;0
WireConnection;25;1;48;0
WireConnection;25;2;23;0
WireConnection;26;0;25;0
WireConnection;30;0;5;0
WireConnection;30;1;27;0
WireConnection;29;0;26;0
WireConnection;32;0;30;0
WireConnection;32;1;31;0
WireConnection;46;0;53;0
WireConnection;34;0;28;4
WireConnection;33;0;29;0
WireConnection;35;0;32;0
WireConnection;44;0;3;0
WireConnection;44;1;28;0
WireConnection;44;2;46;0
WireConnection;36;0;33;0
WireConnection;36;1;34;0
WireConnection;37;0;36;0
WireConnection;37;1;28;0
WireConnection;37;2;35;0
WireConnection;38;0;44;0
WireConnection;39;0;38;0
WireConnection;39;1;37;0
WireConnection;42;0;40;0
WireConnection;42;1;41;0
WireConnection;42;2;54;0
WireConnection;42;4;39;0
WireConnection;43;0;42;0
WireConnection;57;0;43;0
WireConnection;58;0;51;0
WireConnection;59;0;52;0
WireConnection;56;0;2;0
WireConnection;55;0;24;0
WireConnection;0;0;55;0
WireConnection;0;1;56;0
WireConnection;0;2;57;0
WireConnection;0;3;58;0
WireConnection;0;4;59;0
ASEEND*/
//CHKSM=97AF92062DC6D4E455376A0EAC0172F22C2DC490