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
    struct particleGroup
    {
        public int id;
        public Vector3 position;
        public Vector3 velocity;
        public float mass;
    };
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
    };
    private Particle[] points;
    private particleGroup[] pointGroups;
    private Vector2Int[] groupInfs;

    Vector3 EulerToNormal(Vector3 eulerAngles)
    {
        Quaternion rotation = Quaternion.Euler(eulerAngles); // Euler na Quaternion
        return rotation * Vector3.forward; // Aplikujeme rotáciu na vektor (0,0,1)
    }
    
    void spawnPointsSpere()
    {

        points = new Particle[spawnAmount];
        float totalSpaceRadius = Mathf.Pow((SpacePerAmount * spawnAmount) / ((4f / 3f) * Mathf.PI), 1f / 3f);
        for (int i = 0; i < spawnAmount; i++)
        {
            Vector3 pos = Random.onUnitSphere * Mathf.Pow(Random.RandomRange(0f, 1f), 1f / 3f) * totalSpaceRadius;           
                    
            Particle newpoint = new Particle();
            newpoint.position = pos;
            newpoint.velocity = Random.onUnitSphere * Random.RandomRange(0, 0.01f);
            points[i ] = newpoint;                            

        }
    }
    
    void SpawnPelinCube ()
    {
        List<Particle> pointsList = new List<Particle>();
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
                        Particle newpoint = new Particle();
                        float maxPosVar = Mathf.Max(0, perlinCubeLenghtPerPoint - pointSize);
                        newpoint.position = pos+ new Vector3(Random.RandomRange(-maxPosVar, maxPosVar), Random.RandomRange(-maxPosVar, maxPosVar), Random.RandomRange(-maxPosVar, maxPosVar));
                        newpoint.velocity = Random.onUnitSphere * Random.RandomRange(0, 1f);
                        pointsList.Add(newpoint);
                    }
                }
            }
        }
        points = new Particle[pointsList.Count];
        for (int i = 0; i < pointsList.Count; i++)
        {
            points[i] = pointsList[i];
        }
    }
    void spawnTwoPoints()
    {
        points = new Particle[2];
        points[0] = new Particle();
        points[0].velocity = Vector3.right * -0.1f;
        points[0].position = Vector3.right * 0.5f;
        points[1] = new Particle();
        points[1].position = Vector3.left * 0.5f;
        points[1].velocity = Vector3.left * -0.1f;
    }


    void visualizatePositions()
    {
        for (int i = 0; i < frameCal; i++)//repeating calculatin forfaster simulation
        {

            //setting data 
            pointGroupsInBuffer.SetData(pointGroups);

            groupInfsInBuffer.SetData(groupInfs);

            pointsInBuffer.SetData(points);

            //setting data for dispach
            physicsCom.SetFloat("size", pointSize);
            physicsCom.SetFloat("GStrenght", GStrenght);
            physicsCom.SetFloat("pointMass", pointMass);
            physicsCom.SetFloat("frameLenght", Time.deltaTime);
            physicsCom.SetFloat("bounceFrictionLoss", bounceFrictionLoss); 
            physicsCom.SetFloat("framecalSpeedMul", framecalSpeedMul);

            //dispatch
            physicsCom.Dispatch(mainKernel, Mathf.CeilToInt(positionsNum / 128f), 1, 1);

            //taking data from dispach

            points = new Particle[positionsNum];
            pointsOutBuffer.GetData(points);

            points = new Particle[positionsNum];
            pointsOutBuffer.GetData(points);

            points = new Particle[positionsNum];
            pointsOutBuffer.GetData(points);

            Matrix4x4[] pointsTRS = new Matrix4x4[positionsNum];
            outMetrixTransformBuffer.GetData(pointsTRS);
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

        groupInfsInBuffer = new ComputeBuffer(positionsNum, sizeof(int) * 2);
        groupInfsoutBuffer = new ComputeBuffer(positionsNum, sizeof(int) * 2);

        pointsInBuffer = new ComputeBuffer(positionsNum, pointStructuresize);
        pointsOutBuffer = new ComputeBuffer(positionsNum, pointStructuresize);

        outMetrixTransformBuffer = new ComputeBuffer(positionsNum, sizeof(float) * 16);

        mainKernel = physicsCom.FindKernel("CSMain");

        //setting buffers to a compute shader
        physicsCom.SetBuffer(mainKernel, "particleGroupsIn", groupInfsInBuffer);
        physicsCom.SetBuffer(mainKernel, "particleGroupsOut", groupInfsoutBuffer);

        physicsCom.SetBuffer(mainKernel, "particleGroupsIn", pointGroupsInBuffer);
        physicsCom.SetBuffer(mainKernel, "particleGroupsOut", pointGroupsoutBuffer);

        physicsCom.SetBuffer(mainKernel, "pointsIn", pointsInBuffer);
        physicsCom.SetBuffer(mainKernel, "pointsOut", pointsOutBuffer);

        physicsCom.SetBuffer(mainKernel, "MetrixTransforms", outMetrixTransformBuffer);

    }
    void generaeOtherdata()
    {
        pointGroups = new particleGroup[points.Length];
        groupInfs = new Vector2Int[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            Particle Particle = points[i];

            particleGroup pointGroup = new particleGroup();
            pointGroup.id = i;
            pointGroup.position = Particle.position;
            pointGroup.velocity = Particle.velocity;
            pointGroup.mass = pointMass;

            groupInfs[i] =  new Vector2Int(i,i);

        }     
        
            
               
    }
    bool done = false;
    void Start()
    {

        if (spawnPerCube == true) SpawnPelinCube();

        if(spawnSpehere == true)spawnPointsSpere();

        if(Spawn2Points == true) spawnTwoPoints();

        prepareBuffers();

        generaeOtherdata();
        done = true;
    }
    void Update()
    {
        if(done == true)visualizatePositions();
    }
}
