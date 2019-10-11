// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Pixelate"
{
	Properties
	{
		_MainTex ( "Screen", 2D ) = "black" {}
		_PixelzY("PixelzY", Float) = 0
		_PixelzX("PixelzX", Float) = 0
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
			#pragma target 3.5
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
			
			uniform float _PixelzX;
			uniform float _PixelzY;

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
				float4 screenPos = i.ase_texcoord4;
				float4 ase_screenPosNorm = screenPos/screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 appendResult6 = (float2(ase_screenPosNorm.x , ase_screenPosNorm.y));
				float pixelWidth1 =  1.0f / _PixelzX;
				float pixelHeight1 = 1.0f / _PixelzY;
				half2 pixelateduv1 = half2((int)(appendResult6.x / pixelWidth1) * pixelWidth1, (int)(appendResult6.y / pixelHeight1) * pixelHeight1);
				float4 tex2DNode4 = tex2D( _MainTex, pixelateduv1 );
				

				finalColor = tex2DNode4;

				return finalColor;
			} 
			ENDCG 
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=16100
464;433;1669;645;812.127;374.7899;1;True;True
Node;AmplifyShaderEditor.ScreenPosInputsNode;5;-996.3318,-194.5144;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;7;-729.127,-6.789886;Float;False;Property;_PixelzX;PixelzX;1;0;Create;True;0;0;False;0;0;394.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-748.127,90.21011;Float;False;Property;_PixelzY;PixelzY;0;0;Create;True;0;0;False;0;0;182;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;6;-639.5,-131.5;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCPixelate;1;-489.5,-83.5;Float;False;3;0;FLOAT2;0,0;False;1;FLOAT;433.3;False;2;FLOAT;207.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TemplateShaderPropertyNode;2;-467.5,-234.5;Float;False;0;0;_MainTex;Shader;0;5;SAMPLER2D;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;10;-170.127,207.2101;Float;False;Property;_Float0;Float 0;4;0;Create;True;0;0;False;0;138.5412;31;0;256;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;12;132.873,-147.7899;Float;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.PosterizeNode;9;269.873,7.210114;Float;False;1;2;1;COLOR;0,0,0,0;False;0;INT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;13;-168.127,132.2101;Float;False;Property;_Float1;Float 1;2;0;Create;True;0;0;False;0;138.5412;0.543;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;4;-284.5,-185.5;Float;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;15;-269.127,208.2101;Float;False;Property;_lift;lift;3;0;Create;True;0;0;False;0;138.5412;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;14;27.87305,-86.78989;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;462,-131;Float;False;True;2;Float;ASEMaterialInspector;0;2;Pixelate;c71b220b631b6344493ea3cf87110c93;0;0;SubShader 0 Pass 0;1;False;False;True;2;False;-1;False;False;True;2;False;-1;True;7;False;-1;False;True;0;False;0;False;False;False;False;False;False;False;False;False;True;3;0;;0;0;Standard;0;1;0;FLOAT4;0,0,0,0;False;0
WireConnection;6;0;5;1
WireConnection;6;1;5;2
WireConnection;1;0;6;0
WireConnection;1;1;7;0
WireConnection;1;2;8;0
WireConnection;12;0;14;0
WireConnection;12;1;13;0
WireConnection;9;1;12;0
WireConnection;9;0;10;0
WireConnection;4;0;2;0
WireConnection;4;1;1;0
WireConnection;14;0;4;0
WireConnection;14;1;15;0
WireConnection;3;0;4;0
ASEEND*/
//CHKSM=481F82A525BC8B63B84B70F66A10D551FEF87B2D