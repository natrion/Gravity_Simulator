using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public unsafe class physics : MonoBehaviour
{
    [Header("Basic Setup")]
    const int chunkSideDividingNum = 2;//number of chunks in one side of the chunk (change in compute shader too)

    [SerializeField] private ComputeShader physicsCom;
    [SerializeField] private Mesh pointMesh;
    [SerializeField] private Material pointMaterial;
    [SerializeField] private  int NUM_OF_THREADS = 256;
    [SerializeField]  private float smalestChunksSize = 5;
    [SerializeField] private float chunkArea = 1000;
    [SerializeField] private float DECIMALVALUESININT = 10000f;
    [SerializeField] private float chunkCalSize =0.6f;//maximal size of chunk that has adicional calculations liek tolal veloctity 
    [SerializeField] private float chunkAddicionalCalSize =20f;//maximal size of chunk that has adicional calculations liek tolal veloctity 

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

    [System.Serializable]
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;

        public int nextElement;
        public int prevElement;
        public int chunkId;
    };
    [SerializeField] private Particle[] points ;
    [SerializeField] Chunk[] chunks ;


    [System.Serializable]
    struct Chunk
    {
        public Vector3 position;
        public Vector3Int totalVelocity;
        public int mass;
        public int numofPoints;

        public int iteration;
        public int parent;
        public unsafe fixed int children[chunkSideDividingNum * chunkSideDividingNum * chunkSideDividingNum]; // Fixed-size array
        public float size;

        public int startPointId;
        public int endPointId;
        public int curentlyInUse;
    };


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
            //setting data for dispach
            physicsCom.SetFloat("size", pointSize);
            physicsCom.SetFloat("pointMass", pointMass);

            physicsCom.SetFloat("GStrenght", GStrenght); 
            physicsCom.SetFloat("pushStrenght", pushStrenght);
            physicsCom.SetFloat("frictionStrenght", frictionStrenght);

            physicsCom.SetFloat("framecalSpeedMul", framecalSpeedMul);

            physicsCom.SetFloat("NUM_OF_THREADS", NUM_OF_THREADS);
            physicsCom.SetFloat("frameLenght", Time.deltaTime);

            physicsCom.SetFloat("chunkCalSize", chunkCalSize);
            physicsCom.SetFloat("chunkAddicionalCalSize", chunkAddicionalCalSize);
            physicsCom.SetFloat("DECIMALVALUESININT", DECIMALVALUESININT);

            
            pointsOutBuffer.GetData(points);
            ChunksOutBuffer.GetData(chunks);
            //dispatching compute main kernel
            physicsCom.Dispatch(mainKernel, Mathf.CeilToInt(pointsNum /(float)NUM_OF_THREADS), 1, 1);

            pointsOutBuffer.GetData(points);
            ChunksOutBuffer.GetData(chunks);

            //taking data from dispach
            outMetrixTransformBuffer.GetData(pointsTRS);
            //drawing meshes
            if (i == frameCal-1) Graphics.DrawMeshInstanced(pointMesh, 0, pointMaterial, pointsTRS, pointsNum);

            //dispatching compute kernel
            physicsCom.Dispatch(preparationKernel, Mathf.CeilToInt((pointsNum+chunksNum) / (float)NUM_OF_THREADS), 1, 1);
        }
    }

    ComputeBuffer outMetrixTransformBuffer;

    private Matrix4x4[] pointsTRS ;
    private int mainKernel;
    private int preparationKernel;
    private int pointsNum;
    private int chunksNum;
    private int[,,] subChunkLookupTable;

    ComputeBuffer ChunksInBuffer;
    ComputeBuffer ChunksOutBuffer;
    ComputeBuffer pointsInBuffer;
    ComputeBuffer pointsOutBuffer;
    void generateBufferes()
    {
        ////////////////////////////////////////////////////////////////////////////////////////oher data setup
        
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
        Chunk[] chunksArray = new Chunk[chunks.Count];

        for (int i = 0; i < chunksArray.Length; i++)
        {
            chunksArray[i] = chunks[i];
        }

        //making the look up table that will be used to tell in whath inedex the chuck you want to find is in 
        subChunkLookupTable  = new int[chunkSideDividingNum, chunkSideDividingNum, chunkSideDividingNum];
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
        for (int i = 0; i < points.Length; i++)
        {          
            Particle point = points[i];
            point.position = new Vector3(Mathf.Clamp( point.position.x,-chunkArea*0.45f,chunkArea*0.45f)  
                                        ,Mathf.Clamp( point.position.y,-chunkArea*0.45f,chunkArea*0.45f),
                                        Mathf.Clamp( point.position.z,-chunkArea*0.45f,chunkArea*0.45f));
            Vector3 position = point.position;
            
            int inWhatchunk = 0;
            
            while(chunksArray[inWhatchunk].iteration > 0)   //going repetedly to children of childer and asigning values
            {
                int iteration = chunksArray[inWhatchunk].iteration;

                inWhatchunk = findChild(inWhatchunk, position, chunksArray);
                
                iteration = chunksArray[inWhatchunk].iteration;

                if (inWhatchunk != -1)
                {
                    Chunk newChunk = chunksArray[inWhatchunk];
                    newChunk.mass += Mathf.RoundToInt(pointMass * DECIMALVALUESININT);
                    newChunk.numofPoints++;
                    if(newChunk.size<chunkAddicionalCalSize )newChunk.totalVelocity += new Vector3Int(Mathf.RoundToInt(point.velocity.x* DECIMALVALUESININT) , Mathf.RoundToInt(point.velocity.y* DECIMALVALUESININT), Mathf.RoundToInt(point.velocity.z* DECIMALVALUESININT));

                    if (newChunk.iteration == 0)// calculationg data for points in smallest chunks
                    {
                        point.chunkId = inWhatchunk;
                        point.nextElement = -1;
                        if (newChunk.numofPoints > 1)//calculations on not emty chunks
                        {
                           point.prevElement = newChunk.endPointId;
                        }
                        else //calculations on empty chunks
                        {
                            newChunk.startPointId = i;
                            point.prevElement = -1;
                        }
                        newChunk.endPointId = i;

                    }
                    chunksArray[inWhatchunk] = newChunk;
                }
                else
                {
                    print("point out of bounds" + position);
                }
            }
            points[i] = point;
        }
        

        /////////////////////////////////////////////////////////////////////////////////buffer setup
        
        //setting up basic variables
        pointsNum = points.Length;
        chunksNum = chunksArray.Length;
        mainKernel = physicsCom.FindKernel("pointCal");
        preparationKernel = physicsCom.FindKernel("PrepareData");

        int pointStructuresize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Particle));
        int chunkStructuresize = System.Runtime.InteropServices.Marshal.SizeOf(chunksArray[0]);

        pointsTRS = new Matrix4x4[pointsNum];

        //declearing buffers
        /*
        ComputeBuffer ChunksInBuffer;
        ComputeBuffer ChunksOutBuffer;
        ComputeBuffer pointsInBuffer;
        ComputeBuffer pointsOutBuffer;*/


        pointsInBuffer = new ComputeBuffer(pointsNum, pointStructuresize);
        pointsInBuffer.SetData(points);
        pointsOutBuffer = new ComputeBuffer(pointsNum, pointStructuresize);
        pointsOutBuffer.SetData(points);

        ChunksInBuffer = new ComputeBuffer(chunksNum, chunkStructuresize);
        ChunksInBuffer.SetData(chunksArray);
        ChunksOutBuffer = new ComputeBuffer(chunksNum, chunkStructuresize);
        ChunksOutBuffer.SetData(chunksArray);
        
        outMetrixTransformBuffer = new ComputeBuffer(pointsNum, sizeof(float) * 16);

        //seting buffers to main kernel
        physicsCom.SetBuffer(mainKernel, "ChunksOut", ChunksOutBuffer);
        physicsCom.SetBuffer(mainKernel, "ChunksIn", ChunksInBuffer);
        physicsCom.SetBuffer(mainKernel, "pointsIn", pointsInBuffer);
        physicsCom.SetBuffer(mainKernel, "pointsOut", pointsOutBuffer);
        physicsCom.SetBuffer(mainKernel, "MetrixTransforms", outMetrixTransformBuffer);
        //setting bufers to preparation kernel
        physicsCom.SetBuffer(preparationKernel, "ChunksOut", ChunksOutBuffer);
        physicsCom.SetBuffer(preparationKernel, "ChunksIn", ChunksInBuffer);
        physicsCom.SetBuffer(preparationKernel, "pointsIn", pointsInBuffer);
        physicsCom.SetBuffer(preparationKernel, "pointsOut", pointsOutBuffer);
        
        //setting data for dispach
        /*
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
                        numOfSmalestChunks++;
                        subChunk.curentlyInUse = 0;
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

    [SerializeField]private GameObject showCube;

    bool done = false;
    int findChild(int parentId, Vector3 position, Chunk[] chunks)//function that finds the child of a parent chunk based on position
    {
        //calculations
        position -= chunks[parentId].position;
        Vector3 calPos = position / (chunks[parentId].size) +Vector3.one/2;
        calPos = calPos* (float)chunkSideDividingNum ;

        //out of bounce check
        if (calPos.x < chunkSideDividingNum && calPos.y < chunkSideDividingNum && calPos.z < chunkSideDividingNum)
        {
            return chunks[parentId].children[subChunkLookupTable[Mathf.FloorToInt(calPos.x), Mathf.FloorToInt(calPos.y), Mathf.FloorToInt(calPos.z)]];//returning the child
        }
        else
        {
            return -1;//out of bounce exeption
        }
    }
    void Start()
    {
        physicsCom.SetInt("CHUNK_SIDE_DIVISION_NUMBER", chunkSideDividingNum);

        if(spawnPerCube == true) SpawnPelinCube();

        if(spawnSpehere == true)spawnPointsSpere();

        if(Spawn2Points == true) spawnTwoPoints();

        generateBufferes();

        chunks = new Chunk[chunksNum];
        done = true;
    }
    void Update()
    {
        if(done == true)visualizatePositions();
    }
}
