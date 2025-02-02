using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class physics : MonoBehaviour
{
    private List<Vector3> positions;
    public int spawnAmount = 20;
    public float SpacePerAmount = 1;
    void spawnPoints()
    {
        positions = new List<Vector3>();
        float totalSpaceRadius = Mathf.Pow((SpacePerAmount * spawnAmount) / ((4 / 3) * Mathf.PI), 1f / 3f);
        for (int i = 0; i < spawnAmount; i++)
        {
            Vector3 cubeRange = new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1));
            positions.Add(cubeRange.normalized * Random.Range(0, totalSpaceRadius));
        }
    }
    public Mesh pointMesh;
    public Material pointMaterial;

    void visualizatePositions()
    {
        //inicializating variables
        Matrix4x4[] pointsTRS = new Matrix4x4[positions.Count];
        int Poslen = pointsTRS.Length;
        Quaternion pointRot = Quaternion.EulerRotation(Vector3.zero);
        //converting to positions Metrix
        for (int i = 0; i < pointsTRS.Length; i++)
        {
            Matrix4x4 transform = Matrix4x4.TRS(positions[i], pointRot, Vector3.one);
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
