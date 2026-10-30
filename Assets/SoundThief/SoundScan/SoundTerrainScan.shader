Shader "SoundThief/Sound Terrain Scan"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Sound Terrain Scan"
            ZWrite Off ZTest Always Cull Off
            Blend One One
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _ScanOrigins[8]; // xyz world origin, w maximum radius
            float4 _ScanParameters[8]; // elapsed, speed, trail duration
            int _ScanCount;
            float4 _ScanColor, _ScanStyle; // spacing, thickness, brightness, edge strength
            float _ScanFrontWidth;

            float contour(float coord, float spacing, float width)
            {
                float v = coord / spacing;
                float distanceToLine = abs(frac(v + .5) - .5);
                float aa = max(fwidth(v), .001);
                return 1.0 - smoothstep(width, width + aa, distanceToLine);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float depth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    if (depth <= .00001) return 0;
                    float deviceDepth = depth;
                #else
                    if (depth >= .99999) return 0;
                    float deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif
                float3 pos = ComputeWorldSpacePosition(uv, deviceDepth, UNITY_MATRIX_I_VP);
                float3 normal = normalize(SampleSceneNormals(uv));
                float2 pixel = 1.0 / _ScaledScreenParams.xy;
                float depthR = SampleSceneDepth(uv + float2(pixel.x, 0));
                float depthU = SampleSceneDepth(uv + float2(0, pixel.y));
                float eye = LinearEyeDepth(depth, _ZBufferParams);
                float depthEdge = max(abs(eye - LinearEyeDepth(depthR, _ZBufferParams)), abs(eye - LinearEyeDepth(depthU, _ZBufferParams))) / max(eye, .3);
                float3 normalR = SampleSceneNormals(uv + float2(pixel.x, 0));
                float3 normalU = SampleSceneNormals(uv + float2(0, pixel.y));
                float normalEdge = max(length(normal - normalR), length(normal - normalU));
                float edge = saturate(depthEdge * 8 + normalEdge * .8) * _ScanStyle.w;

                // Planar world-space contours remain fixed as the camera moves. Horizontal
                // surfaces get a fine grid; vertical surfaces get height and side contours.
                float spacing = _ScanStyle.x;
                float width = _ScanStyle.y;
                float3 weights = pow(abs(normal), 4);
                weights /= max(weights.x + weights.y + weights.z, .0001);
                float floorLines = max(contour(pos.x, spacing, width), contour(pos.z, spacing, width));
                float wallX = max(contour(pos.y, spacing, width), contour(pos.z, spacing, width) * .65);
                float wallZ = max(contour(pos.y, spacing, width), contour(pos.x, spacing, width) * .65);
                float surfaceGrid = floorLines * weights.y + wallX * weights.x + wallZ * weights.z;
                // Evaluate derivatives outside the variable-length scan loop.
                float slope = saturate((1 - abs(normal.y)) * 32);
                float elevation = contour(pos.y, spacing, width) * slope;
                float lines = max(elevation, surfaceGrid * lerp(.35, .08, slope));

                float illumination = 0;
                [loop] for (int i = 0; i < _ScanCount; i++)
                {
                    float distanceFromOrigin = distance(pos, _ScanOrigins[i].xyz);
                    float elapsed = _ScanParameters[i].x;
                    float speed = _ScanParameters[i].y;
                    float age = elapsed - distanceFromOrigin / speed;
                    float reached = step(0, age) * step(distanceFromOrigin, _ScanOrigins[i].w);
                    float afterglow = reached * (1 - smoothstep(0, _ScanParameters[i].z, age));
                    float front = reached * (1 - smoothstep(0, _ScanFrontWidth, abs(elapsed * speed - distanceFromOrigin)));
                    illumination = max(illumination, afterglow * (lines * .70 + edge * .65) + front * (lines * .9 + edge));
                }
                return half4(_ScanColor.rgb * (_ScanStyle.z * illumination), 0);
            }
            ENDHLSL
        }
    }
}
