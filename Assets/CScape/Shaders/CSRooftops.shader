// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CScape/CSRooftops"
{
	Properties
	{
		_diffuse("diffuse", 2D) = "white" {}
		_normal("normal", 2D) = "white" {}
		_IlluminationStrenght("IlluminationStrenght", Float) = 0
		_Float1("Float 1", Float) = 0
		_Float0("Float 0", Float) = 0
		_Float6("Float 6", Float) = 0.1
		_Float10("Float 10", Range( 0 , 1)) = 0.32
		_Float4("Float 4", Float) = 0.1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.5
		#pragma only_renderers d3d11 glcore gles3 metal 
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows dithercrossfade 
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
			float3 worldPos;
		};

		uniform sampler2D _normal;
		uniform float4 _normal_ST;
		uniform sampler2D _diffuse;
		uniform float4 _diffuse_ST;
		uniform float _CSReLight;
		uniform float _Float10;
		uniform float _IlluminationStrenght;
		uniform float _Float4;
		uniform float _Float6;
		uniform float4 _reLightColor;
		uniform float _CSReLightDistance;
		uniform float _Float0;
		uniform float _Float1;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_normal = i.uv_texcoord * _normal_ST.xy + _normal_ST.zw;
			float4 tex2DNode2 = tex2D( _normal, uv_normal );
			o.Normal = tex2DNode2.rgb;
			float2 uv_diffuse = i.uv_texcoord * _diffuse_ST.xy + _diffuse_ST.zw;
			float4 temp_output_24_0 = ( tex2D( _diffuse, uv_diffuse ) * 0.5 );
			o.Albedo = temp_output_24_0.rgb;
			float4 break54 = tex2DNode2;
			float3 ase_worldPos = i.worldPos;
			float clampResult32 = clamp( ( ( ase_worldPos.y * 0.6 ) * _Float4 ) , 0.0 , 1.0 );
			float3 appendResult36 = (float3(frac( ( ase_worldPos.x * _Float4 ) ) , frac( ( ase_worldPos.z * _Float4 ) ) , clampResult32));
			float clampResult58 = clamp( (( temp_output_24_0 * ( break54.g + break54.b ) * ( 1.0 - distance( ( appendResult36 * _Float6 ) , float3( ( float2( 0.5,0.5 ) * _Float6 ) ,  0.0 ) ) ) )).r , 0.0 , 1.0 );
			float clampResult48 = clamp( ( distance( ase_worldPos , _WorldSpaceCameraPos ) * _CSReLightDistance ) , 0.0 , 1.0 );
			float4 ifLocalVar53 = 0;
			UNITY_BRANCH 
			if( _CSReLight > _Float10 )
				ifLocalVar53 = ( ( 1.0 - i.vertexColor.r ) * ( float4(1,0,0,0) * _IlluminationStrenght ) );
			else if( _CSReLight < _Float10 )
				ifLocalVar53 = ( ( ( 1.0 - i.vertexColor.r ) * ( float4(1,0,0,0) * _IlluminationStrenght ) ) + ( ( pow( clampResult58 , 1.5 ) * ( _reLightColor.a * 10.0 ) ) * _reLightColor * clampResult48 ) );
			o.Emission = ifLocalVar53.rgb;
			o.Metallic = _Float0;
			o.Smoothness = _Float1;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15800
414;557;2067;627;6363.817;1945.463;7.981489;True;True
Node;AmplifyShaderEditor.CommentaryNode;64;-1604.311,1072.044;Float;False;3451.494;1554.365;Comment;39;53;51;50;52;49;63;48;60;59;46;47;58;44;45;43;57;42;56;55;41;61;40;54;38;62;39;37;36;35;32;33;34;29;30;31;27;28;26;65;;1,1,1,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;26;-952.4962,2151.998;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;28;-873.3766,2397.88;Float;False;Property;_Float4;Float 4;7;0;Create;True;0;0;False;0;0.1;0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-571.949,2440.775;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.6;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;31;-299.7116,2325.279;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;-295.467,2457.159;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;29;-335.007,2179.227;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;34;-109.9806,2167.628;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;33;-128.339,2053.947;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;32;-63.26392,2477.381;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;35;105.3279,2135.093;Float;False;Constant;_Vector1;Vector 1;42;0;Create;True;0;0;False;0;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.DynamicAppendNode;36;180.7176,2431.655;Float;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;37;127.9167,2297.281;Float;False;Property;_Float6;Float 6;5;0;Create;True;0;0;False;0;0.1;1.07;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;2;-1588.45,60.48324;Float;True;Property;_normal;normal;1;0;Create;True;0;0;False;0;None;572cf6a9388f60044af3ae9fa5e70b5d;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;39;386.0633,2132.788;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode;62;-1349.03,1614.085;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;25;-188.814,-106.17;Float;False;Constant;_Float2;Float 2;5;0;Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;411.1336,2344.228;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;1;-634.7,-252.8;Float;True;Property;_diffuse;diffuse;0;0;Create;True;0;0;False;0;None;d3639e63ccd8f454bbff8c4a5fcbf532;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BreakToComponentsNode;54;-942.6663,1638.43;Float;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.DistanceOpNode;40;637.6531,2329.826;Float;True;2;0;FLOAT3;0,0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;24;98.48602,-212.7701;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RelayNode;61;-1353.03,1487.261;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;41;888.2917,2248.779;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;55;-615.4851,1618.132;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;56;-458.7384,1603.813;Float;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;42;-48.13304,1957.303;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ComponentMaskNode;57;-303.7028,1661.342;Float;False;True;False;False;False;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;43;328.1692,2075.577;Float;False;Global;_CSReLightDistance;_CSReLightDistance;48;0;Create;True;0;0;False;0;0;0.007220217;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;9;-969.9001,440.1001;Float;False;Constant;_Color0;Color 0;3;0;Create;True;0;0;False;0;1,0,0,0;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DistanceOpNode;45;283.1121,1890.664;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;44;-590.8051,1824.007;Float;False;Global;_reLightColor;_reLightColor;5;0;Create;True;0;0;False;0;0.8676471,0.7320442,0.4402033,0;0.9338235,0.7337997,0.4806445,0.703;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;3;-830.4813,170.6516;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;10;-975.0755,350.5588;Float;False;Property;_IlluminationStrenght;IlluminationStrenght;2;0;Create;True;0;0;False;0;0;11.16;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;58;-68.85272,1637.93;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;506.3768,1901.186;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;59;114.781,1649.214;Float;False;2;0;FLOAT;0;False;1;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;-235.2209,1898.088;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-672.7305,337.1845;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;6;-647.7968,220.8107;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;-425.0921,240.209;Float;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;48;770.4482,2004.234;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;60;307.034,1667.671;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;49;1016.674,1979.783;Float;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RelayNode;63;-1355.973,1746.103;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;50;1181.313,1822.075;Float;False;Property;_Float10;Float 10;6;0;Create;True;0;0;False;0;0.32;0.131;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;51;1232.872,1921.421;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;52;1268.096,1708.933;Float;False;Global;_CSReLight;_CSReLight;45;0;Create;True;0;0;False;0;2;0.3217259;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;53;1630.937,1712.695;Float;False;True;5;0;FLOAT;0;False;1;FLOAT;1;False;2;COLOR;0,0,0,0;False;3;FLOAT;0;False;4;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-245.9001,13.00006;Float;False;Property;_Float0;Float 0;4;0;Create;True;0;0;False;0;0;0.69;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;65;1670.775,1373.869;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;13;-243.5001,101.2001;Float;False;Property;_Float1;Float 1;3;0;Create;True;0;0;False;0;0;0.53;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;69;493.3787,-45.37216;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RelayNode;70;493.3787,23.62784;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;71;489.3787,95.62784;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;68;492.3787,-112.3722;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldPosInputsNode;66;-1020.747,-193.3626;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RelayNode;67;490.3787,-181.3722;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;699.5999,-172.7;Float;False;True;3;Float;ASEMaterialInspector;0;0;Standard;CScape/CSRooftops;False;False;False;False;False;False;False;False;False;False;False;False;True;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;False;True;True;False;True;True;False;False;False;False;False;False;False;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;4;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;1;False;-1;1;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;27;0;26;2
WireConnection;31;0;26;3
WireConnection;31;1;28;0
WireConnection;30;0;27;0
WireConnection;30;1;28;0
WireConnection;29;0;26;1
WireConnection;29;1;28;0
WireConnection;34;0;31;0
WireConnection;33;0;29;0
WireConnection;32;0;30;0
WireConnection;36;0;33;0
WireConnection;36;1;34;0
WireConnection;36;2;32;0
WireConnection;39;0;35;0
WireConnection;39;1;37;0
WireConnection;62;0;2;0
WireConnection;38;0;36;0
WireConnection;38;1;37;0
WireConnection;54;0;62;0
WireConnection;40;0;38;0
WireConnection;40;1;39;0
WireConnection;24;0;1;0
WireConnection;24;1;25;0
WireConnection;61;0;24;0
WireConnection;41;0;40;0
WireConnection;55;0;54;1
WireConnection;55;1;54;2
WireConnection;56;0;61;0
WireConnection;56;1;55;0
WireConnection;56;2;41;0
WireConnection;57;0;56;0
WireConnection;45;0;26;0
WireConnection;45;1;42;0
WireConnection;58;0;57;0
WireConnection;46;0;45;0
WireConnection;46;1;43;0
WireConnection;59;0;58;0
WireConnection;47;0;44;4
WireConnection;8;0;9;0
WireConnection;8;1;10;0
WireConnection;6;0;3;1
WireConnection;4;0;6;0
WireConnection;4;1;8;0
WireConnection;48;0;46;0
WireConnection;60;0;59;0
WireConnection;60;1;47;0
WireConnection;49;0;60;0
WireConnection;49;1;44;0
WireConnection;49;2;48;0
WireConnection;63;0;4;0
WireConnection;51;0;63;0
WireConnection;51;1;49;0
WireConnection;53;0;52;0
WireConnection;53;1;50;0
WireConnection;53;2;63;0
WireConnection;53;4;51;0
WireConnection;65;0;53;0
WireConnection;69;0;65;0
WireConnection;70;0;12;0
WireConnection;71;0;13;0
WireConnection;68;0;2;0
WireConnection;67;0;24;0
WireConnection;0;0;67;0
WireConnection;0;1;68;0
WireConnection;0;2;69;0
WireConnection;0;3;70;0
WireConnection;0;4;71;0
ASEEND*/
//CHKSM=9A46CC69D26CF3322463621F19F42955E49AE0D7