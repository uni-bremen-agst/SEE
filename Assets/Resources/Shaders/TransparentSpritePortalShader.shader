// Please note:
// This shader is currently not intended for semi-transparent sprites.
// It handles full transparency and opaque elements well with depth writing (ZWrite)
// for opaque fragments; fully transparent fragments are discarded.
// To allow for blending of semitransparent areas it is necessary to implement a two-pass
// mechanism akin to the edge shader that draws translucent parts without depth writing.
// Depth writing is necessary to achieve the correct order of edges and resize handles.

Shader "Unlit/TransparentSpritePortalShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Portal ("Portal (x_min, z_min, x_max, z_max) (World Units)", Vector) = (-10, -10, 10, 10)
        _PortalFadeWidth ("Portal Edge Fade Width (World Units)", Float) = 0.01
    }
    SubShader
    {
        Tags {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "ForceNoShadowCasting" = "True"
            "PreviewType" = "Plane"
        }

        // Alpha blending mode for transparency
        Blend SrcAlpha OneMinusSrcAlpha
        // Makes the back of the sprite plane visible
        Cull Off
        // Unity's lighting will not be applied
        Lighting Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Portal;
            float _PortalFadeWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 color = tex2D(_MainTex, i.uv);

                // Discard coordinates if completely transparent
                if (color.a <= 0)
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
                color.a *= portalFade;

                return color;
            }
            ENDHLSL
        }
    }
}
