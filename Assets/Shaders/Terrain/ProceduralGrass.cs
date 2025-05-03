using UnityEngine;
using UnityEngine.UIElements;

public class ProceduralGrass : MonoBehaviour
{
    public ComputeShader computeShader;
    private Mesh terrainMesh;
    public Mesh grassMesh;
    public Material material;

    public float scale = 0.1f;
    public Vector2 minMaxBladeHeight = new Vector2(0.5f, 1.5f);

    private GraphicsBuffer terrainTriangleBuffer;
    private GraphicsBuffer terrainVertexBuffer;

    private GraphicsBuffer transformMatrixBuffer;
    private GraphicsBuffer grassTriangleBuffer;
    private GraphicsBuffer grassVertexBuffer;
    private GraphicsBuffer grassUVBuffer;

    private Bounds bounds;

    private int kernel;
    private uint threadGroupSize;
    private int terrainTriangleCount = 0;


    void Start() {
        kernel = computeShader.FindKernel("TerrainOffsets");

        terrainMesh = GetComponent<MeshFilter>().sharedMesh;

        Vector3[] terrainVertices = terrainMesh.vertices; // Vector3 representing the object-space position of a vertex
        terrainVertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, terrainVertices.Length, sizeof(float) * 3);
        terrainVertexBuffer.SetData(terrainVertices);
        computeShader.SetBuffer(kernel, "_TerrainPositions", terrainVertexBuffer);

        int[] terrainTriangles = terrainMesh.triangles;
        terrainTriangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, terrainTriangles.Length, sizeof(int));
        terrainTriangleBuffer.SetData(terrainTriangles);
        computeShader.SetBuffer(kernel, "_TerrainTriangles", terrainTriangleBuffer);
        terrainTriangleCount = terrainTriangles.Length / 3;


        Vector3[] grassVertices = grassMesh.vertices;
        grassVertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, grassVertices.Length, sizeof(float) * 3);
        grassVertexBuffer.SetData(grassVertices);

        int[] grassTriangles = grassMesh.triangles;
        grassTriangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, grassTriangles.Length, sizeof(int));
        grassTriangleBuffer.SetData(grassTriangles);

        Vector2[] grassUVs = grassMesh.uv;
        grassUVBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, grassUVs.Length, sizeof(float) * 2);
        grassUVBuffer.SetData(grassUVs);

        transformMatrixBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, terrainTriangleCount, sizeof(float) * 16);
        computeShader.SetBuffer(kernel, "_TransformMatrices", transformMatrixBuffer);

        bounds = terrainMesh.bounds;
        bounds.center += transform.position;
        bounds.Expand(minMaxBladeHeight.y);

        RunComputeShader();
    }

    void RunComputeShader() {

        computeShader.SetMatrix("_TerrainObjectToWorld", transform.localToWorldMatrix);
        computeShader.SetInt("_TerrainTriangleCount", terrainTriangleCount);
        computeShader.SetVector("_MinMaxBladeHeight", minMaxBladeHeight);
        computeShader.SetFloat("_Scale", scale);

        computeShader.GetKernelThreadGroupSizes(kernel, out threadGroupSize, out _, out _);
        int threadGroups = Mathf.CeilToInt(terrainTriangleCount / threadGroupSize);
        computeShader.Dispatch(kernel, threadGroups, 1, 1);

    }

    void Update() {

        RenderParams rp = new RenderParams(material);
        rp.worldBounds = bounds;
        rp.matProps = new MaterialPropertyBlock();
        rp.matProps.SetBuffer("_TransformMatrices", transformMatrixBuffer);
        rp.matProps.SetBuffer("_Positions", grassVertexBuffer);
        rp.matProps.SetBuffer("_UVs", grassUVBuffer);
        Graphics.RenderPrimitivesIndexed(rp, MeshTopology.Triangles, grassTriangleBuffer, grassTriangleBuffer.count, instanceCount: terrainTriangleCount);

    }

    void OnDestroy() {
        terrainTriangleBuffer.Dispose();
        terrainVertexBuffer.Dispose();
        transformMatrixBuffer.Dispose();
        grassTriangleBuffer.Dispose();
        grassVertexBuffer.Dispose();
        grassUVBuffer.Dispose();
    }
}
