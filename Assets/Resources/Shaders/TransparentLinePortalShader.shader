Shader "Custom/TransparentLinePortalShader"
{
    Properties
    {
        // Texture
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0

        // Clip Ends
        _VisibleStart ("Start of Visible Segment", Range(0, 1)) = 0.0
        _VisibleEnd ("End of Visible Segment", Range(0, 1)) = 1.0

        // Portal
        _Portal ("Portal (x_min, z_min, x_max, z_max) (World Units)", Vector) = (-10, -10, 10, 10)
        _PortalFadeWidth ("Portal Edge Fade Width (World Units)", Float) = 0.01
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+1"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest On
        Blend One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex    : SV_POSITION;
                fixed4 color     : COLOR;
                float2 uv  : TEXCOORD0;
                float3 worldPos  : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }

            // Texture
            sampler2D _MainTex;
            sampler2D _AlphaTex;

            // Clip Ends
            float _VisibleStart;
            float _VisibleEnd;

            // Portal
            float4 _Portal;
            float _PortalFadeWidth;

            fixed4 SampleSpriteTexture (float2 uv)
            {
                fixed4 color = tex2D (_MainTex, uv);

#if ETC1_EXTERNAL_ALPHA
                // get the color from an external texture (usecase: Alpha support for ETC1 on android)
                color.a = tex2D (_AlphaTex, uv).r;
#endif //ETC1_EXTERNAL_ALPHA

                return color;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(IN.uv) * IN.color;

                // Discard coordinates if completely transparent
                if (color.a <= 0)
                {
                    discard;
                }

                // Clip Ends
                // Unity maps the UVs of a LineRenderer specifically to act as a relative coordinate system:
                //
                // UV.x (Horizontal): This is the distance along the line.
                // 0 is the start (first vertex), and 1 is the end (last vertex).
                //
                // UV.y (Vertical): This is the distance across the width.
                // 0 is one edge, 0.5 is the center, and 1 is the opposite edge.
                if (IN.uv.x < _VisibleStart || IN.uv.x > _VisibleEnd || _VisibleStart >= _VisibleEnd)
                {
                    discard;
                }

                // Portal: Calculate overhang in each direction
                // Note: We use a 2D portal that spans over Unity's XZ plane: (x_min, z_min, x_max, z_max)
                float overhangLeft   = max(_Portal.x - IN.worldPos.x, 0.0);
                float overhangBottom = max(_Portal.y - IN.worldPos.z, 0.0);
                float overhangRight  = max(IN.worldPos.x - _Portal.z, 0.0);
                float overhangTop    = max(IN.worldPos.z - _Portal.w, 0.0);

                // Discard coordinates if outside portal
                if (overhangLeft > _PortalFadeWidth || overhangRight > _PortalFadeWidth ||
                    overhangBottom > _PortalFadeWidth || overhangTop > _PortalFadeWidth)
                {
                    discard;
                }

                // Fade effect
                float fade = saturate(1.0 - (overhangLeft + overhangRight + overhangBottom+ overhangTop) / _PortalFadeWidth);
                color.a *= fade;
                color.rgb *= color.a;

                return color;
            }
            ENDHLSL
        }
    }
}
