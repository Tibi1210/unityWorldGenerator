using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("_Tibi/MyClouds")]
public sealed class CloudSettings : VolumeComponent, IPostProcessComponent
{
    public ClampedIntParameter on = new ClampedIntParameter(1, 1, 2);
    public bool IsActive() => on.value > 1 && active;
    public ColorParameter color = new ColorParameter(new Color(1,1,1,1));
    public ClampedFloatParameter alpha = new ClampedFloatParameter(1, 0, 1);
    public Vector3Parameter BoundsMin = new Vector3Parameter(new Vector3(-250,50,-250));
    public Vector3Parameter BoundsMax = new Vector3Parameter(new Vector3(250,80,250));
    public ClampedFloatParameter RenderDistance = new ClampedFloatParameter(1000, 1, 5000);
    public ClampedIntParameter Steps = new ClampedIntParameter(15, 1, 15);
    public ClampedIntParameter LightSteps = new ClampedIntParameter(10, 1, 15);
    public TextureParameter CloudNoiseTexure = new TextureParameter(null);
    public ClampedFloatParameter CloudScale = new ClampedFloatParameter(0.25f, 0, 10);
    public ClampedFloatParameter CloudSmooth = new ClampedFloatParameter(10, 0, 100);
    public Vector3Parameter Wind = new Vector3Parameter(new Vector3(1,0,0));
    public ClampedFloatParameter LightAbsorptionThroughCloud = new ClampedFloatParameter(0.25f, 0, 1);
    public Vector4Parameter PhaseParams = new Vector4Parameter(new Vector4(0.1f,0.25f,0.5f,0));
    public ClampedFloatParameter ContainerEdgeFadeDst = new ClampedFloatParameter(4000, 0, 5000);
    public ClampedFloatParameter DensityThreshold = new ClampedFloatParameter(0.25f, 0, 1);
    public ClampedFloatParameter DensityMultiplier = new ClampedFloatParameter(5, 0, 10);
    public ClampedFloatParameter LightAbsorptionTowardSun = new ClampedFloatParameter(0.6f, 0, 1);
    public ClampedFloatParameter DarknessThreshold = new ClampedFloatParameter(0.1f, 0, 1);
    public ClampedFloatParameter detailCloudWeight = new ClampedFloatParameter(0.25f, 0, 1);
    public TextureParameter DetailCloudNoiseTexure = new TextureParameter(null);
    public ClampedFloatParameter DetailCloudScale = new ClampedFloatParameter(0.6f, 0, 10);
    public Vector3Parameter DetailCloudWind = new Vector3Parameter(new Vector3(0.5f,0,0));
    public TextureParameter BlueNoiseTexure = new TextureParameter(null);
    public ClampedFloatParameter RayOffsetStrength = new ClampedFloatParameter(30, 1, 50);

    public bool IsTileCompatible() => false;
}
