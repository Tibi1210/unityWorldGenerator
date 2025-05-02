using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CloudPass : ScriptableRenderPass
{
    private Material material;
    private CloudSettings settings;
    private RenderTargetIdentifier source;
    private RenderTargetIdentifier mainTex;
    private string profilerTag;

    public void Setup(ScriptableRenderer renderer, string profilerTag){

        this.profilerTag = profilerTag;
        source = renderer.cameraColorTargetHandle;
        VolumeStack stack = VolumeManager.instance.stack;
        settings = stack.GetComponent<CloudSettings>();
        renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        if (settings != null && settings.IsActive())
        {
            material = new Material(Shader.Find("_Tibi/PostProcess/Clouds"));
        }
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        if (settings == null) return;
        int id = Shader.PropertyToID("_MainTex");
        mainTex = new RenderTargetIdentifier(id);
        cmd.GetTemporaryRT(id, cameraTextureDescriptor);
        base.Configure(cmd, cameraTextureDescriptor);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!settings.IsActive())
        {
            return;
        }
        CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
        
        cmd.Blit(source, mainTex);
                material.SetFloat("_alpha", settings.alpha.value);
                material.SetColor("_color", settings.color.value);
                material.SetVector("_BoundsMin", settings.BoundsMin.value);
                material.SetVector("_BoundsMax", settings.BoundsMax.value);
                material.SetFloat("_CloudScale", Mathf.Abs(settings.CloudScale.value));
                material.SetVector("_Wind", settings.Wind.value);
                material.SetFloat("_detailNoiseScale", Mathf.Abs(settings.DetailCloudScale.value));
                material.SetVector("_detailNoiseWind", settings.DetailCloudWind.value);
                material.SetFloat("_containerEdgeFadeDst", Mathf.Abs(settings.ContainerEdgeFadeDst.value));
                material.SetTexture("_ShapeNoise", settings.CloudNoiseTexure.value);
                material.SetTexture("_DetailNoise", settings.DetailCloudNoiseTexure.value);
                material.SetFloat("_detailNoiseWeight", settings.detailCloudWeight.value);
                material.SetFloat("_DensityThreshold", settings.DensityThreshold.value);
                material.SetFloat("_DensityMultiplier", Mathf.Abs(settings.DensityMultiplier.value));
                material.SetInteger("_NumSteps", settings.Steps.value);
                material.SetFloat("_lightAbsorptionThroughCloud", settings.LightAbsorptionThroughCloud.value);
                material.SetVector("_phaseParams", settings.PhaseParams.value);
                material.SetInteger("_numStepsLight", settings.LightSteps.value);
                material.SetFloat("_lightAbsorptionTowardSun", settings.LightAbsorptionTowardSun.value);
                material.SetFloat("_darknessThreshold", settings.DarknessThreshold.value);
                material.SetFloat("_cloudSmooth", settings.CloudSmooth.value);
                material.SetTexture("_BlueNoise", settings.BlueNoiseTexure.value);
                material.SetFloat("_rayOffsetStrength", settings.RayOffsetStrength.value);
                material.SetFloat("_RenderDistance", settings.RenderDistance.value);
        cmd.Blit(mainTex, source, material);

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd) {
        
        cmd.ReleaseTemporaryRT(Shader.PropertyToID("_MainTex"));
    }
}
