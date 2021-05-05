Shader "Unlit/RotateGizmoShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,0,1,1)
		_MinAngle("MinAngle", Float) = 0.0
		_MaxAngle("MaxAngle", Float) = 0.0
		_Alpha("Alpha", Float) = 1.0
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float4 _Color;
			float _MinAngle;
			float _MaxAngle;
			float _Alpha;

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

				fixed4 color = fixed4(_Color.r, _Color.g, _Color.b, tex2D(_MainTex, i.uv).r);
				float2 dir = (i.uv - 0.5);

				if (dot(dir, dir) < 0.25)
				{
					while (_MaxAngle < 0.0)
						_MaxAngle += 4.0 * PI;

					float angle = atan2(-dir.y, dir.x);
					float delta = _MaxAngle - _MinAngle;
					bool invert = abs(delta) % (4.0 * PI) > 2.0 * PI;

					while (_MaxAngle >= 2.0 * PI && _MaxAngle - 2.0 * PI > _MinAngle)
						_MaxAngle -= 2.0 * PI;

					if (_MaxAngle < _MinAngle)
					{
						float temp = _MinAngle;
						_MinAngle = _MaxAngle;
						_MaxAngle = temp;
					}
					while (angle < _MinAngle)
						angle += 2.0 * PI;

					bool cond = angle >= _MinAngle && angle <= _MaxAngle;
					if ((!invert && cond) || (invert && !cond))
					{
						color = fixed4(_Color.r, _Color.g, _Color.b, _Alpha);
					}
				}

                return color;
            }
            ENDCG
        }
    }
}
