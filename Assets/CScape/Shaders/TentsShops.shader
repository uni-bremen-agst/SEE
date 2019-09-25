// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CScape/CSTentsShops"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.65
		_Texture0("Texture 0", 2D) = "white" {}
		_tentMaskSubSurface("tentMaskSubSurface", 2D) = "white" {}
		_tentMask("tentMask", 2D) = "white" {}
		_Amplitude("Amplitude", Float) = 0
		_Color0("Color 0", Color) = (0,0,0,0)
		_Smoothness("Smoothness", Range( 0 , 1)) = 0
		_Metallic("Metallic", Range( 0 , 1)) = 0
		_SubsurfaceScatter("SubsurfaceScatter", Range( 0 , 1)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "Geometry+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.5
		#pragma exclude_renderers d3d9 
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows dithercrossfade vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float _Amplitude;
		uniform sampler2D _Texture0;
		uniform float4 _Texture0_ST;
		uniform float4 _Color0;
		uniform sampler2D _tentMaskSubSurface;
		uniform float4 _tentMaskSubSurface_ST;
		uniform float _SubsurfaceScatter;
		uniform float _Metallic;
		uniform float _Smoothness;
		uniform sampler2D _tentMask;
		uniform float4 _tentMask_ST;
		uniform float _Cutoff = 0.65;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float temp_output_15_0 = ( 1.0 - v.color.r );
			float3 appendResult21 = (float3(( ( temp_output_15_0 + ( 1.0 - sin( ( ( v.texcoord.xy.x + _Time.y ) * 4.0 ) ) ) + v.texcoord.xy.x ) * 1.0 * temp_output_15_0 * _Amplitude ) , 0.0 , 0.0));
			v.vertex.xyz += appendResult21;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_Texture0 = i.uv_texcoord * _Texture0_ST.xy + _Texture0_ST.zw;
			float3 tex2DNode4 = UnpackNormal( tex2D( _Texture0, uv_Texture0 ) );
			o.Normal = tex2DNode4;
			float4 temp_output_27_0 = _Color0;
			o.Albedo = temp_output_27_0.rgb;
			float2 uv_tentMaskSubSurface = i.uv_texcoord * _tentMaskSubSurface_ST.xy + _tentMaskSubSurface_ST.zw;
			o.Emission = ( _Color0 * tex2D( _tentMaskSubSurface, uv_tentMaskSubSurface ) * _SubsurfaceScatter ).rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
			float2 uv_tentMask = i.uv_texcoord * _tentMask_ST.xy + _tentMask_ST.zw;
			clip( ( 1.0 - tex2D( _tentMask, uv_tentMask ).r ) - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15800
414;557;2067;627;1742.179;292.4024;1.597019;True;True
Node;AmplifyShaderEditor.SimpleTimeNode;23;-858.655,-60.83469;Float;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;12;-829.5646,-303.5814;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;24;-620.6992,-111.9393;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-636.2661,-30.49132;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;22;-488.1466,-32.08833;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;5;-435.4819,-263.5211;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;15;-249.0112,-111.9393;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;17;-361.9968,-48.05854;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-333.2503,284.1214;Float;False;Property;_Amplitude;Amplitude;4;0;Create;True;0;0;False;0;0;0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;16;-114.4587,3.046093;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;27;-93.66772,-263.6561;Float;False;Property;_Color0;Color 0;5;0;Create;True;0;0;False;0;0,0,0,0;0.6102941,0,0.06313363,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;1;-1274.614,19.26149;Float;True;Property;_Texture0;Texture 0;1;0;Create;True;0;0;False;0;None;2abc436b7babf974999c271f0f0c8532;True;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SamplerNode;30;-111.6228,579.5702;Float;True;Property;_tentMaskSubSurface;tentMaskSubSurface;2;0;Create;True;0;0;False;0;308d81826dbd02e4baec08c4a4b2f44a;308d81826dbd02e4baec08c4a4b2f44a;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;9;-815.7742,345.2534;Float;True;Property;_tentMask;tentMask;3;0;Create;True;0;0;False;0;21acf63903b71d6469380c5cda92b8c3;29275026f0fc0c54c82e6789c5c08371;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;-86.5333,229.8227;Float;False;4;4;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;32;-95.65258,442.2262;Float;False;Property;_SubsurfaceScatter;SubsurfaceScatter;8;0;Create;True;0;0;False;0;0;0.083;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SinTimeNode;13;-488.9815,233.0168;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-614.1608,140.5962;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;1,1,-1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SwitchByFaceNode;7;214.6395,59.41245;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;28;197.3928,-51.25253;Float;False;Property;_Metallic;Metallic;7;0;Create;True;0;0;False;0;0;0.059;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;29;201.3928,207.4644;Float;False;Property;_Smoothness;Smoothness;6;0;Create;True;0;0;False;0;0;0.307;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;31;76.82548,156.36;Float;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldPosInputsNode;26;-1035.118,-238.1039;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;21;288.0049,333.6291;Float;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;10;-324.8436,361.8146;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.UnpackScaleNormalNode;3;-505.0181,490.9334;Float;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SamplerNode;4;-989.1782,32.42684;Float;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;567.8774,-72.16585;Float;False;True;3;Float;ASEMaterialInspector;0;0;Standard;CScape/CSTentsShops;False;False;False;False;False;False;False;False;False;False;False;False;True;False;True;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.65;True;True;0;True;TransparentCutout;;Geometry;All;False;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;24;0;12;1
WireConnection;24;1;23;0
WireConnection;25;0;24;0
WireConnection;22;0;25;0
WireConnection;15;0;5;1
WireConnection;17;0;22;0
WireConnection;16;0;15;0
WireConnection;16;1;17;0
WireConnection;16;2;12;1
WireConnection;14;0;16;0
WireConnection;14;2;15;0
WireConnection;14;3;18;0
WireConnection;8;0;4;0
WireConnection;31;0;27;0
WireConnection;31;1;30;0
WireConnection;31;2;32;0
WireConnection;21;0;14;0
WireConnection;10;0;9;1
WireConnection;4;0;1;0
WireConnection;0;0;27;0
WireConnection;0;1;4;0
WireConnection;0;2;31;0
WireConnection;0;3;28;0
WireConnection;0;4;29;0
WireConnection;0;10;10;0
WireConnection;0;11;21;0
ASEEND*/
//CHKSM=B2B7EFAE95B9781D7A6D1316859435B7CF09E047