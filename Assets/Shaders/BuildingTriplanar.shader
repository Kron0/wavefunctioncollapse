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

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _TopColor)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _SideColor)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _BottomColor)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _WeatherColor)
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
                lerp(lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x),
                     lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                     lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y),
                f.z);
        }

        void surf (Input IN, inout SurfaceOutputStandard o) {
            float3 worldPos = IN.worldPos;
            float3 worldNormal = WorldNormalVector(IN, float3(0, 0, 1));
            float3 absNormal = abs(worldNormal);

            float3 blending = pow(absNormal, _BlendSharpness);
            blending /= (blending.x + blending.y + blending.z + 0.0001);

            float2 uvX = worldPos.yz * _TexScale;
            float2 uvY = worldPos.xz * _TexScale;
            float2 uvZ = worldPos.xy * _TexScale;

            fixed4 topColor = UNITY_ACCESS_INSTANCED_PROP(Props, _TopColor);
            fixed4 sideColor = UNITY_ACCESS_INSTANCED_PROP(Props, _SideColor);
            fixed4 bottomColor = UNITY_ACCESS_INSTANCED_PROP(Props, _BottomColor);
            fixed4 weatherColor = UNITY_ACCESS_INSTANCED_PROP(Props, _WeatherColor);

            // Jitter colors per block position
            float jitterSeed = hash(floor(worldPos * 0.5));
            float3 jitter = (jitterSeed - 0.5) * 2.0 * _ColorJitter;
            topColor.rgb += jitter;
            sideColor.rgb += jitter;
            bottomColor.rgb += jitter;

            fixed4 colX = tex2D(_SideTex, uvX) * sideColor;
            fixed4 colZ = tex2D(_SideTex, uvZ) * sideColor;
            fixed4 colY;
            if (worldNormal.y > 0) {
                colY = tex2D(_TopTex, uvY) * topColor;
            } else {
                colY = tex2D(_BottomTex, uvY) * bottomColor;
            }

            fixed4 col = colX * blending.x + colY * blending.y + colZ * blending.z;

            // Normal mapping
            float3 normX = UnpackNormal(tex2D(_SideNormal, uvX));
            float3 normY;
            if (worldNormal.y > 0) {
                normY = UnpackNormal(tex2D(_TopNormal, uvY));
            } else {
                normY = UnpackNormal(tex2D(_BottomNormal, uvY));
            }
            float3 normZ = UnpackNormal(tex2D(_SideNormal, uvZ));
            float3 blendedNormal = normalize(normX * blending.x + normY * blending.y + normZ * blending.z);

            // Weathering: dirt near ground, noise-driven
            float weatherMask = saturate(1.0 - worldPos.y / _WeatherHeight);
            weatherMask *= noise3D(worldPos * _WeatherNoise);
            weatherMask *= _WeatherAmount;
            col.rgb = lerp(col.rgb, weatherColor.rgb, weatherMask);

            o.Albedo = col.rgb;
            o.Normal = blendedNormal;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness * (1.0 - weatherMask * 0.5);
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
