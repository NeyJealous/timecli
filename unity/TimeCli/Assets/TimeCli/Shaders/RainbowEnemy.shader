Shader "TimeCli/RainbowEnemy"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _Speed ("Speed", Float) = 0.18
    }

    SubShader
    {
        Tags
        {
            "Queue"="Geometry"
            "IgnoreProjector"="True"
            "RenderType"="Opaque"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR0;
                float hue : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _OutlineColor;
            float _Speed;

            fixed3 HueToRgb(float h)
            {
                float3 p = abs(frac(h + float3(0.0, 2.0/3.0, 1.0/3.0)) * 6.0 - 3.0);
                return saturate(p - 1.0);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color.a <= 0.001
                    ? fixed4(_OutlineColor.rgb, 0)
                    : v.color * _Color;

                // The original RainbowEnemy vertex program uses both _Time and
                // local vertex Y. Keep that spatial/time relationship here.
                o.hue = frac(_Time.y * _Speed + v.vertex.y * 0.5);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // GeometryHealth uses alpha < 1 for the health indicator and
                // zero-alpha outline shell. The original special shaders keep
                // those sections separate from the animated body.
                if (i.color.a <= 0.001)
                    return fixed4(i.color.rgb, 1);

                if (i.color.a < 0.999)
                    return i.color;

                return fixed4(HueToRgb(i.hue), 1.0);
            }
            ENDCG
        }
    }
}
