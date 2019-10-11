// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CScapeAOEvaluation"
{
	Properties
	{
		_ShapeTex("ShapeTex", 2D) = "white" {}
		_AOOffset("AOOffset", Float) = 0
		_AOBlur("AOBlur", Float) = 0
		_AOBlur2("AOBlur2", Float) = 0
		_AOBlur3("AOBlur3", Float) = 0
		_AOBlur4("AOBlur4", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#pragma target 3.5
		#pragma surface surf Standard keepalpha noshadow 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _ShapeTex;
		uniform float4 _ShapeTex_ST;
		uniform float _AOOffset;
		uniform float _AOBlur;
		uniform float _AOBlur2;
		uniform float _AOBlur3;
		uniform float _AOBlur4;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_ShapeTex = i.uv_texcoord * _ShapeTex_ST.xy + _ShapeTex_ST.zw;
			float4 tex2DNode2 = tex2D( _ShapeTex, uv_ShapeTex );
			float temp_output_40_0 = ( tex2DNode2.a * _AOOffset );
			float2 appendResult7 = (float2(0.0 , temp_output_40_0));
			float2 temp_output_5_0 = ( uv_ShapeTex + appendResult7 );
			float blendOpSrc48 = tex2DNode2.a;
			float blendOpDest48 = tex2Dlod( _ShapeTex, float4( temp_output_5_0, 0, _AOBlur) ).a;
			float temp_output_48_0 = ( saturate( ( blendOpDest48 - blendOpSrc48 ) ));
			float2 appendResult72 = (float2(temp_output_40_0 , 0.0));
			float blendOpSrc73 = tex2DNode2.a;
			float blendOpDest73 = tex2Dlod( _ShapeTex, float4( ( uv_ShapeTex + appendResult72 ), 0, _AOBlur) ).a;
			float2 appendResult76 = (float2(( tex2DNode2.a * ( _AOOffset * -1.0 ) ) , 0.0));
			float blendOpSrc79 = tex2DNode2.a;
			float blendOpDest79 = tex2Dlod( _ShapeTex, float4( ( uv_ShapeTex + appendResult76 ), 0, _AOBlur) ).a;
			float blendOpSrc51 = tex2DNode2.a;
			float blendOpDest51 = tex2Dlod( _ShapeTex, float4( temp_output_5_0, 0, _AOBlur2) ).a;
			float blendOpSrc56 = tex2DNode2.a;
			float blendOpDest56 = tex2Dlod( _ShapeTex, float4( temp_output_5_0, 0, _AOBlur3) ).a;
			float blendOpSrc59 = tex2DNode2.a;
			float blendOpDest59 = tex2Dlod( _ShapeTex, float4( temp_output_5_0, 0, _AOBlur4) ).a;
			float3 temp_cast_0 = (( 1.0 - ( ( ( ( temp_output_48_0 + temp_output_48_0 + ( saturate( ( blendOpDest73 - blendOpSrc73 ) )) + ( saturate( ( blendOpDest79 - blendOpSrc79 ) )) ) + ( saturate( ( blendOpDest51 - blendOpSrc51 ) )) + ( saturate( ( blendOpDest56 - blendOpSrc56 ) )) + ( saturate( ( blendOpDest59 - blendOpSrc59 ) )) ) + saturate( ( 1.0 - ( tex2DNode2.g + 0.5 ) ) ) ) * 0.1 ) )).xxx;
			half3 gammaToLinear49 = temp_cast_0;
			gammaToLinear49 = half3( GammaToLinearSpaceExact(gammaToLinear49.r), GammaToLinearSpaceExact(gammaToLinear49.g), GammaToLinearSpaceExact(gammaToLinear49.b) );
			o.Emission = gammaToLinear49;
			o.Alpha = 1;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15600
301;568;1623;714;2552.865;838.7314;1.90556;True;True
Node;AmplifyShaderEditor.TexturePropertyNode;1;-1725.407,-480.9158;Float;True;Property;_ShapeTex;ShapeTex;0;0;Create;True;0;0;False;0;None;47c78fe4e0481964ea3fe9715b051558;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;3;-1095.437,931.6;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;6;-1498.539,705.3002;Float;False;Property;_AOOffset;AOOffset;1;0;Create;True;0;0;False;0;0;0.01;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;2;-702.0817,-302.229;Float;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;78;-1398.095,357.9606;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;40;-1086.033,-5.443084;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;77;-1371.351,-24.5817;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;72;-841.9313,401.7589;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;76;-1132.965,434.0706;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;7;-882.0267,247.0873;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;71;-657.6745,441.5249;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-786.7041,593.3729;Float;False;Property;_AOBlur;AOBlur;2;0;Create;True;0;0;False;0;0;5.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;75;-981.1031,435.7253;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;5;-682.2128,315.5085;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;50;-790.5858,754.4778;Float;False;Property;_AOBlur2;AOBlur2;3;0;Create;True;0;0;False;0;0;5.13;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;55;-783.0827,933.2053;Float;False;Property;_AOBlur3;AOBlur3;4;0;Create;True;0;0;False;0;0;5.13;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;70;-320.7896,235.931;Float;True;Property;_TextureSample5;Texture Sample 5;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;MipLevel;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;4;-361.3681,29.26049;Float;True;Property;_TextureSample1;Texture Sample 1;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;MipLevel;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;57;-781.7142,1146.16;Float;False;Property;_AOBlur4;AOBlur4;5;0;Create;True;0;0;False;0;0;8.21;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;74;-252.3929,429.132;Float;True;Property;_TextureSample6;Texture Sample 6;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;MipLevel;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;54;-381.1103,864.831;Float;True;Property;_TextureSample3;Texture Sample 3;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;MipLevel;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;63;-7.635896,-136.6003;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;41;-395.2866,626.3296;Float;True;Property;_TextureSample2;Texture Sample 2;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;MipLevel;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BlendOpsNode;48;128.1211,129.7823;Float;False;Subtract;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;79;158.7469,413.2218;Float;False;Subtract;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;73;86.16666,235.3941;Float;False;Subtract;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;58;-389.6734,1106.11;Float;True;Property;_TextureSample4;Texture Sample 4;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;MipLevel;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BlendOpsNode;56;318.6127,710.9918;Float;False;Subtract;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;62;194.0751,-11.65474;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;51;316.0815,597.2219;Float;False;Subtract;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;60;441.9398,86.95422;Float;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;59;323.6891,846.8859;Float;False;Subtract;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;64;450.8721,9.466034;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;53;554.6279,221.5476;Float;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;52;721.0851,495.3915;Float;False;Constant;_Float3;Float 3;5;0;Create;True;0;0;False;0;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;61;714.4265,347.2366;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;891.3008,283.347;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;16;1110.928,285.9815;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;12;-1082.807,210.823;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;36;1203.882,657.7917;Float;False;Property;_Strenght;Strenght;6;0;Create;True;0;0;False;0;1;0.96;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;33;-135.0196,-188.4919;Float;False;Constant;_Float1;Float 1;4;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;18;-1263.42,1088.094;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;39;-602.2002,61.64494;Float;False;Constant;_Float5;Float 5;4;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GammaToLinearNode;49;1387.37,288.0368;Float;False;1;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;13;-1230.559,797.0121;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;32;-347.41,-245.3495;Float;False;Constant;_Float0;Float 0;4;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;38;-596.6347,134.9649;Float;False;Constant;_Float4;Float 4;4;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;19;-1370.544,1145.883;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1753.838,250.2185;Float;False;True;3;Float;ASEMaterialInspector;0;0;Standard;CScapeAOEvaluation;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;False;0;False;Opaque;;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;3;2;1;0
WireConnection;2;0;1;0
WireConnection;2;1;3;0
WireConnection;78;0;6;0
WireConnection;40;0;2;4
WireConnection;40;1;6;0
WireConnection;77;0;2;4
WireConnection;77;1;78;0
WireConnection;72;0;40;0
WireConnection;76;0;77;0
WireConnection;7;1;40;0
WireConnection;71;0;3;0
WireConnection;71;1;72;0
WireConnection;75;0;3;0
WireConnection;75;1;76;0
WireConnection;5;0;3;0
WireConnection;5;1;7;0
WireConnection;70;0;1;0
WireConnection;70;1;71;0
WireConnection;70;2;22;0
WireConnection;4;0;1;0
WireConnection;4;1;5;0
WireConnection;4;2;22;0
WireConnection;74;0;1;0
WireConnection;74;1;75;0
WireConnection;74;2;22;0
WireConnection;54;0;1;0
WireConnection;54;1;5;0
WireConnection;54;2;55;0
WireConnection;63;0;2;2
WireConnection;41;0;1;0
WireConnection;41;1;5;0
WireConnection;41;2;50;0
WireConnection;48;0;2;4
WireConnection;48;1;4;4
WireConnection;79;0;2;4
WireConnection;79;1;74;4
WireConnection;73;0;2;4
WireConnection;73;1;70;4
WireConnection;58;0;1;0
WireConnection;58;1;5;0
WireConnection;58;2;57;0
WireConnection;56;0;2;4
WireConnection;56;1;54;4
WireConnection;62;0;63;0
WireConnection;51;0;2;4
WireConnection;51;1;41;4
WireConnection;60;0;48;0
WireConnection;60;1;48;0
WireConnection;60;2;73;0
WireConnection;60;3;79;0
WireConnection;59;0;2;4
WireConnection;59;1;58;4
WireConnection;64;0;62;0
WireConnection;53;0;60;0
WireConnection;53;1;51;0
WireConnection;53;2;56;0
WireConnection;53;3;59;0
WireConnection;61;0;53;0
WireConnection;61;1;64;0
WireConnection;30;0;61;0
WireConnection;30;1;52;0
WireConnection;16;0;30;0
WireConnection;12;0;40;0
WireConnection;18;0;3;0
WireConnection;18;1;19;0
WireConnection;49;0;16;0
WireConnection;13;0;3;0
WireConnection;19;0;6;0
WireConnection;19;1;6;0
WireConnection;0;2;49;0
ASEEND*/
//CHKSM=E6B89CCBF6F4368685B514EC49EE45DE6591CFAE