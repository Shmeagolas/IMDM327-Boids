using UnityEngine;


public class BoidsHCI : MonoBehaviour
{
    [Header("Pre-Run Settings")]
    public ComputeShader computeShader;
    public Material boidMaterial;
    public int count = 1024;
    public float boidSize = 1f;

    [Header("\nRuntime Settings")]

    public float neighborRadius = 2f;
    public float minSpeed = .5f, maxSpeed = 1f, separationDistance = .1f;
    public float separationWeight = 1f, alignmentWeight = 1f, cohesionWeight = 1f;
    public Color slowColor, fastColor;

    [Header("\nSuperboid Settings")]
    public float SuperBoidStrengthScalar = 50f;
    public float SuperBoidDistance = 5f;


    private ComputeBuffer boidBuffer;

    private int updateKernel;
    private int threadGroupSize = 256;

    private Mesh boidMesh;
    private int worldSizeX = 16, worldSizeY = 9;

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

        updateKernel = computeShader.FindKernel("CSMain");



        boidBuffer = new ComputeBuffer(count, sizeof(float) * 4);

        Boid[] boids = new Boid[count];
        for (int i = 0; i < count; i++)
        {
            boids[i] = new Boid
            {
                pos = new Vector2(Random.Range(-1f * worldSizeX, worldSizeX), Random.Range(-1f * worldSizeY, worldSizeY)),
                vel = Random.insideUnitCircle
            };
        }


        boidBuffer.SetData(boids);


        computeShader.SetInt("_NumBoids", count);
        computeShader.SetFloat("_WorldX", (float)worldSizeX);
        computeShader.SetFloat("_WorldY", (float)worldSizeY);

        computeShader.SetBuffer(updateKernel, "Boids", boidBuffer);
    }

    void Update()
    {
        bool leftDown = Input.GetMouseButton(0);
        bool rightDown = Input.GetMouseButton(1);

        if (leftDown || rightDown)
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            float x = (mouseScreenPos.x / Screen.width - 0.5f) * 2f * worldSizeX;
            float y = (mouseScreenPos.y / Screen.height - 0.5f) * 2f * worldSizeY;
            Vector2 mouseWorldPos = new Vector2(x, y);

            computeShader.SetVector("_SuperBoidPos", mouseWorldPos);
            computeShader.SetVector("_SuperBoidVel", Vector2.zero);
            computeShader.SetFloat("_SuperBoidStrengthScalar", SuperBoidStrengthScalar);
            computeShader.SetFloat("_SuperBoidDistance", SuperBoidDistance);
        }

        computeShader.SetInt("_SuperBoidState", leftDown ? 1 : rightDown ? -1 : 0);

        if (computeShader != null && boidBuffer != null)
        {
            computeShader.SetFloat("_DeltaTime", Time.deltaTime);
            computeShader.SetFloat("_NeighborRadius", neighborRadius);
            computeShader.SetFloat("_MaxSpeed", maxSpeed);
            computeShader.SetFloat("_MinSpeed", minSpeed);
            computeShader.SetFloat("_CohesionWeight", cohesionWeight);
            computeShader.SetFloat("_SeparationWeight", separationWeight);
            computeShader.SetFloat("_AlignmentWeight", alignmentWeight);
            computeShader.SetFloat("_SeparationDistance", separationDistance);

            int groups = (count + threadGroupSize - 1) / threadGroupSize;
            computeShader.Dispatch(updateKernel, groups, 1, 1);

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
        boidMaterial.SetFloat("_MinSpeed", minSpeed);
        boidMaterial.SetFloat("_MaxSpeed", maxSpeed);
        boidMaterial.SetColor("_Color1", slowColor);
        boidMaterial.SetColor("_Color2", fastColor);

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

    }

    private Mesh CreateQuad()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.01f * boidSize, -0.01f * boidSize, 0),
            new Vector3(0.01f * boidSize, -0.01f * boidSize, 0),
            new Vector3(-0.01f * boidSize, 0.01f * boidSize, 0),
            new Vector3(0.01f * boidSize, 0.01f * boidSize, 0)
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
