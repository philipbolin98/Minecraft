using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Noise;

public class Planet : MonoBehaviour {

    public static readonly int PlanetSizeInChunks = 16;
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
    public BlockType[] blockTypes;

    public int seaLevel;

    private OpenSimplex2S Noise4D;
    private int seed;

    public Chunk[,] chunks = new Chunk[PlanetSizeInChunks, PlanetSizeInChunks];
    public List<Vector2Int> activeChunks = new List<Vector2Int>();
    public Queue<GameObject> unusedChunkObjects = new Queue<GameObject>();

    public PlanetData data;

    private void Start() {
        try {

            int seed = Mathf.FloorToInt(Random.Range(0f, 1f) * 100);

            this.seed = seed;
            Random.InitState(seed);
            Noise4D = new OpenSimplex2S(seed);

            data = new PlanetData(this.gameObject.name, seed); //SaveSystem.LoadPlanet(this.gameObject.name, seed);
            data.LoadAllChunkData(this);

            //if (planetData.initialLoad) {
            //    SaveSystem.SavePlanet(planetData);
            //    planetData.initialLoad = false;
            //}

            spawnPosition = new Vector3(1, WorldHeight, 1); //Need to read player spawn position from file once saving is implemented

            GeneratePlanet(spawnPosition);
            
            Instantiate(player);
            player.transform.position = spawnPosition;

        } catch (System.Exception ex) {
            Debug.LogError("Error in Planet Start: " + ex.Message);
        }
    }

    /// <summary>
    /// Generates all the chunk game objects in the current planet
    /// </summary>
    /// <param name="pos">The position to spawn the chunks around</param>
    public void GeneratePlanet(Vector3 pos) {
        try {

            Vector2Int centerChunk = HelperFunctions.GetChunkPosFromBlockPos(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

            int minChunkX = centerChunk.x - ViewDistanceInChunks;
            int maxChunkX = centerChunk.x + ViewDistanceInChunks;

            int minChunkZ = centerChunk.y - ViewDistanceInChunks;
            int maxChunkZ = centerChunk.y + ViewDistanceInChunks;

            for (int i = activeChunks.Count - 1; i >= 0; i--) {

                Vector2Int chunkPos = activeChunks[i];
                int chunkX = chunkPos.x;
                int chunkZ = chunkPos.y;

                if (chunkX < minChunkX || chunkX > maxChunkX || chunkZ < minChunkZ || chunkZ > maxChunkZ) {
                    Vector2Int normalizedChunkPos = HelperFunctions.GetNormalizedChunkPos(chunkX, chunkZ);
                    this.chunks[normalizedChunkPos.x, normalizedChunkPos.y].Disable();
                    activeChunks.Remove(activeChunks[i]);
                }
            }

            for (int x = minChunkX; x <= maxChunkX; x++) {
                for (int z = minChunkZ; z <= maxChunkZ; z++) {

                    Vector2Int chunkPos = new Vector2Int(x, z);
                    Vector2Int normalizedChunkPos = HelperFunctions.GetNormalizedChunkPos(x, z);

                    if (!activeChunks.Contains(chunkPos)) {

                        Chunk chunk = this.chunks[normalizedChunkPos.x, normalizedChunkPos.y];

                        if (chunk != null) {
                            chunk.Enable(chunkPos, normalizedChunkPos);
                        } else {
                            chunk = new Chunk(this, chunkPos, normalizedChunkPos);
                            this.chunks[normalizedChunkPos.x, normalizedChunkPos.y] = chunk;
                        }

                        activeChunks.Add(chunkPos);
                    }
                }
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function Planet.GeneratePlanet: " + e.Message);
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
public class BlockType {

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

        } catch (System.Exception e) {
            Debug.LogError("Error in function Planet.GetTextureID: " + e.Message);
            return 0;
        }
    }
}