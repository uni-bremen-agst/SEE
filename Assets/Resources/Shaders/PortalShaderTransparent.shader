Shader "Custom/PortalShaderTransparent"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_Smoothness("Smoothness", Range(0, 1)) = 0.5
		_Metallic("Metallic", Range(0, 1)) = 0.0
		portalMin("Portal Left Front Corner", vector) = (-10, -10, 0, 0)
		portalMax("Portal Right Back Corner", vector) = (10, 10, 0, 0)
	}

	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "ForceNoShadowCasting" = "False" }

		CGPROGRAM

		#pragma surface main Standard alpha:blend
		#pragma target 3.0

		float2 portalMin;
		float2 portalMax;

		struct Input
		{
			float3 worldPos;
		};

		half _Smoothness;
		half _Metallic;
		fixed4 _Color;

		void main(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = _Color;
			if (IN.worldPos.x < portalMin.x || IN.worldPos.z < portalMin.y ||
				IN.worldPos.x > portalMax.x || IN.worldPos.z > portalMax.y
			)
			{
				c.a = 0.0f;
			}

			o.Albedo = c.rgb;
			o.Emission = 0.5 * c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
