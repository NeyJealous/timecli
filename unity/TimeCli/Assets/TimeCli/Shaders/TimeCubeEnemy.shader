Shader "TimeCli/TimeCubeEnemy"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _PulseAmount ("Pulse Amount", Range(0,0.25)) = 0.08
        _PulseSpeed ("Pulse Speed", Float) = 3.0
        _StripeScale ("Stripe Scale", Float) = 8.0
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
                float2 patternUv : TEXCOORD0;
            };

            fixed4 _Color;
            float _PulseAmount;
            float _PulseSpeed;
            float _StripeScale;

            v2f vert(appdata v)
            {
                v2f o;
                float4 p = v.vertex;

                // The recovered GLES program derives the TimeCube coordinates
                // from vertex Z/Y and applies a sinusoidal body animation.
                o.patternUv = p.zy * 1.1 + 0.5;

                if (v.color.a >= 0.999)
                {
                    float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                    p.xyz *= pulse;
                }

                o.vertex = UnityObjectToClipPos(p);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (i.color.a < 0.999)
                    return i.color;

                // Clean replacement for the obsolete TimeCube texture/shader
                // program. WeaponCube uses the same shader in 1.4.5 and differs
                // by body color + texture; this procedural pattern keeps that
                // shared-shader behavior without publishing the old texture.
                float stripe = 0.5 + 0.5 * sin(
                    (i.patternUv.x + i.patternUv.y) * _StripeScale +
                    _Time.y * 4.0);

                fixed3 rgb = lerp(i.color.rgb, fixed3(1,1,1), stripe * 0.45);
                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }
}
