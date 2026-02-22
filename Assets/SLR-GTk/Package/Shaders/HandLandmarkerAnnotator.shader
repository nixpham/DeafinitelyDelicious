Shader "Nana/HandLandmarkAnnotator"
{
    Properties
    {
        _PointColor ("Point Color", Color) = (1, 0, 0, 1)
        _LineColor ("Line Color", Color) = (0, 0, 1, 1)
        _Radius ("Point Radius", Float) = 10
        _StrokeWidth ("Line Stroke Width", Float) = 0.005
        _MainTex ("Main Texture", 2D) = "white" {}
        _AspectRatio ("Aspect Ratio", Float) = 1.0 // Aspect ratio of the target
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
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
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _PointColor;
            float4 _LineColor;
            float _Radius;
            float _StrokeWidth;
            float _AspectRatio; // Aspect ratio of the target image

            int _DrawingMode;
            const static int IMAGE_ONLY = 0;
            const static int SKELETON_ONLY = 1;
            const static int IMAGE_AND_SKELETON = 2;

            const static float4 NO_COLOR = float4(0.3, 0.3, 0.3, 1.0);

            int _LandmarksPresent;
            float4 _Points[21];
            static const int2 _Connections[21] = {
                int2(0, 1),
                int2(1, 2),
                int2(2, 3),
                int2(3, 4),
                int2(0, 5),
                int2(5, 9),
                int2(9, 13),
                int2(13, 17),
                int2(0, 17),
                int2(5, 6),
                int2(6, 7),
                int2(7, 8),
                int2(9, 10),
                int2(10, 11),
                int2(11, 12),
                int2(13, 14),
                int2(14, 15),
                int2(15, 16),
                int2(17, 18),
                int2(18, 19),
                int2(19, 20)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // Adjust UVs to match the target aspect ratio
                float2 adjustedUV = float2(1 - i.uv.x, i.uv.y);
                float currentAspectRatio = _ScreenParams.x / _ScreenParams.y;

                if (_AspectRatio > currentAspectRatio)
                {
                    // The target aspect ratio is wider; adjust the height in UV space
                    float scale = currentAspectRatio / _AspectRatio;
                    adjustedUV.y = adjustedUV.y * scale + (1.0 - scale) / 2.0;
                }
                else
                {
                    // The target aspect ratio is taller; adjust the width in UV space
                    float scale = _AspectRatio / currentAspectRatio;
                    adjustedUV.x = adjustedUV.x * scale + (1.0 - scale) / 2.0;
                }

                // Sample the texture with the adjusted UVs
                float4 color = _DrawingMode != SKELETON_ONLY ? tex2D(_MainTex, adjustedUV) : NO_COLOR;

                if (_LandmarksPresent != 0) {
                    for (int j = 0; j < 21 && _DrawingMode != IMAGE_ONLY; j++)
                    {
                        int2 connection = _Connections[j];
                        float2 startPos = _Points[connection.x].xy;
                        float2 endPos = _Points[connection.y].xy;

                        float2 lineDir = normalize(endPos - startPos);
                        float2 pointToUV = adjustedUV - startPos;
                        float projection = clamp(dot(pointToUV, lineDir), 0.0, length(endPos - startPos));
                        float2 closestPoint = startPos + lineDir * projection;
                        float distToLine = length(adjustedUV - closestPoint);

                        if (distToLine < _StrokeWidth)
                        {
                            color = _LineColor;
                        }
                    }

                    for (int k = 0; k < 21 && _DrawingMode != IMAGE_ONLY; k++)
                    {
                        float2 pointPos = _Points[k].xy;
                        float distToPoint = distance(adjustedUV, pointPos);

                        if (distToPoint < _Radius)
                        {
                            color = _PointColor;
                        }
                    }
                }

                return color;
            }
            ENDCG
        }
    }
}

