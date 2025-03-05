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
    [SerializeField] private float pointMass = 0.1f;
    [SerializeField] private float pushStrenght = 0.001f;
    [SerializeField] private float frictionStrenght = 1;
    [SerializeField] private float GStrenght = 1;
    [Header("simulation")]
    [SerializeField] private int frameCal = 1;
    [Range(0f, 2f)]
    [SerializeField] private float framecalSpeedMul = 1;
    [SerializeField] private int NUM_OF_THREADS = 2;
    //[System.Serializable]
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
    };
    //[SerializeField]
    private Particle[] points;

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
                        //newpoint.velocity = Random.onUnitSphere * Random.RandomRange(0, 1f);
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
            //putting data to buffers
         
            pointsInBuffer.SetData(points);

            //setting data for dispach
            physicsCom.SetFloat("size", pointSize);
            physicsCom.SetFloat("pointMass", pointMass);

            physicsCom.SetFloat("GStrenght", GStrenght); 
            physicsCom.SetFloat("pushStrenght", pushStrenght);
            physicsCom.SetFloat("frictionStrenght", frictionStrenght);

            physicsCom.SetFloat("framecalSpeedMul", framecalSpeedMul);

            physicsCom.SetFloat("NUM_OF_THREADS", NUM_OF_THREADS);
            physicsCom.SetFloat("frameLenght", Time.deltaTime);

            //dispatch
            physicsCom.Dispatch(mainKernel, Mathf.CeilToInt(positionsNum / 128f), 1, 1);

            //taking data from dispach
            points = new Particle[positionsNum];
            pointsOutBuffer.GetData(points);
            outMetrixTransformBuffer.GetData(pointsTRS);
            //drawing meshes

            if (i == frameCal-1) Graphics.DrawMeshInstanced(pointMesh, 0, pointMaterial, pointsTRS, positionsNum);
        }

        
    }
    ComputeBuffer pointsInBuffer;
    ComputeBuffer pointsOutBuffer;
    ComputeBuffer outMetrixTransformBuffer;
    Matrix4x4[] pointsTRS ;

    int positionsNum ;
    int mainKernel;


    void generateBufferes()
    {
        positionsNum = points.Length;
        mainKernel = physicsCom.FindKernel("CSMain");
        int pointStructuresize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Particle));

        pointsTRS = new Matrix4x4[positionsNum];
        //declearing buffers

        pointsInBuffer = new ComputeBuffer(positionsNum, pointStructuresize);
        pointsOutBuffer = new ComputeBuffer(positionsNum, pointStructuresize);

        outMetrixTransformBuffer = new ComputeBuffer(positionsNum, sizeof(float) * 16);

        //seting buffers to shader

        physicsCom.SetBuffer(mainKernel, "pointsIn", pointsInBuffer);
        physicsCom.SetBuffer(mainKernel, "pointsOut", pointsOutBuffer);

        physicsCom.SetBuffer(mainKernel, "MetrixTransforms", outMetrixTransformBuffer);
        /*
        //setting data for dispach
        physicsCom.SetFloat("size", pointSize);
        physicsCom.SetFloat("GStrenght", GStrenght);
        physicsCom.SetFloat("pointMass", pointMass);
        physicsCom.SetFloat("frameLenght", Time.deltaTime);
        physicsCom.SetFloat("bounceFrictionLoss", bounceFrictionLoss);
        physicsCom.SetFloat("framecalSpeedMul", framecalSpeedMul);
        */
       
    }
    bool done = false;
    void Start()
    {
        if(spawnPerCube == true) SpawnPelinCube();

        if(spawnSpehere == true)spawnPointsSpere();

        if(Spawn2Points == true) spawnTwoPoints();

        generateBufferes();

        done = true;
    }
    void Update()
    {
        if(done == true)visualizatePositions();
    }
}
