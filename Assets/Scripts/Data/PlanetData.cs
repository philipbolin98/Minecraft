using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlanetData {

    public string planetName;
    public int seed;
    public bool initialLoad;

    [System.NonSerialized]
    public Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();

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
    /// Loads all chunk data when creating a new planetdata object
    /// </summary>
    public void LoadAllChunkData(Planet planet) {
        try {

            for (int x = 0; x < VoxelData.PlanetSizeInChunks; x++) {
                for (int z = 0; z < VoxelData.PlanetSizeInChunks; z++) {
                    LoadChunkData(new Vector2Int(x, z), planet);
                }
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function PlanetData.LoadAllChunkData: " + e.Message);
        }
    }

    public void LoadChunkData(Vector2Int pos, Planet planet) {
        try {

            if (chunks.ContainsKey(pos)) {
                return;
            }

            ChunkData chunkData = SaveSystem.LoadChunk(planetName, pos);

            if (chunkData != null) {
                chunks.Add(pos, chunkData);

            } else {
                chunks.Add(pos, new ChunkData(pos, planet));
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function PlanetData.LoadChunkData: " + e.Message);
        }
    }

    public ChunkData RequestChunk(Vector2Int pos) {
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

    public static bool IsVoxelInPlanet(Vector3 pos) {
        try {

            if (pos.x >= 0 && pos.x < VoxelData.PlanetSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.WorldHeight && pos.z >= 0 && pos.z < VoxelData.PlanetSizeInVoxels) {
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
            int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkSize);

            ChunkData chunk = RequestChunk(new Vector2Int(x, z));

            x *= VoxelData.ChunkSize;
            z *= VoxelData.ChunkSize;

            Vector3Int voxel = new Vector3Int((int)(pos.x - x), (int)(pos.y), (int)(pos.z - z));

            return chunk.voxelMap[voxel.x, voxel.y, voxel.z];

        } catch (System.Exception e) {
            Debug.LogError("Error in function PlanetData.GetVoxel: " + e.Message);
            return 0;
        }
    }
}