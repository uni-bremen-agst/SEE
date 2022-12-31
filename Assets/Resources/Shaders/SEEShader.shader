// Made with Amplify Shader Editor v1.9.0.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SEEShader"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_Smoothness("Smoothness", Range( 0 , 1)) = 0.5
		_Metallic("Metallic", Range( 0 , 1)) = 0
		_EmissionStrength("EmissionStrength", Range( 0 , 5)) = 0
		_PortalMin("PortalMin", Vector) = (-10,-10,0,0)
		_PortalMax("PortalMax", Vector) = (10,10,0,0)
		_Color("Color", Color) = (0,0,0,0)
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows exclude_path:deferred 
		struct Input
		{
			float3 worldPos;
		};

		uniform float4 _Color;
		uniform float _EmissionStrength;
		uniform float _Metallic;
		uniform float _Smoothness;
		uniform float2 _PortalMin;
		uniform float2 _PortalMax;
		uniform float _Cutoff = 0.5;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Albedo = _Color.rgb;
			float3 temp_cast_1 = (_EmissionStrength).xxx;
			o.Emission = temp_cast_1;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
			float3 ase_worldPos = i.worldPos;
			float temp_output_26_0 = min( _Color.a , min( (( ase_worldPos.z >= _PortalMin.y && ase_worldPos.z <= _PortalMax.y ) ? 1.0 :  0.0 ) , (( ase_worldPos.x >= _PortalMin.x && ase_worldPos.x <= _PortalMax.x ) ? 1.0 :  0.0 ) ) );
			clip( temp_output_26_0 - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19002
2041;841;958;1057;228.1416;453.7794;1.860369;True;True
Node;AmplifyShaderEditor.WorldPosInputsNode;7;-554.4134,508.8371;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector2Node;6;-540.8467,850.1725;Inherit;False;Property;_PortalMax;PortalMax;5;0;Create;True;0;0;0;False;0;False;10,10;100,100;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;19;-535.8282,992.0525;Inherit;False;Constant;_Zero;Zero;8;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;20;-533.8282,1093.052;Inherit;False;Constant;_One;One;8;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;5;-552.9101,684.367;Inherit;False;Property;_PortalMin;PortalMin;4;0;Create;True;0;0;0;False;0;False;-10,-10;-100,-100;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TFHCCompareWithRange;18;-79.82825,657.0526;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCCompareWithRange;21;-69.82825,922.0526;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMinOpNode;22;187.4657,750.3702;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;24;288.0361,73.82045;Inherit;False;Property;_Color;Color;6;0;Create;True;0;0;0;False;0;False;0,0,0,0;1,0.04,0,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;3;-118.5,291.5;Inherit;False;Property;_Metallic;Metallic;2;0;Create;True;0;0;0;False;0;False;0;0.93;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4;-114.8105,204.346;Inherit;False;Property;_EmissionStrength;EmissionStrength;3;0;Create;True;0;0;0;False;0;False;0;0;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;2;-121.5,387.5;Inherit;True;Property;_Smoothness;Smoothness;1;0;Create;True;0;0;0;False;0;False;0.5;0.85;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMinOpNode;26;320.8713,452.5512;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;636.2697,300.781;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;SEEShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Masked;0.5;True;True;0;True;TransparentCutout;;AlphaTest;ForwardOnly;18;all;True;True;True;True;0;False;_Zero;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;1;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;18;0;7;3
WireConnection;18;1;5;2
WireConnection;18;2;6;2
WireConnection;18;3;20;0
WireConnection;18;4;19;0
WireConnection;21;0;7;1
WireConnection;21;1;5;1
WireConnection;21;2;6;1
WireConnection;21;3;20;0
WireConnection;21;4;19;0
WireConnection;22;0;18;0
WireConnection;22;1;21;0
WireConnection;26;0;24;4
WireConnection;26;1;22;0
WireConnection;0;0;24;0
WireConnection;0;2;4;0
WireConnection;0;3;3;0
WireConnection;0;4;2;0
WireConnection;0;9;26;0
WireConnection;0;10;26;0
ASEEND*/
//CHKSM=39959878DBC2AA31376BF554FDA7FF33C0DEEC3D