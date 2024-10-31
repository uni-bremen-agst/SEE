Shader "Unlit/TransparentSpritePortalShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}

        // Clipping
        _PortalMin("Portal Left Front Corner", vector) = (-10, -10, 0, 0)
        _PortalMax("Portal Right Back Corner", vector) = (10, 10, 0, 0)
    }
    SubShader
    {
        Tags {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector"="True"
            "ForceNoShadowCasting" = "True"
            "PreviewType" = "Plane"
        }

        // Alpha blending mode for transparency
        Blend SrcAlpha OneMinusSrcAlpha
        // Do not write to depth buffer to allow transparency effect
        ZWrite Off
        // Makes the back of the sprite plane visible
        Cull Off
        // Unity's lighting will not be applied
        Lighting Off

        Pass
        {
            CGPROGRAM
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

            // Clipping
            float2 _PortalMin;
            float2 _PortalMax;

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
                fixed4 col = tex2D(_MainTex, i.uv);

                // Discard coordinates if transparent or outside portal
                // Note: We use a 2D portal (x, y) that spans over a (x, z) plane in the Unity 3D space.
                if ( col.a <= 0 ||
                    i.worldPos.x < _PortalMin.x || i.worldPos.x > _PortalMax.x ||
                    i.worldPos.z < _PortalMin.y || i.worldPos.z > _PortalMax.y)
                {
                    discard;
                }

                return col;
            }
            ENDCG
        }
    }
}
