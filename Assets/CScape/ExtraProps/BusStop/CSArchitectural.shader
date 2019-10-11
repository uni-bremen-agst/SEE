// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CSIndustrialProps"
{
	Properties
	{
		[HideInInspector] __dirty( "", Int ) = 1
		_BusStation_albedo("BusStation_albedo", 2D) = "white" {}
		_BusStation_ao("BusStation_ao", 2D) = "white" {}
		_BusStation_gloss("BusStation_gloss", 2D) = "white" {}
		_BusStation_specular("BusStation_specular", 2D) = "white" {}
		_BusStation_normal("BusStation_normal", 2D) = "white" {}
		_Float13("Float 13", Float) = 0.1
		_Lightness("Lightness", Float) = 0
		_Float17("Float 17", Range( 0 , 1)) = 0.32
		_Float11("Float 11", Float) = 0.1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf StandardSpecular keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
			float3 worldPos;
		};

		uniform sampler2D _BusStation_normal;
		uniform float4 _BusStation_normal_ST;
		uniform sampler2D _BusStation_albedo;
		uniform float4 _BusStation_albedo_ST;
		uniform float _Lightness;
		uniform float Float19;
		uniform float _Float17;
		uniform float _Float11;
		uniform float _Float13;
		uniform float4 Color3;
		uniform float Float15;
		uniform sampler2D _BusStation_specular;
		uniform float4 _BusStation_specular_ST;
		uniform sampler2D _BusStation_gloss;
		uniform float4 _BusStation_gloss_ST;
		uniform sampler2D _BusStation_ao;
		uniform float4 _BusStation_ao_ST;

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float2 uv_BusStation_normal = i.uv_texcoord * _BusStation_normal_ST.xy + _BusStation_normal_ST.zw;
			float3 tex2DNode6 = UnpackNormal( tex2D( _BusStation_normal, uv_BusStation_normal ) );
			o.Normal = tex2DNode6;
			float2 uv_BusStation_albedo = i.uv_texcoord * _BusStation_albedo_ST.xy + _BusStation_albedo_ST.zw;
			float4 tex2DNode1 = tex2D( _BusStation_albedo, uv_BusStation_albedo );
			float4 temp_output_7_0 = ( tex2DNode1 * _Lightness );
			o.Albedo = temp_output_7_0.xyz;
			float3 ase_worldPos = i.worldPos;
			float clampResult18 = clamp( ( ( ase_worldPos.y * 0.6 ) * _Float11 ) , 0.0 , 1.0 );
			float3 appendResult20 = (float3(frac( ( ase_worldPos.x * _Float11 ) ) , frac( ( ase_worldPos.z * _Float11 ) ) , clampResult18));
			float componentMask32 = ( temp_output_7_0 * ( tex2DNode6.y + tex2DNode6.z ) * ( 1.0 - distance( ( appendResult20 * _Float13 ) , float3( ( float2( 0.5,0.5 ) * _Float13 ) ,  0.0 ) ) ) ).x;
			float clampResult36 = clamp( componentMask32 , 0.0 , 1.0 );
			float clampResult41 = clamp( ( distance( ase_worldPos , _WorldSpaceCameraPos ) * Float15 ) , 0.0 , 1.0 );
			float4 ifLocalVar47 = 0;
			UNITY_BRANCH if( Float19 > _Float17 )
				ifLocalVar47 = tex2DNode1;
			else UNITY_BRANCH if( Float19 < _Float17 )
				ifLocalVar47 = ( tex2DNode1 + ( ( pow( clampResult36 , 1.5 ) * ( Color3.a * 10.0 ) ) * Color3 * clampResult41 ) );
			o.Emission = ifLocalVar47.xyz;
			float2 uv_BusStation_specular = i.uv_texcoord * _BusStation_specular_ST.xy + _BusStation_specular_ST.zw;
			o.Specular = tex2D( _BusStation_specular, uv_BusStation_specular ).xyz;
			float2 uv_BusStation_gloss = i.uv_texcoord * _BusStation_gloss_ST.xy + _BusStation_gloss_ST.zw;
			o.Smoothness = tex2D( _BusStation_gloss, uv_BusStation_gloss ).r;
			float2 uv_BusStation_ao = i.uv_texcoord * _BusStation_ao_ST.xy + _BusStation_ao_ST.zw;
			o.Occlusion = tex2D( _BusStation_ao, uv_BusStation_ao ).x;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=13105
709;402;1489;538;1213.039;284.7314;3.284635;True;True
Node;AmplifyShaderEditor.WorldPosInputsNode;10;-224.2578,2018.957;Float;False;0;4;FLOAT3;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;11;-145.1382,2264.839;Float;False;Property;_Float11;Float 11;7;0;0.1;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;12;156.2894,2307.734;Float;False;2;2;0;FLOAT;0.0;False;1;FLOAT;0.6;False;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;428.5269,2192.238;Float;False;2;2;0;FLOAT;0.0;False;1;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;432.7714,2324.118;Float;False;2;2;0;FLOAT;0.0;False;1;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;393.2314,2046.186;Float;False;2;2;0;FLOAT;0.0;False;1;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.ClampOpNode;18;664.9745,2344.34;Float;False;3;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;1.0;False;1;FLOAT
Node;AmplifyShaderEditor.FractNode;16;618.2578,2034.587;Float;False;1;0;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.FractNode;17;599.8994,1920.906;Float;False;1;0;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.SamplerNode;6;-709.7687,138.4418;Float;True;Property;_BusStation_normal;BusStation_normal;4;0;Assets/CScape/ExtraProps/BusStop/BusStation_normal.png;True;0;True;white;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;FLOAT3;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;21;856.1552,2164.24;Float;False;Property;_Float13;Float 13;5;0;0.1;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.DynamicAppendNode;20;908.9561,2298.614;Float;False;FLOAT3;4;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;0.0;False;3;FLOAT;0.0;False;1;FLOAT3
Node;AmplifyShaderEditor.Vector2Node;19;833.5663,2002.052;Float;False;Constant;_Vector3;Vector 3;42;0;0.5,0.5;0;3;FLOAT2;FLOAT;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;22;1114.302,1999.747;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0.0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.RangedFloatNode;8;-190.1161,-96.5623;Float;False;Property;_Lightness;Lightness;5;0;0;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.SamplerNode;1;-512.5,-199;Float;True;Property;_BusStation_albedo;BusStation_albedo;0;0;Assets/CScape/ExtraProps/BusStop/BusStation_albedo.png;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;FLOAT4;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.RelayNode;23;-620.7917,1481.044;Float;False;1;0;FLOAT3;0.0;False;1;FLOAT3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;24;1139.372,2211.187;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0,0,0;False;1;FLOAT3
Node;AmplifyShaderEditor.CommentaryNode;9;-876.0728,939.0029;Float;False;3451.494;1554.365;Comment;23;48;46;47;44;45;43;42;41;40;38;39;37;36;35;32;33;34;29;30;31;27;28;26;;1,1,1,1;0;0
Node;AmplifyShaderEditor.BreakToComponentsNode;25;-214.428,1505.389;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;FLOAT;FLOAT;FLOAT;FLOAT;FLOAT;FLOAT;FLOAT;FLOAT;FLOAT;FLOAT;FLOAT;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;110.2693,-236.255;Float;False;2;2;0;FLOAT4;0.0;False;1;FLOAT;0,0,0,0;False;1;FLOAT4
Node;AmplifyShaderEditor.DistanceOpNode;26;1365.891,2196.785;Float;True;2;0;FLOAT3;0,0,0;False;1;FLOAT2;0.5,0.5,0;False;1;FLOAT
Node;AmplifyShaderEditor.OneMinusNode;28;1616.53,2115.738;Float;False;1;0;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.RelayNode;27;-624.7917,1354.22;Float;False;1;0;FLOAT4;0.0;False;1;FLOAT4
Node;AmplifyShaderEditor.SimpleAddOpNode;29;112.7534,1485.092;Float;False;2;2;0;FLOAT;0.0;False;1;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;269.5,1470.772;Float;False;3;3;0;FLOAT4;0;False;1;FLOAT;0.0,0,0,0;False;2;FLOAT;0,0,0,0;False;1;FLOAT4
Node;AmplifyShaderEditor.WorldSpaceCameraPos;31;680.1053,1824.262;Float;False;0;1;FLOAT3
Node;AmplifyShaderEditor.ComponentMaskNode;32;424.5356,1528.301;Float;False;True;False;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;33;1056.408,1942.536;Float;False;Global;Float15;Float 15;48;0;0;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.DistanceOpNode;34;1011.351,1757.623;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0.0,0,0;False;1;FLOAT
Node;AmplifyShaderEditor.ClampOpNode;36;659.3857,1504.889;Float;False;3;0;FLOAT;0,0,0,0;False;1;FLOAT;0.0;False;2;FLOAT;1.0;False;1;FLOAT
Node;AmplifyShaderEditor.ColorNode;35;137.4333,1690.966;Float;False;Global;Color3;Color 3;5;0;0.8676471,0.7320442,0.4402033,0;0;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;1234.615,1768.145;Float;False;2;2;0;FLOAT;0.0;False;1;FLOAT;0.01;False;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;37;493.0175,1765.047;Float;False;2;2;0;FLOAT;0.0;False;1;FLOAT;10.0;False;1;FLOAT
Node;AmplifyShaderEditor.PowerNode;39;843.0194,1516.174;Float;False;2;0;FLOAT;0.0;False;1;FLOAT;1.5;False;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;40;1035.272,1534.631;Float;False;2;2;0;FLOAT;0.0;False;1;FLOAT;0,0,0;False;1;FLOAT
Node;AmplifyShaderEditor.ClampOpNode;41;1498.687,1871.193;Float;False;3;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;1.0;False;1;FLOAT
Node;AmplifyShaderEditor.RelayNode;42;-627.7347,1613.062;Float;False;1;0;FLOAT4;0.0;False;1;FLOAT4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;43;1738.912,1761.742;Float;False;3;3;0;FLOAT;0.0;False;1;COLOR;0;False;2;FLOAT;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.SimpleAddOpNode;46;1961.11,1788.38;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0.0,0,0,0;False;1;FLOAT4
Node;AmplifyShaderEditor.RangedFloatNode;44;1909.551,1689.034;Float;False;Property;_Float17;Float 17;6;0;0.32;0;1;0;1;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;45;2029.353,1609.214;Float;False;Global;Float19;Float 19;45;0;2;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.ConditionalIfNode;47;2359.175,1579.654;Float;False;True;5;0;FLOAT;0.0;False;1;FLOAT;1.0;False;2;FLOAT4;0,0,0,0;False;3;FLOAT;0.0;False;4;FLOAT4;0,0,0,0;False;1;FLOAT4
Node;AmplifyShaderEditor.RelayNode;48;2399.013,1240.828;Float;False;1;0;FLOAT4;0.0;False;1;FLOAT4
Node;AmplifyShaderEditor.SamplerNode;3;-209.974,449.2265;Float;True;Property;_BusStation_gloss;BusStation_gloss;2;0;Assets/CScape/ExtraProps/BusStop/BusStation_gloss.png;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;FLOAT4;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.SamplerNode;5;-1040.849,56.72972;Float;True;Property;_BusStation_specular;BusStation_specular;3;0;Assets/CScape/ExtraProps/BusStop/BusStation_specular.png;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;FLOAT4;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.SamplerNode;2;-907.4563,323.2224;Float;True;Property;_BusStation_ao;BusStation_ao;1;0;Assets/CScape/ExtraProps/BusStop/BusStation_ao.png;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;FLOAT4;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;620.7165,-153.9536;Float;False;True;2;Float;ASEMaterialInspector;0;0;StandardSpecular;CSIndustrialProps;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;0;False;0;0;Opaque;0.5;True;True;0;False;Opaque;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;False;0;255;255;0;0;0;0;False;0;4;10;25;False;0.5;True;0;Zero;Zero;0;Zero;Zero;Add;Add;0;False;0;0,0,0,0;VertexOffset;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;0;0;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.0;False;5;FLOAT;0.0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0.0;False;9;FLOAT;0.0;False;10;OBJECT;0.0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;12;0;10;2
WireConnection;13;0;10;3
WireConnection;13;1;11;0
WireConnection;14;0;12;0
WireConnection;14;1;11;0
WireConnection;15;0;10;1
WireConnection;15;1;11;0
WireConnection;18;0;14;0
WireConnection;16;0;13;0
WireConnection;17;0;15;0
WireConnection;20;0;17;0
WireConnection;20;1;16;0
WireConnection;20;2;18;0
WireConnection;22;0;19;0
WireConnection;22;1;21;0
WireConnection;23;0;6;0
WireConnection;24;0;20;0
WireConnection;24;1;21;0
WireConnection;25;0;23;0
WireConnection;7;0;1;0
WireConnection;7;1;8;0
WireConnection;26;0;24;0
WireConnection;26;1;22;0
WireConnection;28;0;26;0
WireConnection;27;0;7;0
WireConnection;29;0;25;1
WireConnection;29;1;25;2
WireConnection;30;0;27;0
WireConnection;30;1;29;0
WireConnection;30;2;28;0
WireConnection;32;0;30;0
WireConnection;34;0;10;0
WireConnection;34;1;31;0
WireConnection;36;0;32;0
WireConnection;38;0;34;0
WireConnection;38;1;33;0
WireConnection;37;0;35;4
WireConnection;39;0;36;0
WireConnection;40;0;39;0
WireConnection;40;1;37;0
WireConnection;41;0;38;0
WireConnection;42;0;1;0
WireConnection;43;0;40;0
WireConnection;43;1;35;0
WireConnection;43;2;41;0
WireConnection;46;0;42;0
WireConnection;46;1;43;0
WireConnection;47;0;45;0
WireConnection;47;1;44;0
WireConnection;47;2;42;0
WireConnection;47;4;46;0
WireConnection;48;0;47;0
WireConnection;0;0;7;0
WireConnection;0;1;6;0
WireConnection;0;2;48;0
WireConnection;0;3;5;0
WireConnection;0;4;3;1
WireConnection;0;5;2;0
ASEEND*/
//CHKSM=9AA4AD43E5B52600AFBEFDAE16C7E33F265F0FA5