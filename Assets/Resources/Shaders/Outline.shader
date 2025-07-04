﻿//
//  Outline.shader
//  QuickOutline
//
//  Created by Chris Nolet on 5/7/18.
//  Modified by Falko Galperin on 22-11-2021.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//

Shader "Custom/Outline"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMask("ZTest Mask", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestFill("ZTest Fill", Float) = 0
        _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth("Outline Width", Range(0, 10)) = 2
        _Portal("Portal", vector) = (-10, -10, 10, 10)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+100"
            "RenderType" = "Transparent"
            "DisableBatching" = "True"
        }

        Pass
        {
            Name "Mask"
            Cull Off
            ZTest [_ZTestMask]
            ZWrite Off
            ColorMask 0

            Stencil
            {
                Ref 1
                Pass Replace
            }
        }

        Pass
        {
            Name "Fill"
            Cull Off
            ZTest [_ZTestFill]
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
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 smoothNormal : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            uniform fixed4 _OutlineColor;
            uniform float _OutlineWidth;

            float4 _Portal;

            v2f vert(appdata input)
            {
                v2f output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                const float3 normal = any(input.smoothNormal) ? input.smoothNormal : input.normal;
                float3 viewPosition = UnityObjectToViewPos(input.vertex);
                const float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, normal));

                output.position = UnityViewToClipPos(
                    viewPosition + viewNormal * -viewPosition.z * _OutlineWidth / 1000.0);
                output.worldPos = mul(unity_ObjectToWorld, input.vertex);
                output.color = _OutlineColor;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                // Discard coordinates if outside portal
                // Note: We use a 2D portal that spans over Unity's XZ plane: (x_min, z_min, x_max, z_max)
                if (input.worldPos.x < _Portal.x || input.worldPos.z < _Portal.y ||
                    input.worldPos.x > _Portal.z || input.worldPos.z > _Portal.w)
                {
                    discard;
                }

                return input.color;
            }
            ENDCG
        }

        Pass
        {
            Name "Reset"
            Cull Off
            ZTest [_ZTestMask]
            ZWrite Off
            ColorMask 0

            Stencil
            {
                Pass Zero
            }
        }
    }
}
