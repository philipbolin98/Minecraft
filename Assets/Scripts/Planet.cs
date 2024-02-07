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

    public Chunk[,] chunks;
    private List<Vector2Int> activeChunks = new List<Vector2Int>();
    public Queue<GameObject> unusedChunkObjects = new Queue<GameObject>();

    private Thread chunkUpdateThread;

    private List<Vector2Int> chunksToPopulate = new List<Vector2Int>();
    private object chunksToPopulateLock = new object();

    public List<Vector2Int> chunksToMesh = new List<Vector2Int>();
    public object chunksToMeshLock = new object();

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

            this.chunkUpdateThread = new Thread(new ThreadStart(GenerateChunks));
            this.chunkUpdateThread.Start();

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

                    Vector2Int normalizedChunkPos = HelperFunctions.GetNormalizedChunkPos(this, chunkPos.x, chunkPos.y);
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
                    Vector2Int normalizedChunkPos = HelperFunctions.GetNormalizedChunkPos(this, chunkPos.x, chunkPos.y);
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

        } catch (Exception ex) {
            Debug.LogError("Error in Planet CheckRenderDistance: " + ex.Message);
        }
    }

    public void GenerateChunks() {
        while (gameRunning) {
            try {

                if (chunksToPopulate.Count > 0) {

                    Vector2Int chunkPos = chunksToPopulate[0];

                    lock (chunksToPopulateLock) {
                        chunksToPopulate.RemoveAt(0);
                    }

                    Chunk chunk = this.chunks[chunkPos.x, chunkPos.y];
                    chunk.Popuplate(this);
                }

                if (chunksToMesh.Count > 0) {

                    Vector2Int chunkPos;
                    bool proceed = false;

                    lock (chunksToMeshLock) {
                        chunkPos = chunksToMesh[0];

                        if (HelperFunctions.CanChunkMeshBeCreated(this, chunkPos)) {
                            proceed = true;
                            chunksToMesh.Remove(chunkPos);
                        }
                    }

                    if (proceed) {

                        //Debug.Log(DateTime.Now + " Creating mesh for chunk: " + chunkPos);
                        Chunk chunk = this.chunks[chunkPos.x, chunkPos.y];
                        chunk.CreateMeshData();
                    }
                }

            } catch (Exception ex) {
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
                Vector2Int normalizedChunkPos = HelperFunctions.GetNormalizedChunkPos(this, chunkPos.x, chunkPos.y);
                Chunk chunk = this.chunks[normalizedChunkPos.x, normalizedChunkPos.y];

                if (!chunk.isRendered && chunk.isDataPopulated && chunk.isMeshGenerated) {
                    //Debug.Log(DateTime.Now + " Rendering chunk: " + chunkPos);
                    chunk.Enable(chunkPos, normalizedChunkPos);
                }
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Planet GenerateChunks: " + ex.Message);
        }
    }  
}

[System.Serializable]
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
    Sand = 6
}