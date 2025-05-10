using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class InputGenerator : MonoBehaviour
{

    [Header("Terrain preset")]
    [Tooltip("Plane - Mountaneous")]
    [Range(0.1f, 1.0f)]public float terrainFormat = 0.0f;
    [Tooltip("None - Lush")]
    [Range(0.1f, 1.0f)]public float vegetationDensity = 0.0f;

    [Header("Weather preset")]
    [Tooltip("None - Windy")]
    [Range(0.1f, 1.0f)]public float wind = 0.0f;
    [Tooltip("None - Cloudy")]
    [Range(0.1f, 1.0f)]public float cloudAmount = 0.0f;
    [Tooltip("Light - Dark")]
    [Range(0.1f, 1.0f)]public float cloudType = 0.0f;


    [Header("Scripts for generation")]
    [SerializeField] private TerrainScript terrain;
    [SerializeField] private FFTWater_BRDF water;
    [SerializeField] private Volume cloudvolume;

    [Header("Re-Generate")]
    public bool generateTerrain = false;
    public bool generateWater = false;
    public bool generateCloud = false;

      private struct OctaveParams {
        public float lacunarity;
        public float persistence;
    }

    private const int OctaveCount = 4;
    private OctaveParams[] octave = new OctaveParams[OctaveCount];

public void UpdateTerrainParameters()
{

    float effectiveTerrainFormat = UnityEngine.Random.Range(0.0f, 1.0f);

    float lacunarityVariation = (float)(UnityEngine.Random.Range(0.0f, 1.0f) * 0.4 - 0.2);
    float persistenceVariation = (float)(UnityEngine.Random.Range(0.0f, 1.0f) * 0.3 - 0.15);
    
    float mountainFactor = effectiveTerrainFormat > 0.6f ? 
                          Mathf.Pow((effectiveTerrainFormat - 0.6f) / 0.4f, 2) : 0;

    for (int i = 0; i < 4; i++)
    {
        float baseLacunarity = 2.0f + (i * 0.1f);
        baseLacunarity += (float)(UnityEngine.Random.Range(0.0f, 1.0f) * 0.4 - 0.2);
        
        octave[i].lacunarity = Mathf.Lerp(baseLacunarity, 
                                         baseLacunarity * (2.0f + mountainFactor), 
                                         effectiveTerrainFormat) + lacunarityVariation;

        float basePersistence = 0.5f * Mathf.Pow(0.5f, i);
        basePersistence *= 1.0f + (float)(UnityEngine.Random.Range(0.0f, 1.0f) * 0.4 - 0.2);
        
        float persistenceMultiplier = i == 0 ? (2.0f + mountainFactor * 1.5f) : 
                                     (i == 1 ? (1.8f + mountainFactor) : 1.5f);
        
        octave[i].persistence = Mathf.Lerp(basePersistence * 0.5f, 
                                          basePersistence * persistenceMultiplier, 
                                          effectiveTerrainFormat) + persistenceVariation;
        
        octave[i].persistence = Mathf.Clamp01(octave[i].persistence);
    }
    
    float slopeVariation = (float)(UnityEngine.Random.Range(0.0f, 1.0f) * 0.2 - 0.1);
    terrain.vegetationSlope = Mathf.Clamp(Mathf.Lerp(0.3f, 0.8f, effectiveTerrainFormat) + slopeVariation, 0.2f, 0.9f);

    float heightVariation = (float)(UnityEngine.Random.Range(0.0f, 1.0f) * 8 - 4);
    
    float baseMinHeight = Mathf.Lerp(-10f, 30f, effectiveTerrainFormat);
    terrain.vegetationHeight = baseMinHeight + heightVariation;

    if (effectiveTerrainFormat > 0.6f) {
        float enhancedMountainFactor = Mathf.Pow((effectiveTerrainFormat - 0.6f) / 0.4f, 2);
        terrain.vegetationHeight += enhancedMountainFactor * 30f;
    }

    float effectiveVegetationDensity = UnityEngine.Random.Range(0.0f, 1.0f) * vegetationDensity * 1.5f;
    float maxDensity = Mathf.Lerp(1.0f, 0.5f, effectiveTerrainFormat);
    terrain.vegetationDensity = Mathf.Clamp(effectiveVegetationDensity * maxDensity, 0.1f, 1.0f);

    setOctaves();
    terrain.reCalcCollision=true;
}

    private void setOctaves(){
        terrain.octave1.lacunarity = octave[0].lacunarity;
        terrain.octave2.lacunarity = octave[1].lacunarity;
        terrain.octave3.lacunarity = octave[2].lacunarity;
        terrain.octave4.lacunarity = octave[3].lacunarity;

        terrain.octave1.persistence = octave[0].persistence;
        terrain.octave2.persistence = octave[1].persistence;
        terrain.octave3.persistence = octave[2].persistence;
        terrain.octave4.persistence = octave[3].persistence;

        terrain.octave1.rotation = UnityEngine.Random.Range(1, 360);
        terrain.octave2.rotation = UnityEngine.Random.Range(1, 360);
        terrain.octave3.rotation = UnityEngine.Random.Range(1, 360);
        terrain.octave4.rotation = UnityEngine.Random.Range(1, 360);

        terrain.octave1.shift = UnityEngine.Random.Range(0.0f, 100.0f);
        terrain.octave2.shift = UnityEngine.Random.Range(0.0f, 100.0f);
        terrain.octave3.shift = UnityEngine.Random.Range(0.0f, 100.0f);
        terrain.octave4.shift = UnityEngine.Random.Range(0.0f, 100.0f);
    }

    void UpdateWaterParameters(){

        water.spectrum1.scale = UnityEngine.Random.Range(1.0f, 10.0f)*wind;
        water.spectrum1.windSpeed = 10*wind;
        water.spectrum1.windDirection = UnityEngine.Random.Range(1, 360);
        water.spectrum1.fetch = UnityEngine.Random.Range(1, 100000);
        water.spectrum1.spreadBlend = UnityEngine.Random.Range(0.0f, 1.0f)*wind;
        water.spectrum1.swell = UnityEngine.Random.Range(0.0f, 1.0f)*wind;
        water.spectrum1.peakEnhancement = UnityEngine.Random.Range(0.0f, 1.0f)*wind;
        water.spectrum1.shortWavesFade = UnityEngine.Random.Range(0.0f, 1.0f)*wind;
        
        water.spectrum2.scale = UnityEngine.Random.Range(1.0f, 10.0f)*wind;
        water.spectrum2.windSpeed = 10*wind;
        water.spectrum2.windDirection = UnityEngine.Random.Range(1, 360);
        water.spectrum2.fetch = UnityEngine.Random.Range(1, 100000);
        water.spectrum2.spreadBlend = UnityEngine.Random.Range(0.0f, 1.0f)*wind;
        water.spectrum2.swell = UnityEngine.Random.Range(0.0f, 1.0f)*wind;
        water.spectrum2.peakEnhancement = UnityEngine.Random.Range(0.0f, 1.0f)*wind;
        water.spectrum2.shortWavesFade = UnityEngine.Random.Range(0.0f, 1.0f)*wind;

    }

    void UpdateCloudParameters(){
        if (cloudvolume.profile.TryGet<CloudSettings>(out var cloud))
        {
            cloud.Wind.value = new Vector3(Mathf.Lerp(0.1f, 1.0f, wind), 0.0f, Mathf.Lerp(0.1f, 1.0f, wind));
            cloud.CloudScale.value = Mathf.Lerp(4.0f, 0.25f, cloudAmount);
            cloud.CloudSmooth.value = Mathf.Lerp(10.0f, 40.0f, (cloudAmount + cloudType) / 2.0f);
            cloud.LightAbsorptionThroughCloud.value = Mathf.Lerp(0.25f, 0.8f, cloudType);
            cloud.DensityThreshold.value = Mathf.Lerp(0.3f, 0.0f, cloudAmount);
            cloud.DensityMultiplier.value = Mathf.Lerp(4.0f, 10.0f, cloudType);
            cloud.LightAbsorptionTowardSun.value = Mathf.Lerp(0.6f, 1.0f, cloudType);
            cloud.DarknessThreshold.value = Mathf.Lerp(0.1f, 0.2f, cloudType * 0.5f);
            cloud.detailCloudWeight.value = Mathf.Lerp(0.25f, 1.0f, (cloudAmount + cloudType) / 2.0f);
            cloud.DetailCloudScale.value = Mathf.Lerp(4.0f, 0.6f, cloudAmount);
            
            if (cloudType > 0.9f && cloudAmount > 0.9f)
            {
                cloud.LightAbsorptionThroughCloud.value = 0.78f;
                cloud.DensityThreshold.value = 0.0f;
                cloud.DensityMultiplier.value = 10.0f;
                cloud.LightAbsorptionTowardSun.value = 1.0f;
                cloud.DetailCloudScale.value = 0.9f;
            }
            else if (cloudType < 0.2f && cloudAmount < 0.2f)
            {
                cloud.CloudScale.value = 4.0f;
                cloud.CloudSmooth.value = 40.0f;
                cloud.LightAbsorptionThroughCloud.value = 0.3f;
                cloud.DensityThreshold.value = 0.275f;
                cloud.DensityMultiplier.value = 4.25f;
                cloud.DetailCloudScale.value = 4.4f;
            }
        }
    }

    void Start()
    {
        UpdateTerrainParameters();
        UpdateWaterParameters();
        UpdateCloudParameters();
    }

    void Update()
    {
        if (generateTerrain)
        {
            UpdateTerrainParameters();
            generateTerrain = false;
        }
        if (generateWater)
        {
            UpdateWaterParameters();
            generateWater = false;
        }
        if (generateCloud)
        {
            UpdateCloudParameters();
            generateCloud = false;
        }
    }
}
