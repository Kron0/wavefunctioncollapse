Shader "Custom/BuildingTriplanar" {
    Properties {
        _TopTex ("Top Texture", 2D) = "white" {}
        _TopNormal ("Top Normal", 2D) = "bump" {}
        _TopColor ("Top Color", Color) = (0.8, 0.8, 0.8, 1)

        _SideTex ("Side Texture", 2D) = "white" {}
        _SideNormal ("Side Normal", 2D) = "bump" {}
        _SideColor ("Side Color", Color) = (0.7, 0.7, 0.7, 1)

        _BottomTex ("Bottom Texture", 2D) = "white" {}
        _BottomNormal ("Bottom Normal", 2D) = "bump" {}
        _BottomColor ("Bottom Color", Color) = (0.5, 0.5, 0.5, 1)

        _TexScale ("Texture Scale", Float) = 0.5
        _BlendSharpness ("Blend Sharpness", Range(1, 16)) = 4
        _Glossiness ("Smoothness", Range(0, 1)) = 0.3
        _Metallic ("Metallic", Range(0, 1)) = 0.0

        _WeatherAmount ("Weather Amount", Range(0, 1)) = 0.3
        _WeatherColor ("Weather Color", Color) = (0.25, 0.22, 0.18, 1)
        _WeatherHeight ("Weather Height (world Y)", Float) = 4
        _WeatherNoise ("Weather Noise Scale", Float) = 0.2

        _ColorJitter ("Color Jitter", Range(0, 0.15)) = 0.05

        _BrickStrength ("Brick Strength", Range(0, 1)) = 0
        _GlassStrength ("Glass Strength", Range(0, 1)) = 0

        // Default white = no W-layer tint. Overridden globally by WLayerColorizer.
        _WLayerTint ("W Layer Tint", Color) = (1, 1, 1, 1)
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        #pragma instancing_options assumeuniformscaling

        sampler2D _TopTex;
        sampler2D _TopNormal;
        sampler2D _SideTex;
        sampler2D _SideNormal;
        sampler2D _BottomTex;
        sampler2D _BottomNormal;

        half _TexScale;
        half _BlendSharpness;
        half _Glossiness;
        half _Metallic;
        half _WeatherAmount;
        half _WeatherHeight;
        half _WeatherNoise;
        half _ColorJitter;

        // Global tint set by WLayerColorizer each frame
        half4 _WLayerTint;

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _TopColor)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _SideColor)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _BottomColor)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _WeatherColor)
            UNITY_DEFINE_INSTANCED_PROP(float,  _BrickStrength)
            UNITY_DEFINE_INSTANCED_PROP(float,  _GlassStrength)
        UNITY_INSTANCING_BUFFER_END(Props)

        struct Input {
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

        float hash(float3 p) {
            p = frac(p * 0.3183099 + 0.1);
            p *= 17.0;
            return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
        }

        float noise3D(float3 p) {
            float3 i = floor(p);
            float3 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(
                lerp(lerp(hash(i),               hash(i+float3(1,0,0)), f.x),
                     lerp(hash(i+float3(0,1,0)),  hash(i+float3(1,1,0)), f.x), f.y),
                lerp(lerp(hash(i+float3(0,0,1)),  hash(i+float3(1,0,1)), f.x),
                     lerp(hash(i+float3(0,1,1)),  hash(i+float3(1,1,1)), f.x), f.y),
                f.z);
        }

        void surf (Input IN, inout SurfaceOutputStandard o) {
            float3 worldPos    = IN.worldPos;
            float3 worldNormal = WorldNormalVector(IN, float3(0, 0, 1));
            float3 absNormal   = abs(worldNormal);

            float3 blending = pow(absNormal, _BlendSharpness);
            blending /= (blending.x + blending.y + blending.z + 0.0001);

            float2 uvX = worldPos.yz * _TexScale;
            float2 uvY = worldPos.xz * _TexScale;
            float2 uvZ = worldPos.xy * _TexScale;

            fixed4 topColor     = UNITY_ACCESS_INSTANCED_PROP(Props, _TopColor);
            fixed4 sideColor    = UNITY_ACCESS_INSTANCED_PROP(Props, _SideColor);
            fixed4 bottomColor  = UNITY_ACCESS_INSTANCED_PROP(Props, _BottomColor);
            fixed4 weatherColor = UNITY_ACCESS_INSTANCED_PROP(Props, _WeatherColor);
            float  brickStr     = UNITY_ACCESS_INSTANCED_PROP(Props, _BrickStrength);
            float  glassStr     = UNITY_ACCESS_INSTANCED_PROP(Props, _GlassStrength);

            float jitterSeed = hash(floor(worldPos * 0.5));
            float3 jitter = (jitterSeed - 0.5) * 2.0 * _ColorJitter;
            topColor.rgb    += jitter;
            sideColor.rgb   += jitter;
            bottomColor.rgb += jitter;

            fixed4 colX = tex2D(_SideTex,    uvX) * sideColor;
            fixed4 colZ = tex2D(_SideTex,    uvZ) * sideColor;
            fixed4 colY = (worldNormal.y > 0)
                ? tex2D(_TopTex,    uvY) * topColor
                : tex2D(_BottomTex, uvY) * bottomColor;

            fixed4 col = colX * blending.x + colY * blending.y + colZ * blending.z;

            // Weight for vertical (wall) faces only
            float sideW = 1.0 - absNormal.y;

            // ── Brick / masonry pattern ───────────────────────────────────────
            float horz = (abs(worldNormal.z) > abs(worldNormal.x)) ? worldPos.x : worldPos.z;
            float2 brickUV = float2(horz * 1.6, worldPos.y * 3.8);
            float brickRow  = floor(brickUV.y);
            brickUV.x += frac(brickRow * 0.5) * 0.85;
            float2 bf = frac(brickUV);
            float faceMask = smoothstep(0.0, 0.07, bf.x) * smoothstep(1.0, 0.93, bf.x)
                           * smoothstep(0.0, 0.10, bf.y) * smoothstep(1.0, 0.90, bf.y);
            float bv = hash(float3(floor(brickUV.x), brickRow, 1.0)) * 0.22 - 0.11;
            float3 brickFace   = saturate(col.rgb + bv);
            float3 mortarColor = col.rgb * 0.46;
            col.rgb = lerp(col.rgb,
                           lerp(mortarColor, brickFace, faceMask),
                           brickStr * sideW);

            // ── Window / glass pattern ────────────────────────────────────────
            float2 wf = frac(float2(horz * 0.80, worldPos.y * 1.0));
            float glassArea = smoothstep(0.0, 0.14, wf.x) * smoothstep(1.0, 0.86, wf.x)
                            * smoothstep(0.0, 0.12, wf.y) * smoothstep(1.0, 0.88, wf.y);
            float3 glassColor = float3(0.05, 0.08, 0.14) + col.rgb * 0.06;
            float3 frameColor = col.rgb * 1.18;
            col.rgb = lerp(col.rgb,
                           lerp(frameColor, glassColor, glassArea),
                           glassStr * sideW);

            // ── Weathering ────────────────────────────────────────────────────
            float weatherMask  = saturate(1.0 - worldPos.y / _WeatherHeight);
            weatherMask       *= noise3D(worldPos * _WeatherNoise) * _WeatherAmount;
            col.rgb = lerp(col.rgb, weatherColor.rgb, weatherMask);

            // ── Normal mapping ────────────────────────────────────────────────
            float3 normX = UnpackNormal(tex2D(_SideNormal, uvX));
            float3 normY = (worldNormal.y > 0)
                ? UnpackNormal(tex2D(_TopNormal,    uvY))
                : UnpackNormal(tex2D(_BottomNormal, uvY));
            float3 normZ = UnpackNormal(tex2D(_SideNormal, uvZ));
            float3 blendedNormal = normalize(normX * blending.x + normY * blending.y + normZ * blending.z);

            // ── W-layer tint ──────────────────────────────────────────────────
            col.rgb *= _WLayerTint.rgb;

            o.Albedo     = col.rgb;
            o.Normal     = blendedNormal;
            o.Metallic   = _Metallic;
            o.Smoothness = _Glossiness * (1.0 - weatherMask * 0.5)
                         + glassStr * glassArea * sideW * 0.60;
            o.Alpha      = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
