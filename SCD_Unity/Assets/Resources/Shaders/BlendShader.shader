Shader "Custom/BlendShader"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _OverlayTex ("Overlay (RGB)", 2D) = "white" {}
        _OverlayAlpha ("Overlay Alpha", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _OverlayTex;
            float _OverlayAlpha;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseColor = tex2D(_MainTex, i.texcoord);
                fixed4 overlayColor = tex2D(_OverlayTex, i.texcoord);
                return lerp(baseColor, overlayColor, _OverlayAlpha);
            }
            ENDCG
        }
    }
}
