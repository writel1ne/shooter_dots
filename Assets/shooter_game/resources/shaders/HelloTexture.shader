﻿Shader "Examples/HelloTexture"
{
//    Properties
//    {
//        _BaseColor("Base Color", Color) = (1,1,1,1)
//        _BaseTex("Base Texture", 2D) = "white" {}
//    }
//
//    SubShader
//    {
//        Tags
//        {
//            "Render Type" = "Opaque"
//            "Queue" = "Geometry"
//            "RenderPipeline" = "UniversalPipeline"
//        }
//
//        Pass
//        {
//            Tags
//            {
//                "LightMode" = "UniversalForward"
//            }
//
//            HLSLPROGRAM
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//            #pragma vertex vert
//            #pragma fragment frag
//
//            struct appdata
//            {
//                float4 positionOS : POSITION;
//                float2 uv : TEXCOORD0;
//            };
//
//            struct v2f
//            {
//                float4 positionCS : SV_POSITION;
//                float2 uv : TEXCOORD0;
//            };
//
//            sampler2D _BaseTex;
//
//            CBUFFER_START (UnityPerMaterial)
//            float4 _BaseColor;
//            float4 _BaseTex_ST;
//            CBUFFER_END
//
//            v2f vert(appdata v)
//            {
//                v2f o;
//                o.positionCS = TransformObjectToHClip(v.positionOS);
//                o.uv = TRANSFORM_TEX(v.uv, _BaseTex);
//                return o;
//            }
//
//            float4 frag(v2f i) : SV_TARGET
//            {
//                float4 textureSample = tex2D(_BaseTex, i.uv);
//                return _BaseColor * textureSample;
//            }
//            ENDHLSL
//        }
//    }
}