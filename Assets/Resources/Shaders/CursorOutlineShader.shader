Shader "Unlit/CursorOutlineShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,0,1,1)
    }
    SubShader
    {
		Tags
		{
			"Queue" = "Overlay"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZTest Off
		ZWrite Off

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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
				
				float scale = 0.03 * length(_WorldSpaceCameraPos - unity_ObjectToWorld._m03_m13_m23);
				o.vertex = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_V, float4(unity_ObjectToWorld._m03_m13_m23, 1.0)) + float4(v.vertex.x * scale, v.vertex.y * scale, 0.0, 1.0));
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 color = fixed4(_Color.r, _Color.g, _Color.b, _Color.a * tex2D(_MainTex, i.uv).r);
                return color;
            }
            ENDCG
        }
    }
}
