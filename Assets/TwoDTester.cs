using UnityEngine;
using UnityEngine.Rendering;

public class TwoDTester : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material boidMaterial;
    //public int numBoids = 1024;
    public int numCellsX = 32, numCellsY = 32;
    public int maxBoidsPerCell = 32;
    private int worldSizeX = 16, worldSizeY = 9;
    public float neighborRadius = 1f, maxSpeed = 1f, separationDistance = .05f;
    public float separationWeight = 1f, alignmentWeight = 1f, cohesionWeight = 1f;

    private ComputeBuffer boidBuffer;
    private ComputeBuffer cellIndexesBuffer;
    private ComputeBuffer gridCountersBuffer;
    private ComputeBuffer gridBuffer;

    private int clearKernel, assignKernel, updateKernel;
    private int threadGroupSize = 64;

    public int count = 1024;
    private int numCells;

    private Mesh boidMesh;

    struct Boid
    {
        public Vector2 pos;
        public Vector2 vel;
    }

    void Start()
    {
        boidMesh = CreateQuad();
        InitializeBoids();
    }

    private void InitializeBoids()
    {

        clearKernel = computeShader.FindKernel("ClearGrid");
        assignKernel = computeShader.FindKernel("AssignBoidCells");
        updateKernel = computeShader.FindKernel("CSMain");

        numCells = numCellsX * numCellsY;

        boidBuffer = new ComputeBuffer(count, sizeof(float) * 4);
        cellIndexesBuffer = new ComputeBuffer(count, sizeof(uint));
        gridCountersBuffer = new ComputeBuffer(numCells, sizeof(uint));
        gridBuffer = new ComputeBuffer(numCells * maxBoidsPerCell, sizeof(uint));

        Boid[] boids = new Boid[count];
        for (int i = 0; i < count; i++)
        {
            boids[i] = new Boid
            {
                pos = new Vector2(Random.Range(-1f * worldSizeX, worldSizeX), Random.Range(-1f * worldSizeY, worldSizeY)),
                vel = Random.insideUnitCircle
            };
        }

        float cellSizeX = worldSizeX / (float)numCellsX;
        float cellSizeY = worldSizeY / (float)numCellsY;

        if (neighborRadius < cellSizeX || neighborRadius < cellSizeY)
        {
            Debug.LogWarning($"neighbor radius of {neighborRadius} is less than cellsizeX {cellSizeX} or cellsizeY {cellSizeY}");
        }


        boidBuffer.SetData(boids);


        computeShader.SetInt("_NumBoids", count);
        computeShader.SetInt("_NumCells", numCells);
        computeShader.SetInt("_NumCellsX", numCellsX);
        computeShader.SetInt("_NumCellsY", numCellsY);
        computeShader.SetInt("_MaxCellBoids", maxBoidsPerCell);

        computeShader.SetFloat("_CellSizeX", cellSizeX);
        computeShader.SetFloat("_CellSizeY", cellSizeY);
        computeShader.SetFloat("_WorldX", (float)worldSizeX);
        computeShader.SetFloat("_WorldY", (float)worldSizeY);




        computeShader.SetBuffer(clearKernel, "GridCounters", gridCountersBuffer);

        computeShader.SetBuffer(assignKernel, "Boids", boidBuffer);
        computeShader.SetBuffer(assignKernel, "BoidCellIndexes", cellIndexesBuffer);
        computeShader.SetBuffer(assignKernel, "GridCounters", gridCountersBuffer);
        computeShader.SetBuffer(assignKernel, "Grid", gridBuffer);

        computeShader.SetBuffer(updateKernel, "Boids", boidBuffer);
        computeShader.SetBuffer(updateKernel, "BoidCellIndexes", cellIndexesBuffer);
        computeShader.SetBuffer(updateKernel, "GridCounters", gridCountersBuffer);
        computeShader.SetBuffer(updateKernel, "Grid", gridBuffer);
    }

    void Update()
    {
        if (computeShader != null && boidBuffer != null)
        {
            computeShader.SetFloat("_DeltaTime", Time.deltaTime);
            computeShader.SetFloat("_NeighborRadius", neighborRadius);
            computeShader.SetFloat("_MaxSpeed", maxSpeed);
            computeShader.SetFloat("_CohesionWeight", cohesionWeight);
            computeShader.SetFloat("_SeparationWeight", separationWeight);
            computeShader.SetFloat("_AlignmentWeight", alignmentWeight);
            computeShader.SetFloat("_SeparationDistance", separationDistance);

            int clearGroups = Mathf.CeilToInt(numCells / (float)threadGroupSize);
            int boidGroups = Mathf.CeilToInt(count / (float)threadGroupSize);

            computeShader.Dispatch(clearKernel, clearGroups, 1, 1);
            computeShader.Dispatch(assignKernel, threadGroupSize, 1, 1);
            computeShader.Dispatch(updateKernel, threadGroupSize, 1, 1);

        }
    }

    void OnRenderObject()
    {
        if (boidMaterial == null || boidBuffer == null || boidMesh == null || Camera.current == null)
        {
            return;
        }


        if (Camera.current.cameraType != CameraType.Game && Camera.current.cameraType != CameraType.SceneView)
        {
            return;
        }

        boidMaterial.SetBuffer("_Boids", boidBuffer);

        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 20f);

        boidMaterial.SetPass(0);

        Graphics.DrawMeshInstancedProcedural(
            boidMesh,
            0,
            boidMaterial,
            bounds,
            count
        );
    }

    void OnDestroy()
    {
        if (boidBuffer != null) boidBuffer.Release();
        if (cellIndexesBuffer != null) cellIndexesBuffer.Release();
        if (gridCountersBuffer != null) gridCountersBuffer.Release();
        if (gridBuffer != null) gridBuffer.Release();
    }

    private Mesh CreateQuad()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.05f, -0.05f, 0),
            new Vector3(0.05f, -0.05f, 0),
            new Vector3(-0.05f, 0.05f, 0),
            new Vector3(0.05f, 0.05f, 0)
        };

        int[] tris = new int[]
        {
            0, 2, 1,
            2, 3, 1
        };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateBounds();

        return mesh;
    }
}
