using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Noise;

public class Planet{

    public GameObject planetObject;
    public int seaLevel;

    private OpenSimplex2S Noise4D;
    private int seed;

    Chunk[,,] chunks = new Chunk[VoxelData.PlanetSizeInChunks, VoxelData.PlanetSizeInChunks, VoxelData.PlanetSizeInChunks];
    //List<Vector3> activeChunks = new List<Vector3>();

    public PlanetData planetData;

    public Planet(int seed) {
        try {

            this.seed = seed;
            Random.InitState(seed);
            Noise4D = new OpenSimplex2S(seed);

            planetObject = new GameObject("Planet " + seed);
            planetObject.transform.SetParent(UniverseData.universe.transform);

            planetData = SaveSystem.LoadPlanet(planetObject.name, seed);
            planetData.LoadAllChunkData(this);

            if (planetData.initialLoad) {
                SaveSystem.SavePlanet(planetData);
                planetData.initialLoad = false;
            }

            GeneratePlanet();

        } catch (System.Exception e) {
            Debug.LogError("Error in function Planet.Constructor: " + e.Message);
        }
    }

    /// <summary>
    /// Generates all the chunk game objects in the current planet
    /// </summary>
    public void GeneratePlanet() {
        try {

            for (int x = 0; x < VoxelData.PlanetSizeInChunks; x++) {
                for (int z = 0; z < VoxelData.PlanetSizeInChunks; z++) {
                    for (int y = 0; y < VoxelData.PlanetSizeInChunks; y++) {

                        Vector3Int newChunkCoord = new Vector3Int(x, y, z);
                        chunks[x, y, z] = new Chunk(newChunkCoord, this);
                        //activeChunks.Add(newChunkCoord);
                    }
                }
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function Planet.GeneratePlanet: " + e.Message);
        }
    }

    public void CheckViewDistance(Vector3 pos) {
        try {



        } catch (System.Exception e) {
            Debug.LogError("Error in function Planet.CheckViewDistance: " + e.Message);
        }
    }

    /// <summary>
    /// Gets the block type of a voxel at a given position in the planet
    /// </summary>
    /// <param name="pos">The position of the voxel in the planet</param>
    /// <returns>An integer representing the block type</returns>
    public byte GetVoxel(Vector3Int pos) {
        try {

            byte voxelValue;
            int terrainHeight = VoxelData.SurfaceHeightInVoxels + Mathf.FloorToInt(VoxelData.SurfaceHeightInVoxels * (float)TorusNoise(pos.x, pos.z));

            if (!PlanetData.IsVoxelInPlanet(pos) || pos.y > terrainHeight) //Air for everything above the surface
                return 0;

            if (pos.y < 1) //Bedrock for y = 0
                voxelValue = 1;
            else if (pos.y == terrainHeight) //Grass for the top layer
                voxelValue = 3;
            else if (pos.y > terrainHeight - 4) //Dirt for next 3 layers
                voxelValue = 4;
            else //Everything else stone
                voxelValue = 2;

            return voxelValue;

        } catch (System.Exception e) {
            Debug.LogError("Error in function Planet.GetVoxel: " + e.Message);
            return 0;
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

            float nx = x / VoxelData.PlanetSizeInVoxels;
            float nz = z / VoxelData.PlanetSizeInVoxels;

            float angle_x = VoxelData.TAU * nx;
            float angle_z = VoxelData.TAU * nz;

            double cosx = Mathf.Cos(angle_x) / VoxelData.TAU;
            double sinx = Mathf.Sin(angle_x) / VoxelData.TAU;
            double cosz = Mathf.Cos(angle_z) / VoxelData.TAU;
            double sinz = Mathf.Sin(angle_z) / VoxelData.TAU;

            return Noise4D.Noise4_Classic(cosx, sinx, cosz, sinz);

        } catch (System.Exception e) {
            Debug.LogError("Error in function Planet.TorusNoise: " + e.Message);
            return 0;
        }
    }

    public bool CheckForVoxel(Vector3 pos) {
        try {

            byte blockType = planetData.GetVoxel(pos);
            return UniverseData.universe.blockTypes[blockType].isSolid;

        } catch (System.Exception e) {
            Debug.LogError("Error in function Planet.CheckForVoxel: " + e.Message);
            return false;
        }
    }
}

[System.Serializable]
public class BlockType{

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