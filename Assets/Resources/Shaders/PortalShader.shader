Shader "Custom/PortalShader"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Cutoff("Cutoff", Range(0,1)) = 0.5
	}
		SubShader
	{
		Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "ForceNoShadowCasting" = "True" }
		LOD 200

		CGPROGRAM

		#pragma surface surf Standard fullforwardshadows alphatest:_Cutoff addshadow
		#pragma target 3.0

		uniform float2 portalMin;
		uniform float2 portalMax;

        struct Input
        {
			float3 worldPos;
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = _Color;
			if (IN.worldPos.x < portalMin.x || IN.worldPos.z < portalMin.y ||
				IN.worldPos.x > portalMax.x || IN.worldPos.z > portalMax.y
			)
			{
				c = _Color.a = 0.0f;
			}
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
