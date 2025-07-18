﻿// Original Unity sprite shader, modified to work with portal.
// Short summary of modifications:
// 1. Made function parameters const.
// 2. Added _Portal property similarly to e.g. OpaquePortalShader.
// 3. Modify fragment function to discard pixels outside the portal

Shader "Custom/PortalSpriteShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Portal ("Portal (x_min, z_min, x_max, z_max) (World Units)", Vector) = (-10, -10, 10, 10)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float3 worldPos : TEXCOORD1;

            };

            fixed4 _Color;
            float4 _Portal;

            v2f vert(const appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color * _Color;
                OUT.texcoord = IN.texcoord;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex);

                return OUT;
            }

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            float _AlphaSplitEnabled;

            fixed4 SampleSpriteTexture(const float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);

                #if UNITY_TEXTURE_ALPHASPLIT_ALLOWED
                if (_AlphaSplitEnabled)
                    color.a = tex2D (_AlphaTex, uv).r;
                #endif //UNITY_TEXTURE_ALPHASPLIT_ALLOWED

                return color;
            }

            fixed4 frag(const v2f IN) : SV_Target
            {
                // Discard coordinates if outside portal
                // Note: We use a 2D portal that spans over Unity's XZ plane: (x_min, z_min, x_max, z_max)
                if (IN.worldPos.x < _Portal.x || IN.worldPos.z < _Portal.y ||
                    IN.worldPos.x > _Portal.z || IN.worldPos.z > _Portal.w)
                {
                    discard;
                }

                float4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                // Following segment taken from https://forum.unity.com/threads/sprite-tint-shader.520248/#post-3412451
                fixed a = c.a;
                if ( c.a >= 0.1 ){
                    c = c + (IN.color - c) * IN.color.a * c.a;
                    c.a = a;
                }
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
