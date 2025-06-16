#if POSEIDON_URP 
#if !UNITY_6000_0_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pinwheel.Poseidon.FX.Universal
{
    public class PWaterEffectRendererFeature : ScriptableRendererFeature
    {
        public class PWaterEffectPass : ScriptableRenderPass
        {
            public const string PROFILER_TAG = "Water FX";

            private Material underwaterMaterial;
            private Material wetLensMaterial;

#if UNITY_2022_1_OR_NEWER
#pragma warning disable 0649
            private RTHandle cameraTarget = null;
            private RTHandle temporaryRenderTexture = null;
#pragma warning restore 0649
#else
            private RenderTargetIdentifier cameraTarget;
            private RenderTargetHandle temporaryRenderTexture;
#endif

            private static Shader underwaterShader;
            private static Shader UnderwaterShader
            {
                get
                {
                    if (underwaterShader == null)
                    {
                        underwaterShader = Shader.Find(PWaterFX.UNDERWATER_SHADER_URP);
                    }
                    return underwaterShader;
                }
            }

            private static Shader wetLensShader;
            private static Shader WetLensShader
            {
                get
                {
                    if (wetLensShader == null)
                    {
                        wetLensShader = Shader.Find(PWaterFX.WETLENS_SHADER_URP);
                    }
                    return wetLensShader;
                }
            }

            public PWaterEffectPass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {

            }

            private void ConfigureMaterial(ref RenderingData renderingData, PUnderwaterOverride underwater, PWetLensOverride wetLens)
            {
                if (underwater.intensity.value > 0)
                {
                    if (underwaterMaterial == null)
                    {
                        Shader shader = UnderwaterShader;
                        underwaterMaterial = CoreUtils.CreateEngineMaterial(shader);
                    }

                    underwaterMaterial.SetFloat(PMat.PP_WATER_LEVEL, underwater.waterLevel.value);
                    underwaterMaterial.SetFloat(PMat.PP_MAX_DEPTH, underwater.maxDepth.value);
                    underwaterMaterial.SetFloat(PMat.PP_SURFACE_COLOR_BOOST, underwater.surfaceColorBoost.value);

                    underwaterMaterial.SetColor(PMat.PP_SHALLOW_FOG_COLOR, underwater.shallowFogColor.value);
                    underwaterMaterial.SetColor(PMat.PP_DEEP_FOG_COLOR, underwater.deepFogColor.value);
                    underwaterMaterial.SetFloat(PMat.PP_VIEW_DISTANCE, underwater.viewDistance.value);

                    if (underwater.enableCaustic.value == true)
                    {
                        underwaterMaterial.EnableKeyword(PMat.KW_PP_CAUSTIC);
                        underwaterMaterial.SetTexture(PMat.PP_CAUSTIC_TEX, underwater.causticTexture.value);
                        underwaterMaterial.SetFloat(PMat.PP_CAUSTIC_SIZE, underwater.causticSize.value);
                        underwaterMaterial.SetFloat(PMat.PP_CAUSTIC_STRENGTH, underwater.causticStrength.value);
                    }
                    else
                    {
                        underwaterMaterial.DisableKeyword(PMat.KW_PP_CAUSTIC);
                    }

                    if (underwater.enableDistortion.value == true)
                    {
                        underwaterMaterial.EnableKeyword(PMat.KW_PP_DISTORTION);
                        underwaterMaterial.SetTexture(PMat.PP_DISTORTION_TEX, underwater.distortionNormalMap.value);
                        underwaterMaterial.SetFloat(PMat.PP_DISTORTION_STRENGTH, underwater.distortionStrength.value);
                        underwaterMaterial.SetFloat(PMat.PP_WATER_FLOW_SPEED, underwater.waterFlowSpeed.value);
                    }
                    else
                    {
                        underwaterMaterial.DisableKeyword(PMat.KW_PP_DISTORTION);
                    }

                    underwaterMaterial.SetTexture(PMat.PP_NOISE_TEX, PPoseidonSettings.Instance.NoiseTexture);
                    underwaterMaterial.SetVector(PMat.PP_CAMERA_VIEW_DIR, renderingData.cameraData.camera.transform.forward);
                    underwaterMaterial.SetFloat(PMat.PP_CAMERA_FOV, renderingData.cameraData.camera.fieldOfView);
                    underwaterMaterial.SetMatrix(PMat.PP_CAMERA_TO_WORLD_MATRIX, renderingData.cameraData.camera.cameraToWorldMatrix);
                    underwaterMaterial.SetFloat(PMat.PP_INTENSITY, underwater.intensity.value);
                }

                if (wetLens.strength.value * wetLens.intensity.value > 0)
                {
                    if (wetLensMaterial == null)
                    {
                        Shader shader = WetLensShader;
                        wetLensMaterial = CoreUtils.CreateEngineMaterial(shader);
                    }

                    Texture normalMap = wetLens.normalMap.value ?? PPoseidonSettings.Instance.DefaultNormalMap;
                    wetLensMaterial.SetTexture(PMat.PP_WET_LENS_TEX, normalMap);
                    wetLensMaterial.SetFloat(PMat.PP_WET_LENS_STRENGTH, wetLens.strength.value * wetLens.intensity.value);
                }
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (renderingData.cameraData.camera != Camera.main)
                    return;

                VolumeStack stack = VolumeManager.instance.stack;
                PUnderwaterOverride underwater = stack.GetComponent<PUnderwaterOverride>();
                PWetLensOverride wetLens = stack.GetComponent<PWetLensOverride>();

                bool willRenderUnderwater = underwater.intensity.value > 0;
                bool willRenderWetLens = wetLens.strength.value * wetLens.intensity.value > 0;
                if (!willRenderUnderwater && !willRenderWetLens)
                    return;

                ConfigureMaterial(ref renderingData, underwater, wetLens);

                Material material = willRenderUnderwater ? underwaterMaterial : wetLensMaterial;
                CommandBuffer cmd = CommandBufferPool.Get(PROFILER_TAG);
                RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                cameraTargetDescriptor.depthBufferBits = 0;

#if UNITY_2022_1_OR_NEWER
                cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                temporaryRenderTexture = RTHandles.Alloc(cameraTargetDescriptor);
                material.SetTexture(PMat.MAIN_TEX, cameraTarget);
                Blit(cmd, cameraTarget, temporaryRenderTexture, material, 0);
                Blit(cmd, temporaryRenderTexture, cameraTarget);
#elif UNITY_2021_2_OR_NEWER
                cameraTarget = renderingData.cameraData.renderer.cameraColorTarget;
                cmd.GetTemporaryRT(temporaryRenderTexture.id, cameraTargetDescriptor);
                Blit(cmd, cameraTarget, temporaryRenderTexture.Identifier(), material, 0);
                Blit(cmd, temporaryRenderTexture.Identifier(), cameraTarget);
#else
                cameraTarget = UniversalRenderPipeline.asset.scriptableRenderer.cameraColorTarget;
                cmd.GetTemporaryRT(temporaryRenderTexture.id, cameraTargetDescriptor);
                Blit(cmd, cameraTarget, temporaryRenderTexture.Identifier(), material, 0);
                Blit(cmd, temporaryRenderTexture.Identifier(), cameraTarget);
#endif

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
#if UNITY_2022_1_OR_NEWER
                RTHandles.Release(temporaryRenderTexture);
#else
                cmd.ReleaseTemporaryRT(temporaryRenderTexture.id);
#endif
            }
        }

        private PWaterEffectPass waterEffectPass;

        public override void Create()
        {
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            waterEffectPass = new PWaterEffectPass();
            renderer.EnqueuePass(waterEffectPass);
        }

    }
}
#else
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Pinwheel.Poseidon.FX.Universal
{
    public sealed class PWaterEffectRendererFeature : ScriptableRendererFeature
    {
        public Material underwaterMaterial;
        public Material wetLensMaterial;

        private WaterEffectPass waterFxPass;

        public override void Create()
        {
            name = "Poseidon Water FX";

            if (underwaterMaterial == null)
                underwaterMaterial = Resources.Load<Material>("Poseidon/Materials/UnderwaterURP");
            if (wetLensMaterial == null)
                wetLensMaterial = Resources.Load<Material>("Poseidon/Materials/WetLensURP");

            if (underwaterMaterial != null && wetLensMaterial != null)
                waterFxPass = new WaterEffectPass(name, underwaterMaterial, wetLensMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // Skip rendering if m_Material or the pass instance are null for whatever reason
            if (underwaterMaterial == null ||
                wetLensMaterial == null ||
                waterFxPass == null)
                return;

            if (renderingData.cameraData.cameraType != CameraType.Game)
                return;

            //PUnderwaterOverride uVolume = VolumeManager.instance.stack?.GetComponent<PUnderwaterOverride>();
            //PWetLensOverride wlVolume = VolumeManager.instance.stack?.GetComponent<PWetLensOverride>();

            waterFxPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

            ScriptableRenderPassInput passInput = ScriptableRenderPassInput.Depth;
            waterFxPass.ConfigureInput(passInput);

            renderer.EnqueuePass(waterFxPass);
        }

        protected override void Dispose(bool disposing)
        {

        }

        private class WaterEffectPass : ScriptableRenderPass
        {
            private Material underwaterMaterial;
            private Material wetLensMaterial;

            private static MaterialPropertyBlock s_SharedPropertyBlock = new MaterialPropertyBlock();
            private static readonly int kBlitTexturePropertyId = Shader.PropertyToID("_BlitTexture");
            private static readonly int kBlitScaleBiasPropertyId = Shader.PropertyToID("_BlitScaleBias");
            private static readonly int kIntensityPropertyId = Shader.PropertyToID("_Intensity");

            private RTHandle m_CopiedColor;

            public WaterEffectPass(string passName, Material underwaterMaterial, Material wetLensMaterial)
            {
                profilingSampler = new ProfilingSampler(passName);
                this.underwaterMaterial = underwaterMaterial;
                this.wetLensMaterial = wetLensMaterial;
                requiresIntermediateTexture = true;
            }

            #region PASS_RENDER_GRAPH_PATH
            private class PassData
            {
                public Material underwaterMaterial;
                public Material wetLensMaterial;
                public TextureHandle inputTexture;
                public Vector3 cameraForward;
                public float cameraFov;
                public Matrix4x4 cameraToWorldMatrix;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                VolumeStack stack = VolumeManager.instance.stack;
                PUnderwaterOverride underwater = stack.GetComponent<PUnderwaterOverride>();
                PWetLensOverride wetLens = stack.GetComponent<PWetLensOverride>();

                bool willRenderUnderwater = underwater.intensity.value > 0;
                bool willRenderWetLens = wetLens.strength.value * wetLens.intensity.value > 0;
                if (!willRenderUnderwater && !willRenderWetLens)
                    return;

                UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                using (var builder = renderGraph.AddRasterRenderPass<WaterEffectPass.PassData>(passName, out var passData, profilingSampler))
                {
                    passData.underwaterMaterial = this.underwaterMaterial;
                    passData.wetLensMaterial = this.wetLensMaterial;
                    passData.cameraForward = cameraData.camera.transform.forward;
                    passData.cameraFov = cameraData.camera.fieldOfView;
                    passData.cameraToWorldMatrix = cameraData.camera.cameraToWorldMatrix;

                    var cameraColorDesc = renderGraph.GetTextureDesc(resourcesData.cameraColor);
                    cameraColorDesc.name = "_CameraColor_WaterFX";
                    cameraColorDesc.clearBuffer = false;

                    TextureHandle destination = renderGraph.CreateTexture(cameraColorDesc);
                    passData.inputTexture = resourcesData.cameraColor;

                    builder.UseTexture(passData.inputTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc((WaterEffectPass.PassData data, RasterGraphContext context) => ExecuteMainPass(data, context));

                    //Swap cameraColor to the new temp resource (destination) for the next pass
                    resourcesData.cameraColor = destination;
                }
            }

            private static void ExecuteMainPass(WaterEffectPass.PassData data, RasterGraphContext context)
            {
                ExecuteMainPass(context.cmd, data.inputTexture.IsValid() ? data.inputTexture : null, data);
            }
            #endregion

            #region PASS_NON_RENDER_GRAPH_PATH
            [System.Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                ResetTarget();
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_CopiedColor, GetCopyPassTextureDescriptor(renderingData.cameraData.cameraTargetDescriptor), name: "_WaterFXPassCopyColor");
            }

            private static RenderTextureDescriptor GetCopyPassTextureDescriptor(RenderTextureDescriptor desc)
            {
                desc.msaaSamples = 1;
                desc.depthBufferBits = (int)DepthBits.None;

                return desc;
            }

            [System.Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                VolumeStack stack = VolumeManager.instance.stack;
                PUnderwaterOverride underwater = stack.GetComponent<PUnderwaterOverride>();
                PWetLensOverride wetLens = stack.GetComponent<PWetLensOverride>();

                bool willRenderUnderwater = underwater.intensity.value > 0;
                bool willRenderWetLens = wetLens.strength.value * wetLens.intensity.value > 0;
                if (!willRenderUnderwater && !willRenderWetLens)
                    return;

                ref var cameraData = ref renderingData.cameraData;
                var cmd = CommandBufferPool.Get();

                using (new ProfilingScope(cmd, profilingSampler))
                {
                    PassData passData = new PassData();
                    passData.underwaterMaterial = this.underwaterMaterial;
                    passData.wetLensMaterial = this.wetLensMaterial;
                    passData.cameraForward = cameraData.camera.transform.forward;
                    passData.cameraFov = cameraData.camera.fieldOfView;
                    passData.cameraToWorldMatrix = cameraData.camera.cameraToWorldMatrix;

                    RasterCommandBuffer rasterCmd = CommandBufferHelpers.GetRasterCommandBuffer(cmd);

                    CoreUtils.SetRenderTarget(cmd, m_CopiedColor);
                    ExecuteCopyColorPass(rasterCmd, cameraData.renderer.cameraColorTargetHandle);

                    CoreUtils.SetRenderTarget(cmd, cameraData.renderer.cameraColorTargetHandle, cameraData.renderer.cameraDepthTargetHandle);

                    ExecuteMainPass(rasterCmd, m_CopiedColor, passData);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                CommandBufferPool.Release(cmd);
            }

            private static void ExecuteCopyColorPass(RasterCommandBuffer cmd, RTHandle sourceTexture)
            {
                Blitter.BlitTexture(cmd, sourceTexture, new Vector4(1, 1, 0, 0), 0.0f, false);
            }
            #endregion

            #region PASS_SHARED_RENDERING_CODE
            private static void ExecuteMainPass(RasterCommandBuffer cmd, RTHandle sourceTexture, PassData passData)
            {
                s_SharedPropertyBlock.Clear();
                if (sourceTexture != null)
                    s_SharedPropertyBlock.SetTexture(kBlitTexturePropertyId, sourceTexture);

                // This uniform needs to be set for user materials with shaders relying on core Blit.hlsl to work as expected
                s_SharedPropertyBlock.SetVector(kBlitScaleBiasPropertyId, new Vector4(1, 1, 0, 0));

                //The shader use _MainTex for copied screen color
                s_SharedPropertyBlock.SetTexture(PMat.MAIN_TEX, sourceTexture);

                VolumeStack stack = VolumeManager.instance.stack;
                PUnderwaterOverride underwater = stack.GetComponent<PUnderwaterOverride>();
                PWetLensOverride wetLens = stack.GetComponent<PWetLensOverride>();

                bool willRenderUnderwater = underwater.intensity.value > 0;
                bool willRenderWetLens = wetLens.strength.value * wetLens.intensity.value > 0;
                if (!willRenderUnderwater && !willRenderWetLens)
                    return;

                Material underwaterMaterial = passData.underwaterMaterial;
                Material wetLensMaterial = passData.wetLensMaterial;
                Material materialToRender = null;

                if (willRenderUnderwater)
                {
                    s_SharedPropertyBlock.SetFloat(PMat.PP_WATER_LEVEL, underwater.waterLevel.value);
                    s_SharedPropertyBlock.SetFloat(PMat.PP_MAX_DEPTH, underwater.maxDepth.value);
                    s_SharedPropertyBlock.SetFloat(PMat.PP_SURFACE_COLOR_BOOST, underwater.surfaceColorBoost.value);

                    s_SharedPropertyBlock.SetColor(PMat.PP_SHALLOW_FOG_COLOR, underwater.shallowFogColor.value);
                    s_SharedPropertyBlock.SetColor(PMat.PP_DEEP_FOG_COLOR, underwater.deepFogColor.value);
                    s_SharedPropertyBlock.SetFloat(PMat.PP_VIEW_DISTANCE, underwater.viewDistance.value);

                    if (underwater.enableCaustic.value == true)
                    {
                        underwaterMaterial.EnableKeyword(PMat.KW_PP_CAUSTIC);
                        s_SharedPropertyBlock.SetTexture(PMat.PP_CAUSTIC_TEX, underwater.causticTexture.value);
                        s_SharedPropertyBlock.SetFloat(PMat.PP_CAUSTIC_SIZE, underwater.causticSize.value);
                        s_SharedPropertyBlock.SetFloat(PMat.PP_CAUSTIC_STRENGTH, underwater.causticStrength.value);
                    }
                    else
                    {
                        underwaterMaterial.DisableKeyword(PMat.KW_PP_CAUSTIC);
                    }

                    if (underwater.enableDistortion.value == true)
                    {
                        underwaterMaterial.EnableKeyword(PMat.KW_PP_DISTORTION);
                        s_SharedPropertyBlock.SetTexture(PMat.PP_DISTORTION_TEX, underwater.distortionNormalMap.value);
                        s_SharedPropertyBlock.SetFloat(PMat.PP_DISTORTION_STRENGTH, underwater.distortionStrength.value);
                        s_SharedPropertyBlock.SetFloat(PMat.PP_WATER_FLOW_SPEED, underwater.waterFlowSpeed.value);
                    }
                    else
                    {
                        underwaterMaterial.DisableKeyword(PMat.KW_PP_DISTORTION);
                    }

                    s_SharedPropertyBlock.SetTexture(PMat.PP_NOISE_TEX, PPoseidonSettings.Instance.NoiseTexture);
                    s_SharedPropertyBlock.SetVector(PMat.PP_CAMERA_VIEW_DIR, passData.cameraForward);
                    s_SharedPropertyBlock.SetFloat(PMat.PP_CAMERA_FOV, passData.cameraFov);
                    s_SharedPropertyBlock.SetMatrix(PMat.PP_CAMERA_TO_WORLD_MATRIX, passData.cameraToWorldMatrix);
                    s_SharedPropertyBlock.SetFloat(PMat.PP_INTENSITY, underwater.intensity.value);

                    materialToRender = underwaterMaterial;
                }

                if (willRenderWetLens)
                {
                    Texture normalMap = wetLens.normalMap.value ?? PPoseidonSettings.Instance.DefaultNormalMap;
                    s_SharedPropertyBlock.SetTexture(PMat.PP_WET_LENS_TEX, normalMap);
                    s_SharedPropertyBlock.SetFloat(PMat.PP_WET_LENS_STRENGTH, wetLens.strength.value * wetLens.intensity.value);

                    materialToRender = wetLensMaterial;
                }

                if (materialToRender)
                {
                    cmd.DrawProcedural(Matrix4x4.identity, materialToRender, 0, MeshTopology.Triangles, 3, 1, s_SharedPropertyBlock);
                }
            }
            #endregion
        }
    }
}
#endif
#endif