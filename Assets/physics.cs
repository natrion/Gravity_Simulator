using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class physics : MonoBehaviour
{
    public ComputeShader physicsCom;
    Vector3 EulerToNormal(Vector3 eulerAngles)
    {
        Quaternion rotation = Quaternion.Euler(eulerAngles); // Euler na Quaternion
        return rotation * Vector3.forward; // Aplikujeme rotáciu na vektor (0,0,1)
    }

    public Vector3[] positions;
    public int spawnAmount = 20;
    public float SpacePerAmount = 1;
    void spawnPoints()
    {
        positions = new Vector3[spawnAmount];
        float totalSpaceRadius = Mathf.Pow((SpacePerAmount * spawnAmount) / ((4f / 3f) * Mathf.PI), 1f / 3f);
        for (int i = 0; i < spawnAmount; i++)
        {
            positions[i] = Random.onUnitSphere * Mathf.Pow( Random.RandomRange(0f, 1f),1f/3f) * totalSpaceRadius;
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
        int positionsNum = positions.Length;
        ComputeBuffer inPositionsBuffer = new ComputeBuffer(positionsNum, sizeof(float) * 3);
        inPositionsBuffer.SetData(positions);
        ComputeBuffer outPositionsBuffer = new ComputeBuffer(positionsNum, sizeof(float) * 3);
        int mainKernel = physicsCom.FindKernel("CSMain");
        ComputeBuffer outMetrixTransformBuffer = new ComputeBuffer(positionsNum, sizeof(float)*16);

        //seting buffers to shader
        physicsCom.SetBuffer(mainKernel, "positionsIn", inPositionsBuffer);
        physicsCom.SetBuffer(mainKernel, "positionsOut", outPositionsBuffer);
        physicsCom.SetBuffer(mainKernel, "MetrixTransforms", outMetrixTransformBuffer);

        //setting data for dispach
        physicsCom.SetFloat("size", pointSize);
        physicsCom.SetFloat("GStrenght", GStrenght);
        physicsCom.SetFloat("pointMass", pointMass);
        physicsCom.SetFloat("frameLenght", Time.deltaTime);
        //dispatch
        physicsCom.Dispatch(mainKernel, Mathf.CeilToInt(positionsNum / 64f), 1, 1);
        
        //taking data from dispach
        positions = new Vector3[positionsNum];
        outPositionsBuffer.GetData(positions);

        Matrix4x4[] pointsTRS = new Matrix4x4[positionsNum];
        outMetrixTransformBuffer.GetData(pointsTRS);

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
