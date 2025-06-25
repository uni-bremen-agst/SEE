Shader "Custom/OpaqueMetallicPortalShader"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_Smoothness("Smoothness", Range(0, 1)) = 0.5
		_Metallic("Metallic", Range(0, 1)) = 0.0
		_EmissionStrength("Emission Strength", Range(0, 5)) = 0.0
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2 // 0: Off, 1: Front, 2: Back
		_Portal ("Portal (x_min, z_min, x_max, z_max) (World Units)", Vector) = (-10, -10, 10, 10)
	}

	SubShader
	{
		Tags {
			// "Queue" = "Transparent"
			// "RenderType" = "Transparent"
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
			"ForceNoShadowCasting" = "False"
			"IgnoreProjector" = "True"
		}

		Cull [_Cull]

		CGPROGRAM

		#pragma surface surf Standard keepalpha addshadow fullforwardshadows exclude_path:deferred
		#pragma target 3.0

		struct Input
		{
			float3 worldPos;
		};

		fixed4 _Color;
		half _Smoothness;
		half _Metallic;
		float _EmissionStrength;
		float4 _Portal;

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			// Discard coordinates if outside portal
			// Note: We use a 2D portal that spans over Unity's XZ plane: (x_min, z_min, x_max, z_max)
			if (IN.worldPos.x < _Portal.x || IN.worldPos.z < _Portal.y ||
				IN.worldPos.x > _Portal.z || IN.worldPos.z > _Portal.w)
			{
					discard;
			}

			o.Albedo = _Color.rgb;
			o.Emission = _EmissionStrength.xxx * 0.3 * _Color.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
		}
		ENDCG
	}
}
