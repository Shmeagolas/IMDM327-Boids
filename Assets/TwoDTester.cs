using UnityEngine;

public class TwoDTester : MonoBehaviour
{
    private ComputeBuffer boidBuffer;
    public ComputeShader computeShader;
    private int kernel;
    private int threadGroupSize = 64;

    private int count = 1024;

    [Header("Rendering")]
    public Material boidMaterial;
    private Mesh boidMesh;

    // Using this simple struct definition for the buffer data
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
        Boid[] boids = new Boid[count];
        for (int i = 0; i < count; i++)
        {
            boids[i] = new Boid
            {
                pos = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f)),
                vel = Random.insideUnitCircle
            };
        }

        // IMPORTANT: Stride must match the struct (4 floats * 4 bytes/float = 16 bytes)
        int stride = sizeof(float) * 4;
        boidBuffer = new ComputeBuffer(count, stride);
        boidBuffer.SetData(boids);

        // Setup Compute Shader
        kernel = computeShader.FindKernel("CSMain");
        computeShader.SetBuffer(kernel, "Boids", boidBuffer);
    }

    void Update()
    {
        if (computeShader != null && boidBuffer != null)
        {
            computeShader.SetFloat("_DeltaTime", Time.deltaTime);
            int groups = Mathf.CeilToInt(count / (float)threadGroupSize);

            // Dispatch the simulation
            computeShader.Dispatch(kernel, groups, 1, 1);
        }
    }

    // FIX: Reverting to OnRenderObject, the correct injection point for Built-in Pipeline
    void OnRenderObject()
    {
        // Safety check
        if (boidMaterial == null || boidBuffer == null || boidMesh == null || Camera.current == null)
        {
            return;
        }

        // Only draw if the current camera is a Game or Scene View camera
        if (Camera.current.cameraType != CameraType.Game && Camera.current.cameraType != CameraType.SceneView)
        {
            return;
        }

        // Pass the buffer to the material
        boidMaterial.SetBuffer("_Boids", boidBuffer);

        // CRITICAL: Use massive bounds to guarantee no geometry is culled by bounds check.
        Bounds hugeBounds = new Bounds(Vector3.zero, Vector3.one * 200000f);

        // CRITICAL FIX: Explicitly set the pass
        boidMaterial.SetPass(0);

        // Draw call relies on OnRenderObject timing, massive bounds, and shader settings (ZTest Always)
        Graphics.DrawMeshInstancedProcedural(
            boidMesh,
            0,
            boidMaterial,
            hugeBounds,
            count
        );
    }

    void OnDestroy()
    {
        // Resource release
        if (boidBuffer != null) boidBuffer.Release();
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
