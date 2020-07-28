// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/CursorShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
				
				float scale = 0.05 * length(_WorldSpaceCameraPos - unity_ObjectToWorld._m03_m13_m23);
				o.vertex = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_V, float4(unity_ObjectToWorld._m03_m13_m23, 1.0)) + float4(v.vertex.x * scale, v.vertex.y * scale, 0.0, 1.0));
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 color = fixed4(1.0, 0.25, 0.0, 0.8 * tex2D(_MainTex, i.uv).r);
                return color;
            }
            ENDCG
        }
    }
}
