// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "CSDepthToNormalEffect"
{
	Properties
	{
		_MainTex ( "Screen", 2D ) = "black" {}
		_Offset("Offset", Range( 0 , 1)) = 0.18
		_TextureSample4("Texture Sample 4", 2D) = "bump" {}
		_GlassNormalStrenght("GlassNormalStrenght", Range( 0 , 1)) = 0
		_Strenght("Strenght", Range( 0 , 150)) = 5.4
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
	}

	SubShader
	{
		
		
		ZTest Always
		Cull Off
		ZWrite On

		
		Pass
		{ 
			CGPROGRAM 

			#pragma vertex vert_img_custom 
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"


			struct appdata_img_custom
			{
				float4 vertex : POSITION;
				half2 texcoord : TEXCOORD0;
				
			};

			struct v2f_img_custom
			{
				float4 pos : SV_POSITION;
				half2 uv   : TEXCOORD0;
				half2 stereoUV : TEXCOORD2;
		#if UNITY_UV_STARTS_AT_TOP
				half4 uv2 : TEXCOORD1;
				half4 stereoUV2 : TEXCOORD3;
		#endif
				
			};

			uniform sampler2D _MainTex;
			uniform half4 _MainTex_TexelSize;
			uniform half4 _MainTex_ST;
			
			uniform float _Offset;
			uniform float _Strenght;
			uniform float _GlassNormalStrenght;
			uniform sampler2D _TextureSample4;
			uniform float4 _TextureSample4_ST;

			v2f_img_custom vert_img_custom ( appdata_img_custom v  )
			{
				v2f_img_custom o;
				
				o.pos = UnityObjectToClipPos ( v.vertex );
				o.uv = float4( v.texcoord.xy, 1, 1 );

				#if UNITY_UV_STARTS_AT_TOP
					o.uv2 = float4( v.texcoord.xy, 1, 1 );
					o.stereoUV2 = UnityStereoScreenSpaceUVAdjust ( o.uv2, _MainTex_ST );

					if ( _MainTex_TexelSize.y < 0.0 )
						o.uv.y = 1.0 - o.uv.y;
				#endif
				o.stereoUV = UnityStereoScreenSpaceUVAdjust ( o.uv, _MainTex_ST );
				return o;
			}

			half4 frag ( v2f_img_custom i ) : SV_Target
			{
				#ifdef UNITY_UV_STARTS_AT_TOP
					half2 uv = i.uv2;
					half2 stereoUV = i.stereoUV2;
				#else
					half2 uv = i.uv;
					half2 stereoUV = i.stereoUV;
				#endif	
				
				half4 finalColor;

				// ase common template code
				float2 uv_MainTex = i.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float2 temp_output_2_0_g11 = uv_MainTex;
				float2 break6_g11 = temp_output_2_0_g11;
				float temp_output_25_0_g11 = ( pow( _Offset , 3.0 ) * 0.1 );
				float2 appendResult8_g11 = (float2(( break6_g11.x + temp_output_25_0_g11 ) , break6_g11.y));
				float4 tex2DNode14_g11 = tex2D( _MainTex, temp_output_2_0_g11 );
				float temp_output_4_0_g11 = _Strenght;
				float3 appendResult13_g11 = (float3(1.0 , 0.0 , ( ( tex2D( _MainTex, appendResult8_g11 ).g - tex2DNode14_g11.g ) * temp_output_4_0_g11 )));
				float2 appendResult9_g11 = (float2(break6_g11.x , ( break6_g11.y + temp_output_25_0_g11 )));
				float3 appendResult16_g11 = (float3(0.0 , 1.0 , ( ( tex2D( _MainTex, appendResult9_g11 ).g - tex2DNode14_g11.g ) * temp_output_4_0_g11 )));
				float3 normalizeResult22_g11 = normalize( cross( appendResult13_g11 , appendResult16_g11 ) );
				float3 temp_output_4_0 = ( normalizeResult22_g11 * float3( 0.5,0.5,1 ) );
				float3 break7 = ( temp_output_4_0 + float3( 0.5,0.5,0 ) );
				float2 appendResult13 = (float2(break7.x , break7.y));
				float2 uv_TextureSample4 = i.uv.xy * _TextureSample4_ST.xy + _TextureSample4_ST.zw;
				float3 temp_output_39_0 = ( ( UnpackScaleNormal( tex2D( _TextureSample4, uv_TextureSample4 ), _GlassNormalStrenght ) * float3( 0.5,0.5,1 ) ) + float3( 0.5,0.5,0 ) );
				float4 tex2DNode5 = tex2D( _MainTex, uv_MainTex );
				float3 lerpResult12 = lerp( float3( appendResult13 ,  0.0 ) , temp_output_39_0 , step( tex2DNode5.r , 0.01 ));
				float4 appendResult6 = (float4(lerpResult12.xy , tex2DNode5.r , tex2DNode5.g));
				

				finalColor = appendResult6;

				return finalColor;
			} 
			ENDCG 
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=15800
419;432;2480;850;1815.863;306.9322;1.434836;True;True
Node;AmplifyShaderEditor.TemplateShaderPropertyNode;1;-1043.413,-117;Float;False;0;0;_MainTex;Shader;0;5;SAMPLER2D;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;10;-788.3555,78.4305;Float;False;Property;_Offset;Offset;4;0;Create;True;0;0;False;0;0.18;0.095;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;9;-789.6216,158.2993;Float;False;Property;_Strenght;Strenght;9;0;Create;True;0;0;False;0;5.4;150;0;150;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;2;-418.3825,-82.31625;Float;False;NormalCreate;0;;11;e12f7ae19d416b942820e3932b56220f;0;4;1;SAMPLER2D;;False;2;FLOAT2;0,0;False;3;FLOAT;0.15;False;4;FLOAT;8;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;-85.40382,-102.6412;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0.5,0.5,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;3;126.5934,-104.2886;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0.5,0.5,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WireNode;51;-580.8455,-52.76876;Float;False;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;52;-690.318,380.3872;Float;False;Property;_GlassNormalStrenght;GlassNormalStrenght;8;0;Create;True;0;0;False;0;0;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;7;276.5648,-104.8613;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.WireNode;50;-471.6397,261.849;Float;False;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.DynamicAppendNode;13;568.9272,-105.144;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;15;-334.0092,86.3582;Float;True;Property;_TextureSample4;Texture Sample 4;5;0;Create;True;0;0;False;0;None;77f5feff8fd410d4e85cb24c60731d33;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;5;-351.1501,358.3687;Float;True;Property;_TextureSample3;Texture Sample 3;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;37.74178,56.76664;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0.5,0.5,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StepOpNode;11;221.2496,307.7134;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;45;772.2551,-54.37662;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;39;224.2841,111.2044;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0.5,0.5,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WireNode;49;951.7317,310.8333;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;46;901.9846,121.441;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;12;1052.019,119.9653;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0.5,0.5,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WireNode;48;1121.377,358.6;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;47;1117.133,332.6949;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;43;759.7766,196.881;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0.5,0.5,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DynamicAppendNode;6;1286.529,176.2277;Float;False;FLOAT4;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;65;-763.9579,-26.39377;Float;False;Property;_offsetY;offsetY;6;0;Create;True;0;0;False;0;0.001;0.001;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendNormalsNode;53;752.6499,506.5289;Float;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;518.757,189.1286;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0.5;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;54;-1003.781,285.4492;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;55;-662.3345,271.1867;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.1,0.05;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;8;119.9253,-213.0112;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;73;-431.7509,-319.6649;Float;False;NormalCreateHQ;2;;17;aed274115fb9251439a4539d2672ee60;0;6;32;FLOAT;0;False;31;FLOAT;0;False;1;SAMPLER2D;;False;2;FLOAT2;0,0;False;3;FLOAT;0.5;False;4;FLOAT;2;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;68;-786.1968,-206.351;Float;False;Property;_offsetX;offsetX;7;0;Create;True;0;0;False;0;0.001;0.001;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;1497.367,178.5694;Float;False;True;2;Float;ASEMaterialInspector;0;2;CSDepthToNormalEffect;c71b220b631b6344493ea3cf87110c93;0;0;SubShader 0 Pass 0;1;False;False;True;2;False;-1;False;False;True;0;False;-1;True;7;False;-1;False;True;0;False;0;False;False;False;False;False;False;False;False;False;True;2;0;;0;0;Standard;0;1;0;FLOAT4;0,0,0,0;False;0
WireConnection;2;1;1;0
WireConnection;2;3;10;0
WireConnection;2;4;9;0
WireConnection;4;0;2;0
WireConnection;3;0;4;0
WireConnection;51;0;1;0
WireConnection;7;0;3;0
WireConnection;50;0;51;0
WireConnection;13;0;7;0
WireConnection;13;1;7;1
WireConnection;15;5;52;0
WireConnection;5;0;50;0
WireConnection;38;0;15;0
WireConnection;11;0;5;1
WireConnection;45;0;13;0
WireConnection;39;0;38;0
WireConnection;49;0;11;0
WireConnection;46;0;45;0
WireConnection;12;0;46;0
WireConnection;12;1;39;0
WireConnection;12;2;49;0
WireConnection;48;0;5;2
WireConnection;47;0;5;1
WireConnection;43;0;44;0
WireConnection;6;0;12;0
WireConnection;6;2;47;0
WireConnection;6;3;48;0
WireConnection;44;0;39;0
WireConnection;44;1;52;0
WireConnection;54;2;1;0
WireConnection;55;0;54;0
WireConnection;8;0;4;0
WireConnection;73;32;65;0
WireConnection;73;31;68;0
WireConnection;73;1;1;0
WireConnection;73;3;10;0
WireConnection;73;4;9;0
WireConnection;0;0;6;0
ASEEND*/
//CHKSM=D0221CB74D7EBC844CF949F8BDE9B2D6C480AA68