using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class physics : MonoBehaviour
{
    Vector3 EulerToNormal(Vector3 eulerAngles)
    {
        Quaternion rotation = Quaternion.Euler(eulerAngles); // Euler na Quaternion
        return rotation * Vector3.forward; // Aplikujeme rot�ciu na vektor (0,0,1)
    }

    private List<Vector3> positions;
    public int spawnAmount = 20;
    public float SpacePerAmount = 1;
    void spawnPoints()
    {
        positions = new List<Vector3>();
        float totalSpaceRadius = Mathf.Pow((SpacePerAmount * spawnAmount) / ((4f / 3f) * Mathf.PI), 1f / 3f);
        for (int i = 0; i < spawnAmount; i++)
        {
            positions.Add(Random.onUnitSphere * Random.RandomRange(0f, totalSpaceRadius));
        }
    }
    public Mesh pointMesh;
    public Material pointMaterial;
    public float pointSize = 0.1f;
    void visualizatePositions()
    {
        //inicializating variables
        Matrix4x4[] pointsTRS = new Matrix4x4[positions.Count];
        int Poslen = pointsTRS.Length;
        Quaternion pointRot = Quaternion.EulerRotation(Vector3.zero);
        Vector3 pointSizeVector = Vector3.one * pointSize;
        //converting to positions Metrix
        for (int i = 0; i < pointsTRS.Length; i++)
        {
            Matrix4x4 transform = Matrix4x4.TRS(positions[i], pointRot, pointSizeVector);
            pointsTRS[i] = transform;
        }
        //drawing meshes
        Graphics.DrawMeshInstanced(pointMesh, 0, pointMaterial, pointsTRS, Poslen);
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
