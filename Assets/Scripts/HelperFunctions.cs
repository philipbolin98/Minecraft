using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class HelperFunctions {
    
    /// <summary>
    /// Gets the chunk position of a block
    /// </summary>
    /// <param name="blockX"></param>
    /// <param name="blockZ"></param>
    /// <returns></returns>
    public static Vector2Int GetChunkPosFromBlockPos(int blockX, int blockY, int blockZ) {
		try {

            int chunkX = Mathf.FloorToInt(blockX / VoxelData.ChunkSize);
            int chunkZ = Mathf.FloorToInt(blockZ / VoxelData.ChunkSize);

            return new Vector2Int(chunkX, chunkZ);

		} catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions GetChunkPosFromBlockPos: " + ex.Message);
            return new Vector2Int();
        }
	}

    /// <summary>
    /// Gets the position of a block in the world in case of wrapping
    /// </summary>
    /// <param name="blockX"></param>
    /// <param name="blockZ"></param>
    /// <returns></returns>
    public static Vector3Int GetNormalizedBlockPos(int blockX, int blockY, int blockZ) {
        try {

            return new Vector3Int(Modulo(blockX, Planet.PlanetSizeInVoxels), blockY, Modulo(blockZ, Planet.PlanetSizeInVoxels));

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions GetNormalizedBlockPos: " + ex.Message);
            return new Vector3Int();
        }
    }

    /// <summary>
    /// Gets the position of a chunk in the world in case of wrapping
    /// </summary>
    /// <param name="chunkX"></param>
    /// <param name="chunkZ"></param>
    /// <returns></returns>
    public static Vector2Int GetNormalizedChunkPos(int chunkX, int chunkZ) {
        try {

            return new Vector2Int(Modulo(chunkX, Planet.PlanetSizeInChunks), Modulo(chunkZ, Planet.PlanetSizeInChunks));

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions GetNormalizedChunkPos: " + ex.Message);
            return new Vector2Int();
        }
    }

    /// <summary>
    /// A modulo function that returns a value between 0 and m since C# % operator can return negative values
    /// </summary>
    /// <param name="x"></param>
    /// <param name="m"></param>
    /// <returns></returns>
    public static int Modulo(int x, int m) {
        try {

            int r = x % m;
            return r < 0 ? r + m : r;

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions Modulo: " + ex.Message);
            return x;
        }
    }

    /// <summary>
    /// Checks if there is a block at a given position in a chunk
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="blockXInChunk"></param>
    /// <param name="blockY"></param>
    /// <param name="blockZInChunk"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Checks if there is a block at a given position in a planet
    /// </summary>
    /// <param name="planet"></param>
    /// <param name="blockX"></param>
    /// <param name="blockY"></param>
    /// <param name="blockZ"></param>
    /// <returns></returns>
    public static bool CheckVoxelInPlanet(Planet planet, int blockX, int blockY, int blockZ) {
        try {

            byte blockType = GetVoxel(planet, blockX, blockY, blockZ);
            return planet.blockTypes[blockType].isSolid;

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions CheckVoxelInPlanet: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Gets the type of block at a given position in a planet
    /// </summary>
    /// <param name="planet"></param>
    /// <param name="blockX"></param>
    /// <param name="blockY"></param>
    /// <param name="blockZ"></param>
    /// <returns></returns>
    public static byte GetVoxel(Planet planet, int blockX, int blockY, int blockZ) {
        try {

            if (!IsVoxelInPlanet(blockX, blockY, blockZ)) {
                return 0;
            }

            Vector3Int normalizedBlockPos = GetNormalizedBlockPos(blockX, blockY, blockZ);
            blockX = normalizedBlockPos.x;
            blockZ = normalizedBlockPos.z;

            Vector2Int chunkPos = GetChunkPosFromBlockPos(blockX, blockY, blockZ);
            chunkPos = GetNormalizedChunkPos(chunkPos.x, chunkPos.y);
            ChunkData chunkData = planet.data.RequestChunk(chunkPos.x, chunkPos.y);

            int blockXInChunk = blockX % VoxelData.ChunkSize;
            int blockZInChunk = blockZ % VoxelData.ChunkSize;

            return chunkData.blocks[blockXInChunk, blockY, blockZInChunk];

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions GetVoxel: " + ex.Message); 
            return 0;
        }
    }

    /// <summary>
    /// Checks if a block position is within the planet boundaries
    /// Since the planet loops, we only need to check the y position
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static bool IsVoxelInPlanet(int x, int y, int z) {
        try {

            return y >= 0 && y < Planet.WorldHeight;

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions IsVoxelInPlanet: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Checks if a block position is within the chunk boundaries
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static bool IsVoxelInChunk(int x, int y, int z) {
        try {

            return x >= 0 && x < VoxelData.ChunkSize && y >= 0 && y < Planet.WorldHeight && z >= 0 && z < VoxelData.ChunkSize;

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions IsVoxelInChunk: " + ex.Message);
            return false;
        }
    }
}
