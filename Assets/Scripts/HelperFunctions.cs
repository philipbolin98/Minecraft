using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class HelperFunctions {
    
    public static Vector2Int GetChunkPosFromBlockPos(int blockX, int blockZ) {
		try {

            Vector2Int normalizedBlockPos = GetNormalizedBlockPos(blockX, blockZ);

            int chunkX = Mathf.FloorToInt(normalizedBlockPos.x / VoxelData.ChunkSize);
            int chunkZ = Mathf.FloorToInt(normalizedBlockPos.y / VoxelData.ChunkSize);

            return new Vector2Int(chunkX, chunkZ);

		} catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions GetChunkPosFromBlockPos: " + ex.Message);
            return new Vector2Int();
        }
	}

    public static Vector2Int GetNormalizedBlockPos(int blockX, int blockZ) {
        try {

            return new Vector2Int(blockX % Planet.PlanetSizeInVoxels, blockZ % Planet.PlanetSizeInVoxels);

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions GetNormalizedBlockPos: " + ex.Message);
            return new Vector2Int();
        }
    }

    public static Vector2Int GetNormalizedChunkPos(int chunkX, int chunkZ) {
        try {

            return new Vector2Int(chunkX % Planet.PlanetSizeInChunks, chunkZ % Planet.PlanetSizeInChunks);

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions GetNormalizedChunkPos: " + ex.Message);
            return new Vector2Int();
        }
    }

    public static bool CheckVoxelInChunk(Chunk chunk, int blockXInChunk, int blockY, int blockZInChunk) {
        try {

            if (!IsVoxelInChunk(blockXInChunk, blockY, blockZInChunk)) {
                int blockX = (VoxelData.ChunkSize * chunk.pos.x) + blockXInChunk;
                int blockZ = (VoxelData.ChunkSize * chunk.pos.y) + blockZInChunk;
                return CheckVoxelInPlanet(chunk.planet, blockX, blockY, blockZ);
            }

            byte blockType = chunk.data.blocks[blockXInChunk, blockY, blockZInChunk];
            return chunk.planet.blockTypes[blockType].isSolid;

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions CheckVoxelInChunk: " + ex.Message);
            return false;
        }
    }

    public static bool CheckVoxelInPlanet(Planet planet, int blockX, int blockY, int blockZ) {
        try {

            byte blockType = GetVoxel(planet, blockX, blockY, blockZ);
            return planet.blockTypes[blockType].isSolid;

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions CheckVoxelInPlanet: " + ex.Message);
            return false;
        }
    }

    public static byte GetVoxel(Planet planet, int blockX, int blockY, int blockZ) {
        try {

            if (!IsVoxelInPlanet(blockX, blockY, blockZ)) {
                return 0;
            }

            Vector2Int chunkPos = GetChunkPosFromBlockPos(blockX, blockZ);
            ChunkData chunkData = planet.data.RequestChunk(chunkPos.x, chunkPos.y);

            int blockXInChunk = blockX % VoxelData.ChunkSize;
            int blockZInChunk = blockZ % VoxelData.ChunkSize;

            return chunkData.blocks[blockXInChunk, blockY, blockZInChunk];

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions GetVoxel: " + ex.Message); 
            return 0;
        }
    }

    public static bool IsVoxelInPlanet(int x, int y, int z) {
        try {

            return x >= 0 && x < Planet.PlanetSizeInVoxels && y >= 0 && y < Planet.WorldHeight && z >= 0 && z < Planet.PlanetSizeInVoxels;

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions IsVoxelInPlanet: " + ex.Message);
            return false;
        }
    }

    public static bool IsVoxelInChunk(int x, int y, int z) {
        try {

            return x >= 0 && x < VoxelData.ChunkSize && y >= 0 && y < Planet.WorldHeight && z >= 0 && z < VoxelData.ChunkSize;

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions IsVoxelInChunk: " + ex.Message);
            return false;
        }
    }
}
