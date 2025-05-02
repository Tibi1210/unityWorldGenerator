using UnityEngine.Rendering.Universal;

public class FogFeature : ScriptableRendererFeature
{
    FogPass pass;

    public override void Create()
    {
        name = "Fog Feature";
        pass = new FogPass();
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        pass.Setup(renderer, "Fog Pass");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }

}
