Shader "Unlit/VFXWarp"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _center ("Center", Vector) = (0, 0, 0, 0)
        _evolutionSpeed ("Evolution Speed", Float) = 10
        _scrollSpeed ("Scroll Speed", Float) = 1
        _noiseScale ("Noise Scale", Float) = 1
        _rand ("Rand", Float) = 1
        
        _scaleA ("Scale A", Float) = 1
        _scaleB ("Scale B", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "IngoreProjector"="True" "RenderType"="TransparentCutout" }
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float2 _center;
            float _evolutionSpeed;
            float _scrollSpeed;
            float _noiseScale;
            float _rand;
            float _scaleA;
            float _scaleB;

            inline float2 unity_voronoi_noise_randomVector (float2 UV, float offset)
            {
                float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
                UV = frac(sin(mul(UV, m)) * 46839.32);
                return float2(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5);
            }

            void Unity_Voronoi_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
            {
                float2 g = floor(UV * CellDensity);
                float2 f = frac(UV * CellDensity);
                float t = 8.0;
                float3 res = float3(8.0, 0.0, 0.0);

                for(int y=-1; y<=1; y++)
                {
                    for(int x=-1; x<=1; x++)
                    {
                        float2 lattice = float2(x,y);
                        float2 offset = unity_voronoi_noise_randomVector(lattice + g, AngleOffset);
                        float d = distance(lattice + offset, f);
                        if(d < res.x)
                        {
                            res = float3(d, offset.x, offset.y);
                            Out = res.x;
                            Cells = res.y;
                        }
                    }
                }
            }

            v2f vert (meshdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                // fixed4 col = tex2D(_MainTex, o);
                float o1, o2;
                Unity_Voronoi_float(i.uv, _Time * _evolutionSpeed+_rand, _noiseScale, o1, o2);
                float yMult = 1 - pow(2.5 * (i.uv.y - 0.5), 2);
                float xMult = 1 - pow(2 * abs(i.uv.x - 0.5), 2);
                //o1 *= xMult * yMult;
                o1 = saturate((o1 - 0.5) * 20) - 0.5;
                fixed4 col = o1;

                clip(col.a + 0.3);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }

            
            ENDCG
        }
    }
}
