Shader "Nana/WebcamControlShader"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _RotationAngle ("Rotation Angle", Float) = 0
        _SwapBR ("Swap Red and Blue", Int) = 0
        _HorizontalFlip("Flip the texture horizontally", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _RotationAngle;
            int _SwapBR;
            int _HorizontalFlip = 0;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float angle = radians(_RotationAngle);
                float _cos = cos(angle);
                float _sin = sin(angle);
                float2 coords = mul(float2x2(
                    _cos, -_sin,
                    _sin,  _cos
                ), (_HorizontalFlip > 0 ? float2(1 - i.uv.x, i.uv.y) :i.uv) - 0.5) + 0.5;
                float4 color = tex2D(_MainTex, coords);
                if (_SwapBR > 0) return float4(color.z, color.y, color.x, color.w);
                return color;
            }
            ENDCG
        }
    }
}
