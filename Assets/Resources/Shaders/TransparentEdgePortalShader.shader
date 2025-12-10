Shader "Unlit/SEE/TransparentEdgePortalShader"
{
    Properties
    {
        // Color
        _Color("(Start) Color", color) = (1,0,0,1)
        _EndColor("End Color", color) = (0,0,1,1)
        [Toggle] _ColorGradientEnabled("Enable Color Gradient?", Range(0, 1)) = 0 // 0 = false, 1 = true

        // Clip Ends
        _VisibleStart ("Start of Visible Segment", Range(0, 1)) = 0.0
        _VisibleEnd ("End of Visible Segment", Range(0, 1)) = 1.0

        // Data Flow
        [Toggle] _EdgeFlowEnabled("Enable Data Flow Visualization?", Range(0, 1)) = 0 // 0 = false, 1 = true
        _AnimationFactor("Animation Speed Factor", Range(0, 3)) = 0.4
        _AnimationPause("Pause Between Animations", Range(0, 3)) = 0.4
        _EffectWidth("Effect Width", Range(0, 1.0)) = 0.03
        _GrowthAmount("Growth Amount", Range(0, 0.04)) = 0.005

        // Portal
        _Portal ("Portal (x_min, z_min, x_max, z_max) (World Units)", Vector) = (-10, -10, 10, 10)
        _PortalFadeWidth ("Portal Edge Fade Width (World Units)", Float) = 0.01

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2 // 0: Off, 1: Front, 2: Back
    }
    SubShader
    {
        Tags {
            "Queue" = "Transparent+1"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "ForceNoShadowCasting" = "True"
        }

        // Alpha blending mode for transparency
        Blend SrcAlpha OneMinusSrcAlpha
        // Makes the inside visible at clipping planes
        Cull [_Cull]
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
            float4 color : COLOR;
            float2 uv : TEXCOORD0;
            float3 worldPos : TEXCOORD1;
        };

        // Color
        fixed4 _Color;
        fixed4 _EndColor;
        float _ColorGradientEnabled;
        float _AlphaThreshold;

        // Clip Ends
        float _VisibleStart;
        float _VisibleEnd;

        // Data Flow
        float _EdgeFlowEnabled;
        float _AnimationFactor;
        float _AnimationPause;
        float _EffectWidth;
        float _GrowthAmount;

        // Portal
        float4 _Portal;
        float _PortalFadeWidth;

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
            o.uv = v.uv;
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

            // Color Gradient
            // Note: Even though the color is calculated per vertex, it is automatically linearly interpolated
            //       by the GPU for the fragment step.
            o.color = (_ColorGradientEnabled > 0.5 ? lerp(_Color, _EndColor, v.uv.y) : _Color);

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
                // Discard fragment if not completely opaque
                if (i.color.a < 1.0)
                {
                    discard;
                }

                // Clip Ends
                if (i.uv.y < _VisibleStart || i.uv.y > _VisibleEnd || _VisibleStart == _VisibleEnd)
                {
                    discard;
                }

                // Portal
                if (i.worldPos.x < _Portal.x || i.worldPos.z < _Portal.y ||
                    i.worldPos.x > _Portal.z || i.worldPos.z > _Portal.w)
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
                // Clip Ends
                if (i.uv.y < _VisibleStart || i.uv.y > _VisibleEnd || _VisibleStart == _VisibleEnd)
                {
                    discard;
                }

                // Portal: Calculate overhang in each direction
                // Note: We use a 2D portal that spans over Unity's XZ plane: (x_min, z_min, x_max, z_max)
                float overhangLeft   = max(_Portal.x - i.worldPos.x, 0.0);
                float overhangBottom = max(_Portal.y - i.worldPos.z, 0.0);
                float overhangRight  = max(i.worldPos.x - _Portal.z, 0.0);
                float overhangTop    = max(i.worldPos.z - _Portal.w, 0.0);

                // Discard coordinates if outside portal
                if (overhangLeft > _PortalFadeWidth || overhangRight > _PortalFadeWidth ||
                    overhangBottom > _PortalFadeWidth || overhangTop > _PortalFadeWidth)
                {
                    discard;
                }

                // Fade effect
                float portalFade = saturate(1.0 - (overhangLeft + overhangRight + overhangBottom + overhangTop) / _PortalFadeWidth);
                i.color *= portalFade;

                // Discard fragment if completely transparent or opaque
                if (i.color.a <= 0 || i.color.a >= 1)
                {
                    discard;
                }

                return i.color;
            }
            ENDHLSL
        }
    }
}
