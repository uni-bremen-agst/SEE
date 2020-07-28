Shader "Unlit/3DUIShader"
{
    Properties
    {
		_Color("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
		Tags
		{
			"Queue" = "Overlay"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		ZTest[_ZTest]
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
            };

            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				float depth = (i.vertex.xyz / i.vertex.w).z;
				
                fixed4 col = _Color;
                return col;
            }
            ENDCG
        }
    }
}
