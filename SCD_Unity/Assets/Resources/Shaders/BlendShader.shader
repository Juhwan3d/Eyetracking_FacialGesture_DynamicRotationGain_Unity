Shader "Custom/BlendShader"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _OverlayTex ("Overlay (RGB)", 2D) = "black" {}
        _DecayRate ("Decay Rate", Range(0,1)) = 0.95
        _OverlayAlpha ("Overlay Alpha", Range(0,1)) = 0.5
    }
    SubShader
    {
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

                            // Blend overlayColor with baseColor
                overlayColor.a *= _OverlayAlpha;
                fixed4 finalColor = lerp(baseColor, overlayColor, overlayColor.a);

                return finalColor;
            }
            ENDCG
        }
    }
}
