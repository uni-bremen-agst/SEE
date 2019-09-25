// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CScapeToolset/Window"
{
	Properties
	{
		_TextureSample0("Texture Sample 0", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		GrabPass{ }
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Unlit alpha:fade keepalpha addshadow fullforwardshadows exclude_path:deferred 
		struct Input
		{
			float2 uv_texcoord;
			float4 screenPos;
		};

		uniform sampler2D _TextureSample0;
		uniform float4 _TextureSample0_ST;
		uniform sampler2D _GrabTexture;


		inline float4 ASE_ComputeGrabScreenPos( float4 pos )
		{
			#if UNITY_UV_STARTS_AT_TOP
			float scale = -1.0;
			#else
			float scale = 1.0;
			#endif
			float4 o = pos;
			o.y = pos.w * 0.5f;
			o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
			return o;
		}


		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_TextureSample0 = i.uv_texcoord * _TextureSample0_ST.xy + _TextureSample0_ST.zw;
			float4 tex2DNode11 = tex2D( _TextureSample0, uv_TextureSample0 );
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ase_screenPos );
			float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
			float4 screenColor2 = tex2D( _GrabTexture, ase_grabScreenPosNorm.xy );
			float4 blendOpSrc9 = ( tex2DNode11 * float4( 0.1176471,0.1176471,0.1176471,0 ) );
			float4 blendOpDest9 = ( screenColor2 * float4( 0.6470588,0.6470588,0.6470588,0 ) );
			float ifLocalVar16 = 0;
			if( tex2DNode11.r == 0.0 )
				ifLocalVar16 = 0.0;
			else
				ifLocalVar16 = 1.0;
			float4 lerpResult17 = lerp( float4( 0,0,0,0 ) , ( saturate( ( 1.0 - ( 1.0 - blendOpSrc9 ) * ( 1.0 - blendOpDest9 ) ) )) , ifLocalVar16);
			o.Emission = lerpResult17.rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15308
549;361;1374;537;927.1505;581.2897;1.945321;True;True
Node;AmplifyShaderEditor.GrabScreenPosition;7;-488.9319,-196.0452;Float;False;0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;11;-203.1067,-378.2321;Float;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;False;0;None;3ff5e5ee87933b748be807ec47d9d432;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenColorNode;2;-191.4317,-127.1452;Float;False;Global;_GrabScreen0;Grab Screen 0;0;0;Create;True;0;0;False;0;Object;-1;False;False;1;0;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;19;180.7469,150.4754;Float;False;Constant;_1;1;2;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;159.0365,-330.5855;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0.1176471,0.1176471,0.1176471,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;75.42007,-106.582;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0.6470588,0.6470588,0.6470588,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;18;185.6588,268.362;Float;False;Constant;_Float0;Float 0;2;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;9;377.5905,-216.0833;Float;False;Screen;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ConditionalIfNode;16;365.95,35.86408;Float;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;10;-439.4681,-418.9183;Float;True;Property;_Texture0;Texture 0;0;0;Create;True;0;0;False;0;None;None;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.LerpOp;17;735.4129,-62.23487;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CameraWorldClipPlanes;4;-537.132,93.85478;Float;False;Left;0;1;FLOAT4;0
Node;AmplifyShaderEditor.BlendOpsNode;5;-258.8319,101.6549;Float;False;ColorBurn;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleContrastOpNode;8;8.145585,130.9352;Float;False;2;1;COLOR;0,0,0,0;False;0;FLOAT;1.16;False;1;COLOR;0
Node;AmplifyShaderEditor.ComputeScreenPosHlpNode;6;-620.2316,-154.4451;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;13;1051.735,-100.8672;Float;False;True;2;Float;ASEMaterialInspector;0;0;Unlit;CScapeToolset/Window;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;ForwardOnly;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;-1;False;-1;-1;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;0;0;False;0;0;0;False;-1;-1;0;False;-1;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;2;0;7;0
WireConnection;15;0;11;0
WireConnection;14;0;2;0
WireConnection;9;0;15;0
WireConnection;9;1;14;0
WireConnection;16;0;11;1
WireConnection;16;2;19;0
WireConnection;16;3;18;0
WireConnection;16;4;19;0
WireConnection;17;1;9;0
WireConnection;17;2;16;0
WireConnection;8;1;2;0
WireConnection;13;2;17;0
ASEEND*/
//CHKSM=07DB2F5DCEDCC7DFDA7B7E77BE8160C139EC9420