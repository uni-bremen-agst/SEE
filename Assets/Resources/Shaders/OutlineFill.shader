//
//  OutlineFill.shader
//  QuickOutline
//
//  Created by Chris Nolet on 2/21/18.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//

Shader "Custom/Outline Fill"
{
	Properties
	{
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0
		_OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
		_OutlineWidth("Outline Width", Range(0, 10)) = 2
		portalMin("Portal Left Front Corner", vector) = (-10, -10, 0, 0)
		portalMax("Portal Right Back Corner", vector) = (10, 10, 0, 0)
	}
	
	SubShader
	{
		Tags
		{
			"Queue" = "Transparent+1"
			"RenderType" = "Transparent"
			"DisableBatching" = "True"
		}

    Pass
	{
		Name "Fill"
		Cull Off
		ZTest [_ZTest]
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask RGB

		Stencil
		{
			Ref 1
			Comp NotEqual
		}

		CGPROGRAM
		#include "UnityCG.cginc"

		#pragma vertex vert
		#pragma fragment frag

		struct appdata
		{
			float4 vertex       : POSITION;
			float3 normal       : NORMAL;
			float3 smoothNormal : TEXCOORD3;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct v2f
		{
			float4 position : SV_POSITION;
			float3 worldPos : TEXCOORD1;
			fixed4 color    : COLOR;
			UNITY_VERTEX_OUTPUT_STEREO
		};

		uniform fixed4 _OutlineColor;
		uniform float _OutlineWidth;

		float2 portalMin;
		float2 portalMax;

		v2f vert(appdata input)
		{
			v2f output;

			UNITY_SETUP_INSTANCE_ID(input);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

			float3 normal = any(input.smoothNormal) ? input.smoothNormal : input.normal;
			float3 viewPosition = UnityObjectToViewPos(input.vertex);
			float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, normal));

			output.position = UnityViewToClipPos(viewPosition + viewNormal * -viewPosition.z * _OutlineWidth / 1000.0);
			output.worldPos = mul(unity_ObjectToWorld, float4(input.vertex.x, input.vertex.y, input.vertex.z, 1.0)).xyz;
			output.color = _OutlineColor;

			return output;
		}

		fixed4 frag(v2f input) : SV_Target
		{
			fixed4 c = input.color;
			if (input.worldPos.x < portalMin.x || input.worldPos.z < portalMin.y ||
				input.worldPos.x > portalMax.x || input.worldPos.z > portalMax.y
			)
			{
				c.a = 0.0f;
			}
			return c;
		}

		ENDCG
		}
	}
}
