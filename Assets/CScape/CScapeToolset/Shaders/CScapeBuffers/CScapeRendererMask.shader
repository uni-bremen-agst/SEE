// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CScapeRendererCScapeMask2"
{
	Properties
	{
		_Color0("Color 0", Color) = (0,0,0,0)
		_Color1("Color 1", Color) = (0,0,0,0)
		_ShapeTex("ShapeTex", 2D) = "white" {}
		_Color2("Color 2", Color) = (0,0,0,0)
		_Smooth1("Smooth1", Range( 0 , 0.1)) = 0
		_Smooth2("Smooth2", Range( 0 , 0.1)) = 0
		_Pos1("Pos1", Range( 0 , 1)) = 0
		_pos2("pos2", Range( 0 , 1)) = 0
		_MipBias("MipBias", Float) = -5
	}
	
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend Off
		Cull Back
		ColorMask RGBA
		ZWrite On
		ZTest LEqual
		Offset 0 , 0
		
		

		Pass
		{
			Name "Unlit"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			

			struct appdata
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				float4 ase_texcoord : TEXCOORD0;
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
				float4 ase_texcoord : TEXCOORD0;
			};

			uniform float4 _Color1;
			uniform float4 _Color2;
			uniform float4 _Color0;
			uniform float _Pos1;
			uniform float _Smooth1;
			uniform sampler2D _ShapeTex;
			uniform float4 _ShapeTex_ST;
			uniform float _MipBias;
			uniform float _pos2;
			uniform float _Smooth2;
			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.ase_texcoord.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord.zw = 0;
				
				v.vertex.xyz +=  float3(0,0,0) ;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				fixed4 finalColor;
				float temp_output_16_0_g12 = _Pos1;
				float2 uv_ShapeTex = i.ase_texcoord.xy * _ShapeTex_ST.xy + _ShapeTex_ST.zw;
				float4 tex2DNode2 = tex2Dbias( _ShapeTex, float4( uv_ShapeTex, 0, _MipBias) );
				float smoothstepResult7_g12 = smoothstep( temp_output_16_0_g12 , ( temp_output_16_0_g12 + _Smooth1 ) , tex2DNode2.b);
				float4 lerpResult11_g12 = lerp( _Color2 , _Color0 , smoothstepResult7_g12);
				float temp_output_19_0_g12 = _pos2;
				float smoothstepResult8_g12 = smoothstep( temp_output_19_0_g12 , ( temp_output_19_0_g12 + _Smooth2 ) , tex2DNode2.b);
				float4 lerpResult9_g12 = lerp( _Color1 , lerpResult11_g12 , smoothstepResult8_g12);
				float4 temp_output_166_0 = lerpResult9_g12;
				float temp_output_263_0 = step( tex2DNode2.a , 0.001 );
				float4 lerpResult261 = lerp( temp_output_166_0 , float4( 0,0,0,0 ) , temp_output_263_0);
				float3 temp_output_208_0 = (lerpResult261).rgb;
				float4 appendResult258 = (float4(temp_output_208_0 , 1.0));
				
				
				finalColor = appendResult258;
				return finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=15600
302;446;1623;714;3019.534;491.5582;1.759927;True;True
Node;AmplifyShaderEditor.TexturePropertyNode;6;-2447.505,266.215;Float;True;Property;_ShapeTex;ShapeTex;8;0;Create;True;0;0;False;0;None;47c78fe4e0481964ea3fe9715b051558;True;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;190;-1223.318,-69.13556;Float;False;Property;_MipBias;MipBias;27;0;Create;True;0;0;False;0;-5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;11;-2043.227,-280.5245;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;163;310.245,-1569.479;Float;False;Property;_Smooth2;Smooth2;15;0;Create;True;0;0;False;0;0;0.0263;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;161;138.7117,-1337.253;Float;False;Property;_Color1;Color 1;4;0;Create;True;0;0;False;0;0,0,0,0;0,0,1,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;159;355.7688,-1485.199;Float;False;Property;_Color0;Color 0;0;0;Create;True;0;0;False;0;0,0,0,0;0,1,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;160;306.3982,-1836.628;Float;False;Property;_Pos1;Pos1;16;0;Create;True;0;0;False;0;0;0.561;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;165;304.222,-1659.747;Float;False;Property;_pos2;pos2;18;0;Create;True;0;0;False;0;0;0.437;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;2;-1042.923,-159.7489;Float;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;False;0;None;47c78fe4e0481964ea3fe9715b051558;True;0;False;white;Auto;False;Object;-1;MipBias;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;164;306.0778,-1754.823;Float;False;Property;_Smooth1;Smooth1;13;0;Create;True;0;0;False;0;0;0.0016;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;162;355.1351,-1310.228;Float;False;Property;_Color2;Color 2;11;0;Create;True;0;0;False;0;0,0,0,0;1,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;166;910.3984,-1076.234;Float;False;3ColorGradient;-1;;12;af7134d49ad8c844294e466f39ac2e86;0;8;15;FLOAT;0;False;23;COLOR;0,0,0,0;False;21;COLOR;0,0,0,0;False;22;COLOR;0,0,0,0;False;16;FLOAT;0;False;19;FLOAT;0;False;17;FLOAT;0;False;20;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StepOpNode;263;3394.285,1364.006;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.001;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;261;3958.246,1322.404;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;28;-2454.822,-4120.985;Float;False;6082.19;1413.166;Comment;28;56;55;54;53;52;50;49;48;47;46;45;44;43;42;41;40;39;38;37;36;35;34;33;32;31;30;29;51;;1,1,1,1;0;0
Node;AmplifyShaderEditor.ComponentMaskNode;208;4142.046,1347.662;Float;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;214;223.5226,1918.514;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;225;1260.25,1589.814;Float;True;Property;_TextureSample6;Texture Sample 6;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;MipLevel;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;222;939.4047,1876.062;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;215;919.5358,1258.324;Float;True;Property;_TextureSample5;Texture Sample 5;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;220;779.6863,1962.312;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;219;739.5909,1807.641;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;233;1231.944,2666.664;Float;True;Property;_TextureSample9;Texture Sample 9;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;MipLevel;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;218;488.6526,1994.624;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;212;123.0786,2265.854;Float;False;Property;_AOOffset;AOOffset;2;0;Create;True;0;0;False;0;0;0.01;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;260;554.4402,1168.038;Float;False;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;213;526.1805,2492.153;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;221;640.5145,1996.279;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;227;1369.225,1989.685;Float;True;Property;_TextureSample7;Texture Sample 7;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;MipLevel;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;223;834.9135,2153.926;Float;False;Property;_AOBlur;AOBlur;5;0;Create;True;0;0;False;0;0;5.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;229;831.0317,2315.031;Float;False;Property;_AOBlur2;AOBlur2;7;0;Create;True;0;0;False;0;0;5.13;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;224;963.9431,2002.078;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;228;1300.828,1796.484;Float;True;Property;_TextureSample8;Texture Sample 8;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;MipLevel;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;22;-849.2803,-819.2297;Float;False;Property;_Mat1Index;Mat1Index;19;0;Create;True;0;0;False;0;0;1.28;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;230;838.5349,2493.759;Float;False;Property;_AOBlur3;AOBlur3;9;0;Create;True;0;0;False;0;0;5.13;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;226;839.9034,2706.713;Float;False;Property;_AOBlur4;AOBlur4;10;0;Create;True;0;0;False;0;0;8.21;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;157;3029.816,966.4309;Float;False;156;Occlusion;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;231;1780.365,1973.775;Float;False;Subtract;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;216;250.2666,1535.972;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;234;1707.784,1795.948;Float;False;Subtract;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;241;1940.23,2271.545;Float;False;Subtract;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;236;1613.982,1423.953;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;239;2063.557,1647.508;Float;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;244;2072.49,1570.02;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GammaToLinearNode;250;3008.988,1848.59;Float;False;1;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.PowerNode;92;49.89481,213.8936;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;188;2368.566,-552.5143;Float;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.OneMinusNode;242;1815.693,1548.899;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;237;1226.331,2186.883;Float;True;Property;_TextureSample11;Texture Sample 11;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;MipLevel;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BlendOpsNode;240;1945.307,2407.439;Float;False;Subtract;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;232;1749.739,1690.336;Float;False;Subtract;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;243;2176.246,1782.101;Float;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;248;2732.545,1846.535;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;146;2584.18,7.02181;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;8,8;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;217;535.5846,1555.11;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;76;1104.685,-559.4252;Float;False;3;3;0;COLOR;1,0,0,0;False;1;FLOAT;0.5;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;247;2512.918,1843.9;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;235;1240.507,2425.385;Float;True;Property;_TextureSample10;Texture Sample 10;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;MipLevel;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;245;2336.044,1907.79;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;246;2342.703,2055.945;Float;False;Constant;_Float3;Float 3;5;0;Create;True;0;0;False;0;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;57;722.8144,17.33135;Float;False;51;Enterier;0;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;-678.6764,-3429.456;Float;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;45;1954.634,-3482.042;Float;False;62;NoiseTex;0;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;53;2472.734,-3543.176;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;32;-1451.386,-3488.557;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.UnpackScaleNormalNode;118;2320.666,-272.7055;Float;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0.2;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SamplerNode;143;2767.028,81.38976;Float;True;Property;_TextureSample4;Texture Sample 4;15;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;48;2444.936,-3441.439;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;6;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;67;379.1805,-564.6974;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;1,1,1,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TexturePropertyNode;142;2601.664,-213.2125;Float;True;Property;_Texture1;Texture 1;26;0;Create;True;0;0;False;0;None;351a0be17d79558409a4ed61472aa426;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.OneMinusNode;192;2791.122,971.709;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;41;-57.35859,-3076.743;Float;False;Constant;_Float4;Float 4;2;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LayeredBlendNode;20;-94.20151,-1240.591;Float;False;6;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CustomExpressionNode;37;-918.2714,-3139.858;Float;False;min(min(C.x, C.y), C.z);1;False;1;True;C;FLOAT3;0,0,0;In;;expr3;True;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;258;4547.815,1461.956;Float;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;141;-1314.669,-227.661;Float;False;138;POMuv;0;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;209;4129.592,1525.802;Float;False;True;False;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;82;-1249.664,1024.506;Float;False;Property;_Blurring;Blurring;23;0;Create;True;0;0;False;0;0;4.68;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;36;-1230.574,-3125.956;Float;False;abs(B) - A * B;3;False;2;True;A;FLOAT3;0,0,0;In;;True;B;FLOAT3;0,0,0;In;;expr4;True;False;0;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;94;-1168.132,882.7029;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0.03;False;1;FLOAT2;0
Node;AmplifyShaderEditor.UnpackScaleNormalNode;1;7.565404,-104.6572;Float;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;156;1014.074,-181.4698;Float;False;Occlusion;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;44;1096.733,-2941.843;Float;False;Constant;_Float5;Float 5;3;0;Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;93;-255.8197,599.58;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;33;-1737.467,-3217.456;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;106;-1631.428,900.6926;Float;False;Property;_ShadowOffset;ShadowOffset;24;0;Create;True;0;0;False;0;0;0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;100;733.4094,-369.4919;Float;False;3;0;FLOAT;0;False;1;FLOAT;0.005;False;2;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;189;2653.522,-543.3956;Float;False;FLOAT4;4;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.BlendOpsNode;96;-341.3443,826.3843;Float;False;LinearBurn;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;91;700.5237,-482.8893;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;47;2562.531,-3252.873;Float;False;-1;;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;51;3395.914,-3405.318;Float;False;Enterier;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DynamicAppendNode;107;-1663.523,677.3142;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;211;-103.7894,1079.638;Float;True;Property;_Texture0;Texture 0;1;0;Create;True;0;0;False;0;None;47c78fe4e0481964ea3fe9715b051558;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;147;120.7066,76.05547;Float;False;GlassMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;145;2338.879,7.021453;Float;False;138;POMuv;0;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureArrayNode;123;1341.625,-845.1407;Float;True;Property;_TextureArray1;Texture Array 1;19;0;Create;True;0;0;False;0;None;0;Instance;116;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;120;2121.717,-100.023;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,1;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;39;-506.3744,-3451.857;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;58;957.6638,60.48274;Float;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0.3970588,0.3970588,0.3970588,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleAddOpNode;80;-191.3666,528.8115;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;23;-478.9412,-1038.954;Float;True;Property;_TextureArray0;Texture Array 0;3;0;Create;True;0;0;False;0;None;0;Instance;21;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;148;1880.144,-56.846;Float;False;147;GlassMask;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureArrayNode;116;1416.462,-1139.872;Float;True;Property;_SurfaceNormalArray;SurfaceNormalArray;25;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;144;2887.373,355.1479;Float;False;3;0;COLOR;1,1,1,0;False;1;COLOR;1,1,1,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TextureArrayNode;50;2915.635,-3530.575;Float;True;Property;_EnterierMap;EnterierMap;20;0;Create;True;0;0;False;0;None;0;Object;-1;MipLevel;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BlendNormalsNode;115;1533.345,57.51233;Float;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;251;1274.208,1315.204;Float;False;Constant;_Float1;Float 1;4;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;140;-1434.3,735.688;Float;False;138;POMuv;0;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;256;358.1975,2648.647;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CustomExpressionNode;43;885.8407,-3217.141;Float;False;(1.0 - (1.0 / realZ)) * (depthScale +1.0);1;False;2;True;depthScale;FLOAT;0;In;;True;realZ;FLOAT;0;In;;expr6;True;False;0;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LayeredBlendNode;124;2146.048,-672.0236;Float;False;6;0;COLOR;0,0,0,0;False;1;COLOR;1,0.5,1,1;False;2;COLOR;0.5,0.5,1,1;False;3;COLOR;0,0,0,0;False;4;COLOR;1,0.5,1,0.5;False;5;COLOR;1,1,1,0;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;86;-346.6856,614.6371;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;253;1024.983,1695.518;Float;False;Constant;_Float2;Float 2;4;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DdxOpNode;71;-1409.251,-108.5906;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;9;-1558.314,-1050.231;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;139;-1228.874,495.8593;Float;False;138;POMuv;0;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;262;3564.777,1404.409;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DdyOpNode;186;-1403.988,35.36983;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DistanceBasedTessNode;202;3868.708,945.5687;Float;False;3;0;FLOAT;10000;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;66;749.5804,300.4803;Float;False;Property;_Float0;Float 0;22;0;Create;True;0;0;False;0;1.2;4.61;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;257;1486.598,1372.062;Float;False;Constant;_Float8;Float 8;4;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;63;1616.379,-1596.401;Float;False;Variation;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;249;391.0586,2357.565;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;30;-1994.868,-3230.157;Float;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;65;682.1287,211.4594;Float;False;63;Variation;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;64;1135.317,232.3642;Float;False;3;3;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;153;299.754,-460.953;Float;False;147;GlassMask;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;62;1627.233,-1717.097;Float;False;NoiseTex;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;155;808.8601,-161.7908;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ParallaxOcclusionMappingNode;5;-1802.307,252.4408;Float;False;3;10;True;194;52;True;195;4;0.02;0.49;False;1,1;True;0,0;Texture2D;7;0;FLOAT2;0,0;False;1;SAMPLER2D;;False;2;FLOAT;0.02;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT2;0,0;False;6;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode;132;-1953.676,-43.08847;Float;False;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;138;-1330.613,293.4731;Float;False;POMuv;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCCompareEqual;179;3389.241,281.2149;Float;False;4;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT4;0,0,0,0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DynamicAppendNode;255;538.8105,1771.376;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;7;-2299.107,422.8035;Float;False;Tangent;True;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;195;-2092.048,683.991;Float;False;Property;_MaxSamplesBias;MaxSamplesBias;28;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;194;-2098.498,619.5837;Float;False;Property;_MinSamplesBias;MinSamplesBias;29;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;200;-1679.635,927.4346;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;201;-1973.409,273.3181;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-2174.146,320.8826;Float;False;Property;_Scale;Scale;6;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;4;-368.2405,-814.8309;Float;False;Property;_WindowBorder;WindowBorder;3;0;Create;True;0;0;False;0;0.9852941,0.9852941,0.9852941,0;1,1,1,0.066;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;252;251.0736,2706.437;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;56;3359.377,-3590.463;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;152;2333.345,938.052;Float;False;147;GlassMask;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;191;2844.101,703.4075;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;26;590.5706,-575.9547;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0.3073097,0.3623703,0.4264706,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;83;-523.1858,497.3757;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;130;3040.679,788.1839;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;151;2244.51,711.8819;Float;False;147;GlassMask;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;52;3082.128,-3218.985;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;81;-982.7628,447.924;Float;True;Property;_TextureSample2;Texture Sample 2;0;0;Create;True;0;0;False;0;None;47c78fe4e0481964ea3fe9715b051558;True;0;False;white;Auto;False;Instance;2;Derivative;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RelayNode;131;3039.863,871.1954;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;70;2574.533,720.3206;Float;False;5;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;3;FLOAT;0.5;False;4;FLOAT;0.18;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;29;-2006.169,-3527.779;Float;False;0;6;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;205;3566.375,1166.349;Float;False;Property;_TessMax;TessMax;30;0;Create;True;0;0;False;0;0;-2406.4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;54;2560.035,-2996.64;Float;False;Property;_Float6;Float 6;14;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;150;703.1785,114.2757;Float;False;147;GlassMask;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;90;243.0958,196.8031;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-1372.348,881.3477;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode;128;3055.094,613.3909;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RelayNode;158;497.8739,-867.9783;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;3;-522.0994,72.69286;Float;False;FLOAT4;4;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;254;1019.417,1622.198;Float;False;Constant;_Float7;Float 7;4;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;187;2847.571,-541.116;Float;False;FLOAT4;4;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CustomExpressionNode;34;-1236.771,-3566.956;Float;False;float3(X * 2 - 1, -1);3;False;1;True;X;FLOAT2;0,0;In;;expr5;True;False;0;1;0;FLOAT2;0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CustomExpressionNode;42;268.5384,-3327.643;Float;False;saturate(interp1) / depthScale + 1;1;False;2;True;depthScale;FLOAT;0;In;;True;interp1;FLOAT;0;In;;Expr1;True;False;0;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;129;3043.756,696.4732;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-783.5156,-380.1265;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;7,7;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;61;1109.476,-1767.775;Float;True;Property;_TextureSample1;Texture Sample 1;21;0;Create;True;0;0;False;0;None;be53a91e4e185d9448d300ead53b5f1f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;55;2301.733,-3544.478;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;3.00456;False;1;FLOAT;0
Node;AmplifyShaderEditor.ParallaxOffsetHlpNode;197;-2105.869,161.038;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PowerNode;99;-40.20959,835.5598;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.3;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;127;3079.26,528.6057;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.BreakToComponentsNode;181;197.802,-638.6409;Float;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.BlendOpsNode;238;1937.699,2157.775;Float;False;Subtract;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;84;-521.7095,609.6806;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;49;2017.171,-3088.057;Float;False;interiorUV * -0.5 - 0.5;2;False;1;True;interiorUV;FLOAT2;0,0;In;;expr8;True;False;0;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureArrayNode;21;-510.2889,-1273.936;Float;True;Property;_Surface;Surface;12;0;Create;True;0;0;False;0;None;0;Object;-1;Auto;False;7;6;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;95;-940.8721,912.6491;Float;True;Property;_TextureSample3;Texture Sample 3;0;0;Create;True;0;0;False;0;None;47c78fe4e0481964ea3fe9715b051558;True;0;False;white;Auto;False;Instance;2;MipLevel;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;97;-566.6407,847.1938;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;24;-961.5226,-519.3093;Float;False;Property;_Mat2Index;Mat2Index;17;0;Create;True;0;0;False;0;0;5.73;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;46;1491.436,-3130.743;Float;False;pos.xy * lerp(1.0, farFrac, interp2);2;False;3;True;pos;FLOAT3;0,0,0;In;;True;interp2;FLOAT;0;In;;True;farFrac;FLOAT;0;In;;expr7;True;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;149;2541.881,390.8065;Float;False;147;GlassMask;0;1;FLOAT;0
Node;AmplifyShaderEditor.ParallaxMappingNode;198;-1682.036,-5.53418;Float;False;Normal;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.AbsOpNode;85;-360.0459,512.7529;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCCompareEqual;180;3729.05,228.4395;Float;False;4;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RelayNode;126;-489.0309,-91.97888;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;88;-152.7834,208.6204;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;6;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;98;179.8056,823.5486;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;210;4460.025,1867.861;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TFHCCompareEqual;178;3408.669,113.502;Float;False;4;0;FLOAT;0;False;1;FLOAT;1;False;2;COLOR;0,0,0,0;False;3;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SmoothstepOpNode;154;-137.3085,41.87126;Float;False;3;0;FLOAT;0;False;1;FLOAT;0.01;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;31;-1699.507,-3501.93;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;-1,-1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;204;3547.227,1075.463;Float;False;Property;_TessMin;TessMin;31;0;Create;True;0;0;False;0;0;5107.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;40;-89.76366,-3412.142;Float;False;pos.z * 0.5 + 0.5;1;False;1;True;pos;FLOAT3;0,0,0;In;;expr2;True;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;203;3578.085,952.1918;Float;False;Property;_TessFactor;TessFactor;32;0;Create;True;0;0;False;0;0;69;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;35;-1483.573,-3092.764;Float;False;2;0;FLOAT3;1,1,1;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TFHCRemapNode;69;2585.042,944.5467;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0.46;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;265;4798.362,1483.678;Float;False;True;2;Float;ASEMaterialInspector;0;1;CScapeRendererCScapeMask2;0770190933193b94aaa3065e307002fa;0;0;Unlit;2;True;0;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;True;0;False;-1;0;False;-1;True;0;False;-1;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;RenderType=Opaque;True;2;0;False;False;False;False;False;False;False;False;False;False;0;;0;0;Standard;0;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;0
WireConnection;11;2;6;0
WireConnection;2;0;6;0
WireConnection;2;1;11;0
WireConnection;2;2;190;0
WireConnection;166;15;2;3
WireConnection;166;23;159;0
WireConnection;166;21;161;0
WireConnection;166;22;162;0
WireConnection;166;16;160;0
WireConnection;166;19;165;0
WireConnection;166;17;164;0
WireConnection;166;20;163;0
WireConnection;263;0;2;4
WireConnection;261;0;166;0
WireConnection;261;2;263;0
WireConnection;208;0;261;0
WireConnection;214;0;212;0
WireConnection;225;0;260;0
WireConnection;225;1;222;0
WireConnection;225;2;223;0
WireConnection;222;0;213;0
WireConnection;222;1;219;0
WireConnection;215;0;260;0
WireConnection;215;1;213;0
WireConnection;220;0;217;0
WireConnection;219;1;217;0
WireConnection;233;0;260;0
WireConnection;233;1;222;0
WireConnection;233;2;226;0
WireConnection;218;0;216;0
WireConnection;260;0;6;0
WireConnection;213;2;260;0
WireConnection;221;0;213;0
WireConnection;221;1;218;0
WireConnection;227;0;260;0
WireConnection;227;1;221;0
WireConnection;227;2;223;0
WireConnection;224;0;213;0
WireConnection;224;1;220;0
WireConnection;228;0;260;0
WireConnection;228;1;224;0
WireConnection;228;2;223;0
WireConnection;231;0;215;4
WireConnection;231;1;227;4
WireConnection;216;0;215;4
WireConnection;216;1;214;0
WireConnection;234;0;215;4
WireConnection;234;1;228;4
WireConnection;241;0;215;4
WireConnection;241;1;235;4
WireConnection;236;0;215;2
WireConnection;239;0;232;0
WireConnection;239;1;232;0
WireConnection;239;2;234;0
WireConnection;239;3;231;0
WireConnection;244;0;242;0
WireConnection;250;0;248;0
WireConnection;92;0;88;0
WireConnection;188;0;124;0
WireConnection;242;0;236;0
WireConnection;237;0;260;0
WireConnection;237;1;222;0
WireConnection;237;2;229;0
WireConnection;240;0;215;4
WireConnection;240;1;233;4
WireConnection;232;0;215;4
WireConnection;232;1;225;4
WireConnection;243;0;239;0
WireConnection;243;1;238;0
WireConnection;243;2;241;0
WireConnection;243;3;240;0
WireConnection;248;0;247;0
WireConnection;146;0;145;0
WireConnection;217;0;215;4
WireConnection;217;1;212;0
WireConnection;76;0;26;0
WireConnection;76;1;100;0
WireConnection;76;2;156;0
WireConnection;247;0;245;0
WireConnection;247;1;246;0
WireConnection;235;0;260;0
WireConnection;235;1;222;0
WireConnection;235;2;230;0
WireConnection;245;0;243;0
WireConnection;245;1;244;0
WireConnection;38;0;37;0
WireConnection;38;1;33;0
WireConnection;53;0;55;0
WireConnection;32;0;31;0
WireConnection;118;0;189;0
WireConnection;143;0;142;0
WireConnection;143;1;146;0
WireConnection;48;0;45;0
WireConnection;67;0;20;0
WireConnection;192;0;157;0
WireConnection;20;0;158;0
WireConnection;20;2;21;0
WireConnection;20;3;23;0
WireConnection;20;4;4;0
WireConnection;37;0;36;0
WireConnection;258;0;208;0
WireConnection;209;0;250;0
WireConnection;36;0;34;0
WireConnection;36;1;35;0
WireConnection;94;0;140;0
WireConnection;94;1;105;0
WireConnection;1;0;3;0
WireConnection;156;0;155;0
WireConnection;93;0;126;0
WireConnection;33;0;30;0
WireConnection;100;0;126;0
WireConnection;189;3;188;0
WireConnection;96;0;95;4
WireConnection;96;1;97;0
WireConnection;91;0;90;0
WireConnection;51;0;50;0
WireConnection;107;1;93;0
WireConnection;147;0;154;0
WireConnection;123;0;25;0
WireConnection;123;1;24;0
WireConnection;120;0;118;0
WireConnection;120;2;148;0
WireConnection;39;0;34;0
WireConnection;39;1;38;0
WireConnection;58;1;57;0
WireConnection;58;2;150;0
WireConnection;80;0;85;0
WireConnection;80;1;86;0
WireConnection;23;0;25;0
WireConnection;23;1;24;0
WireConnection;116;0;25;0
WireConnection;116;1;22;0
WireConnection;144;0;76;0
WireConnection;144;1;143;1
WireConnection;144;2;149;0
WireConnection;50;0;49;0
WireConnection;50;1;48;0
WireConnection;50;2;47;0
WireConnection;115;0;1;0
WireConnection;115;1;120;0
WireConnection;256;0;213;0
WireConnection;256;1;252;0
WireConnection;43;0;41;0
WireConnection;43;1;42;0
WireConnection;124;0;158;0
WireConnection;124;2;116;0
WireConnection;124;3;123;0
WireConnection;86;0;84;0
WireConnection;71;0;5;0
WireConnection;262;0;263;0
WireConnection;186;0;5;0
WireConnection;202;0;203;0
WireConnection;202;1;204;0
WireConnection;202;2;205;0
WireConnection;63;0;61;2
WireConnection;249;0;213;0
WireConnection;64;0;58;0
WireConnection;64;1;65;0
WireConnection;64;2;66;0
WireConnection;62;0;61;1
WireConnection;155;0;91;0
WireConnection;155;1;98;0
WireConnection;5;0;11;0
WireConnection;5;1;132;0
WireConnection;5;2;201;0
WireConnection;5;3;7;0
WireConnection;132;0;6;0
WireConnection;138;0;5;0
WireConnection;179;2;129;0
WireConnection;179;3;130;0
WireConnection;255;0;217;0
WireConnection;200;0;194;0
WireConnection;200;2;195;0
WireConnection;201;0;200;0
WireConnection;201;1;8;0
WireConnection;252;0;212;0
WireConnection;252;1;212;0
WireConnection;56;0;50;0
WireConnection;56;1;52;0
WireConnection;191;1;192;0
WireConnection;26;0;67;0
WireConnection;26;2;153;0
WireConnection;83;0;2;1
WireConnection;130;0;191;0
WireConnection;52;0;47;0
WireConnection;81;0;6;0
WireConnection;81;1;139;0
WireConnection;81;3;71;0
WireConnection;81;4;186;0
WireConnection;70;0;151;0
WireConnection;90;0;92;0
WireConnection;105;0;107;0
WireConnection;105;1;106;0
WireConnection;128;0;115;0
WireConnection;158;0;166;0
WireConnection;3;1;2;2
WireConnection;3;3;2;1
WireConnection;34;0;32;0
WireConnection;42;0;41;0
WireConnection;42;1;40;0
WireConnection;129;0;64;0
WireConnection;25;0;138;0
WireConnection;55;0;45;0
WireConnection;99;0;96;0
WireConnection;127;0;144;0
WireConnection;181;0;20;0
WireConnection;238;0;215;4
WireConnection;238;1;237;4
WireConnection;84;0;2;2
WireConnection;49;0;46;0
WireConnection;21;0;25;0
WireConnection;21;1;22;0
WireConnection;95;0;6;0
WireConnection;95;1;94;0
WireConnection;95;2;82;0
WireConnection;97;0;2;4
WireConnection;46;0;39;0
WireConnection;46;1;43;0
WireConnection;46;2;44;0
WireConnection;85;0;83;0
WireConnection;180;2;178;0
WireConnection;180;3;179;0
WireConnection;126;0;2;3
WireConnection;88;0;80;0
WireConnection;88;1;93;0
WireConnection;98;0;99;0
WireConnection;210;0;208;0
WireConnection;210;2;150;0
WireConnection;178;2;127;0
WireConnection;178;3;128;0
WireConnection;154;0;126;0
WireConnection;31;0;29;0
WireConnection;40;0;39;0
WireConnection;35;1;33;0
WireConnection;69;0;152;0
WireConnection;265;0;258;0
ASEEND*/
//CHKSM=75953D9E020D2CFEE8B959F42A1F8C8B9E2477AA