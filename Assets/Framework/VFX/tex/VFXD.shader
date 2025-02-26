Shader "Unlit/VFXWarp"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorA ("Color A", Color) = (1,1,1,1) 
        _ColorB ("Color B", Color) = (1,1,1,1) 
        _sx ("ScrollX", Float) = 1
        _sy ("ScrollY", Float) = 1
        _am ("AlphaMult", Float) = 0.3
        _ao ("AlphaOffset", Float) = 0.3
        _frameMult ("Frame Mult", Float) = 4
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        //ZWrite Off
        Cull off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct meshdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _scrollSpeed;
            float4 _ColorA, _ColorB;
            float _am, _ao;
            float _sx, _sy;
            float _frameMult;

            v2f vert (meshdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                i.uv += floor(_Time / _frameMult) * _frameMult * float2(_sx, _sy);
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 c = lerp(_ColorA, _ColorB, 1 - col.r);
                c.a = saturate(col.r * _am + _ao) * i.color.a;
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }

            ENDCG
        }
    }
}
