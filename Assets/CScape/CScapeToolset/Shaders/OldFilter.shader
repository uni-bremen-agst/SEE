// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CScapeToolset/OldFilter"
{
	Properties
	{
		_TextureSample0("Texture Sample 0", 2D) = "black" {}
		_Float0("Float 0", Range( 0 , 0.3)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		GrabPass{ "_GrabScreen0" }
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Unlit alpha:fade keepalpha addshadow fullforwardshadows exclude_path:deferred 
		struct Input
		{
			float4 screenPos;
			float2 uv_texcoord;
		};

		uniform sampler2D _GrabScreen0;
		uniform sampler2D _TextureSample0;
		uniform float4 _TextureSample0_ST;
		uniform float _Float0;


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


		float4 CalculateContrast( float contrastValue, float4 colorTarget )
		{
			float t = 0.5 * ( 1.0 - contrastValue );
			return mul( float4x4( contrastValue,0,0,t, 0,contrastValue,0,t, 0,0,contrastValue,t, 0,0,0,1 ), colorTarget );
		}

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ase_screenPos );
			float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
			float2 uv_TextureSample0 = i.uv_texcoord * _TextureSample0_ST.xy + _TextureSample0_ST.zw;
			float4 tex2DNode11 = tex2D( _TextureSample0, uv_TextureSample0 );
			float4 screenColor2 = tex2D( _GrabScreen0, ( ase_grabScreenPosNorm + ( tex2DNode11 * _Float0 ) ).xy );
			o.Emission = CalculateContrast(2.0,( screenColor2 - ( tex2DNode11 * float4( 0.1544118,0.1544118,0.1544118,0 ) ) )).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15308
354;173;1374;537;1294.489;528.1437;1.372741;True;True
Node;AmplifyShaderEditor.SamplerNode;11;-836.9089,-618.3676;Float;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;False;0;None;28c7aad1372ff114b90d330f8a2dd938;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;18;-834.4769,-386.6919;Float;False;Property;_Float0;Float 0;2;0;Create;True;0;0;False;0;0;0.009;0;0.3;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-482.541,-441.0243;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0.007352948;False;1;COLOR;0
Node;AmplifyShaderEditor.GrabScreenPosition;7;-777.0991,-287.7346;Float;False;0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;16;-425.9605,-260.9202;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;-227.4825,-358.7278;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0.1544118,0.1544118,0.1544118,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ScreenColorNode;2;-217.6288,-176.2646;Float;False;Global;_GrabScreen0;Grab Screen 0;0;0;Create;True;0;0;False;0;Object;-1;True;False;1;0;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;22;133.3542,-100.5073;Float;False;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TexturePropertyNode;10;-1044.69,-557.2147;Float;True;Property;_Texture0;Texture 0;0;0;Create;True;0;0;False;0;None;None;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SimpleAddOpNode;19;60.89246,-206.6599;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleContrastOpNode;21;317.8986,-96.00611;Float;False;2;1;COLOR;0,0,0,0;False;0;FLOAT;2;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleContrastOpNode;8;216.9119,100.0696;Float;False;2;1;COLOR;0,0,0,0;False;0;FLOAT;1.16;False;1;COLOR;0
Node;AmplifyShaderEditor.ComputeScreenPosHlpNode;6;-790.5121,-3.812453;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;137.6379,-332.5308;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0.4926471,0.4926471,0.4926471,0;False;1;COLOR;0
Node;AmplifyShaderEditor.BlendOpsNode;9;332.8481,-221.9193;Float;False;ColorDodge;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CameraWorldClipPlanes;4;-756.5317,98.76671;Float;False;Left;0;1;FLOAT4;0
Node;AmplifyShaderEditor.BlendOpsNode;5;-758.2113,180.2459;Float;False;ColorBurn;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;13;884.7623,-122.2953;Float;False;True;2;Float;ASEMaterialInspector;0;0;Unlit;CScapeToolset/OldFilter;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;ForwardOnly;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;-1;False;-1;-1;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;0;0;False;0;0;0;False;-1;-1;0;False;-1;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;17;0;11;0
WireConnection;17;1;18;0
WireConnection;16;0;7;0
WireConnection;16;1;17;0
WireConnection;14;0;11;0
WireConnection;2;0;16;0
WireConnection;22;0;2;0
WireConnection;22;1;14;0
WireConnection;19;0;2;0
WireConnection;21;1;22;0
WireConnection;8;1;2;0
WireConnection;15;0;11;0
WireConnection;9;0;15;0
WireConnection;13;2;21;0
ASEEND*/
//CHKSM=F1B0C5D9FE79EB6C170A453EC608C9E14AE271BC