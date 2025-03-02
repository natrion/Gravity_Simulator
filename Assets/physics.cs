using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class physics : MonoBehaviour
{
    [Header("Basic Setup")]
    [SerializeField] private ComputeShader physicsCom;
    [SerializeField] private Mesh pointMesh;
    [SerializeField] private Material pointMaterial;
    [Header("Spawning Points Sphere")]
    [SerializeField] private bool spawnSpehere;
    [SerializeField] private int spawnAmount = 20;
    [SerializeField] private float SpacePerAmount = 1;
    [Header("Spawning Points Perlin Cube")]
    [SerializeField] private bool spawnPerCube;
    [SerializeField] private float perlinFreqency = 0.5f;
    [SerializeField] private float perlinCubeLenghtPerPoint = 2;
    [SerializeField] private int sideNum = 30;
    [Header("Spawning Other")]
    [SerializeField] private bool Spawn2Points;
    [SerializeField] private float distanceOfTwoPoints;
    [SerializeField] private float velocitiOfTwoPoints;
    [Header("physics")]
    [SerializeField] private float pointSize = 0.1f;
    [SerializeField] private float pointMass = 1;
    [SerializeField] private float GStrenght = 1;
    [Range(0, 1f)]
    [SerializeField] private float bounceFrictionLoss = 1;
    [Header("simulation")]
    [SerializeField] private int frameCal = 1;
    [Range(0f, 2f)]
    [SerializeField] private float framecalSpeedMul = 1;
    [SerializeField] private int NUM_THREADS = 64;
    [System.Serializable]
    struct particleGroup
    {
        public int id;
        public Vector3 position;
        public Vector3 velocity;
        public float mass;
    };
    [System.Serializable]
    struct Particle
    {
        public int GroupId;
        public Vector3 position;
    };
    [SerializeField] private Particle[] points;
    [SerializeField] private particleGroup[] pointGroups;
    private Vector2Int[] groupInfs;

    Vector3 EulerToNormal(Vector3 eulerAngles)
    {
        Quaternion rotation = Quaternion.Euler(eulerAngles); // Euler na Quaternion
        return rotation * Vector3.forward; // Aplikujeme rotáciu na vektor (0,0,1)
    }
    
    void spawnPointsSpere()
    {

        pointGroups = new particleGroup[spawnAmount];
        float totalSpaceRadius = Mathf.Pow((SpacePerAmount * spawnAmount) / ((4f / 3f) * Mathf.PI), 1f / 3f);
        for (int i = 0; i < spawnAmount; i++)
        {
            Vector3 pos = Random.onUnitSphere * Mathf.Pow(Random.RandomRange(0f, 1f), 1f / 3f) * totalSpaceRadius;

            particleGroup newpoint = new particleGroup();
            newpoint.position = pos;
            newpoint.velocity = Random.onUnitSphere * Random.RandomRange(0, 0.01f);
            pointGroups[i ] = newpoint;                            

        }
    }
    
    void SpawnPelinCube ()
    {
        List<particleGroup> pointsList = new List<particleGroup>();
        float sideLenght = perlinCubeLenghtPerPoint * (float)sideNum;
        Vector3 seed = new Vector3(Random.RandomRange(-10000, 10000), Random.RandomRange(-10000, 10000), Random.RandomRange(-10000, 10000));
        for (float x = 0; x < sideLenght; x+= perlinCubeLenghtPerPoint)
        {
            for (float y = 0; y < sideLenght; y+= perlinCubeLenghtPerPoint)
            {
                for (float z = 0; z < sideLenght; z+= perlinCubeLenghtPerPoint)
                {
                    Vector3 pos = new Vector3(x, y, z);
                    float perNum = perlinNoise.get3DPerlinNoise(pos+ seed, perlinFreqency);
                    if (perNum>0.5)
                    {
                        particleGroup newpoint = new particleGroup();
                        float maxPosVar = Mathf.Max(0, perlinCubeLenghtPerPoint - pointSize);
                        newpoint.position = pos+ new Vector3(Random.RandomRange(-maxPosVar, maxPosVar), Random.RandomRange(-maxPosVar, maxPosVar), Random.RandomRange(-maxPosVar, maxPosVar));
                        newpoint.velocity = Random.onUnitSphere * Random.RandomRange(0, 1f);
                        pointsList.Add(newpoint);
                    }
                }
            }
        }
        pointGroups = new particleGroup[pointsList.Count];
        for (int i = 0; i < pointsList.Count; i++)
        {
            pointGroups[i] = pointsList[i];
        }
    }
    void spawnTwoPoints()
    {
        pointGroups = new particleGroup[2];
        pointGroups[0] = new particleGroup();
        pointGroups[0].velocity = Vector3.right * -velocitiOfTwoPoints/2;
        pointGroups[0].position = Vector3.right * distanceOfTwoPoints/2;
        pointGroups[1] = new particleGroup();
        pointGroups[1].position = Vector3.left * distanceOfTwoPoints/2;
        pointGroups[1].velocity = Vector3.left * -velocitiOfTwoPoints/2;
    }


    void visualizatePositions()
    {
        for (int i = 0; i < frameCal; i++)//repeating calculatin forfaster simulation
        {

            //setting data 
            Vector3[] debugInfs = new Vector3[100];
            debugInfBuffer.SetData(debugInfs);

            pointGroupsInBuffer.SetData(pointGroups);

            pointsInBuffer.SetData(points);
            //setting data for dispach
            physicsCom.SetFloat("NUM_THREADS", NUM_THREADS);
            physicsCom.SetFloat("size", pointSize);
            physicsCom.SetFloat("GStrenght", GStrenght);
            physicsCom.SetFloat("pointMass", pointMass);
            physicsCom.SetFloat("frameLenght", Time.deltaTime);
            physicsCom.SetFloat("bounceFrictionLoss", bounceFrictionLoss); 
            physicsCom.SetFloat("framecalSpeedMul", framecalSpeedMul);
            int numthreadCeil = Mathf.CeilToInt((float)positionsNum / (float)NUM_THREADS);
            physicsCom.SetFloat("numthreadCeil", numthreadCeil);
            //dispatch
            physicsCom.Dispatch(mainKernel, numthreadCeil*2, 1, 1);

            //taking data from dispach

            points = new Particle[positionsNum];
            pointsOutBuffer.GetData(points);

            pointGroups = new particleGroup[positionsNum];
            pointGroupsoutBuffer.GetData(pointGroups);

            Matrix4x4[] pointsTRS = new Matrix4x4[positionsNum];
            outMetrixTransformBuffer.GetData(pointsTRS);

            debugInfBuffer.GetData(debugInfs);
            debugInfBuffer.SetCounterValue(0);
            //debugkinData
            for (int n = 0; n < debugInfs.Length; n++) if (Vector3.zero != debugInfs[n]) print(debugInfs[n]);






            print("---------------------------------------");
            //drawing meshes

            if (i == frameCal-1) Graphics.DrawMeshInstanced(pointMesh, 0, pointMaterial, pointsTRS, positionsNum);
        }

        
    }
    ComputeBuffer pointGroupsInBuffer;
    ComputeBuffer pointGroupsoutBuffer;

    ComputeBuffer groupInfsInBuffer;
    ComputeBuffer groupInfsoutBuffer;

    ComputeBuffer pointsInBuffer;
    ComputeBuffer pointsOutBuffer;

    ComputeBuffer outMetrixTransformBuffer;

    ComputeBuffer debugInfBuffer;
    int mainKernel;
    int pointStructuresize;
    int pointGrupStructuresize;
    int positionsNum;
    void prepareBuffers()
    {
        pointStructuresize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Particle));
        pointGrupStructuresize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(particleGroup));

        positionsNum = points.Length;
        //declearing buffers
        pointGroupsInBuffer = new ComputeBuffer(positionsNum, pointGrupStructuresize);
        pointGroupsoutBuffer = new ComputeBuffer(positionsNum, pointGrupStructuresize);

        pointsInBuffer = new ComputeBuffer(positionsNum, pointStructuresize);
        pointsOutBuffer = new ComputeBuffer(positionsNum, pointStructuresize);

        debugInfBuffer = new ComputeBuffer(100, sizeof(float)*3, ComputeBufferType.Append);
        debugInfBuffer.SetCounterValue(0);

        outMetrixTransformBuffer = new ComputeBuffer(positionsNum, sizeof(float) * 16);

        mainKernel = physicsCom.FindKernel("CSMain");

        //setting buffers to a compute shader
        //physicsCom.SetBuffer(mainKernel, "particleGroupsIn", groupInfsInBuffer);
        //physicsCom.SetBuffer(mainKernel, "particleGroupsOut", groupInfsoutBuffer);

        physicsCom.SetBuffer(mainKernel, "particleGroupsIn", pointGroupsInBuffer);
        physicsCom.SetBuffer(mainKernel, "particleGroupsOut", pointGroupsoutBuffer);

        physicsCom.SetBuffer(mainKernel, "pointsIn", pointsInBuffer);
        physicsCom.SetBuffer(mainKernel, "pointsOut", pointsOutBuffer);

        physicsCom.SetBuffer(mainKernel, "MetrixTransforms", outMetrixTransformBuffer);

        physicsCom.SetBuffer(mainKernel, "debugInf", debugInfBuffer);

    }
    void generaeOtherdata()
    {
        points = new Particle[pointGroups.Length];
        for (int i = 0; i < points.Length; i++)
        {
            Particle Particle = points[i];

            pointGroups[i].id = i;
            pointGroups[i].mass = pointMass;

            Particle.GroupId = i;
            Particle.position = Vector3.zero;

            points[i] = Particle;

        }                        
               
    }
    bool done = false;
    void Start()
    {

        if (spawnPerCube == true) SpawnPelinCube();

        if(spawnSpehere == true)spawnPointsSpere();

        if(Spawn2Points == true) spawnTwoPoints();

        generaeOtherdata();

        prepareBuffers();

        done = true;
    }
    void Update()
    {
        if(done == true)visualizatePositions();
    }
}
