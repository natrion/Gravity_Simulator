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

    public  List<Vector3> positions;
    public int spawnAmount = 20;
    public float SpacePerAmount = 1;
    void spawnPoints()
    {
        positions = new List<Vector3>();
        float totalSpaceRadius = Mathf.Pow((SpacePerAmount * spawnAmount) / ((4f / 3f) * Mathf.PI), 1f / 3f);
        for (int i = 0; i < spawnAmount; i++)
        {
            positions.Add(Random.onUnitSphere * Mathf.Pow( Random.RandomRange(0f, 1f),1f/3f) * totalSpaceRadius);
        }
    }
    public Mesh pointMesh;
    public Material pointMaterial;
    public float pointSize = 0.1f;
    void visualizatePositions()
    {
        /*
        //inicializating variables
        Matrix4x4[] pointsTRS = new Matrix4x4[positions.Count];
        int Poslen = pointsTRS.Length;
        Quaternion pointRot = Quaternion.EulerRotation(Vector3.zero);
        print(pointRot.x+ " "+ pointRot.y+ " "+ pointRot.z+" "+ pointRot.w);
        Vector3 pointSizeVector = Vector3.one * pointSize;
        //converting to positions Metrix
        for (int i = 0; i < pointsTRS.Length; i++)
        {
            Matrix4x4 transform = Matrix4x4.TRS(positions[i], pointRot, pointSizeVector);
            pointsTRS[i] = transform;
        }
        //drawing meshes
        Graphics.DrawMeshInstanced(pointMesh, 0, pointMaterial, pointsTRS, Poslen);
        */
        //declearing buffers
        int positionsNum = positions.Count;
        ComputeBuffer inPositionsBuffer = new ComputeBuffer(positionsNum, sizeof(float) * 3);
        inPositionsBuffer.SetData(positions);
        ComputeBuffer outPositionsBuffer = new ComputeBuffer(positionsNum, sizeof(float) * 3);
        int mainKernel = physicsCom.FindKernel("CSMain");
        ComputeBuffer outMetrixTransformBuffer = new ComputeBuffer(positionsNum, sizeof(float)*16);

        //seting buffers to shader
        physicsCom.SetBuffer(mainKernel, "positionsIn", inPositionsBuffer);
        physicsCom.SetBuffer(mainKernel, "positionsOut", outPositionsBuffer);
        physicsCom.SetBuffer(mainKernel, "MetrixTransforms", outMetrixTransformBuffer);
        //dispatch
        physicsCom.SetFloat("size", pointSize);

        //taking data from dispach
        physicsCom.Dispatch(mainKernel, Mathf.CeilToInt(positionsNum / 64f), 1, 1);
        posOut = new Vector3[positionsNum];
        outPositionsBuffer.GetData(posOut);

        Matrix4x4[] pointsTRS = new Matrix4x4[positionsNum];
        outMetrixTransformBuffer.GetData(pointsTRS);

        //drawing meshes
        Graphics.DrawMeshInstanced(pointMesh, 0, pointMaterial, pointsTRS, positionsNum);
    }
    public Vector3[] posOut;
    private void Start()
    {
   
    }
    void Update()
    {
        spawnPoints();
        visualizatePositions();
    }
}
