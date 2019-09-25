// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "TestShader"
{
	Properties
	{
		[HideInInspector] __dirty( "", Int ) = 1
		_TextureArray0("Texture Array 0", 2DArray ) = "" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#pragma target 3.5
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform UNITY_DECLARE_TEX2DARRAY( _TextureArray0 );
		uniform float4 _TextureArray0_ST;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_TextureArray0 = i.uv_texcoord * _TextureArray0_ST.xy + _TextureArray0_ST.zw;
			float4 texArray1 = UNITY_SAMPLE_TEX2DARRAY_LOD(_TextureArray0, float3(uv_TextureArray0, 0.0) , 0.0 );
			float3 temp_cast_0 = (texArray1.a).xxx;
			o.Albedo = temp_cast_0;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=11001
444;526;1470;595;986;276.5;1;True;True
Node;AmplifyShaderEditor.TextureArrayNode;1;-427,-75.5;Float;True;Property;_TextureArray0;Texture Array 0;0;0;None;0;Object;-1;MipLevel;False;4;0;FLOAT2;0,0;False;1;FLOAT;0.0;False;2;FLOAT;0.0;False;3;FLOAT;1.0;False;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;3;Float;ASEMaterialInspector;0;Standard;TestShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;0;False;0;0;Opaque;0.5;True;True;0;False;Opaque;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;False;0;255;255;0;0;0;0;False;0;4;10;25;False;0.5;True;0;Zero;Zero;0;Zero;Zero;Add;Add;0;False;0;0,0,0,0;VertexOffset;False;Cylindrical;Relative;0;;-1;-1;-1;-1;0;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0.0;False;4;FLOAT;0.0;False;5;FLOAT;0.0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0.0;False;9;FLOAT;0.0;False;10;OBJECT;0.0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;0;0;1;4
ASEEND*/
//CHKSM=E95721965F05C4A879635CC80C3860EAA4FFF65B