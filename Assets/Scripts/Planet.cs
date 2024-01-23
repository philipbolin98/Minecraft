using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Noise;
using System.Linq;

public class Planet : MonoBehaviour {

    public static readonly int PlanetSizeInChunks = 128;
    public static readonly int ViewDistanceInChunks = 4;
    public static readonly int OceanFloorLevel = 20;
    public static readonly int SeaLevel = 30;
    public static readonly int MaxTerrainLevel = 50;
    public static readonly int WorldHeight = 128;

    public static int PlanetSizeInVoxels {
        get { return PlanetSizeInChunks * VoxelData.ChunkSize; }
    }

    public GameObject player;
    public GameObject chunkPrefab;
    public Vector3 spawnPosition;
    public Block[] blockTypes;

    private OpenSimplex2S Noise4D;
    private int seed;

    public Chunk[,] chunks = new Chunk[PlanetSizeInChunks, PlanetSizeInChunks];
    public List<Vector2Int> activeChunks = new List<Vector2Int>();
    public Queue<GameObject> unusedChunkObjects = new Queue<GameObject>();

    public PlanetData data;

    public Thread chunkUpdateThread;

    public List<Vector2Int> chunksToPopulate = new List<Vector2Int>();
    public object chunksToPopulateLock = new object();

    public List<Vector2Int> chunksToMesh = new List<Vector2Int>();
    public object chunksToMeshLock = new object();

    private void Start() {
        try {

            int seed = 17; //Mathf.FloorToInt(Random.Range(0f, 1f) * 100);

            this.seed = seed;
            Random.InitState(seed);
            Noise4D = new OpenSimplex2S(seed);

            this.data = new PlanetData(this.gameObject.name, seed); //SaveSystem.LoadPlanet(this.gameObject.name, seed);

            this.spawnPosition = new Vector3(1, WorldHeight, 1); //Need to read player spawn position from file once saving is implemented

            this.chunkUpdateThread = new Thread(new ThreadStart(GenerateChunks));
            this.chunkUpdateThread.Start();

            CheckRenderDistance(spawnPosition);
            
            Instantiate(this.player);
            this.player.transform.position = spawnPosition;

        } catch (System.Exception ex) {
            Debug.LogError("Error in Planet Start: " + ex.Message);
        }
    }

    private void OnDisable() {
        chunkUpdateThread.Abort();
    }

    public void CheckRenderDistance(Vector3 pos) {
        try {

            Vector2Int centerChunk = HelperFunctions.GetChunkPosFromBlockPos(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

            //Important: We need to populate chunks in a radius that is 1 greater than the view distance
            //This is because in order to generate chunk border meshes, the chunks adjacent to the target chunk need to be populated with block data
            int radius = ViewDistanceInChunks;

            int minChunkX = centerChunk.x - radius;
            int maxChunkX = centerChunk.x + radius;

            int minChunkZ = centerChunk.y - radius;
            int maxChunkZ = centerChunk.y + radius;

            for (int i = this.activeChunks.Count - 1; i >= 0; i--) {

                Vector2Int chunkPos = this.activeChunks[i];

                if (chunkPos.x <= minChunkX || chunkPos.x >= maxChunkX || chunkPos.y <= minChunkZ || chunkPos.y >= maxChunkZ) {

                    Vector2Int normalizedChunkPos = HelperFunctions.GetNormalizedChunkPos(chunkPos.x, chunkPos.y);
                    Chunk chunk = this.chunks[normalizedChunkPos.x, normalizedChunkPos.y];

                    if (chunk != null && chunk.isRendered) {
                        chunk.Disable();
                        this.activeChunks.Remove(chunkPos);
                    }
                }
            }

            //All chunks within the radius should populated
            for (int x = minChunkX; x <= maxChunkX; x++) {
                for (int z = minChunkZ; z <= maxChunkZ; z++) {

                    Vector2Int chunkPos = new Vector2Int(x, z);
                    Vector2Int normalizedChunkPos = HelperFunctions.GetNormalizedChunkPos(chunkPos.x, chunkPos.y);
                    Chunk chunk = this.chunks[normalizedChunkPos.x, normalizedChunkPos.y];

                    if (chunk == null) {
                        chunk = new Chunk(this, normalizedChunkPos);
                    }

                    if (!chunk.isDataPopulated && !chunksToPopulate.Contains(normalizedChunkPos)) {
                        lock (chunksToPopulateLock) {
                            chunksToPopulate.Add(normalizedChunkPos);
                        }
                    }

                    //Only chunks within the inner radius should have their meshes created
                    if (x > minChunkX && x < maxChunkX && z > minChunkZ && z < maxChunkZ) {

                        if (!chunk.isMeshGenerated && !chunksToMesh.Contains(normalizedChunkPos)) {
                            lock (chunksToMeshLock) {
                                chunksToMesh.Add(normalizedChunkPos);
                            }
                        }

                        if (!activeChunks.Contains(chunkPos)) {
                            activeChunks.Add(chunkPos);
                        }
                    }
                }
            }

        } catch (System.Exception ex) {
            Debug.LogError("Error in Planet CheckRenderDistance: " + ex.Message);
        }
    }

    public void GenerateChunks() {
        while (true) {
            try {

                if (chunksToPopulate.Count > 0) {

                    List<Vector2Int> chunks = null;

                    lock (chunksToPopulateLock) {
                        chunks = chunksToPopulate.ToList();
                        chunksToPopulate.Clear();
                    }

                    foreach (Vector2Int normalizedChunkPos in chunks) {
                        Chunk chunk = this.chunks[normalizedChunkPos.x, normalizedChunkPos.y];
                        chunk.data.Popuplate(this);
                        chunk.isDataPopulated = true;
                    }
                }

                if (chunksToMesh.Count > 0) {

                    List<Vector2Int> chunks = new List<Vector2Int>();

                    lock (chunksToMeshLock) {
                        for (int i = chunksToMesh.Count - 1; i >= 0; i--) {
                            Vector2Int normalizedChunkPos = chunksToMesh[i];
                            if (HelperFunctions.CanChunkMeshBeCreated(this, normalizedChunkPos)) {
                                chunks.Add(normalizedChunkPos);
                                chunksToMesh.Remove(normalizedChunkPos);
                            }
                        }
                    }

                    foreach (Vector2Int normalizedChunkPos in chunks) {
                        Chunk chunk = this.chunks[normalizedChunkPos.x, normalizedChunkPos.y];
                        chunk.CreateMeshData();
                        chunk.isMeshGenerated = true;
                    }
                }

            } catch (System.Exception ex) {
                Debug.LogError("Error in Planet GenerateChunks: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Generates all the chunk game objects in the current planet
    /// </summary>
    /// <param name="pos">The position to spawn the chunks around</param>
    public void RenderChunks() {
        try {

            foreach (Vector2Int chunkPos in this.activeChunks) {
                Vector2Int normalizedChunkPos = HelperFunctions.GetNormalizedChunkPos(chunkPos.x, chunkPos.y);
                Chunk chunk = this.chunks[normalizedChunkPos.x, normalizedChunkPos.y];

                if (!chunk.isRendered && chunk.isDataPopulated && chunk.isMeshGenerated) {
                    chunk.Enable(chunkPos, normalizedChunkPos);
                }
            }

        } catch (System.Exception ex) {
            Debug.LogError("Error in Planet GenerateChunks: " + ex.Message);
        }
    }

    /// <summary>
    /// Function that uses simplex noise to generate a terrain height y at a given x and z position in the planet
    /// The y levels generated are continuous along the xz plane so that the planet terrain seamlessly loops
    /// </summary>
    /// <param name="x">The x coord in the planet</param>
    /// <param name="z">The z coord in the planet</param>
    /// <returns></returns>
    public double TorusNoise(float x, float z) {
        try {

            float nx = x / PlanetSizeInVoxels;
            float nz = z / PlanetSizeInVoxels;

            float angle_x = VoxelData.TAU * nx;
            float angle_z = VoxelData.TAU * nz;

            double cosx = Mathf.Cos(angle_x) / VoxelData.TAU;
            double sinx = Mathf.Sin(angle_x) / VoxelData.TAU;
            double cosz = Mathf.Cos(angle_z) / VoxelData.TAU;
            double sinz = Mathf.Sin(angle_z) / VoxelData.TAU;

            double noiseValue = Noise4D.Noise4_Classic(cosx, sinx, cosz, sinz);
            noiseValue = (noiseValue + 1) / 2;
            return noiseValue;

        } catch (System.Exception e) {
            Debug.LogError("Error in function Planet.TorusNoise: " + e.Message);
            return 0;
        }
    }
}

[System.Serializable]
public class Block {

    public string blockName;
    public bool isSolid;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    // Back, Front, Top, Bottom, Left, Right

    public int GetTextureID(int faceIndex) {
        try {

            switch (faceIndex) {

                case 0:
                    return backFaceTexture;
                case 1:
                    return frontFaceTexture;
                case 2:
                    return topFaceTexture;
                case 3:
                    return bottomFaceTexture;
                case 4:
                    return leftFaceTexture;
                case 5:
                    return rightFaceTexture;
                default:
                    throw new System.ArgumentException();
            }

        } catch (System.Exception ex) {
            Debug.LogError("Error in Block GetTextureID: " + ex.Message);
            return 0;
        }
    }
}

public enum BlockID {
    Air = 0,
    Bedrock = 1,
    Stone = 2,
    Grass = 3,
    Dirt = 4
}