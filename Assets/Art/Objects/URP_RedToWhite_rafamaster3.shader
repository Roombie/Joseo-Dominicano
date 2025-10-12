Shader "Custom/WhiteToColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ReplaceColor ("Replace Color", Color) = (1,0,0,1) // default red
        _Tolerance ("Tolerance", Range(0,1)) = 0.05

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _ReplaceColor;
            float _Tolerance;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // Check if pixel is white (within a tolerance)
                if (all(abs(col.rgb - 1.0) < _Tolerance)) // tolerance so it's not exact float compare
                {
                    col = _ReplaceColor;
                }

                return col;
            }
            ENDCG
        }
    }
}