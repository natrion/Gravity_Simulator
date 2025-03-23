using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public unsafe class physics : MonoBehaviour
{
    [Header("Basic Setup")]
    const int chunkPointGroupsNum = 200;
    const int chunkSideDividingNum = 2;
    [SerializeField] private ComputeShader physicsCom;
    [SerializeField] private Mesh pointMesh;
    [SerializeField] private Material pointMaterial;
    [SerializeField] private  int NUM_OF_THREADS = 256;
    [SerializeField]  private float smalestChunksSize = 5;
    [SerializeField] private float chunkArea = 1000;
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
    [Range(0f, 5f)]
    [SerializeField] private float framecalSpeedMul = 1;
    //[System.Serializable]
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
    };
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct Chunk
    {
        public Vector3 position;

        public float mass;
        public int iteration;
        public int parent;
        public fixed int children[chunkSideDividingNum * chunkSideDividingNum * chunkSideDividingNum];
        public float size;

        public int pointGroupId;
        public int numofPoints;

    };
    //[SerializeField]
    private Particle[] points;

    Vector3 EulerToNormal(Vector3 eulerAngles)
    {
        Quaternion rotation = Quaternion.Euler(eulerAngles); // Euler na Quaternion
        return rotation * Vector3.forward; // Aplikujeme rot�ciu na vektor (0,0,1)
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
            ChunksInBuffer.SetData(chunksArray);
            chunkPointDataInBuffer.SetData(ChunksGroupPointers);


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
            pointsOutBuffer.GetData(points);
            outMetrixTransformBuffer.GetData(pointsTRS);
            ChunksOutBuffer.GetData(chunksArray);
            chunkPointDataOutBuffer.GetData(ChunksGroupPointers);

            //drawing meshes

            if (i == frameCal-1) Graphics.DrawMeshInstanced(pointMesh, 0, pointMaterial, pointsTRS, positionsNum);
        }

        
    }
    ComputeBuffer pointsInBuffer;
    ComputeBuffer pointsOutBuffer;
    ComputeBuffer outMetrixTransformBuffer;
    ComputeBuffer ChunksInBuffer;
    ComputeBuffer ChunksOutBuffer;
    ComputeBuffer chunkPointDataInBuffer;
    ComputeBuffer chunkPointDataOutBuffer;

    Matrix4x4[] pointsTRS ;

    int positionsNum ;
    int chunksNum;
    int mainKernel;


    void generateBufferes()
    {
        chunksNum = chunksArray.Length;
        positionsNum = points.Length;
        mainKernel = physicsCom.FindKernel("pointCal");
        int pointStructuresize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Particle));
        int chunkStructuresize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Chunk));
        int chunkPointDataStructuresize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(ChunkPointData));

        pointsTRS = new Matrix4x4[positionsNum];

        //declearing buffers
        pointsInBuffer = new ComputeBuffer(positionsNum, pointStructuresize);
        pointsOutBuffer = new ComputeBuffer(positionsNum, pointStructuresize);

        ChunksInBuffer = new ComputeBuffer(chunksNum, chunkStructuresize);
        ChunksInBuffer.SetData(chunksArray);
        ChunksOutBuffer = new ComputeBuffer(chunksNum, chunkStructuresize);

        chunkPointDataInBuffer = new ComputeBuffer(ChunksGroupPointers.Length, chunkPointDataStructuresize);
        chunkPointDataInBuffer.SetData(ChunksGroupPointers);
        chunkPointDataOutBuffer = new ComputeBuffer(ChunksGroupPointers.Length, chunkPointDataStructuresize);

        outMetrixTransformBuffer = new ComputeBuffer(positionsNum, sizeof(float) * 16);

        //seting buffers to shader
        
        physicsCom.SetBuffer(mainKernel, "ChunksOut", ChunksOutBuffer);
        physicsCom.SetBuffer(mainKernel, "ChunksIn", ChunksInBuffer);

        physicsCom.SetBuffer(mainKernel, "ChunksGroupPointersIn", chunkPointDataInBuffer); 
        physicsCom.SetBuffer(mainKernel, "ChunksGroupPointersOut", chunkPointDataOutBuffer);

        physicsCom.SetBuffer(mainKernel, "pointsIn", pointsInBuffer);
        physicsCom.SetBuffer(mainKernel, "pointsOut", pointsOutBuffer);

        physicsCom.SetBuffer(mainKernel, "MetrixTransforms", outMetrixTransformBuffer);
        //seting data to outputput buffers 
        pointsOutBuffer.SetData(points);
        ChunksOutBuffer.SetData(chunksArray);
        chunkPointDataOutBuffer.SetData(ChunksGroupPointers);
        /*
        //setting data for dispach
        physicsCom.SetFloat("size", pointSize);
        physicsCom.SetFloat("GStrenght", GStrenght);
        physicsCom.SetFloat("pointMass", pointMass);
        physicsCom.SetFloat("frameLenght", Time.deltaTime);
        physicsCom.SetFloat("bounceFrictionLoss", bounceFrictionLoss);
        physicsCom.SetFloat("framecalSpeedMul", framecalSpeedMul);
        */

        //puting subchunk lookup table to shader
       
        int flatSize = chunkSideDividingNum * chunkSideDividingNum * chunkSideDividingNum;
        SubChunkLookupTableStructure lookupTable = new SubChunkLookupTableStructure();

        // Flatten the multidimensional array into a one-dimensional array
        int index = 0;
        for (int x = 0; x <chunkSideDividingNum; x++)
        {
            for (int y = 0; y < chunkSideDividingNum; y++)
            {
                for (int z = 0; z <chunkSideDividingNum; z++)
                {
                    lookupTable.data[index++] = subChunkLookupTable[x, y, z];
                }
            }
        }
        // Create a ComputeBuffer for the struct
        ComputeBuffer SubChunkLookupTableBuffer = new ComputeBuffer(1, flatSize * sizeof(int));
        SubChunkLookupTableBuffer.SetData(new SubChunkLookupTableStructure[] { lookupTable });

        // Set the buffer to the compute shader
        physicsCom.SetBuffer(mainKernel, "subChunkLookupTable", SubChunkLookupTableBuffer);


    }
    int numOfSmalestChunks = 0;
    void makeSubChunks(int chunkId, ref List<Chunk> chunks )//function that makes sub chunks insade of subchanks... until it makes all chunks needed
    {
        
        Chunk chunk = chunks[chunkId];
        //show cube cisuals visuals
        /*
        GameObject showCubeInstance = Instantiate(showCube);
        showCubeInstance.transform.position = chunk.position;
        showCubeInstance.transform.localScale = Vector3.one * chunk.size;
        showCubeInstance.SetActive(false);
        */

        if (chunk.iteration-1 < 0) return;//not making sub chanjks when on the smallest chunks


        float chunkSize = chunk.size;
        float subChunkSize = (float)chunkSize / (float)chunkSideDividingNum;

        int i = 0;
        //making all subchanks
        for (float x = 0; x < chunkSideDividingNum; x++)
        {
            for (float y =0 ; y < chunkSideDividingNum; y++)
            {
                for (float z = 0; z < chunkSideDividingNum; z++)
                {
                    
                    Chunk subChunk = new Chunk();
                    subChunk.position = chunk.position + new Vector3(x, y, z)* subChunkSize -(Vector3.one* chunkSize)/2 + (Vector3.one * subChunkSize)/2;
                    subChunk.iteration = chunk.iteration-1;
                    subChunk.mass = 0;
                    subChunk.numofPoints = 0;
                    subChunk.parent = chunkId;
                    subChunk.size = chunkSize / (float)chunkSideDividingNum;

                    chunks.Add(subChunk);

                    int subChunkId = chunks.Count -1;
                    chunk.children[i] = subChunkId;
                    if (subChunk.iteration == 0) {
                        subChunk.pointGroupId = numOfSmalestChunks;
                        numOfSmalestChunks++;
                    }
                    makeSubChunks(subChunkId, ref chunks);
                    i++;
                }
            }
        }
        chunks[chunkId] = chunk;
    }
    struct SubChunkLookupTableStructure
    {
        public fixed int data[chunkSideDividingNum*chunkSideDividingNum*chunkSideDividingNum];
    };
    private int[,,] subChunkLookupTable ;
    Chunk[] chunksArray;
    struct ChunkPointData { public fixed int points[chunkPointGroupsNum]; };
    ChunkPointData[] ChunksGroupPointers;

    [SerializeField]private GameObject showCube;
    void prepareOtherData()
    {
        //calculating the actual chunk area becose it needs to be a some number of powesr of smallestChunkSize powerd by chunkSideDividingNum 
        float chunkAreaCheck = smalestChunksSize;
        int maxIteration = 0;
        while (chunkAreaCheck < chunkArea)
        {
            chunkAreaCheck *= chunkSideDividingNum;
            maxIteration++;
        }
        chunkArea = chunkAreaCheck;
        //making the bigest parent chunk 
        List<Chunk> chunks = new List<Chunk>();

        Chunk chunk = new Chunk();
        chunk.iteration = maxIteration;
        chunk.position = Vector3.zero;
        chunk.mass = 0;
        chunk.numofPoints = 0;
        chunk.size = chunkArea;
        numOfSmalestChunks = 0;
        chunks.Add(chunk);
        //making all chunks insade that chunk
        makeSubChunks(0, ref chunks);

        //convering chunk list to chunk array
        chunksArray = new Chunk[chunks.Count];
        for (int i = 0; i < chunksArray.Length; i++)chunksArray[i] = chunks[i];

         //decleration of groups of paritivles 
        ChunksGroupPointers = new ChunkPointData[numOfSmalestChunks];// the chunks at teh lowest level point to these groups of paritivles 

        //making the look up table that will be used to tell in whath inedex the chuck you want to find is in 
        subChunkLookupTable = new int[chunkSideDividingNum, chunkSideDividingNum, chunkSideDividingNum];
        int lookUpSetupI = 0;
        for (int x = 0; x < chunkSideDividingNum ; x++)
        {
            for (int y = 0; y < chunkSideDividingNum ; y++)
            {
                for (int z = 0; z < chunkSideDividingNum; z ++)
                {
                    subChunkLookupTable[x, y, z] = lookUpSetupI;
                    lookUpSetupI++;
                }
            }
        }
        //soritng particle in to there respective chunks
        int pointID = 0;
        foreach (Particle point in points)
        {          
            Vector3 position = point.position;

            int inWhatchunk = 0;

            for (int i = 0; i <= maxIteration; i++)//going repetedly to children of childer and asigning values
            {
                inWhatchunk = findChild(0, position, chunksArray);

                if (inWhatchunk != -1)
                {
                    Chunk newChunk = chunks[inWhatchunk];
                    newChunk.mass += pointMass;
                    newChunk.numofPoints++;
                    chunksArray[inWhatchunk] = newChunk;

                    ChunkPointData newChunkPointData = ChunksGroupPointers[newChunk.pointGroupId];
                    newChunkPointData.points[newChunk.pointGroupId] = pointID;
                }
                else
                {
                    print("point out of bounds" + position);
                }
            }

            pointID++;
        }
       
    }
    bool done = false;
    int findChild(int parentId, Vector3 position, Chunk[] chunks)//function that finds the child of a parent chunk based on position
    {
        //calculations
        position -= chunks[parentId].position;
        Vector3 calPos = position / (chunks[parentId].size / (float)chunkSideDividingNum);
        calPos += Vector3.one * (float)chunkSideDividingNum / 2f;

        //out of bounce check
        if (calPos.x < chunkSideDividingNum && calPos.y < chunkSideDividingNum && calPos.z < chunkSideDividingNum)
        {
            return chunks[0].children[subChunkLookupTable[Mathf.FloorToInt(calPos.x), Mathf.FloorToInt(calPos.y), Mathf.FloorToInt(calPos.z)]];//returning the child
        }
        else
        {
            return -1;//out of bounce exeption
        }
    }
    void Start()
    {
        if(spawnPerCube == true) SpawnPelinCube();

        if(spawnSpehere == true)spawnPointsSpere();

        if(Spawn2Points == true) spawnTwoPoints();

        prepareOtherData();

        generateBufferes();

        done = true;
    }
    void Update()
    {
        if(done == true)visualizatePositions();
    }
}
