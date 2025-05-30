﻿using UnityEditor.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace shooter_game.resources.render
{

#if UNITY_EDITOR
    [CustomPassDrawer(typeof(FPSForeground))]
    internal class FPSForegroundEditor : CustomPassDrawer
    {
        protected override PassUIFlag commonPassUIFlags => PassUIFlag.Name;
    }

#endif

    internal class FPSForeground : CustomPass
    {
        private const string kCameraTag = "_FPSForegroundCamera";
        public Quaternion cameraRotation = Quaternion.identity;

        private Material depthClearMaterial;

        private Camera foregroundCamera;
        public LayerMask foregroundMask;
        public float fov = 45;
        public Vector3 localPosition = Vector3.zero;
        public Transform parent;

        private readonly ProfilingSampler s_RenderFromCameraSampler = new("Render From Camera");
        
        protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters,
            HDCamera hdCamera)
        {
            cullingParameters.cullingMask |= (uint)foregroundMask.value;
        }

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            // Hidden foreground camera:
            var cam = GameObject.Find(kCameraTag);
            if (cam == null)
            {
                cam = new GameObject(kCameraTag);
                //cam = new GameObject(kCameraTag) { hideFlags = HideFlags.HideAndDontSave };
                cam.transform.SetParent(parent);
                cam.AddComponent<Camera>();
            }

            depthClearMaterial = new Material(Shader.Find("Hidden/Renderers/ForegroundDepthClear"));

            // var trueDethBuffer = new RenderTargetIdentifier(BuiltinRenderTextureType.Depth);

            // trueDepthBuffer = RTHandles.Alloc(trueDethBuffer);

            foregroundCamera = cam.GetComponent<Camera>();

            //            var outline = new Outline
            //    {
            //        outlineColor = Color.red,
            //        outlineLayer = LayerMask.GetMask("Outline"),
            //        threshold = 0,
            //    };
            //    CustomPassVolume.RegisterGlobalCustomPass(CustomPassInjectionPoint.BeforePostProcess, outline);
        }

        protected override void Execute(CustomPassContext ctx)
        {
            // Disable it for scene view because it's horrible
            if (ctx.hdCamera.camera.cameraType == CameraType.SceneView)
                return;

            var currentCam = ctx.hdCamera.camera;

            // Copy settings of our current camera
            foregroundCamera.CopyFrom(ctx.hdCamera.camera);
            foregroundCamera.transform.SetLocalPositionAndRotation(localPosition, cameraRotation);
            // Make sure the camera is disabled, we don't want it to render anything.
            foregroundCamera.enabled = false;
            foregroundCamera.fieldOfView = fov;
            foregroundCamera.cullingMask = foregroundMask;

            var depthTestOverride = new RenderStateBlock(RenderStateMask.Depth)
            {
                depthState = new DepthState(true, CompareFunction.LessEqual)
            };


            // Render the object color or normal + depth depending on the injection point
            if (injectionPoint == CustomPassInjectionPoint.AfterOpaqueDepthAndNormal)
            {
                CoreUtils.SetKeyword(ctx.cmd, "WRITE_NORMAL_BUFFER", true);
                {
                    // Override depth to 0 so that screen space effects don't apply to the foreground objects.
                    ctx.cmd.SetRenderTarget(ctx.cameraNormalBuffer, ctx.cameraDepthBuffer, 0, CubemapFace.Unknown,
                        0); // TODO: make it work in VR
                    RenderPassFromCamera(ctx, foregroundCamera, null, ctx.cameraDepthBuffer, ClearFlag.None,
                        foregroundMask,
                        overrideMaterial: depthClearMaterial, overrideMaterialIndex: 0, renderDepth: true);

                    // Render the object normals with the cleared depth buffer.
                    RenderPassFromCamera(ctx, foregroundCamera, ctx.cameraNormalBuffer, ctx.cameraDepthBuffer,
                        ClearFlag.None, foregroundMask, overrideRenderState: depthTestOverride, renderDepth: true);
                }
                CoreUtils.SetKeyword(ctx.cmd, "WRITE_NORMAL_BUFFER", false);
            }
            else
            {
                var depthTestOverride2 = new RenderStateBlock(RenderStateMask.Depth)
                {
                    depthState = new DepthState(false, CompareFunction.Equal)
                };

                // Before rendering the transparent objects, we render the foreground objects into the color buffer.
                CustomPassUtils.RenderFromCamera(ctx, foregroundCamera, ctx.cameraColorBuffer, ctx.cameraDepthBuffer,
                    ClearFlag.None, foregroundMask, overrideRenderState: depthTestOverride);


                // Finally, clear the motion vectors to avoid ghosting.
                //RenderPassFromCamera(ctx, foregroundCamera, ctx.cameraMotionVectorsBuffer, null, ClearFlag.None, foregroundMask, overrideMaterial: depthClearMaterial, overrideMaterialIndex: 0, renderDepth: true);
            }
        }

        public void RenderPassFromCamera(in CustomPassContext ctx, Camera view, RTHandle targetColor,
            RTHandle targetDepth,
            ClearFlag clearFlag, LayerMask layerMask, RenderQueueType renderQueueFilter = RenderQueueType.All,
            Material overrideMaterial = null, int overrideMaterialIndex = 0,
            RenderStateBlock overrideRenderState = default,
            bool renderDepth = true)
        {
            ShaderTagId[] depthTags = { HDShaderPassNames.s_DepthForwardOnlyName, HDShaderPassNames.s_DepthOnlyName };
            ShaderTagId[] motionTags = { HDShaderPassNames.s_MotionVectorsName, new("FirstPass") };

            if (targetColor != null && targetDepth != null)
                CoreUtils.SetRenderTarget(ctx.cmd, targetColor, targetDepth, clearFlag);
            else if (targetColor != null)
                CoreUtils.SetRenderTarget(ctx.cmd, targetColor, clearFlag);
            else if (targetDepth != null)
                CoreUtils.SetRenderTarget(ctx.cmd, targetDepth, clearFlag);

            using (new CustomPassUtils.DisableSinglePassRendering(ctx))
            {
                using (new CustomPassUtils.OverrideCameraRendering(ctx, view))
                {
                    using (new ProfilingScope(ctx.cmd, s_RenderFromCameraSampler))
                    {
                        CustomPassUtils.DrawRenderers(ctx, renderDepth ? depthTags : motionTags, layerMask,
                            renderQueueFilter, overrideMaterial, overrideMaterialIndex, overrideRenderState);
                    }
                }
            }
        }

        protected override void Cleanup()
        {
            // trueDepthBuffer.Release();
            CoreUtils.Destroy(depthClearMaterial);
        }
    }
}