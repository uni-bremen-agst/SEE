Shader "Unlit/SEE/TransparentEdgePortalShader"
{
    Properties
    {
        // Color
        _Color("(Start) Color", color) = (1,0,0,1)
        _EndColor("End Color", color) = (0,0,1,1)
        _ColorGradientEnabled("Enable Color Gradient?", Range(0, 1)) = 0 // 0 = false, 1 = true

        // Data Flow
        _EdgeFlowEnabled("Enable Data Flow Visualization?", Range(0, 1)) = 0 // 0 = false, 1 = true
        _AnimationFactor("Animation Speed Factor", Range(0, 3)) = 0.4
        _AnimationPause("Pause Between Animations", Range(0, 3)) = 0.4
        _EffectWidth("Effect Width", Range(0, 1.0)) = 0.03
        _GrowthAmount("Growth Amount", Range(0, 0.04)) = 0.005

        // Clipping
        _Portal("Portal", vector) = (-10, -10, 10, 10)
    }
    SubShader
    {
        Tags {
            "Queue" = "Transparent+1"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "ForceNoShadowCasting" = "True"
            "PreviewType" = "Plane"
        }

        // Alpha blending mode for transparency
        Blend SrcAlpha OneMinusSrcAlpha
        // Makes the inside visible at clipping planes
        Cull Off
        // Unity's lighting will not be applied
        Lighting Off

        // Shared code for both passes
        HLSLINCLUDE

        #include "UnityCG.cginc"

        struct appdata
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            float3 worldPos : TEXCOORD0;
            float1 doDiscard : TEXCOORD1;
            float4 color : TEXCOORD2;
        };

        // Color
        fixed4 _Color;
        fixed4 _EndColor;
        float _ColorGradientEnabled;
        float _AlphaThreshold;

        // Data Flow
        float _EdgeFlowEnabled;
        float _AnimationFactor;
        float _AnimationPause;
        float _EffectWidth;
        float _GrowthAmount;

        // Clipping
        float4 _Portal;

        v2f SharedVertexManipulation(appdata v)
        {
            v2f o;

            if (_EdgeFlowEnabled > 0.5)
            {
                // The effect is supposed to move automatically based on the time and the animation factor.
                // The position is calculated based on the assumption that the object has a uniform UV mapping (0.0 to 1.0 along the y axis).
                // We stretch the effect scale by the effect width so that the effect fades in and out smoothly at both ends, respectively.
                // Additionally, the effect scale is stretched to add a pause between the animations.
                float effectPosition = frac(_Time.y * _AnimationFactor) * (1.0 + 2 * _EffectWidth + _AnimationPause) - _EffectWidth;

                // Distance between the vertex and the effect position on the y axis in world-space
                float distance = abs(v.uv.y - effectPosition);

                if (distance < _EffectWidth)
                {
                    // The effect strength is based on the distance to the effect position
                    float effectFactor = 1.0 - pow(distance / _EffectWidth, 3);
                    effectFactor = clamp(effectFactor, 0.0, 1.0);
                    // We use the direction of the normal to grow outward
                    float3 outwardDir = normalize(v.normal);
                    v.vertex.xyz += outwardDir * effectFactor * _GrowthAmount;
                }
            }

            o.vertex = UnityObjectToClipPos(v.vertex);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex);

            // Note: The following is not strictly vertex related but we do this in the shared function
            // to prevent clutter and code duplication.

            // Flag coordinates as discardable outside portal
            // Note: We use a 2D portal that spans over Unity's XZ plane: (x_min, z_min, x_max, z_max)
            if (o.worldPos.x < _Portal.x || o.worldPos.z < _Portal.y ||
                o.worldPos.x > _Portal.z || o.worldPos.z > _Portal.w)
            {
                o.doDiscard = 1;
            }
            else
            {
                o.doDiscard = 0;
            }

            o.color = _ColorGradientEnabled > 0.5 ? lerp(_Color, _EndColor, v.uv.y) : _Color;;

            return o;
        }
        ENDHLSL

        // Pass 1: Render opaque fragments with depth writing
        Pass
        {
            Name "OpaquePass"
            // Write to depth buffer to make opaque fragments occlude other objects.
            // All other fragments will be discarded in this step.
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            v2f vert (appdata v)
            {
                return SharedVertexManipulation(v);
            }

            fixed4 frag (v2f i) : SV_Target
            {
               // Discard fragment if transparent or flagged earlier
               if (i.doDiscard || i.color.a < 1.0)
                {
                    discard;
                }

                return i.color;
            }
            ENDHLSL
        }

        // Pass 2: Render semitransparent fragments without depth writing
        Pass
        {
            Name "TransparentPass"
            // Do not write to depth buffer to allow transparency effect
            // Note: We will be able to see parts of the edge through other parts of the same edge, that should be occluded
            //       on full opacity. This is not a desired effect but not a big issue either.
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            v2f vert (appdata v)
            {
                return SharedVertexManipulation(v);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Discard fragment if transparent or flagged earlier
                if (i.doDiscard || i.color.a <= 0)
                {
                    discard;
                }

                return i.color;
            }
            ENDHLSL
        }
    }
}
