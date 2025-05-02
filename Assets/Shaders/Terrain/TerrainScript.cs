using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainScript : MonoBehaviour {
    public Shader materialShader;
    public ComputeShader computeShader;
    private int FractalNoiseCS;

    // Plane
    private const int planeSize = 100;
    private const float halfLength = planeSize * 0.5f;
    private const int sideVertCount = planeSize * 2;
    private const int vert_num = sideVertCount + 1;
    private Mesh mesh;
    private Mesh Cmesh;
    private Vector3[] vertices;
    private Vector3[] normals;
    private Material objMaterial;
    private RenderTexture computeResult;
    private int GRID_DIM, resN;

    public struct OctaveParams {
        public float lacunarity;
        public float persistence;
        public int rotation;
        public float shift;
    }

    const int OctaveCount = 4;
    OctaveParams[] octaves = new OctaveParams[OctaveCount];
    float[] heightMap = new float[vert_num * vert_num];

    [System.Serializable]
    public struct UI_OctaveParams {
        public float lacunarity;
        [Range(0.0f, 1.0f)] public float persistence;
        [Range(0, 360)] public int rotation;
        public float shift;
    }

    public bool updateOctave = false;
    public float baseFrequency = 1;
    [SerializeField] public UI_OctaveParams octave1;
    [SerializeField] public UI_OctaveParams octave2;
    [SerializeField] public UI_OctaveParams octave3;
    [SerializeField] public UI_OctaveParams octave4;
    private ComputeBuffer octaveBuffer;
    private ComputeBuffer heightBuffer;
    public bool reCalcCollision = false;

#region Material Settings
    [Header("Material Settings")]
    [ColorUsageAttribute(false, true)] public Color ambient;
    [ColorUsageAttribute(false, true)] public Color ambient2;
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
#endregion



    void FillOctaveStruct(UI_OctaveParams displaySettings, ref OctaveParams computeSettings) {
        computeSettings.lacunarity = displaySettings.lacunarity;
        computeSettings.persistence = displaySettings.persistence;
        computeSettings.rotation = displaySettings.rotation;
        computeSettings.shift = displaySettings.shift;
    }

    void SetSOctaveBuffers() {
        FillOctaveStruct(octave1, ref octaves[0]);
        FillOctaveStruct(octave2, ref octaves[1]);
        FillOctaveStruct(octave3, ref octaves[2]);
        FillOctaveStruct(octave4, ref octaves[3]);
        octaveBuffer.SetData(octaves);
        computeShader.SetBuffer(FractalNoiseCS, "_Octaves", octaveBuffer);
    }

    RenderTexture CreateRenderTex(int width, int height, int depth, RenderTextureFormat format, bool useMips) {
        RenderTexture rt = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear);
        rt.dimension = TextureDimension.Tex2DArray;
        rt.filterMode = FilterMode.Bilinear;
        rt.wrapMode = TextureWrapMode.Repeat;
        rt.enableRandomWrite = true;
        rt.volumeDepth = depth;
        rt.useMipMap = useMips;
        rt.autoGenerateMips = false;
        rt.anisoLevel = 16;
        rt.Create();
        return rt;
    }

    private void CreatePlaneMesh() {
        mesh = GetComponent<MeshFilter>().mesh = new Mesh();
        mesh.name = "TerrainMesh";
        
        vertices = new Vector3[vert_num * vert_num];
        Vector2[] uv = new Vector2[vertices.Length];
        Vector4[] tangents = new Vector4[vertices.Length];
        Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);
        
        for (int i = 0, x = 0; x <= sideVertCount; ++x) {
            for (int z = 0; z <= sideVertCount; ++z, ++i) {
                vertices[i] = new Vector3(((float)x / sideVertCount * planeSize) - halfLength, heightMap[z*resN+x]*100, 
                                          ((float)z / sideVertCount * planeSize) - halfLength);
                uv[i] = new Vector2((float)x / sideVertCount, (float)z / sideVertCount);
                tangents[i] = tangent;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.tangents = tangents;
        int[] triangles = new int[sideVertCount * sideVertCount * 6];
        
        for (int ti = 0, vi = 0, x = 0; x < sideVertCount; ++vi, ++x) {
            for (int z = 0; z < sideVertCount; ti += 6, ++vi, ++z) {
                triangles[ti + 0] = vi;
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

    private void CreateCollisionPlane() {
        Cmesh = new Mesh();
        Cmesh.name = "CollisionPlane";

        //heightBuffer.GetData(heightMap);

        vertices = new Vector3[vert_num * vert_num];
        Vector2[] uv = new Vector2[vertices.Length];
        Vector4[] tangents = new Vector4[vertices.Length];
        Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);
        
        for (int i = 0, x = 0; x <= sideVertCount; ++x) {
            for (int z = 0; z <= sideVertCount; ++z, ++i) {
                vertices[i] = new Vector3(((float)x / sideVertCount * planeSize) - halfLength, heightMap[z*resN+x]*100, 
                                          ((float)z / sideVertCount * planeSize) - halfLength);
                uv[i] = new Vector2((float)x / sideVertCount, (float)z / sideVertCount);
                tangents[i] = tangent;
            }
        }

        Cmesh.vertices = vertices;
        Cmesh.uv = uv;
        Cmesh.tangents = tangents;
        int[] triangles = new int[sideVertCount * sideVertCount * 6];
        
        for (int ti = 0, vi = 0, x = 0; x < sideVertCount; ++vi, ++x) {
            for (int z = 0; z < sideVertCount; ti += 6, ++vi, ++z) {
                triangles[ti + 0] = vi;
                triangles[ti + 1] = vi + 1;
                triangles[ti + 2] = vi + sideVertCount + 2;
                triangles[ti + 3] = vi;
                triangles[ti + 4] = vi + sideVertCount + 2;
                triangles[ti + 5] = vi + sideVertCount + 1;
            }
        }

        Cmesh.SetTriangles(triangles, 0);
        Cmesh.RecalculateNormals();
        Cmesh.RecalculateBounds();
        GetComponent<MeshCollider>().sharedMesh = Cmesh;
    }

    void CreateMaterial() {
        if (materialShader == null) return;
        objMaterial = new Material(materialShader);
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.material = objMaterial;
    }

    int getGridDimFor(int kernelIdx) {
        computeShader.GetKernelThreadGroupSizes(kernelIdx, out uint BLOCK_DIM, out _, out _);
        return (int)((resN + (BLOCK_DIM - 1)) / BLOCK_DIM);
    }

    void Start() {
        FractalNoiseCS = computeShader.FindKernel("FractalNoiseCS");
        resN = vert_num;
        GRID_DIM = getGridDimFor(FractalNoiseCS);
        computeResult = CreateRenderTex(resN, resN, 1, RenderTextureFormat.Default, true);
        octaveBuffer = new ComputeBuffer(OctaveCount, 3 * sizeof(float) + sizeof(int));
        heightBuffer = new ComputeBuffer(vert_num*vert_num, sizeof(float));
        SetSOctaveBuffers();
        computeShader.SetTexture(FractalNoiseCS, "_Result", computeResult);
        computeShader.SetBuffer(FractalNoiseCS, "_Collision", heightBuffer);
        computeShader.SetInt("_OctaveCount", OctaveCount);
        computeShader.SetInt("_N", resN);
        computeShader.SetFloat("_BaseFrequency", baseFrequency);
        computeShader.Dispatch(FractalNoiseCS, GRID_DIM, GRID_DIM, 1);

        heightBuffer.GetData(heightMap);

        CreatePlaneMesh();
        CreateCollisionPlane();
        CreateMaterial();
        objMaterial.SetTexture("_BaseTex", computeResult);

    }

    void Update() {
        if (updateOctave) {
            SetSOctaveBuffers();
            computeShader.SetTexture(FractalNoiseCS, "_Result", computeResult);
            computeShader.SetFloat("_BaseFrequency", baseFrequency);
            computeShader.Dispatch(FractalNoiseCS, GRID_DIM, GRID_DIM, 1);
        }
        if(reCalcCollision){
            if (Cmesh != null) {
                Destroy(Cmesh);
                Cmesh = null;
                vertices = null;
                normals = null;
            }
            CreateCollisionPlane();
            reCalcCollision = false;
        }
        objMaterial.SetVector("_TopColor", ambient);
        objMaterial.SetVector("_BotColor", ambient2);
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

    void OnDisable() {
        if (objMaterial != null) {
            Destroy(objMaterial);
            objMaterial = null;
        }

        if (mesh != null) {
            Destroy(mesh);
            mesh = null;
            vertices = null;
            normals = null;
        }
        if (Cmesh != null) {
            Destroy(Cmesh);
            Cmesh = null;
            vertices = null;
            normals = null;
        }
        Destroy(computeResult);
        octaveBuffer.Dispose();
        heightBuffer.Dispose();
    }
}
