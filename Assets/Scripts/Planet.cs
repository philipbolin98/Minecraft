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

    public Material material;
    public GameObject player;
    public Vector3 spawnPosition;
    public BlockType[] blockTypes;

    public int seaLevel;

    private OpenSimplex2S Noise4D;
    private int seed;

    Chunk[,] chunks = new Chunk[PlanetSizeInChunks, PlanetSizeInChunks];
    HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();

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

            spawnPosition = new Vector3(PlanetSizeInVoxels / 2, WorldHeight, PlanetSizeInVoxels / 2);

            GeneratePlanet();
            
            Instantiate(player);
            player.transform.position = spawnPosition;

        } catch (System.Exception ex) {
            Debug.LogError("Error in Planet Start: " + ex.Message);
        }
    }

    /// <summary>
    /// Generates all the chunk game objects in the current planet
    /// </summary>
    public void GeneratePlanet() {
        try {

            Vector2Int spawnChunk = HelperFunctions.GetChunkPosFromBlockPos(Mathf.FloorToInt(spawnPosition.x), Mathf.FloorToInt(spawnPosition.y));

            int minChunkX = spawnChunk.x - ViewDistanceInChunks;
            int maxChunkX = spawnChunk.x + ViewDistanceInChunks;

            int minChunkZ = spawnChunk.y - ViewDistanceInChunks;
            int maxChunkZ = spawnChunk.y + ViewDistanceInChunks;

            for (int x = minChunkX; x <= maxChunkX; x++) {
                for (int z = minChunkZ; z <= maxChunkZ; z++) {
                    chunks[x, z] = new Chunk(this, x, z);
                    activeChunks.Add(new Vector2Int(x, z));
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