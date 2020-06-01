// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

// Upgrade NOTE: upgraded instancing buffer 'MyProperties' to new syntax.

// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

Shader "Hidden/Stippling"
{
    Properties
    {
    }
    SubShader
    {
		Tags { "RenderType" = "Opaque" }
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

			#pragma multi_compile_instancing

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
				float3 color: COLOR0;
            };

			float3 _Color;
			float _NibRadius;
			#if SHADER_TARGET >= 45
			StructuredBuffer<float3> _Particles;
			#endif

			float4x4 translate(float3 v) {
				return float4x4(
					1, 0, 0, v.x,
					0, 1, 0, v.y,
					0, 0, 1, v.z,
					0, 0, 0, 1
				);
			}

			// float4x4 translate(float v) {
			// 	return translate(v.xxx);
			// }

			float4x4 scale(float3 s) {
				return float4x4(
					s.x, 0,   0,   0,
					0,   s.y, 0,   0,
					0,   0,   s.z, 0,
					0,   0,   0,   1
				);
			}

			float4x4 scale(float s) {
				return scale(s.xxx);
			}

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;

                //o.vertex = float4(2 * (pos + _NibRadius * v.vertex) - 1, 0.0, 1.0);
                //o.vertex = float4(_NibRadius * v.vertex.xy, 0.0, 1.0);
                //o.vertex = float4(v.vertex.xy, 0.0, 1.0);
                //o.vertex = UnityObjectToClipPos(v.vertex);
			//#if 0
			#if SHADER_TARGET >= 45
				float3 inst = _Particles[instanceID];

				inst.y = 1 - inst.y;
				
				float4x4 transform = mul(
					translate(float3(2 * inst.xy - 1, 0.0)),
					scale(_NibRadius));

				float luma = inst.z;
			#else
				float4x4 transform = scale(1);
				float luma = 0.5;
			#endif

				o.vertex = mul(transform, float4(v.vertex.xyz, 1.0));
				o.color = float3(luma, luma, luma);
                o.uv = v.uv;

				// if (instanceID % 2 == 0) {
				// 	o.color = float3(1.0, 0.0, 0.0);
				// } else {
				// 	o.color = float3(0.0, 1.0, 0.0);
				// }

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //fixed4 col = tex2D(_MainTex, i.uv);

				//return fixed4(i.uv, 0.0, 1.0);
				float alpha = 1 - smoothstep(0.45, 0.5, length(i.uv - 0.5));
                //return fixed4(lerp(_Color, float3(1.0, 1.0, 1.0), col), 1.0);
				//return fixed4(i.color, alpha);
				return fixed4(0,0,0, alpha);
				//return fixed4(0.0, 1.0, 1.0, 1.0);

				//return float4(i.color, 1.0);
				//return float4(0, 1, 0, 1);
            }
            ENDCG
        }
    }
}

