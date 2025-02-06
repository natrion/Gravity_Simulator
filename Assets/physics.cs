using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class physics : MonoBehaviour
{
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
    }
    public ComputeShader physicsCom;
    Vector3 EulerToNormal(Vector3 eulerAngles)
    {
        Quaternion rotation = Quaternion.Euler(eulerAngles); // Euler na Quaternion
        return rotation * Vector3.forward; // Aplikujeme rotáciu na vektor (0,0,1)
    }

    private Particle[] points;
    public int spawnAmount = 20;
    public float SpacePerAmount = 1;
    void spawnPoints()
    {
        points = new Particle[spawnAmount];
        float totalSpaceRadius = Mathf.Pow((SpacePerAmount * spawnAmount) / ((4f / 3f) * Mathf.PI), 1f / 3f);
        for (int i = 0; i < spawnAmount; i++)
        {
            Particle newpoint = new Particle();
            newpoint.position = Random.onUnitSphere * Mathf.Pow(Random.RandomRange(0f, 1f), 1f / 3f) * totalSpaceRadius;
            newpoint.velocity = Random.onUnitSphere * Random.RandomRange(0, 0.2f);
            points[i] = newpoint;
        }
    }
    public Mesh pointMesh;
    public Material pointMaterial;
    public float pointSize = 0.1f;
    public float pointMass = 1;
    public float GStrenght = 1;


    void visualizatePositions()
    {

        //declearing buffers
        int pointStructuresize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Particle));
        int positionsNum = points.Length;
        ComputeBuffer pointsInBuffer = new ComputeBuffer(positionsNum, pointStructuresize);
        pointsInBuffer.SetData(points);
        ComputeBuffer pointsOutBuffer = new ComputeBuffer(positionsNum, pointStructuresize);
        int mainKernel = physicsCom.FindKernel("CSMain");
        ComputeBuffer outMetrixTransformBuffer = new ComputeBuffer(positionsNum, sizeof(float)*16);

        //seting buffers to shader
        physicsCom.SetBuffer(mainKernel, "pointsIn", pointsInBuffer);
        physicsCom.SetBuffer(mainKernel, "pointsOut", pointsOutBuffer);
        physicsCom.SetBuffer(mainKernel, "MetrixTransforms", outMetrixTransformBuffer);

        //setting data for dispach
        physicsCom.SetFloat("size", pointSize);
        physicsCom.SetFloat("GStrenght", GStrenght);
        physicsCom.SetFloat("pointMass", pointMass);
        physicsCom.SetFloat("frameLenght", Time.deltaTime);
        //dispatch
        physicsCom.Dispatch(mainKernel, Mathf.CeilToInt(positionsNum / 64f), 1, 1);
        
        //taking data from dispach
        points = new Particle[positionsNum];
        pointsOutBuffer.GetData(points);
        pointsOutBuffer.Release();
        Matrix4x4[] pointsTRS = new Matrix4x4[positionsNum];
        outMetrixTransformBuffer.GetData(pointsTRS);
        outMetrixTransformBuffer.Release();

        //drawing meshes
        Graphics.DrawMeshInstanced(pointMesh, 0, pointMaterial, pointsTRS, positionsNum);
    }
    private void Start()
    {
        spawnPoints();
    }
    void Update()
    {
        visualizatePositions();
    }
}
