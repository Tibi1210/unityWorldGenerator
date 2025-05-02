using UnityEngine.Rendering.Universal;

public class CloudFeature : ScriptableRendererFeature
{
    CloudPass pass;

    public override void Create()
    {
        name = "MyClouds";
        pass = new CloudPass();
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        pass.Setup(renderer, "MyClouds");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }

}
