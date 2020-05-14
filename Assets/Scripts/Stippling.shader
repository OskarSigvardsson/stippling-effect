Shader "GK/Stippling"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Texture", 2D) = "white" {}
		_Cells ("Cells", Range(1, 1024)) = 16
		_Regularity ("Regularity", Range(0, 1)) = 0
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

            #include "UnityCG.cginc"

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

			sampler2D _NoiseTex;
            sampler2D _MainTex;
            float4 _MainTex_ST;
			float4 _NoiseTex_TexelSize;
			float _Cells;
			float _Regularity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

			float luma(float3 rgb) {
				return dot(rgb, float3(0.2126, 0.7152, 0.0722));
			}

			float2 noise(float2 v) {
				return tex2Dlod(_NoiseTex, float4(v * _NoiseTex_TexelSize.xy, 0.0, 0.0)).rg;
			}

			float2 worley(float2 cell) {
				return floor(cell) + noise(floor(cell));
			}

			float2 voronoi_site(float2 cell) {
				float2 a = worley(cell);
				float2 b = float2(0,0);

				// for (int dx = -1; dx <= 1; dx++) 
				// for (int dy = -1; dy <= 1; dy++) 
				// {
					
				// 	b += worley(cell + float2(dx, dy));
				// }
				b += worley(cell + float2( 0,  0));
				b += worley(cell + float2( 1,  0));
				b += worley(cell + float2(-1,  0));
				b += worley(cell + float2( 0,  1));
				b += worley(cell + float2( 0, -1));

				return lerp(a, b / 5, _Regularity);
				return a;
			}

            fixed4 frag (v2f i) : SV_Target
            {
				float2 p = i.uv * floor(_Cells);
				float2 cell = floor(p);

				float dist = 1000;
				float site;

				for (int dx = -1; dx <= 1; dx++) 
				for (int dy = -1; dy <= 1; dy++) 
				{
					float2 currSite = voronoi_site(cell + float2(dx, dy));
					float2 diff = currSite - p;
					float currDist = dot(diff, diff);


					if (currDist < dist) {
						dist = currDist;
						site = currSite;
					}
				}

				if (dist < 0.1f) {
					return fixed4(0.0, 0.0, 0.0, 1.0);
				} else {
					return fixed4(1.0, 1.0, 1.0, 1.0);
				}
            }
            ENDCG
        }
    }
}
