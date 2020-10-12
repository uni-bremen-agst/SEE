Shader "Custom/InvisibleShader"
{
	Properties
	{
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

		void main(Input IN, inout SurfaceOutputStandard o)
		{
			o.Alpha = 0.0f;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
