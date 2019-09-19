// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "4GradientShader"
{
	Properties
	{
		_Color0("Color 0", Color) = (0,0,0,0)
		_Color1("Color 1", Color) = (0,0,0,0)
		_Color2("Color 2", Color) = (0,0,0,0)
		_Grad1Smooth("Grad1Smooth", Range( 0 , 0.1)) = 0
		_Grad2Smooth("Grad2Smooth", Range( 0 , 0.1)) = 0
		_Grad1Pos("Grad1Pos", Range( 0 , 1)) = 0
		_Grad2Pos("Grad2Pos", Range( 0 , 1)) = 0
	}
	
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		ZWrite On
		ZTest LEqual
		Offset 0 , 0
		ColorMask RGBA
		Blend Off
		Cull Off
		

		Pass
		{
			CGPROGRAM
			#pragma target 3.0 
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
			uniform float _Grad1Pos;
			uniform float _Grad1Smooth;
			uniform float _Grad2Pos;
			uniform float _Grad2Smooth;
			
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
				float temp_output_16_0_g12 = _Grad1Pos;
				float2 uv1 = i.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float smoothstepResult7_g12 = smoothstep( temp_output_16_0_g12 , ( temp_output_16_0_g12 + _Grad1Smooth ) , uv1.x);
				float4 lerpResult11_g12 = lerp( _Color2 , _Color0 , smoothstepResult7_g12);
				float temp_output_19_0_g12 = _Grad2Pos;
				float smoothstepResult8_g12 = smoothstep( temp_output_19_0_g12 , ( temp_output_19_0_g12 + _Grad2Smooth ) , uv1.x);
				float4 lerpResult9_g12 = lerp( _Color1 , lerpResult11_g12 , smoothstepResult8_g12);
				
				
				finalColor = lerpResult9_g12;
				return finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15308
692;550;1374;798;2957.1;557.7596;3.294472;True;True
Node;AmplifyShaderEditor.ColorNode;9;-784.7238,687.4442;Float;False;Property;_Color0;Color 0;0;0;Create;True;0;0;False;0;0,0,0,0;1,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;4;-820.4051,146.9615;Float;False;Property;_Grad1Pos;Grad1Pos;5;0;Create;True;0;0;False;0;0;0.954;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;10;-1003.595,819.4572;Float;False;Property;_Color1;Color 1;1;0;Create;True;0;0;False;0;0,0,0,0;0,1,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;12;-761.3326,890.4086;Float;False;Property;_Color2;Color 2;2;0;Create;True;0;0;False;0;0,0,0,0;0,0,1,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;15;-829.4777,556.2249;Float;False;Property;_Grad2Smooth;Grad2Smooth;4;0;Create;True;0;0;False;0;0;0.0087;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-836.2289,244.2701;Float;False;Property;_Grad1Smooth;Grad1Smooth;3;0;Create;True;0;0;False;0;0;0.0027;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;1;-1410.346,348.362;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;14;-813.6539,458.9163;Float;False;Property;_Grad2Pos;Grad2Pos;6;0;Create;True;0;0;False;0;0;0.564;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;28;-295.2081,682.8552;Float;False;3ColorGradient;-1;;12;af7134d49ad8c844294e466f39ac2e86;0;8;15;FLOAT;0;False;23;COLOR;0,0,0,0;False;21;COLOR;0,0,0,0;False;22;COLOR;0,0,0,0;False;16;FLOAT;0;False;19;FLOAT;0;False;17;FLOAT;0;False;20;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;27;-1059.649,1178.401;Float;False;DecodeMeshInformation;-1;;11;dd70cb26047247348ae4c75a354e137b;0;1;6;FLOAT4;0,0,0,0;False;16;FLOAT;37;FLOAT;38;FLOAT;39;FLOAT;40;FLOAT;0;FLOAT;72;FLOAT;73;FLOAT;74;FLOAT;112;FLOAT;113;FLOAT;114;FLOAT;115;FLOAT;116;FLOAT;155;FLOAT3;156;FLOAT;157
Node;AmplifyShaderEditor.RelayNode;2;-1059.197,373.0016;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;267.6963,681.6206;Float;False;True;2;Float;ASEMaterialInspector;0;1;4GradientShader;0770190933193b94aaa3065e307002fa;0;0;SubShader 0 Pass 0;2;True;0;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;True;2;False;-1;True;True;True;True;True;0;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;RenderType=Opaque;False;0;0;0;False;False;False;False;False;False;False;False;False;True;2;0;0;0;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;0
WireConnection;28;15;1;1
WireConnection;28;23;9;0
WireConnection;28;21;10;0
WireConnection;28;22;12;0
WireConnection;28;16;4;0
WireConnection;28;19;14;0
WireConnection;28;17;5;0
WireConnection;28;20;15;0
WireConnection;2;0;1;1
WireConnection;0;0;28;0
ASEEND*/
//CHKSM=85B1ED4AC3F20BDFE129C882665EA70ABB0E830A