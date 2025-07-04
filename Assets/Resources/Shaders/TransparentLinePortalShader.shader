Shader "Custom/TransparentLinePortalShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        _Portal ("Portal (x_min, z_min, x_max, z_max) (World Units)", Vector) = (-10, -10, 10, 10)
        _PortalFade ("Portal Edge Fade (World Units)", Float) = 0.01
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
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex    : SV_POSITION;
                float3 worldPos  : TEXCOORD1;
                fixed4 color     : COLOR;
                float2 texcoord  : TEXCOORD0;
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
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            float4 _Portal;
            float _PortalFade;

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
                fixed4 color = SampleSpriteTexture(IN.texcoord) * IN.color;

                // Discard coordinates if completely transparent
                if (color.a <= 0)
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
                if (overhangLeft > _PortalFade || overhangRight > _PortalFade ||
                    overhangBottom > _PortalFade || overhangTop > _PortalFade)
                {
                    discard;
                }

                // Fade effect
                float fade = saturate(1.0 - (overhangLeft + overhangRight + overhangBottom+ overhangTop) / _PortalFade);
                color.a *= fade;
                color.rgb *= color.a;

                return color;
            }
            ENDHLSL
        }
    }
}
