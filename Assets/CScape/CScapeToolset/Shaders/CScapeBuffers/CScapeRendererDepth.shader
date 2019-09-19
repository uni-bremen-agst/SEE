// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CScapeToolset/CScapeRendererDepth"
{
	Properties
	{
		_ShapeTex("ShapeTex", 2D) = "white" {}
		_Pos1("Pos1", Range( 0 , 1)) = 0
		_pos2("pos2", Range( 0 , 1)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
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

			uniform sampler2D _ShapeTex;
			uniform float4 _ShapeTex_ST;
			uniform float _Pos1;
			uniform float _pos2;
			
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
				float2 uv_ShapeTex = i.ase_texcoord.xy * _ShapeTex_ST.xy + _ShapeTex_ST.zw;
				float4 tex2DNode1 = tex2D( _ShapeTex, uv_ShapeTex );
				float temp_output_3_0_g13 = ( tex2DNode1.b - _Pos1 );
				float lerpResult16 = lerp( 0.6666666 , 1.0 , saturate( ( temp_output_3_0_g13 / fwidth( temp_output_3_0_g13 ) ) ));
				float temp_output_3_0_g14 = ( tex2DNode1.b - _pos2 );
				float lerpResult20 = lerp( 0.3333333 , lerpResult16 , saturate( ( temp_output_3_0_g14 / fwidth( temp_output_3_0_g14 ) ) ));
				float4 appendResult3 = (float4(tex2DNode1.a , tex2DNode1.a , lerpResult20 , 1.0));
				
				
				finalColor = appendResult3;
				return finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=15600
450;642;1705;624;922.3969;579.0546;1.668657;True;True
Node;AmplifyShaderEditor.TexturePropertyNode;2;-662.5,-87;Float;True;Property;_ShapeTex;ShapeTex;1;0;Create;True;0;0;False;0;None;47c78fe4e0481964ea3fe9715b051558;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SamplerNode;1;-393.5,-86;Float;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;11;-669.7496,-1037.291;Float;False;Property;_Pos1;Pos1;6;0;Create;True;0;0;False;0;0;0.561;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;9;-671.9258,-860.4099;Float;False;Property;_pos2;pos2;7;0;Create;True;0;0;False;0;0;0.437;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;14;817.0331,-386.7616;Float;False;Step Antialiasing;-1;;13;2a825e80dfb3290468194f83380797bd;0;2;1;FLOAT;0;False;2;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;17;723.615,-630.0441;Float;False;Step Antialiasing;-1;;14;2a825e80dfb3290468194f83380797bd;0;2;1;FLOAT;0;False;2;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;16;1026.292,-279.4852;Float;False;3;0;FLOAT;0.6666666;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;20;1229.901,-334.2509;Float;False;3;0;FLOAT;0.3333333;False;1;FLOAT;0.5;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;12;353.3688,-407.6403;Float;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleAddOpNode;22;1341.331,-183.388;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.3333333;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;18;1200.345,-81.71898;Float;False;Step Antialiasing;-1;;17;2a825e80dfb3290468194f83380797bd;0;2;1;FLOAT;0.01;False;2;FLOAT;0.001;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;15;602.1969,-503.377;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;3;148.0321,80.09344;Float;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DynamicAppendNode;13;328.0387,-30.00392;Float;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.FunctionNode;4;-39.24939,-419.1969;Float;False;3ColorGradient;-1;;18;af7134d49ad8c844294e466f39ac2e86;0;8;15;FLOAT;0;False;23;COLOR;0,0,0,0;False;21;COLOR;0,0,0,0;False;22;COLOR;0,0,0,0;False;16;FLOAT;0;False;19;FLOAT;0;False;17;FLOAT;0;False;20;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;5;-670.236,-670.9158;Float;False;Property;_Color1;Color 1;2;0;Create;True;0;0;False;0;0,0,0,0;0.2509804,0.2509804,0.2509804,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;10;-670.0699,-955.4859;Float;False;Property;_Smooth1;Smooth1;4;0;Create;True;0;0;False;0;0;0.0016;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;7;-673.579,-503.4614;Float;False;Property;_Color0;Color 0;0;0;Create;True;0;0;False;0;0,0,0,0;1,1,1,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;6;-664.7127,-332.2909;Float;False;Property;_Color2;Color 2;3;0;Create;True;0;0;False;0;0,0,0,0;0.5019608,0.5019608,0.5019608,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;21;1494.197,-148.7843;Float;False;Simple HUE;-1;;16;32abb5f0db087604486c2db83a2e817a;0;1;1;FLOAT;0;False;4;FLOAT3;6;FLOAT;7;FLOAT;5;FLOAT;8
Node;AmplifyShaderEditor.RangedFloatNode;8;-665.9028,-770.1418;Float;False;Property;_Smooth2;Smooth2;5;0;Create;True;0;0;False;0;0;0.0263;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;19;1786.896,-45.44282;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;2431.104,61.80353;Float;False;True;2;Float;ASEMaterialInspector;0;1;CScapeToolset/CScapeRendererDepth;0770190933193b94aaa3065e307002fa;0;0;Unlit;2;True;0;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;True;0;False;-1;0;False;-1;True;0;False;-1;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;RenderType=Opaque;True;2;0;False;False;False;False;False;False;False;False;False;False;0;;0;0;Standard;0;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;0
WireConnection;1;0;2;0
WireConnection;14;1;11;0
WireConnection;14;2;1;3
WireConnection;17;1;9;0
WireConnection;17;2;1;3
WireConnection;16;2;14;0
WireConnection;20;1;16;0
WireConnection;20;2;17;0
WireConnection;12;0;4;0
WireConnection;22;0;20;0
WireConnection;18;2;1;3
WireConnection;3;0;1;4
WireConnection;3;1;1;4
WireConnection;3;2;20;0
WireConnection;4;15;1;4
WireConnection;4;23;7;0
WireConnection;4;21;5;0
WireConnection;4;22;6;0
WireConnection;4;16;11;0
WireConnection;4;19;9;0
WireConnection;4;17;10;0
WireConnection;4;20;8;0
WireConnection;21;1;22;0
WireConnection;19;1;21;6
WireConnection;19;2;18;0
WireConnection;0;0;3;0
ASEEND*/
//CHKSM=CBE9AD86A0B63AE6D58392A4A9B4350BA5D7D4E3