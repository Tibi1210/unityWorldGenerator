using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FFTWater_BRDF : MonoBehaviour{
    public Shader waterShader;
    public ComputeShader computeShader;

    private const int planeLength = 100;
    private const int quadRes = 2;

    private Material waterMaterial;
    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] normals;

    public struct SpectrumSettings{
        public float scale;
        public float angle;
        public float spreadBlend;
        public float swell;
        public float alpha;
        public float peakFrequency;
        public float gamma;
        public float shortWavesFade;
    }
    SpectrumSettings[] spectrums = new SpectrumSettings[8];

    [Serializable]
    public struct UI_SpectrumSettings{
        [Range(0, 5)] public float scale;
        public float windSpeed;
        [Range(0.0f, 360.0f)] public float windDirection;
        public float fetch;
        [Range(0, 1)] public float spreadBlend;
        [Range(0.01f, 1)] public float swell;
        [Range(0.001f, 7.0f)] public float peakEnhancement;
        public float shortWavesFade;
    }

    [Header("Spectrum Settings")]
    [Range(0, 100000)] public int seed = 0;
    [Range(0.0f, 0.1f)] public float lowCutoff = 0.0001f;
    [Range(0.1f, 9000.0f)] public float highCutoff = 9000.0f;
    [Range(0.0f, 20.0f)] public float gravity = 9.81f;
    [Range(2.0f, 20.0f)] public float depth = 20.0f;
    [Range(0.0f, 200.0f)] public float repeatTime = 200.0f;
    [Range(0.0f, 5.0f)] public float speed = 1.0f;
    public Vector2 lambda = new Vector2(1.0f, 1.0f);
    [Range(0.0f, 10.0f)] public float displacementDepthFalloff = 1.0f;
    public bool updateSpectrum = false;


    [Header("Layer One")]
    [Range(0, 2048)] public int lengthScale1 = 256;
    [Range(0.01f, 3.0f)] public float tile0 = 8.0f;
    public bool debugTile0 = false;
    public bool visualizeLayer0 = false;
    public bool contributeDisplacement0 = false;
    [SerializeField] public UI_SpectrumSettings spectrum1;
    [SerializeField] public UI_SpectrumSettings spectrum2;

    [Header("Layer Two")]
    [Range(0, 2048)] public int lengthScale2 = 256;
    [Range(0.01f, 3.0f)] public float tile1 = 8.0f;
    public bool debugTile1 = false;
    public bool visualizeLayer1 = false;
    public bool contributeDisplacement1 = false;
    [SerializeField] public UI_SpectrumSettings spectrum3;
    [SerializeField] public UI_SpectrumSettings spectrum4;

    [Header("Layer Three")]
    [Range(0, 2048)] public int lengthScale3 = 256;
    [Range(0.01f, 3.0f)] public float tile2 = 8.0f;
    public bool debugTile2 = false;
    public bool visualizeLayer2 = false;
    public bool contributeDisplacement2 = false;
    [SerializeField] public UI_SpectrumSettings spectrum5;
    [SerializeField] public UI_SpectrumSettings spectrum6;

    [Header("Layer Four")]
    [Range(0, 2048)] public int lengthScale4 = 256;
    [Range(0.01f, 3.0f)] public float tile3 = 8.0f;
    public bool debugTile3 = false;
    public bool visualizeLayer3 = false;
    public bool contributeDisplacement3 = false;
    [SerializeField] public UI_SpectrumSettings spectrum7;
    [SerializeField] public UI_SpectrumSettings spectrum8;


    [Header("Normal Settings")]
    [Range(0.0f, 20.0f)] public float normalStrength = 1;

    [Header("Material Settings")]
    [ColorUsageAttribute(false, true)] public Color ambient;
    [ColorUsageAttribute(false, true)] public Color tipColor;
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


    [Header("Foam Settings")]
    [Range(-2.0f, 2.0f)] public float foamBias = -0.5f;
    [Range(-10.0f, 10.0f)] public float foamThreshold = 0.0f;
    [Range(0.0f, 1.0f)] public float foamAdd = 0.5f;
    [Range(0.0f, 1.0f)] public float foamDecayRate = 0.05f;
    [Range(0.0f, 10.0f)] public float foamDepthFalloff = 1.0f;
    [Range(-2.0f, 2.0f)] public float foamSubtract1 = 0.0f;
    [Range(-2.0f, 2.0f)] public float foamSubtract2 = 0.0f;
    [Range(-2.0f, 2.0f)] public float foamSubtract3 = 0.0f;
    [Range(-2.0f, 2.0f)] public float foamSubtract4 = 0.0f;

    public RenderTexture displacementTextures,
                          slopeTextures,
                          initialSpectrumTextures,
                          spectrumTextures;

    private ComputeBuffer spectrumBuffer;

    private int resN;
    private int GRID_DIM;

    private int CS_InitializeSpectrum,
            CS_PackSpectrumConjugate,
            CS_UpdateSpectrumForFFT,
            CS_HorizontalFFT,
            CS_VerticalFFT,
            CS_AssembleMaps;


    /**
    * Sík létrehozása a vízfelület ábrázolásához.
    * Csúcspontok, UV-k, érintők és háromszögek generálása.
    * Méreteit a planeLength és a quadRes határozza meg.
    */
    private void CreatePlane(){
        mesh = GetComponent<MeshFilter>().mesh = new Mesh();
        mesh.name = "water";

        float halfLength = planeLength * 0.5f;
        int sideVertCount = planeLength * quadRes;

        vertices = new Vector3[(sideVertCount + 1) * (sideVertCount + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        Vector4[] tangents = new Vector4[vertices.Length];
        Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);

        for (int i = 0, x = 0; x <= sideVertCount; ++x){
            for (int z = 0; z <= sideVertCount; ++z, ++i){
                vertices[i] = new Vector3(((float)x / sideVertCount * planeLength) - halfLength, 0,
                                          ((float)z / sideVertCount * planeLength) - halfLength);
                uv[i] = new Vector2((float)x / sideVertCount, (float)z / sideVertCount);
                tangents[i] = tangent;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.tangents = tangents;

        int[] triangles = new int[sideVertCount * sideVertCount * 6];

        for (int ti = 0, vi = 0, x = 0; x < sideVertCount; ++vi, ++x){
            for (int z = 0; z < sideVertCount; ti += 6, ++vi, ++z){
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

    /**
    * Anyagmodell beállítasa a síkhoz a megadott víz shader használatával.
    */
    void CreateMaterial(){
        if (waterShader == null) return;
        waterMaterial = new Material(waterShader);
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.material = waterMaterial;
    }

    /**
    * FFT-vel kapcsolatos értékek beállítása a compute shaderben.
    * Tartalmazza a hullám szimulációhoz, FFT konfigurációhoz és vizuális effektekhez szükséges paramétereket.
    */
    void SetFFTUniforms(){
        computeShader.SetVector("_Lambda", lambda);
        computeShader.SetFloat("_FrameTime", Time.time * speed);
        computeShader.SetFloat("_DeltaTime", Time.deltaTime);
        computeShader.SetFloat("_Gravity", gravity);
        computeShader.SetFloat("_RepeatTime", repeatTime);

        computeShader.SetInt("_N", resN);
        computeShader.SetInt("_Seed", seed);
        computeShader.SetInt("_LengthScale0", lengthScale1);
        computeShader.SetInt("_LengthScale1", lengthScale2);
        computeShader.SetInt("_LengthScale2", lengthScale3);
        computeShader.SetInt("_LengthScale3", lengthScale4);

        computeShader.SetFloat("_NormalStrength", normalStrength);
        computeShader.SetFloat("_Depth", depth);
        computeShader.SetFloat("_FoamBias", foamBias);
        computeShader.SetFloat("_FoamDecayRate", foamDecayRate);
        computeShader.SetFloat("_FoamAdd", foamAdd);
        computeShader.SetFloat("_FoamThreshold", foamThreshold);
        computeShader.SetFloat("_LowCutoff", lowCutoff);
        computeShader.SetFloat("_HighCutoff", highCutoff);
    }

    /**
    * Anyag tulajdonságok beállítása a víz shaderben.
    */
    void SetMaterialUniforms(){
        waterMaterial.SetFloat("_NormalStrength", normalStrength);

        waterMaterial.SetVector("_Ambient", ambient);
        waterMaterial.SetVector("_TipColor", tipColor);

        waterMaterial.SetFloat("_Metallic", metalic);
        waterMaterial.SetFloat("_Subsurface", subsurface);
        waterMaterial.SetFloat("_Specular", specular);
        waterMaterial.SetFloat("_Roughness", roughness);
        waterMaterial.SetFloat("_SpecularTint", specularTint);
        waterMaterial.SetFloat("_Anisotropic", anisotropic);
        waterMaterial.SetFloat("_Sheen", sheen);
        waterMaterial.SetFloat("_SheenTint", sheenTint);
        waterMaterial.SetFloat("_ClearCoat", clearCoat);
        waterMaterial.SetFloat("_ClearCoatGloss", clearCoatGloss);


        waterMaterial.SetFloat("_DisplacementDepthFalloff", displacementDepthFalloff);
        waterMaterial.SetFloat("_FoamDepthAttenuation", foamDepthFalloff);

        waterMaterial.SetFloat("_Tile0", tile0);
        waterMaterial.SetFloat("_Tile1", tile1);
        waterMaterial.SetFloat("_Tile2", tile2);
        waterMaterial.SetFloat("_Tile3", tile3);

        waterMaterial.SetInt("_VisualizeLayer0", visualizeLayer0 ? 1 : 0);
        waterMaterial.SetInt("_VisualizeLayer1", visualizeLayer1 ? 1 : 0);
        waterMaterial.SetInt("_VisualizeLayer2", visualizeLayer2 ? 1 : 0);
        waterMaterial.SetInt("_VisualizeLayer3", visualizeLayer3 ? 1 : 0);

        waterMaterial.SetInt("_ContributeDisplacement0", contributeDisplacement0 ? 1 : 0);
        waterMaterial.SetInt("_ContributeDisplacement1", contributeDisplacement1 ? 1 : 0);
        waterMaterial.SetInt("_ContributeDisplacement2", contributeDisplacement2 ? 1 : 0);
        waterMaterial.SetInt("_ContributeDisplacement3", contributeDisplacement3 ? 1 : 0);

        waterMaterial.SetInt("_DebugTile0", debugTile0 ? 1 : 0);
        waterMaterial.SetInt("_DebugTile1", debugTile1 ? 1 : 0);
        waterMaterial.SetInt("_DebugTile2", debugTile2 ? 1 : 0);
        waterMaterial.SetInt("_DebugTile3", debugTile3 ? 1 : 0);

        waterMaterial.SetFloat("_FoamSubtract0", foamSubtract1);
        waterMaterial.SetFloat("_FoamSubtract1", foamSubtract2);
        waterMaterial.SetFloat("_FoamSubtract2", foamSubtract3);
        waterMaterial.SetFloat("_FoamSubtract3", foamSubtract4);
    }

    /**
    * Alfa paramétert a JONSWAP spektrumhoz.
    *
    * @param fetch Távolság, amelyen keresztül a szél hat a vízfelületre
    * @param windSpeed A szélsebesség m/s-ban
    * @return A kiszámított alfa érték
    */
    float JonswapAlpha(float fetch, float windSpeed){
        //https://wikiwaves.org/Ocean-Wave_Spectra#JONSWAP_Spectrum
        return 0.076f * Mathf.Pow(windSpeed * windSpeed / (fetch * gravity), 0.22f);

        //https://apps.dtic.mil/sti/pdfs/ADA157975.pdf
        //return 0.076f * Mathf.Pow(gravity * fetch / (windSpeed * windSpeed), -0.22f);
    }

    /**
    * Kiszámítja a csúcsfrekvenciát a JONSWAP spektrumhoz.
    *
    * @param fetch Távolság, amelyen keresztül a szél hat a vízfelületre
    * @param windSpeed A szélsebesség m/s-ban
    * @return A kiszámított csúcsfrekvencia
    */
    float JonswapPeakFrequency(float fetch, float windSpeed){
        //https://apps.dtic.mil/sti/pdfs/ADA157975.pdf
        //return 3.5f * (gravity / windSpeed) * Mathf.Pow(gravity * fetch / (windSpeed * windSpeed), -0.33f);
        
        //https://wikiwaves.org/Ocean-Wave_Spectra#JONSWAP_Spectrum
        return 22 * Mathf.Pow(gravity * gravity / (windSpeed * fetch), 0.33f);
    }

    /**
    * A felhasználói felület spektrum beállításainak átalakítása a compute shader spektrum paramétereivé.
    *
    * @param displaySettings A felhasználói felület beállítási struktúrája
    * @param computeSettings Referencia a compute shader beállítási struktúrájára
    */
    void FillSpectrumStruct(UI_SpectrumSettings displaySettings, ref SpectrumSettings computeSettings){
        computeSettings.scale = displaySettings.scale;
        computeSettings.angle = displaySettings.windDirection / 180 * Mathf.PI;
        computeSettings.spreadBlend = displaySettings.spreadBlend;
        computeSettings.swell = displaySettings.swell;
        computeSettings.alpha = JonswapAlpha(displaySettings.fetch, displaySettings.windSpeed);
        computeSettings.peakFrequency = JonswapPeakFrequency(displaySettings.fetch, displaySettings.windSpeed);
        computeSettings.gamma = displaySettings.peakEnhancement;
        computeSettings.shortWavesFade = displaySettings.shortWavesFade;
    }

    /**
    * Spektrum puffer feltöltése.
    * Előkészíti a spektrum adatokat a compute shaderben való használatra.
    */
    void SetSpectrumBuffers(){
        FillSpectrumStruct(spectrum1, ref spectrums[0]);
        FillSpectrumStruct(spectrum2, ref spectrums[1]);
        FillSpectrumStruct(spectrum3, ref spectrums[2]);
        FillSpectrumStruct(spectrum4, ref spectrums[3]);
        FillSpectrumStruct(spectrum5, ref spectrums[4]);
        FillSpectrumStruct(spectrum6, ref spectrums[5]);
        FillSpectrumStruct(spectrum7, ref spectrums[6]);
        FillSpectrumStruct(spectrum8, ref spectrums[7]);

        spectrumBuffer.SetData(spectrums);
        computeShader.SetBuffer(CS_InitializeSpectrum, "_Spectrums", spectrumBuffer);
    }

    /**
    * Inverz FFT művelet.
    * Végrehajtja a vízszintes és függőleges FFT-ket a frekvenciatartományból a térbeli tartományba való átalakításhoz.
    *
    * @param spectrumTextures A spektrum adatokat tartalmazó textúra, amit transzformálni kell
    */
    void InverseFFT(RenderTexture spectrumTextures){
        computeShader.SetTexture(CS_HorizontalFFT, "_FourierTarget", spectrumTextures);
        computeShader.Dispatch(CS_HorizontalFFT, 1, resN, 1);

        computeShader.SetTexture(CS_VerticalFFT, "_FourierTarget", spectrumTextures);
        computeShader.Dispatch(CS_VerticalFFT, 1, resN, 1);
    }

    /**
    * Render textúra létrehozása.
    * A spektrum, elmozdulás és lejtés adatok textúráinak létrehozásához.
    *
    * @param width A textúra szélessége
    * @param height A textúra magassága
    * @param depth A textúra tömb mélysége/rétegei
    * @param format A textúra formátuma
    * @param useMips Generáljon-e mipmapeket
    * @return A létrehozott RenderTexture
    */
    RenderTexture CreateRenderTex(int width, int height, int depth, RenderTextureFormat format, bool useMips){
        RenderTexture rt = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear);
        rt.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
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

    /**
    * Rács dimenzió a compute shader indításához.
    *
    * @param kernelIdx A compute shader kernel indexe
    * @return A kiszámított rács dimenzió
    */
    int getGridDimFor(int kernelIdx){
        computeShader.GetKernelThreadGroupSizes(kernelIdx, out uint BLOCK_DIM, out _, out _);
        return (int)((resN + (BLOCK_DIM - 1)) / BLOCK_DIM);
    }

    void Start(){
        CreatePlane();
        CreateMaterial();

        CS_InitializeSpectrum = computeShader.FindKernel("CS_InitializeSpectrum");
        CS_PackSpectrumConjugate = computeShader.FindKernel("CS_PackSpectrumConjugate");
        CS_UpdateSpectrumForFFT = computeShader.FindKernel("CS_UpdateSpectrumForFFT");
        CS_HorizontalFFT = computeShader.FindKernel("CS_HorizontalFFT");
        CS_VerticalFFT = computeShader.FindKernel("CS_VerticalFFT");
        CS_AssembleMaps = computeShader.FindKernel("CS_AssembleMaps");

        resN = 1024;

        initialSpectrumTextures = CreateRenderTex(resN, resN, 4, RenderTextureFormat.ARGBHalf, true);
        displacementTextures = CreateRenderTex(resN, resN, 4, RenderTextureFormat.ARGBHalf, true);
        slopeTextures = CreateRenderTex(resN, resN, 4, RenderTextureFormat.RGHalf, true);
        spectrumTextures = CreateRenderTex(resN, resN, 8, RenderTextureFormat.ARGBHalf, true);

        spectrumBuffer = new ComputeBuffer(8, 8 * sizeof(float));

        SetFFTUniforms();
        SetSpectrumBuffers();
        
        // Compute initial JONSWAP spectrum
        GRID_DIM = getGridDimFor(CS_InitializeSpectrum);
        computeShader.SetTexture(CS_InitializeSpectrum, "_InitialSpectrumTextures", initialSpectrumTextures);
        computeShader.Dispatch(CS_InitializeSpectrum, GRID_DIM, GRID_DIM, 1);

        GRID_DIM = getGridDimFor(CS_PackSpectrumConjugate);
        computeShader.SetTexture(CS_PackSpectrumConjugate, "_InitialSpectrumTextures", initialSpectrumTextures);
        computeShader.Dispatch(CS_PackSpectrumConjugate, GRID_DIM, GRID_DIM, 1);
    }

    void Update(){

        SetMaterialUniforms();
        SetFFTUniforms();
        
        if (updateSpectrum){
            SetSpectrumBuffers();

            GRID_DIM = getGridDimFor(CS_InitializeSpectrum);
            computeShader.SetTexture(CS_InitializeSpectrum, "_InitialSpectrumTextures", initialSpectrumTextures);
            computeShader.Dispatch(CS_InitializeSpectrum, GRID_DIM, GRID_DIM, 1);

            GRID_DIM = getGridDimFor(CS_PackSpectrumConjugate);
            computeShader.SetTexture(CS_PackSpectrumConjugate, "_InitialSpectrumTextures", initialSpectrumTextures);
            computeShader.Dispatch(CS_PackSpectrumConjugate, GRID_DIM, GRID_DIM, 1);
        }

        // Progress Spectrum For FFT
        GRID_DIM = getGridDimFor(CS_UpdateSpectrumForFFT);
        computeShader.SetTexture(CS_UpdateSpectrumForFFT, "_InitialSpectrumTextures", initialSpectrumTextures);
        computeShader.SetTexture(CS_UpdateSpectrumForFFT, "_SpectrumTextures", spectrumTextures);
        computeShader.Dispatch(CS_UpdateSpectrumForFFT, GRID_DIM, GRID_DIM, 1);

        // Compute FFT For Height
        InverseFFT(spectrumTextures);

        // Assemble maps
        GRID_DIM = getGridDimFor(CS_AssembleMaps);
        computeShader.SetTexture(CS_AssembleMaps, "_DisplacementTextures", displacementTextures);
        computeShader.SetTexture(CS_AssembleMaps, "_SpectrumTextures", spectrumTextures);
        computeShader.SetTexture(CS_AssembleMaps, "_SlopeTextures", slopeTextures);
        computeShader.Dispatch(CS_AssembleMaps, GRID_DIM, GRID_DIM, 1);

        displacementTextures.GenerateMips();
        slopeTextures.GenerateMips();

        waterMaterial.SetTexture("_DisplacementTextures", displacementTextures);
        waterMaterial.SetTexture("_SlopeTextures", slopeTextures);
    }

    void OnDisable(){
        if (waterMaterial != null){
            Destroy(waterMaterial);
            waterMaterial = null;
        }

        if (mesh != null){
            Destroy(mesh);
            mesh = null;
            vertices = null;
            normals = null;
        }

        Destroy(displacementTextures);
        Destroy(slopeTextures);
        Destroy(initialSpectrumTextures);
        Destroy(spectrumTextures);

        spectrumBuffer.Dispose();
    }
}
