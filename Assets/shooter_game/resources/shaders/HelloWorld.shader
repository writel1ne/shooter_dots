Shader "Examples/HelloWorld"
{
//    Properties
//    {
//        _BaseColor("Base Color", Color) = (1,1,1,1)
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
//            };
//
//            struct v2f
//            {
//                float4 positionCS : SV_POSITION;
//            };
//
//            CBUFFER_START (UnityPerMaterial)
//            float4 _BaseColor;
//            CBUFFER_END
//
//            v2f vert(appdata v)
//            {
//                v2f o;
//                o.positionCS = TransformObjectToHClip(v.positionOS);
//                return o;
//            }
//
//            float4 frag(v2f i) : SV_TARGET
//            {
//                return _BaseColor;
//            }
//            ENDHLSL
//        }
//    }
}