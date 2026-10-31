using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace SoundThief.SoundScan
{
    public sealed class SoundScanRendererFeature : ScriptableRendererFeature
    {
        public Shader shader;
        Material material;
        ScanPass pass;
        public override void Create()
        {
            if (material) CoreUtils.Destroy(material);
            if (shader) material = CoreUtils.CreateEngineMaterial(shader);
            pass = new ScanPass { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing };
            pass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            if (!material || renderingData.cameraData.cameraType != CameraType.Game || !camera) return;
            var controller = camera.GetComponent<SoundScanController>();
            if (!controller || !controller.isActiveAndEnabled || controller.ActiveCount == 0) return;
            pass.Setup(material, controller);
            renderer.EnqueuePass(pass);
        }
        protected override void Dispose(bool disposing) { if (material) CoreUtils.Destroy(material); }

        sealed class ScanPass : ScriptableRenderPass
        {
            Material material;
            SoundScanController controller;
            readonly Vector4[] origins = new Vector4[SoundScanController.Capacity];
            readonly Vector4[] parameters = new Vector4[SoundScanController.Capacity];
            readonly Vector4[] attenuation = new Vector4[SoundScanController.Capacity * 16];
            sealed class PassData { public Material material; public MaterialPropertyBlock properties; }
            public void Setup(Material value, SoundScanController owner) { material = value; controller = owner; }
            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                if (!resources.cameraDepthTexture.IsValid() || !resources.cameraNormalsTexture.IsValid() || !resources.activeColorTexture.IsValid()) return;
                int count = controller.CopyWaves(origins, parameters, attenuation);
                if (count == 0) return;
                var block = new MaterialPropertyBlock();
                block.SetVector("_BlitScaleBias", new Vector4(1, 1, 0, 0));
                block.SetVectorArray("_ScanOrigins", origins); block.SetVectorArray("_ScanParameters", parameters);
                block.SetVectorArray("_ScanAttenuation", attenuation);
                block.SetInt("_ScanCount", count); block.SetColor("_ScanColor", controller.scanColor);
                block.SetVector("_ScanStyle", new Vector4(Mathf.Max(.02f, controller.contourSpacing), controller.contourThickness, controller.brightness, controller.edgeStrength));
                block.SetFloat("_ScanFrontWidth", Mathf.Max(.01f, controller.frontWidth));
                using (var builder = graph.AddRasterRenderPass<PassData>("Sound terrain scan", out var data))
                {
                    data.material = material; data.properties = block;
                    builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                    builder.UseTexture(resources.cameraNormalsTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
                        context.cmd.DrawProcedural(Matrix4x4.identity, d.material, 0, MeshTopology.Triangles, 3, 1, d.properties));
                }
            }
        }
    }
}
