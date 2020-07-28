Shader "Unlit/CircleShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (0,1,0,0)
		_MaxAngle("MaxAngle", Float) = 0.0
    }
    SubShader
    {
		Tags
		{
			"Queue" = "Overlay"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		ZTest Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag alpha:fade

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float4 _Color;
			float _MaxAngle;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				const float PI = 3.14159274;

				float alpha = tex2D(_MainTex, i.uv).r;
				fixed4 col = fixed4(_Color.r, _Color.g, _Color.b, alpha);

				float2 uv = (i.uv - 0.5);
				float2 dir = uv - float2(0.0, 0.0);
				float angle = atan2(-dir.y, dir.x);
				angle += angle < 0.0 ? 2.0 * PI : 0.0;

				if (dot(uv, uv) < 0.25 && angle < _MaxAngle)
				{
					col = fixed4(_Color.r, _Color.g, _Color.b, 0.8); // TODO(torben): alpha as uniform?
				}

                return col;
            }
            ENDCG
        }
    }
}
