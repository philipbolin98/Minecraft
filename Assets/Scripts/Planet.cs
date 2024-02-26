using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Linq;
using System;

public class Planet : MonoBehaviour {

    public int seed;
    public int PlanetSizeInChunks;
    [HideInInspector]
    public int PlanetSizeInVoxels;
    public int ViewDistanceInChunks;
    public int WorldHeight;
    public float gravity = -40f;

    public GameObject player;
    public GameObject chunkPrefab;
    public Vector3 spawnPosition;
    public Block[] blockTypes;
    public Biome[] biomes;

    public Chunk[,] chunks;
    private List<Vector2Int> chunksInView = new();
    public Queue<GameObject> unusedChunkObjects = new();

    private Thread chunkPopulateThread;
    private Thread structureGenerateThread;
    private Thread meshCreationThread;

    private List<Vector2Int> chunksToPopulate = new();
    private object chunksToPopulateLock = new();

    private List<Vector2Int> chunksToGenerateStructures = new();
    private object chunksToGenerateStructuresLock = new();

    [HideInInspector]
    public List<Vector2Int> chunksToMesh = new();
    public object chunksToMeshLock = new();

    [HideInInspector]
    public TerrainGenerator terrainGenerator;

    private bool gameRunning = true;

    private void Start() {
        try {

            this.PlanetSizeInVoxels = PlanetSizeInChunks * VoxelData.ChunkSize;

            this.chunks = new Chunk[this.PlanetSizeInChunks, this.PlanetSizeInChunks];

            this.seed = UnityEngine.Random.Range(0, int.MaxValue);
            this.terrainGenerator = GameObject.Find("TerrainGenerator").GetComponent<TerrainGenerator>();
            this.terrainGenerator.Initialize(this.seed);

            //TO DO: Implement wait handles for these 3 threads so that they are not running in infinite loops
            this.chunkPopulateThread = new Thread(new ThreadStart(PopulateChunks));
            this.chunkPopulateThread.Start();

            this.structureGenerateThread = new Thread(new ThreadStart(GenerateStructures));
            this.structureGenerateThread.Start();

            this.meshCreationThread = new Thread(new ThreadStart(CreateChunkMeshes));
            this.meshCreationThread.Start();

            CheckRenderDistance(spawnPosition); //Need to do one initial check when the planet loads

            Instantiate(this.player);
            this.player.transform.position = this.spawnPosition;

        } catch (Exception ex) {
            Debug.LogError("Error in Planet Start: " + ex.Message);
        }
    }

    private void OnDisable() {
        try {
            gameRunning = false;
        } catch (Exception ex) {
            Debug.LogError("Error in Planet OnDisable: " + ex.Message);
        }
    }

    public void CheckRenderDistance(Vector3 pos) {
        try {

            Vector2Int centerChunk = HelperFunctions.GetChunkPosFromBlockPos(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.z));

            int minChunkX = centerChunk.x - ViewDistanceInChunks;
            int maxChunkX = centerChunk.x + ViewDistanceInChunks;

            int minChunkZ = centerChunk.y - ViewDistanceInChunks;
            int maxChunkZ = centerChunk.y + ViewDistanceInChunks;

            for (int i = this.chunksInView.Count - 1; i >= 0; i--) {

                Vector2Int chunkPos = this.chunksInView[i];

                if (chunkPos.x <= minChunkX || chunkPos.x >= maxChunkX || chunkPos.y <= minChunkZ || chunkPos.y >= maxChunkZ) {

                    this.chunksInView.Remove(chunkPos);

                    Vector2Int normalizedChunkPos = HelperFunctions.NormalizeChunkPos(this, chunkPos.x, chunkPos.y);
                    Chunk chunk = this.chunks[normalizedChunkPos.x, normalizedChunkPos.y];

                    if (chunk != null && chunk.stage == ChunkStage.Rendered) {
                        chunk.Disable();
                    }
                }
            }

            List<Vector2Int> chunks = new();

            //All chunks within the radius should populated
            for (int x = minChunkX; x <= maxChunkX; x++) {
                for (int z = minChunkZ; z <= maxChunkZ; z++) {
                    chunks.Add(new Vector2Int(x, z));
                }
            }

            chunks.Sort(new ChunkComparer(centerChunk));

            foreach (Vector2Int chunkPos in chunks) {

                Vector2Int normalizedChunkPos = HelperFunctions.NormalizeChunkPos(this, chunkPos.x, chunkPos.y);
                Chunk chunk = this.chunks[normalizedChunkPos.x, normalizedChunkPos.y];

                if (chunk == null) {
                    chunk = new Chunk(this, normalizedChunkPos);
                }

                if (chunk.stage == ChunkStage.Null && !chunksToPopulate.Contains(normalizedChunkPos)) {
                    lock (chunksToPopulateLock) {
                        chunksToPopulate.Add(normalizedChunkPos);
                    }
                }

                //Only chunks within the inner radius should have their meshes created
                if (chunkPos.x > minChunkX && chunkPos.x < maxChunkX && chunkPos.y > minChunkZ && chunkPos.y < maxChunkZ) {

                    if (!chunksInView.Contains(chunkPos)) {
                        chunksInView.Add(chunkPos);
                    }
                }
            }
            
        } catch (Exception ex) {
            Debug.LogError("Error in Planet CheckRenderDistance: " + ex.Message);
        }
    }

    public void PopulateChunks() {
        while (gameRunning) {
            try {

                if (chunksToPopulate.Count > 0) {

                    Vector2Int[] chunks = null;

                    lock (chunksToPopulateLock) {
                        chunks = chunksToPopulate.ToArray<Vector2Int>();
                    }

                    foreach (Vector2Int chunkPos in chunks) {
                        Chunk chunk = this.chunks[chunkPos.x, chunkPos.y];
                        chunk.Popuplate();

                        lock (chunksToPopulateLock) {
                            chunksToPopulate.Remove(chunkPos);
                        }

                        if (chunk.stage == ChunkStage.StructuresGenerated) {
                            lock (chunksToMeshLock) {
                                chunksToMesh.Add(chunkPos);
                            }
                        } else {
                            lock (chunksToGenerateStructuresLock) {
                                chunksToGenerateStructures.Add(chunkPos);
                            }
                        }
                    }
                }

            } catch (Exception ex) {
                Debug.LogError("Error in Planet PopulateChunks: " + ex.Message);
            }

            Thread.Sleep(10);
        }
    }

    private void GenerateStructures() {
        while (gameRunning) {
            try {

                if (chunksToGenerateStructures.Count > 0) {

                    List<Vector2Int> chunks = new();

                    lock (chunksToGenerateStructuresLock) {
                        
                        for (int i = chunksToGenerateStructures.Count - 1; i >= 0; i--) {
                            Vector2Int chunkPos = chunksToGenerateStructures[i];
                            if (HelperFunctions.CheckNeighboringChunks(this, chunkPos, Chunk.NeighboringChunks, ChunkStage.DataPopulated)) {
                                chunks.Add(chunkPos);
                            }
                        }
                    }

                    if (chunks.Count > 0) {

                        foreach (Vector2Int chunkPos in chunks) {

                            Chunk chunk = this.chunks[chunkPos.x, chunkPos.y];
                            chunk.GenerateStructures();

                            lock (chunksToGenerateStructuresLock) {
                                chunksToGenerateStructures.Remove(chunkPos);
                            }

                            lock (chunksToMeshLock) {
                                chunksToMesh.Add(chunkPos);
                            }
                        }
                    }
                }

            } catch (Exception ex) {
                Debug.LogError("Error in Planet GenerateStructures: " + ex.Message);
            }

            Thread.Sleep(10);
        }
    }

    private void CreateChunkMeshes() {
        while (gameRunning) {
            try {

                if (chunksToMesh.Count > 0) {

                    List<Vector2Int> chunks = new();

                    lock (chunksToMeshLock) {

                        for (int i = chunksToMesh.Count - 1; i >= 0; i--) {
                            Vector2Int chunkPos = chunksToMesh[i];
                            if (HelperFunctions.CheckNeighboringChunks(this, chunkPos, Chunk.NeighboringChunks, ChunkStage.StructuresGenerated)) {
                                chunks.Add(chunkPos);
                            }
                        }
                    }

                    if (chunks.Count > 0) {

                        foreach (Vector2Int chunkPos in chunks) {

                            Chunk chunk = this.chunks[chunkPos.x, chunkPos.y];
                            chunk.CreateMeshData();

                            lock (chunksToMeshLock) {
                                chunksToMesh.Remove(chunkPos);
                            }
                        }
                    }
                }

            } catch (Exception ex) {
                Debug.LogError("Error in Planet CreateChunkMeshes: " + ex.Message);
            }

            Thread.Sleep(10);
        }
    }

    /// <summary>
    /// Generates all the chunk game objects in the current planet
    /// </summary>
    /// <param name="pos">The position to spawn the chunks around</param>
    public void RenderChunks() {
        try {

            foreach (Vector2Int chunkPos in this.chunksInView) {
                Vector2Int normalizedChunkPos = HelperFunctions.NormalizeChunkPos(this, chunkPos.x, chunkPos.y);
                Chunk chunk = this.chunks[normalizedChunkPos.x, normalizedChunkPos.y];

                if (chunk.stage == ChunkStage.MeshCreated) {
                    chunk.Enable(chunkPos, normalizedChunkPos);
                }
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Planet GenerateChunks: " + ex.Message);
        }
    }  
}

public class ChunkComparer : IComparer<Vector2Int> {

    private Vector2Int chunkPos;

    public ChunkComparer(Vector2Int chunkPos) {
        this.chunkPos = chunkPos;
    }

    int IComparer<Vector2Int>.Compare(Vector2Int chunkPos1, Vector2Int chunkPos2) {

        int dx1 = Mathf.Abs(this.chunkPos.x - chunkPos1.x);
        int dz1 = Mathf.Abs(this.chunkPos.y - chunkPos1.y);

        int dx2 = Mathf.Abs(this.chunkPos.x - chunkPos2.x);
        int dz2 = Mathf.Abs(this.chunkPos.y - chunkPos2.y);

        int ring1 = Mathf.Max(dx1, dz1);
        int ring2 = Mathf.Max(dx2, dz2);

        if (ring1 > ring2) {
            return 1;
        } else if (ring2 > ring1) {
            return -1;
        } else {

            int d1 = dx1 + dz1;
            int d2 = dx2 + dz2;

            if (d1 > d2) {
                return 1;
            } else if (d2 > d1) {
                return -1;
            } else {
                return 0;
            }
        }
    }
}

[Serializable]
public class Block {

    public string blockName;
    public bool isSolid;

    [Header("Texture Values")]
    public int rightFaceTexture;
    public int topFaceTexture;
    public int frontFaceTexture;
    public int leftFaceTexture;
    public int bottomFaceTexture;
    public int backFaceTexture;

    public int GetTextureID(int faceIndex) {

        int index = 0;
        
        try {

            switch (faceIndex) {
                case 0:
                    index = this.rightFaceTexture;
                    break;
                case 1:
                    index = this.topFaceTexture;
                    break;
                case 2:
                    index = this.frontFaceTexture;
                    break;
                case 3:
                    index = this.leftFaceTexture;
                    break;
                case 4:
                    index = this.bottomFaceTexture;
                    break;
                case 5:
                    index = this.backFaceTexture;
                    break;
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Block GetTextureID: " + ex.Message);
        }

        return index;
    }
}

public enum BlockID {
    Air = 0,
    Bedrock = 1,
    Stone = 2,
    Grass = 3,
    Dirt = 4,
    Water = 5,
    Sand = 6,
    Wood = 7,
    Leaf = 8
}