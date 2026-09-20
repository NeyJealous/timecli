Shader "TimeCli/WeaponDiffuseColor"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB) Illumin (A)", 2D) = "white" {}
        _CubeMap ("CubeMap (RGB)", CUBE) = "white" {}
    }

    SubShader
    {
        LOD 200
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            samplerCUBE _CubeMap;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // Canonical Unity 5.4 GLES program multiplies the local normal
                // by unity_ObjectToWorld's upper-left 3x3 directly. It does
                // not normalize and does not compute a reflection vector.
                o.worldNormal =
                    mul((float3x3)unity_ObjectToWorld, v.normal);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texcol =
                    tex2D(_MainTex, i.uv);
                fixed4 cubeCol =
                    texCUBE(_CubeMap, i.worldNormal);

                fixed4 baseColor;
                baseColor.rgb =
                    texcol.rgb * cubeCol.rgb;
                baseColor.a =
                    texcol.a;

                // Exact clean-room translation of the canonical 5.4 GLES
                // fragment program:
                //   base = mix(base, base * _Color.aaaa, texcol.aaaa)
                //   output = base + texcol.a * _Color
                baseColor =
                    lerp(
                        baseColor,
                        baseColor * _Color.aaaa,
                        texcol.aaaa);

                return
                    baseColor +
                    texcol.a * _Color;
            }
            ENDCG
        }
    }

    Fallback Off
}
