// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "BlurHiQ"
{
	Properties
	{
		_MainTex ( "Screen", 2D ) = "black" {}
		_Int0("Int 0", Int) = 80
		_Float0("Float 0", Range( 0 , 0.1)) = 0.01
		_Focus("Focus", Float) = 0
		_Exposure("Exposure", Range( 0 , 3)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
	}

	SubShader
	{
		
		
		ZTest Always
		Cull Off
		ZWrite Off

		
		Pass
		{ 
			CGPROGRAM 

			#pragma vertex vert_img_custom 
			#pragma fragment frag
			#pragma target 4.5
			#include "UnityCG.cginc"
			#include "UnityCG.cginc"


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
				float4 ase_texcoord4 : TEXCOORD4;
			};

			uniform sampler2D _MainTex;
			uniform half4 _MainTex_TexelSize;
			uniform half4 _MainTex_ST;
			
			uniform int _Int0;
			uniform float _Float0;
			uniform sampler2D _CameraDepthTexture;
			uniform float _Focus;
			uniform sampler2D sampler42;
			uniform float _Exposure;
			float4 Blur2( sampler2D tex , float2 uv_Texture0 , int Iterations , float OffsetX , sampler2D noise , out float4 finalColor )
			{
				for(int i=1; i<Iterations; i++)
				{
				finalColor = tex2D( tex, uv_Texture0 + float2(0  , OffsetX * i  ) - float2(0, Iterations * OffsetX / 2)) * i/Iterations * 10+ finalColor;
				}
				for(int i=0; i<Iterations; i++)
				{
				finalColor = tex2D( tex, uv_Texture0 + float2(OffsetX * i  , 0 ) - float2(Iterations * OffsetX /2, 0)) * i/Iterations * 10+ finalColor;
				}
				for(int i=0; i<Iterations; i++)
				{
				finalColor = tex2D( tex, uv_Texture0 + float2(OffsetX/1.4 * i   , OffsetX/1.4 * i) - float2(Iterations * OffsetX /2/1.4, Iterations * OffsetX /2/1.4))  * i/Iterations * 10 + finalColor;
				}
				for(int i=0; i<Iterations; i++)
				{
				finalColor = tex2D( tex, uv_Texture0 + float2(OffsetX/1.4 * -i  , OffsetX/1.4 * i  ) - float2(Iterations * OffsetX /-2/1.4, Iterations * OffsetX/2/1.4))  * i/Iterations * 10+ finalColor;
				}
							return finalColor;
			}
			

			v2f_img_custom vert_img_custom ( appdata_img_custom v  )
			{
				v2f_img_custom o;
				float4 ase_clipPos = UnityObjectToClipPos(v.vertex);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord4 = screenPos;
				
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
				sampler2D tex2 = _MainTex;
				float2 uv_MainTex = i.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float2 uv_Texture02 = uv_MainTex;
				int Iterations2 = _Int0;
				float4 screenPos = i.ase_texcoord4;
				float eyeDepth52 = LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture,UNITY_PROJ_COORD( screenPos ))));
				float4 tex2DNode74 = tex2D( _MainTex, uv_MainTex );
				float clampResult60 = clamp( abs( ( ( _Float0 * ( eyeDepth52 + _Focus ) * 0.0001 ) + ( ( tex2DNode74.r + tex2DNode74.g + tex2DNode74.b ) * 0.0002 ) ) ) , 0.0 , 1.0 );
				float OffsetX2 = clampResult60;
				sampler2D noise2 = sampler42;
				float4 finalColor2 = float4( 0,0,0,0 );
				float4 localBlur2 = Blur2( tex2 , uv_Texture02 , Iterations2 , OffsetX2 , noise2 , finalColor2 );
				float4 temp_output_10_0 = ( finalColor2 / ( _Int0 * 3.0 ) );
				

				finalColor = ( temp_output_10_0 * _Exposure );

				return finalColor;
			} 
			ENDCG 
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=15600
594;383;1705;624;2209.046;640.9861;2.72996;True;True
Node;AmplifyShaderEditor.TemplateShaderPropertyNode;43;-1302.281,216.6467;Float;False;0;0;_MainTex;Shader;0;5;SAMPLER2D;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;41;-1558.536,1160.582;Float;False;Property;_Focus;Focus;3;0;Create;True;0;0;False;0;0;-172.93;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenDepthNode;52;-1689.487,615.0165;Float;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;74;-1527.589,418.4182;Float;True;Property;_TextureSample0;Texture Sample 0;4;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;9;-1640.534,915.5352;Float;False;Property;_Float0;Float 0;2;0;Create;True;0;0;False;0;0.01;0.0056;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;40;-1067.727,1203.713;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;75;-1207.735,450.2655;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;42;-1168.292,960.6779;Float;False;Constant;_Float4;Float 4;7;0;Create;True;0;0;False;0;0.0001;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;76;-977.339,670.919;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.0002;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;-994.5587,811.7206;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;55;-784.0717,703.2353;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;86;-642.5819,712.2968;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;60;-429.295,692.4352;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-265.4783,230.6437;Float;False;Constant;_Float1;Float 1;3;0;Create;True;0;0;False;0;3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;6;-521.4395,269.6994;Float;False;Property;_Int0;Int 0;0;0;Create;True;0;0;False;0;80;30;0;1;INT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;48;-759.7263,389.7661;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CustomExpressionNode;2;-138.0695,436.9427;Float;False;$for(int i=1@ i<Iterations@ i++)${$finalColor = tex2D( tex, uv_Texture0 + float2(0  , OffsetX * i  ) - float2(0, Iterations * OffsetX / 2)) * i/Iterations * 10+ finalColor@$}$for(int i=0@ i<Iterations@ i++)${$finalColor = tex2D( tex, uv_Texture0 + float2(OffsetX * i  , 0 ) - float2(Iterations * OffsetX /2, 0)) * i/Iterations * 10+ finalColor@$}$$for(int i=0@ i<Iterations@ i++)${$finalColor = tex2D( tex, uv_Texture0 + float2(OffsetX/1.4 * i   , OffsetX/1.4 * i) - float2(Iterations * OffsetX /2/1.4, Iterations * OffsetX /2/1.4))  * i/Iterations * 10 + finalColor@$}$$for(int i=0@ i<Iterations@ i++)${$finalColor = tex2D( tex, uv_Texture0 + float2(OffsetX/1.4 * -i  , OffsetX/1.4 * i  ) - float2(Iterations * OffsetX /-2/1.4, Iterations * OffsetX/2/1.4))  * i/Iterations * 10+ finalColor@$}$			return finalColor@;4;False;6;True;tex;SAMPLER2D;sampler02;In;;True;uv_Texture0;FLOAT2;0,0;In;;True;Iterations;INT;2;In;;True;OffsetX;FLOAT;0;In;;True;noise;SAMPLER2D;sampler42;In;;True;finalColor;FLOAT4;0,0,0,0;Out;;Blur;True;False;0;6;0;SAMPLER2D;sampler02;False;1;FLOAT2;0,0;False;2;INT;2;False;3;FLOAT;0;False;4;SAMPLER2D;sampler42;False;5;FLOAT4;0,0,0,0;False;2;FLOAT4;0;FLOAT4;6
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;0.159314,86.68621;Float;False;2;2;0;INT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;10;432.1753,-169.9341;Float;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;78;1340.757,-414.8368;Float;False;Property;_Exposure;Exposure;4;0;Create;True;0;0;False;0;0;1.03;0;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;68;1261.444,78.27448;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;479.0518,573.1327;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;1,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;5;-650.8944,81.50063;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;89;1419.316,-126.2671;Float;True;Property;_TextureSample2;Texture Sample 2;5;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ClampOpNode;61;-1314.183,694.3351;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;200;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;69;1152.764,239.5226;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.DdyOpNode;54;-1028.848,1267.946;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;58;-701.8083,1244.51;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;63;593.0841,407.4799;Float;True;Property;_TextureSample1;Texture Sample 1;4;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DesaturateOpNode;79;1537.038,164.681;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.AbsOpNode;56;-597.0624,1110.406;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;23;-66.40401,1171.118;Float;True;Property;_Texture1;Texture 1;1;0;Create;True;0;0;False;0;None;68c46b765bdd61f47a58500a062b0b5c;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SurfaceDepthNode;51;-1837.593,423.25;Float;False;1;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenDepthNode;33;-1717.543,342.3168;Float;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DdxOpNode;53;-1021.745,1110.739;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenColorNode;18;269.8286,575.2834;Float;False;Global;_GrabScreen0;Grab Screen 0;4;0;Create;True;0;0;True;0;Instance;-1;True;False;1;0;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;90;1892.363,-102.2854;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;70;807.0122,664.9845;Float;False;Constant;_Float3;Float 3;4;0;Create;True;0;0;False;0;1.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;81;599.8878,-310.7115;Float;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleAddOpNode;80;900.5039,-199.0318;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;85;1076.223,-209.8395;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;84;1058.21,-13.49913;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;67;932.9387,275.2177;Float;False;2;0;COLOR;0,0,0,0;False;1;COLOR;0.4852941,0.4852941,0.4852941,1;False;1;COLOR;0
Node;AmplifyShaderEditor.ScreenColorNode;35;-450.2861,-114.8605;Float;False;Global;_GrabScreen1;Grab Screen 1;6;0;Create;True;0;0;False;0;Object;-1;False;False;1;0;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;45;-1001.822,451.3079;Float;False;Constant;_Float2;Float 2;5;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;88;2097.352,-237.1809;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0.3,0.3,0.3,0.3;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;77;1869.884,-302.0628;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.AbsOpNode;57;-829.5961,1185.399;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;44;2334.763,-235.8284;Float;False;True;2;Float;ASEMaterialInspector;0;2;BlurHiQ;c71b220b631b6344493ea3cf87110c93;0;0;SubShader 0 Pass 0;1;False;False;True;2;False;-1;False;False;True;2;False;-1;True;7;False;-1;False;True;0;False;0;False;False;False;False;False;False;False;False;False;True;5;0;;0;0;Standard;0;1;0;FLOAT4;0,0,0,0;False;0
WireConnection;74;0;43;0
WireConnection;40;0;52;0
WireConnection;40;1;41;0
WireConnection;75;0;74;1
WireConnection;75;1;74;2
WireConnection;75;2;74;3
WireConnection;76;0;75;0
WireConnection;34;0;9;0
WireConnection;34;1;40;0
WireConnection;34;2;42;0
WireConnection;55;0;34;0
WireConnection;55;1;76;0
WireConnection;86;0;55;0
WireConnection;60;0;86;0
WireConnection;48;2;43;0
WireConnection;2;0;43;0
WireConnection;2;1;48;0
WireConnection;2;2;6;0
WireConnection;2;3;60;0
WireConnection;11;0;6;0
WireConnection;11;1;12;0
WireConnection;10;0;2;6
WireConnection;10;1;11;0
WireConnection;68;0;69;0
WireConnection;19;0;18;0
WireConnection;89;0;43;0
WireConnection;61;0;52;0
WireConnection;69;0;63;0
WireConnection;69;1;70;0
WireConnection;54;0;52;0
WireConnection;58;0;53;0
WireConnection;58;1;54;0
WireConnection;63;0;43;0
WireConnection;79;0;10;0
WireConnection;79;1;85;0
WireConnection;56;0;58;0
WireConnection;53;0;52;0
WireConnection;90;1;89;0
WireConnection;81;0;10;0
WireConnection;80;0;81;0
WireConnection;80;1;81;1
WireConnection;80;2;81;2
WireConnection;85;0;80;0
WireConnection;84;0;80;0
WireConnection;67;0;63;0
WireConnection;88;0;10;0
WireConnection;77;0;10;0
WireConnection;77;1;78;0
WireConnection;57;0;54;0
WireConnection;44;0;77;0
ASEEND*/
//CHKSM=61753AF3E8FD23E2CC074825E54AEA734236B0A4