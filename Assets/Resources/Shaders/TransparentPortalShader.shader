Shader "Custom/TransparentPortalShader"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_Smoothness("Smoothness", Range(0, 1)) = 0.5
		_Metallic("Metallic", Range(0, 1)) = 0.0
		_PortalMin("Portal Left Front Corner", vector) = (-10, -10, 0, 0)
		_PortalMax("Portal Right Back Corner", vector) = (10, 10, 0, 0)
		_EmissionStrength("Emission Strength", Range(0, 5)) = 0.0
	}

	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "ForceNoShadowCasting" = "False" }

		CGPROGRAM

		#pragma surface main Standard alpha:blend
		#pragma target 3.0

		struct Input
		{
			float3 worldPos;
		};

		fixed4 _Color;
		half _Smoothness;
		half _Metallic;
		float2 _PortalMin;
		float2 _PortalMax;
		float _EmissionStrength;

		void main(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = _Color;
			if (IN.worldPos.x < _PortalMin.x || IN.worldPos.z < _PortalMin.y ||
				IN.worldPos.x > _PortalMax.x || IN.worldPos.z > _PortalMax.y
			)
			{
				c.a = 0.0f;
			}

			o.Albedo = c.rgb;
			o.Emission = 0.5 * c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Alpha = c.a;
			// see comment by bgolus at https://forum.unity.com/threads/how-to-change-hdr-colors-intensity-via-shader.531861/
			half intensityMul = pow(2.0, _EmissionStrength);
#ifndef UNITY_COLORSPACE_GAMMA
			intensityMul = pow(intensityMul, 2.2);
#endif
			o.Emission = c * intensityMul;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
