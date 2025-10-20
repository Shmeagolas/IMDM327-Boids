Shader "Unlit/BoidRenderInstanced"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
    }
    SubShader
    {

        Tags { "RenderType"="Opaque" "DisableBatching"="True" "Queue"="Transparent" }
        
        Pass
        {
            ZTest Always 
            ZWrite Off  
            Cull Off    

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // NOTE: We don't use multi_compile_instancing here, as this is procedural
            #pragma target 4.5
            #include "UnityCG.cginc"

            struct Boid {
                float2 pos;
                float2 vel;
            };

            StructuredBuffer<Boid> _Boids; 
            float4 _Color;

            struct appdata
            {
                float3 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 col : COLOR;
            };

            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;

                Boid b = _Boids[instanceID];

                float4x4 model = float4x4(
                    1, 0, 0, b.pos.x,
                    0, 1, 0, b.pos.y,
                    0, 0, 1, 0,       
                    0, 0, 0, 1        
                );

                float4 worldPos = mul(model, float4(v.vertex.xyz, 1.0));

                o.pos = UnityWorldToClipPos(worldPos.xyz);

                o.col = _Color;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return i.col;
            }
            ENDCG
        }
    }
}
