Shader "PlayableAd/Soldier Hit Flash"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _GlossMapScale ("Smoothness Scale", Range(0,1)) = 1
        [Gamma] _Metallic ("Metallic", Range(0,1)) = 0
        _MetallicGlossMap ("Metallic", 2D) = "white" {}
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1

        [HDR] _HitFlashColor ("Hit Flash Color", Color) = (1,1,1,1)
        _HitFlashActive ("Hit Flash Active", Range(0,1)) = 0
        _HitFlashOpacity ("Hit Flash Opacity", Range(0,1)) = 0
        _HitFlashEmission ("Hit Flash Emission", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows
        #pragma shader_feature_local _NORMALMAP
        #pragma shader_feature_local _METALLICGLOSSMAP

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _MetallicGlossMap;
        fixed4 _Color;
        fixed4 _HitFlashColor;
        half _Glossiness;
        half _GlossMapScale;
        half _Metallic;
        half _BumpScale;
        half _HitFlashActive;
        half _HitFlashOpacity;
        half _HitFlashEmission;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float2 uv_MetallicGlossMap;
        };

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed4 textureSample = tex2D(_MainTex, input.uv_MainTex);
            half flashActive = saturate(_HitFlashActive);
            half flashOpacity = saturate(_HitFlashOpacity);
            fixed3 normalColor = textureSample.rgb * _Color.rgb;
            fixed3 tintedFlash = textureSample.rgb * _HitFlashColor.rgb;
            fixed3 opaqueFlash = lerp(tintedFlash, _HitFlashColor.rgb, flashOpacity);

            output.Albedo = lerp(normalColor, opaqueFlash, flashActive);

            #if defined(_NORMALMAP)
                output.Normal = UnpackScaleNormal(tex2D(_BumpMap, input.uv_BumpMap), _BumpScale);
            #endif

            #if defined(_METALLICGLOSSMAP)
                fixed4 metallicGloss = tex2D(_MetallicGlossMap, input.uv_MetallicGlossMap);
                output.Metallic = metallicGloss.r;
                output.Smoothness = metallicGloss.a * _GlossMapScale;
            #else
                output.Metallic = _Metallic;
                output.Smoothness = _Glossiness;
            #endif

            output.Emission = _HitFlashColor.rgb * max(0, _HitFlashEmission) * flashActive;
            output.Alpha = textureSample.a * _Color.a;
        }
        ENDCG
    }

    FallBack "Standard"
}
