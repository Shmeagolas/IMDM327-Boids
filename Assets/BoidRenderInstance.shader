Shader "Unlit/BoidRenderInstanced"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        // Set queue and tags for robust drawing
        Tags { "RenderType"="Opaque" "DisableBatching"="True" "Queue"="Transparent" }
        
        // Final Pass setup: Disable Depth
        Pass
        {
            ZTest Always // Always pass the depth test (don't check depth)
            ZWrite Off  // Don't write to the depth buffer (don't cover other objects)
            Cull Off    // Disable culling

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

            // Buffer name used in C# `boidMaterial.SetBuffer("_Boids", boidBuffer);`
            StructuredBuffer<Boid> _Boids; 
            float4 _Color;

            struct appdata
            {
                float3 vertex : POSITION;
                // No UNITY_VERTEX_INPUT_INSTANCE_ID needed for procedural
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 col : COLOR;
            };

            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;

                // 1. Get boid data
                Boid b = _Boids[instanceID];

                // 2. Create a 4x4 Translation Matrix for the boid's position (b.pos.x, b.pos.y, 0)
                // This ensures the object-to-world transform is correctly applied regardless of view type.
                float4x4 model = float4x4(
                    1, 0, 0, b.pos.x,
                    0, 1, 0, b.pos.y,
                    0, 0, 1, 0,       
                    0, 0, 0, 1        
                );

                // 3. Transform the quad vertex from object space to world space
                float4 worldPos = mul(model, float4(v.vertex.xyz, 1.0));

                // 4. Transform from world space to clip space (The correct function)
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
