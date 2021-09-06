using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlanetData {

    public string planetName;
    public int seed;
    public bool initialLoad;

    [System.NonSerialized]
    public Dictionary<Vector3Int, ChunkData> chunks = new Dictionary<Vector3Int, ChunkData>();

    [System.NonSerialized]
    public List<ChunkData> modifiedChunks = new List<ChunkData>();

    /// <summary>
    /// Creates new planet data when save file does not exist
    /// </summary>
    /// <param name="planetName">Name of the planet to create</param>
    /// <param name="seed">Seed of the planet</param>
    public PlanetData(string planetName, int seed) {

        try {

            this.planetName = planetName;
            this.seed = seed;
            initialLoad = true;

        } catch (System.Exception e) {
            Debug.LogError("Error in function PlanetData.Constructor1: " + e.Message);
        }
    }

    /// <summary>
    /// Loads planet data from a save file
    /// </summary>
    /// <param name="planetData">The planet data</param>
    public PlanetData(PlanetData planetData) {

        try {

            planetName = planetData.planetName;
            seed = planetData.seed;
            initialLoad = false;
            LoadAllChunkData();

        } catch (System.Exception e) {
            Debug.LogError("Error in function PlanetData.Constructor2: " + e.Message);
        }
    }

    /// <summary>
    /// Loads all chunk data when creating a new planetdata object
    /// </summary>
    public void LoadAllChunkData() {

        try {

            for (int x = 0; x < VoxelData.PlanetSizeInChunks; x++) {
                for (int z = 0; z < VoxelData.PlanetSizeInChunks; z++) {
                    for (int y = 0; y < VoxelData.PlanetSizeInChunks; y++) {
                        LoadChunkData(new Vector3Int(x, y, z));
                    }
                }
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function PlanetData.LoadAllChunkData: " + e.Message);
        }
    }

    public void LoadChunkData(Vector3Int pos) {

        try {

            if (chunks.ContainsKey(pos)) {
                return;
            }

            ChunkData chunk = SaveSystem.LoadChunk(planetName, pos);

            if (chunk != null) {
                chunks.Add(pos, chunk);

            } else {
                chunks.Add(pos, new ChunkData(pos));
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function PlanetData.LoadChunkData: " + e.Message);
        }
    }

    public ChunkData RequestChunk(Vector3Int pos) {

        try {

            if (chunks.ContainsKey(pos)) {
                return chunks[pos];
            } else {
                return null;
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function PlanetData.RequestChunk: " + e.Message);
            return null;
        }      
    }

    public void AddToModifiedChunkList(ChunkData chunk) {

        try {

            if (!modifiedChunks.Contains(chunk)) {
                modifiedChunks.Add(chunk);
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function PlanetData.AddToModifiedChunkList: " + e.Message);
        }
    }

    public bool IsVoxelInPlanet(Vector3 pos) {

        try {

            if (pos.x >= 0 && pos.x < VoxelData.PlanetSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.PlanetSizeInVoxels && pos.z >= 0 && pos.z < VoxelData.PlanetSizeInVoxels) {
                return true;
            } else {
                return false;
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function PlanetData.IsVoxelInPlanet: " + e.Message);
            return false;
        }
    }

    public byte GetVoxel(Vector3 pos) {

        try {

            if (!IsVoxelInPlanet(pos)) {
                return 0;
            }

            int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkSize);
            int y = Mathf.FloorToInt(pos.y / VoxelData.ChunkSize);
            int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkSize);

            ChunkData chunk = RequestChunk(new Vector3Int(x, y, z));

            x *= VoxelData.ChunkSize;
            y *= VoxelData.ChunkSize;
            z *= VoxelData.ChunkSize;

            Vector3Int voxel = new Vector3Int((int)(pos.x - x), (int)(pos.y - y), (int)(pos.z - z));

            return chunk.voxelMap[voxel.x, voxel.y, voxel.z];

        } catch (System.Exception e) {
            Debug.LogError("Error in function PlanetData.GetVoxel: " + e.Message);
            return 0;
        }
    }
}