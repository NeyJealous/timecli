Shader "TimeCli/TimeCubeEnemy"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _MainTex ("Texture", 2D) = "white" {}
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR0;
                float2 patternUv : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _OutlineColor;
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
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                if (v.color.a >= 0.999)
                {
                    float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                    p.xyz *= pulse;
                }

                o.vertex = UnityObjectToClipPos(p);
                o.color = v.color.a <= 0.001
                    ? fixed4(_OutlineColor.rgb, 0)
                    : v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (i.color.a <= 0.001)
                    return fixed4(i.color.rgb, 1);

                if (i.color.a < 0.999)
                    return i.color;

                // Clean replacement for the obsolete TimeCube texture/shader
                // program. WeaponCube uses the same shader in 1.4.5 and differs
                // by body color + texture; this procedural pattern keeps that
                // shared-shader behavior without publishing the old texture.
                float stripe = 0.5 + 0.5 * sin(
                    (i.patternUv.x + i.patternUv.y) * _StripeScale +
                    _Time.y * 4.0);

                fixed4 texel = tex2D(_MainTex, i.uv);
                fixed mask = max(max(texel.r, texel.g), max(texel.b, texel.a));

                fixed3 rgb = lerp(
                    i.color.rgb,
                    fixed3(1,1,1),
                    saturate(stripe * 0.25 + mask * 0.75));

                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }
}
