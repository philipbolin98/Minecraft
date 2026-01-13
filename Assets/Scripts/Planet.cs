using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System;
using System.Collections.Concurrent;
using diagnostics = System.Diagnostics;

public class Planet : MonoBehaviour {

    public string planetName = "test";

    public static int seed;
    public int PlanetSizeInChunks;
    [HideInInspector]
    public int PlanetSizeInVoxels;

    public int PlanetHeightInChunks;
    [HideInInspector]
    public int PlanetHeightInVoxels;

    public float gravity;
    public float terminalVelocity;

    public GameObject gameUI;
    public GameObject pauseMenu;
    public GameObject settingsMenu;
   
    public GameObject player;
    public GameObject chunkPrefab;
    public Vector3 spawnPosition;
    public Block[] blockTypes;
    public Biome[] biomes;

    public Chunk[,,] chunks;
    public List<Chunk> activeChunks = new();
    public Dictionary<SerializableVector3Int, ChunkData> modifiedChunks = new();
    public Queue<GameObject> unusedChunkObjects = new();

    private EventWaitHandle populateChunksWaitHandle = new(false, EventResetMode.AutoReset);
    private EventWaitHandle createChunkMeshesWaitHandle = new(false, EventResetMode.AutoReset);
    private Semaphore threadPoolSemaphore = new(Settings.maxThreadPoolThreads, Settings.maxThreadPoolThreads);
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

    public bool gamePaused = false;
    private bool gameRunning = true;

    public Mesh targetedBlockMesh;

    private void Start() {
        try {

            if (Settings.gameSettings == null) {
                Settings.gameSettings = new GameSettings() {
                    renderDistance = DefaultGameSettings.renderDistance,
                    mouseSensitivity = DefaultGameSettings.mouseSensitivity
                };
            }

            SaveSystem.LoadPlanet(this);

            this.PlanetSizeInVoxels = PlanetSizeInChunks * Chunk.ChunkSize;
            this.PlanetHeightInVoxels = PlanetHeightInChunks * Chunk.ChunkSize;

            this.chunks = new Chunk[this.PlanetSizeInChunks, this.PlanetHeightInChunks, this.PlanetSizeInChunks];

            LoadExistingChunks();

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

    public void LoadExistingChunks() {
        try {

            foreach (KeyValuePair<SerializableVector3Int, ChunkData> kvp in this.modifiedChunks) {
                SerializableVector3Int serializablePos = kvp.Key;
                Vector3Int pos = new Vector3Int(serializablePos.x, serializablePos.y, serializablePos.z);
                ChunkData chunkData = kvp.Value;
                Chunk chunk = new Chunk(this, pos, pos, chunkData);
                chunk.stage = ChunkStage.Populated;
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Planet LoadExistingChunks: " + ex.Message);
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

            Vector3Int centerChunk = HelperFunctions.GetChunkPosFromBlockPos(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

            int minChunkX = centerChunk.x - Settings.gameSettings.renderDistance;
            int maxChunkX = centerChunk.x + Settings.gameSettings.renderDistance;

            int minChunkY = Math.Max(centerChunk.y - Settings.gameSettings.renderDistance, 0);
            int maxChunkY = Math.Min(centerChunk.y + Settings.gameSettings.renderDistance, this.PlanetHeightInChunks - 1);

            int minChunkZ = centerChunk.z - Settings.gameSettings.renderDistance;
            int maxChunkZ = centerChunk.z + Settings.gameSettings.renderDistance;

            //diagnostics.Stopwatch watch = diagnostics.Stopwatch.StartNew();

            for (int i = this.activeChunks.Count - 1; i >= 0; i--) {

                Chunk chunk = this.activeChunks[i];
                Vector3 chunkPos = chunk.pos;

                if (chunkPos.x < minChunkX || chunkPos.x > maxChunkX || chunkPos.y < minChunkY || chunkPos.y > maxChunkY || chunkPos.z < minChunkZ || chunkPos.z > maxChunkZ) {
                    chunk.Disable();
                    this.activeChunks.RemoveAt(i);
                }
            }

            //Debug.Log($"Disabling out of view chunks: {watch.Elapsed.TotalSeconds}");
            //watch.Restart();

            List<Vector3Int> chunks = new();

            //All chunks within the radius should populated
            for (int x = minChunkX; x <= maxChunkX; x++) {
                for (int y = minChunkY; y <= maxChunkY; y++) {
                    for (int z = minChunkZ; z <= maxChunkZ; z++) {
                        chunks.Add(new Vector3Int(x, y, z));
                    }
                }
            }

            //Debug.Log($"Adding coords to chunks list: {watch.Elapsed.TotalSeconds}");
            //watch.Restart();

            chunks.Sort(new ChunkComparer(centerChunk));

            //Debug.Log($"Sorting chunks list: {watch.Elapsed.TotalSeconds}");
            //watch.Restart();

            foreach (Vector3Int chunkPos in chunks) {

                Vector3Int normalizedChunkPos = HelperFunctions.NormalizeChunkPos(this, chunkPos.x, chunkPos.y, chunkPos.z);
                Chunk chunk = this.chunks[normalizedChunkPos.x, normalizedChunkPos.y, normalizedChunkPos.z];

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

            //Debug.Log($"Enabling chunks in view: {watch.Elapsed.TotalSeconds}");

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

            bool topLayer = chunk.normalizedPos.y == chunk.planet.PlanetHeightInChunks - 1;
            bool bottomLayer = chunk.normalizedPos.y == 0;

            Chunk frontChunk = HelperFunctions.GetNeighboringChunk(chunk, Vector3Int.forward);
            if (frontChunk != null && frontChunk.stage >= ChunkStage.Populated) {
                Chunk rightChunk = HelperFunctions.GetNeighboringChunk(chunk, Vector3Int.right);
                if (rightChunk != null && rightChunk.stage >= ChunkStage.Populated) {
                    if (topLayer) {
                        if (chunk.planet.chunksToMesh.TryEnqueue(chunk)) {
                            chunk.stage = ChunkStage.CreatingMesh;
                            chunk.planet.createChunkMeshesWaitHandle.Set();
                        }
                    } else {
                        Chunk upChunk = HelperFunctions.GetNeighboringChunk(chunk, Vector3Int.up);
                        if (upChunk != null && upChunk.stage >= ChunkStage.Populated) {
                            if (chunk.planet.chunksToMesh.TryEnqueue(chunk)) {
                                chunk.stage = ChunkStage.CreatingMesh;
                                chunk.planet.createChunkMeshesWaitHandle.Set();
                            }
                        }
                    }
                }
            }

            Chunk leftChunk = HelperFunctions.GetNeighboringChunk(chunk, Vector3Int.left);
            if (leftChunk != null && leftChunk.stage == ChunkStage.Populated) {
                Chunk leftFrontChunk = HelperFunctions.GetNeighboringChunk(leftChunk, Vector3Int.forward);
                if (leftFrontChunk != null && leftFrontChunk.stage >= ChunkStage.Populated) {
                    if (topLayer) {
                        if (leftChunk.planet.chunksToMesh.TryEnqueue(leftChunk)) {
                            leftChunk.stage = ChunkStage.CreatingMesh;
                            leftChunk.planet.createChunkMeshesWaitHandle.Set();
                        }
                    } else {
                        Chunk leftTopChunk = HelperFunctions.GetNeighboringChunk(leftChunk, Vector3Int.up);
                        if (leftTopChunk != null && leftTopChunk.stage >= ChunkStage.Populated) {
                            if (leftChunk.planet.chunksToMesh.TryEnqueue(leftChunk)) {
                                leftChunk.stage = ChunkStage.CreatingMesh;
                                leftChunk.planet.createChunkMeshesWaitHandle.Set();
                            }
                        }
                    }
                }
            }

            Chunk backChunk = HelperFunctions.GetNeighboringChunk(chunk, Vector3Int.back);
            if (backChunk != null && backChunk.stage == ChunkStage.Populated) {
                Chunk backRightChunk = HelperFunctions.GetNeighboringChunk(backChunk, Vector3Int.right);
                if (backRightChunk != null && backRightChunk.stage >= ChunkStage.Populated) {
                    if (topLayer) {
                        if (backChunk.planet.chunksToMesh.TryEnqueue(backChunk)) {
                            backChunk.stage = ChunkStage.CreatingMesh;
                            backChunk.planet.createChunkMeshesWaitHandle.Set();
                        }
                    } else {
                        Chunk backTopChunk = HelperFunctions.GetNeighboringChunk(backChunk, Vector3Int.up);
                        if (backTopChunk != null && backTopChunk.stage >= ChunkStage.Populated) {
                            if (backChunk.planet.chunksToMesh.TryEnqueue(backChunk)) {
                                backChunk.stage = ChunkStage.CreatingMesh;
                                backChunk.planet.createChunkMeshesWaitHandle.Set();
                            }
                        }
                    }
                }
            }

            if (!bottomLayer) {
                Chunk bottomChunk = HelperFunctions.GetNeighboringChunk(chunk, Vector3Int.down);
                if (bottomChunk != null && bottomChunk.stage == ChunkStage.Populated) {
                    Chunk bottomRightChunk = HelperFunctions.GetNeighboringChunk(bottomChunk, Vector3Int.right);
                    if (bottomRightChunk != null && bottomRightChunk.stage >= ChunkStage.Populated) {
                        if (topLayer) {
                            if (bottomChunk.planet.chunksToMesh.TryEnqueue(bottomChunk)) {
                                bottomChunk.stage = ChunkStage.CreatingMesh;
                                bottomChunk.planet.createChunkMeshesWaitHandle.Set();
                            }
                        } else {
                            Chunk bottomFrontChunk = HelperFunctions.GetNeighboringChunk(bottomChunk, Vector3Int.forward);
                            if (bottomFrontChunk != null && bottomFrontChunk.stage >= ChunkStage.Populated) {
                                if (bottomChunk.planet.chunksToMesh.TryEnqueue(bottomChunk)) {
                                    bottomChunk.stage = ChunkStage.CreatingMesh;
                                    bottomChunk.planet.createChunkMeshesWaitHandle.Set();
                                }
                            }
                        }
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

    public void PauseGame() {
        try {

            this.gamePaused = true;
            this.gameUI.SetActive(false);
            this.pauseMenu.SetActive(true);

            Cursor.lockState = CursorLockMode.None;

        } catch (Exception ex) {
            Debug.LogError("Error in Planet PauseGame: " + ex.Message);
        }
    }

    public void ResumeGame() {
        try {

            this.gamePaused = false;
            this.gameUI.SetActive(true);
            this.pauseMenu.SetActive(false);
            this.settingsMenu.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;

        } catch (Exception ex) {
            Debug.LogError("Error in Planet ResumeGame: " + ex.Message);
        }
    }
}

[Serializable]
public class PlanetData {

    public int seed;
    public string name;
    public int sizeInChunks;
    public Dictionary<SerializableVector3Int, ChunkData> modifiedChunks;

    public PlanetData(Planet planet) {
        try {

            this.seed = Planet.seed;
            this.name = planet.name;
            this.sizeInChunks = planet.PlanetSizeInChunks;
            this.modifiedChunks = planet.modifiedChunks;

        } catch (Exception ex) {
            Debug.LogError("Error in PlanetData Constructor: " + ex.Message);
        }
    }
}

[Serializable]
public class SerializableVector3Int {
    public int x;
    public int y;
    public int z;
}

public class ChunkComparer : IComparer<Vector3Int> {

    private Vector3Int chunkPos;

    public ChunkComparer(Vector3Int chunkPos) {
        this.chunkPos = chunkPos;
    }

    int IComparer<Vector3Int>.Compare(Vector3Int chunkPos1, Vector3Int chunkPos2) {

        int dx1 = Mathf.Abs(this.chunkPos.x - chunkPos1.x);
        int dy1 = Mathf.Abs(this.chunkPos.y - chunkPos1.y);
        int dz1 = Mathf.Abs(this.chunkPos.z - chunkPos1.z);

        int dx2 = Mathf.Abs(this.chunkPos.x - chunkPos2.x);
        int dy2 = Mathf.Abs(this.chunkPos.y - chunkPos2.y);
        int dz2 = Mathf.Abs(this.chunkPos.z - chunkPos2.z);

        int ring1 = Mathf.Max(dx1, dy1, dz1);
        int ring2 = Mathf.Max(dx2, dy2, dz2);

        if (ring1 > ring2) {
            return 1;
        } else if (ring2 > ring1) {
            return -1;
        } else {

            int d1 = dx1 + dy1 + dz1;
            int d2 = dx2 + dy2 + dz2;

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
    readonly HashSet<Vector3Int> set = new();
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