using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System;
using System.Collections.Concurrent;

public class Planet : MonoBehaviour {

    public static int seed;
    public int PlanetSizeInChunks;
    [HideInInspector]
    public int PlanetSizeInVoxels;
    public int ViewDistanceInChunks;
    public int WorldHeight;
    public float gravity;

    public GameObject player;
    public GameObject chunkPrefab;
    public Vector3 spawnPosition;
    public Block[] blockTypes;
    public Biome[] biomes;

    public Chunk[,] chunks;
    public List<Chunk> activeChunks = new();
    public Queue<GameObject> unusedChunkObjects = new();

    private EventWaitHandle populateChunksWaitHandle = new(false, EventResetMode.AutoReset);
    private EventWaitHandle createChunkMeshesWaitHandle = new(false, EventResetMode.AutoReset);
    private const int maxThreads = 8;
    private Semaphore threadPoolSemaphore = new(maxThreads, maxThreads);
    private Thread chunkPopulateThread;
    private Thread meshCreationThread;

    private ChunkQueue chunksToMesh = new();
    private ConcurrentQueue<Chunk> chunksToPopulateQueue = new();
    private ConcurrentQueue<Chunk> chunksToRenderQueue = new();

    [HideInInspector]
    public TerrainGenerator terrainGenerator;
    [HideInInspector]
    public StructureGenerator structureGenerator;

    public Material material;
    public Material transparentMaterial;
    public Material waterMaterial;

    private bool gameRunning = true;

    private void Start() {
        try {

            this.PlanetSizeInVoxels = PlanetSizeInChunks * Chunk.ChunkSize;

            this.chunks = new Chunk[this.PlanetSizeInChunks, this.PlanetSizeInChunks];

            UnityEngine.Random.InitState(Planet.seed);

            this.terrainGenerator = this.gameObject.GetComponent<TerrainGenerator>();
            this.terrainGenerator.Initialize(this);

            this.structureGenerator = this.gameObject.GetComponent<StructureGenerator>();
            this.structureGenerator.Initialize(this);

            this.chunkPopulateThread = new Thread(new ThreadStart(PopulateChunks));
            this.chunkPopulateThread.Start();

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
            populateChunksWaitHandle.Set();
            chunkPopulateThread.Join();
            createChunkMeshesWaitHandle.Set();
            meshCreationThread.Join();
            Array.Clear(this.chunks, 0, this.chunks.Length);
            this.chunksToPopulateQueue.Clear();
            this.chunksToMesh.Clear();
            this.chunksToRenderQueue.Clear();
            this.activeChunks.Clear();
            this.unusedChunkObjects.Clear();
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

            for (int i = this.activeChunks.Count - 1; i >= 0; i--) {

                Chunk chunk = this.activeChunks[i];
                Vector2Int chunkPos = chunk.pos;

                if (chunkPos.x < minChunkX || chunkPos.x > maxChunkX || chunkPos.y < minChunkZ || chunkPos.y > maxChunkZ) {
                    chunk.Disable();
                    this.activeChunks.RemoveAt(i);
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
                    chunk = new Chunk(this, chunkPos, normalizedChunkPos);
                } else {
                    chunk.pos = chunkPos;
                }

                if (chunk.stage == ChunkStage.Null) {
                    chunk.stage = ChunkStage.Populating;
                    chunksToPopulateQueue.Enqueue(chunk);
                    populateChunksWaitHandle.Set();
                } else if (chunk.stage == ChunkStage.Disabled) {
                    chunk.stage = ChunkStage.Rendering;
                    chunksToRenderQueue.Enqueue(chunk);
                }
            }
            
        } catch (Exception ex) {
            Debug.LogError("Error in Planet CheckRenderDistance: " + ex.Message);
        }
    }

    public void PopulateChunks() {
        while (gameRunning) {
            try {

                populateChunksWaitHandle.WaitOne();

                while (gameRunning && chunksToPopulateQueue.Count > 0) {
                    threadPoolSemaphore.WaitOne();
                    chunksToPopulateQueue.TryDequeue(out Chunk chunk);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(PopulateChunk), chunk);
                }

            } catch (Exception ex) {
                Debug.LogError("Error in Planet PopulateChunks: " + ex.Message);
            }
        }
    }

    public void CreateChunkMeshes() {
        while (gameRunning) {
            try {

                createChunkMeshesWaitHandle.WaitOne();

                while (gameRunning && chunksToMesh.Count > 0) {
                    threadPoolSemaphore.WaitOne();
                    chunksToMesh.TryDequeue(out Chunk chunk);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(CreateChunkMesh), chunk);
                }

            } catch (Exception ex) {
                Debug.LogError("Error in Planet CreateChunkMeshes: " + ex.Message);
            }
        }
    }

    public static void PopulateChunk(object obj) {
        try {

            Chunk chunk = (Chunk)obj;
            chunk.Popuplate();

            Chunk upChunk = HelperFunctions.GetNeighboringChunk(chunk, Vector2Int.up);
            if (upChunk != null && upChunk.stage >= ChunkStage.Populated) {
                Chunk rightChunk = HelperFunctions.GetNeighboringChunk(chunk, Vector2Int.right);
                if (rightChunk != null && rightChunk.stage >= ChunkStage.Populated) {
                    if (chunk.planet.chunksToMesh.TryEnqueue(chunk)) {
                        chunk.stage = ChunkStage.CreatingMesh;
                        chunk.planet.createChunkMeshesWaitHandle.Set();
                    }
                }
            }

            Chunk leftChunk = HelperFunctions.GetNeighboringChunk(chunk, Vector2Int.left);
            if (leftChunk != null && leftChunk.stage == ChunkStage.Populated) {
                Chunk leftUpChunk = HelperFunctions.GetNeighboringChunk(leftChunk, Vector2Int.up);
                if (leftUpChunk != null && leftUpChunk.stage >= ChunkStage.Populated) {
                    if (leftChunk.planet.chunksToMesh.TryEnqueue(leftChunk)) {
                        leftChunk.stage = ChunkStage.CreatingMesh;
                        leftChunk.planet.createChunkMeshesWaitHandle.Set();
                    }
                }
            }

            Chunk downChunk = HelperFunctions.GetNeighboringChunk(chunk, Vector2Int.down);
            if (downChunk != null && downChunk.stage == ChunkStage.Populated) {
                Chunk downRightChunk = HelperFunctions.GetNeighboringChunk(downChunk, Vector2Int.right);
                if (downRightChunk != null && downRightChunk.stage >= ChunkStage.Populated) {
                    if (downChunk.planet.chunksToMesh.TryEnqueue(downChunk)) {
                        downChunk.stage = ChunkStage.CreatingMesh;
                        downChunk.planet.createChunkMeshesWaitHandle.Set();
                    }
                }
            }

            chunk.planet.threadPoolSemaphore.Release();

        } catch (Exception ex) {
            Debug.LogError("Error in Planet PopulateChunk: " + ex.Message);
        }
    }

    public static void CreateChunkMesh(object obj) {
        try {

            Chunk chunk = (Chunk)obj;
            chunk.CreateMeshData();
            chunk.stage = ChunkStage.Rendering;
            chunk.planet.chunksToRenderQueue.Enqueue(chunk);
            chunk.planet.threadPoolSemaphore.Release();

        } catch (Exception ex) {
            Debug.LogError("Error in Planet CreateChunkMesh: " + ex.Message);
        }
    }

    /// <summary>
    /// Generates all the chunk game objects in the current planet
    /// </summary>
    /// <param name="pos">The position to spawn the chunks around</param>
    public void RenderChunks() {
        try {

            while (this.chunksToRenderQueue.Count > 0) {
                this.chunksToRenderQueue.TryDequeue(out Chunk chunk);
                chunk.Enable();
                this.activeChunks.Add(chunk);
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

public class ChunkQueue {
    readonly List<Chunk> queue = new();
    readonly HashSet<Vector2Int> set = new();
    readonly object lockObject = new();

    public int Count {
        get {
            lock (lockObject) {
                return set.Count;
            }
        }
    }

    public bool TryDequeue(out Chunk chunk) {
        lock (lockObject) {
            if (set.Count == 0) {
                chunk = null;
                return false;
            } else {
                chunk = queue[0];
                queue.RemoveAt(0);
                set.Remove(chunk.normalizedPos);
                return true;
            }
        }
    }

    public bool TryEnqueue(Chunk chunk) {
        lock (lockObject) {
            if (set.Add(chunk.normalizedPos)) {
                queue.Add(chunk);
                return true;
            } else {
                return false;
            }
        }
    }

    //public bool TryRemove(Vector2Int chunkPos) {
    //    lock (lockObject) {
    //        if (set.Remove(chunkPos)) {
    //            int index = queue.IndexOf(chunkPos);
    //            return true;
    //        } else {
    //            return false;
    //        }
    //    }
    //}

    public bool Contains(Chunk chunk) {
        lock (lockObject) {
            return set.Contains(chunk.normalizedPos);
        }
    }

    public void Clear() {
        lock (lockObject) {
            queue.Clear();
            set.Clear();
        }
    }
}