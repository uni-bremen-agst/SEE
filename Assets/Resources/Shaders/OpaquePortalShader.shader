Shader "Custom/OpaquePortalShader"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _Metallic("Metallic", Range(0, 1)) = 0.0
        _Cutoff("Cutoff", Range(0, 1)) = 0.5
        _Portal ("Portal (x_min, z_min, x_max, z_max) (World Units)", Vector) = (-10, -10, 10, 10)
        _EmissionStrength("Emission Strength", Range(0, 5)) = 0.0
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "ForceNoShadowCasting" = "True" }
        LOD 200

        Cull Back

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows alphatest:_Cutoff addshadow
        #pragma target 3.0

        struct Input
        {
            float3 worldPos;
            float2 uv_MainTex;
        };

        fixed4 _Color;
        half _Smoothness;
        half _Metallic;
        float4 _Portal;
        float _EmissionStrength;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Discard coordinates if transparent or outside portal
            // Note: We use a 2D portal that spans over Unity's XZ plane: (x_min, z_min, x_max, z_max)
            if (IN.worldPos.x < _Portal.x || IN.worldPos.z < _Portal.y ||
                IN.worldPos.x > _Portal.z || IN.worldPos.z > _Portal.w)
            {
                discard;
            }
            fixed4 c = _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = c.a;
            // see comment by bgolus at https://forum.unity.com/threads/how-to-change-hdr-colors-intensity-via-shader.531861/
            half intensityMul = pow(2.0, _EmissionStrength);
#ifndef UNITY_COLORSPACE_GAMMA
            intensityMul = pow(intensityMul, 2.2);
#endif
            o.Emission = c * intensityMul;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
