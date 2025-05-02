using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("_Tibi/MyFog")]
public sealed class FogSettings : VolumeComponent, IPostProcessComponent
{
    public ClampedIntParameter on = new ClampedIntParameter(1, 1, 2);
    public bool IsActive() => on.value > 1 && active;
    public ColorParameter color = new ColorParameter(new Color(1,1,1,1));
    public ColorParameter color2 = new ColorParameter(new Color(1,1,1,1));
    public ClampedFloatParameter alpha = new ClampedFloatParameter(0.005f, 0, 1);
    public ClampedFloatParameter RenderDistance = new ClampedFloatParameter(200, 1, 1000);

    public bool IsTileCompatible() => false;
}
