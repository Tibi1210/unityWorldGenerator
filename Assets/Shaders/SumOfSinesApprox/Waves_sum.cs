using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Waves_sum : MonoBehaviour
{

    public Shader materialShader;

    //plane
    private int planeSize = 100;
    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] normals;
    private Material objMaterial;

    [Header("Sine Wave Settings")]
    public float amplitude;
    public float waveLen;
    public float speed;

    [Header("Gerstner Wave Settings")]
    public bool enable = false;
    [Range(0.0f, 1.0f)] public float steepness;
    public float waveLen_g;
    [Range(0, 360)] public int direction; 

    [Header("Material Settings")]
    [ColorUsageAttribute(false, true)] public Color ambient;
    [Range(0.0f, 1.0f)] public float metalic = 1;
    [Range(0.0f, 1.0f)] public float roughness = 1;
    [Range(0.0f, 1.0f)] public float subsurface = 1;
    [Range(0.0f, 2.0f)] public float specular = 1;
    [Range(0.0f, 1.0f)] public float specularTint = 1;
    [Range(0.0f, 1.0f)] public float anisotropic = 1;
    [Range(0.0f, 1.0f)] public float sheen = 1;
    [Range(0.0f, 1.0f)] public float sheenTint = 1;
    [Range(0.0f, 1.0f)] public float clearCoat = 1;
    [Range(0.0f, 1.0f)] public float clearCoatGloss = 1;



    private void CreatePlaneMesh()
    {
        mesh = GetComponent<MeshFilter>().mesh = new Mesh();
        mesh.name = "mesh";

        float halfLength = planeSize * 0.5f;
        int sideVertCount = planeSize * 2;

        vertices = new Vector3[(sideVertCount + 1) * (sideVertCount + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        Vector4[] tangents = new Vector4[vertices.Length];
        Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);

        for (int i = 0, x = 0; x <= sideVertCount; ++x)
        {
            for (int z = 0; z <= sideVertCount; ++z, ++i)
            {
                vertices[i] = new Vector3(((float)x / sideVertCount * planeSize) - halfLength, 0, ((float)z / sideVertCount * planeSize) - halfLength);
                uv[i] = new Vector2((float)x / sideVertCount, (float)z / sideVertCount);
                tangents[i] = tangent;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.tangents = tangents;

        int[] triangles = new int[sideVertCount * sideVertCount * 6];

        for (int ti = 0, vi = 0, x = 0; x < sideVertCount; ++vi, ++x)
        {
            for (int z = 0; z < sideVertCount; ti += 6, ++vi, ++z)
            {
                triangles[ti] = vi;
                triangles[ti + 1] = vi + 1;
                triangles[ti + 2] = vi + sideVertCount + 2;
                triangles[ti + 3] = vi;
                triangles[ti + 4] = vi + sideVertCount + 2;
                triangles[ti + 5] = vi + sideVertCount + 1;
            }
        }

        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        normals = mesh.normals;
    }

    void CreateMaterial()
    {
        if (materialShader == null) return;

        objMaterial = new Material(materialShader);

        MeshRenderer renderer = GetComponent<MeshRenderer>();

        renderer.material = objMaterial;
    }


    void Start(){
        CreatePlaneMesh();
        CreateMaterial();

    }

    void Update(){
        objMaterial.SetFloat("_Amplitude", amplitude);
        objMaterial.SetFloat("_WaveLen", waveLen);
        objMaterial.SetFloat("_Speed", speed);

        objMaterial.SetInteger("_isGerstner", enable ? 1 : 0);
        objMaterial.SetFloat("_Steepness", steepness);
        objMaterial.SetFloat("_WaveLen_g", waveLen_g);
        objMaterial.SetInteger("_Direction", direction);

        objMaterial.SetVector("_BaseColor", ambient);
        objMaterial.SetFloat("_Metalic", metalic);
        objMaterial.SetFloat("_Subsurface", subsurface);
        objMaterial.SetFloat("_Specular", specular);
        objMaterial.SetFloat("_Roughness", roughness);
        objMaterial.SetFloat("_SpecularTint", specularTint);
        objMaterial.SetFloat("_Anisotropic", anisotropic);
        objMaterial.SetFloat("_Sheen", sheen);
        objMaterial.SetFloat("_SheenTint", sheenTint);
        objMaterial.SetFloat("_ClearCoat", clearCoat);
        objMaterial.SetFloat("_ClearCoatGloss", clearCoatGloss);
    }

    void OnDisable(){
        if (objMaterial != null){
            Destroy(objMaterial);
            objMaterial = null;
        }

        if (mesh != null){
            Destroy(mesh);
            mesh = null;
            vertices = null;
            normals = null;
        }

    }
}
