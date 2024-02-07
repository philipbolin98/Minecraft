using UnityEngine;

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
    public static Vector3Int GetNormalizedBlockPosInPlanet(Planet planet, int blockX, int blockY, int blockZ) {
        try {

            return new Vector3Int((int)Modulo(blockX, planet.PlanetSizeInVoxels), blockY, (int)Modulo(blockZ, planet.PlanetSizeInVoxels));

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions GetNormalizedBlockPosInPlanet: " + ex.Message);
            return new Vector3Int();
        }
    }

    public static Vector3Int GetNormalizedBlockPosInChunk(Vector3Int blockPos) {
        try {

            return new Vector3Int((int)Modulo(blockPos.x, VoxelData.ChunkSize), blockPos.y, (int)Modulo(blockPos.z, VoxelData.ChunkSize));

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions GetNormalizedBlockPosInChunk: " + ex.Message);
            return new Vector3Int();
        }
    }

    /// <summary>
    /// Gets the position of a chunk in the world in case of wrapping
    /// </summary>
    /// <param name="chunkX"></param>
    /// <param name="chunkZ"></param>
    /// <returns></returns>
    public static Vector2Int GetNormalizedChunkPos(Planet planet, int chunkX, int chunkZ) {
        try {

            return new Vector2Int((int)Modulo(chunkX, planet.PlanetSizeInChunks), (int)Modulo(chunkZ, planet.PlanetSizeInChunks));

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions GetNormalizedChunkPos: " + ex.Message);
            return new Vector2Int();
        }
    }

    /// <summary>
    /// Overload of the previous function that takes a Vector2Int
    /// </summary>
    /// <param name="chunkPos"></param>
    /// <returns></returns>
    public static Vector2Int GetNormalizedChunkPos(Planet planet, Vector2Int chunkPos) {
        try {

            return GetNormalizedChunkPos(planet, chunkPos.x, chunkPos.y);

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
    public static float Modulo(float x, int m) {
        try {

            float r = x % m;
            return r < 0 ? r + m : r;

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions Modulo: " + ex.Message);
            return x;
        }
    }

    /// <summary>
    /// Checks if a block at a given position within a chunk is solid
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="blockXInChunk"></param>
    /// <param name="blockY"></param>
    /// <param name="blockZInChunk"></param>
    /// <returns></returns>
    public static bool CheckBlockInChunk(Chunk chunk, int blockXInChunk, int blockY, int blockZInChunk) {
        try {

            BlockID blockID = chunk.data.blocks[blockXInChunk, blockY, blockZInChunk];
            return chunk.planet.blockTypes[(int)blockID].isSolid;

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions CheckBlockInChunk: " + ex.Message);
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
    public static bool CheckBlockInPlanet(Planet planet, int blockX, int blockY, int blockZ) {
        try {

            BlockID blockID = GetBlockID(planet, blockX, blockY, blockZ);
            return planet.blockTypes[(int)blockID].isSolid;

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
    public static BlockID GetBlockID(Planet planet, int blockX, int blockY, int blockZ) {
        try {

            if (!IsBlockInPlanet(planet, blockY)) {
                return BlockID.Air;
            }

            Vector3Int normalizedBlockPos = GetNormalizedBlockPosInPlanet(planet, blockX, blockY, blockZ);
            blockX = normalizedBlockPos.x;
            blockZ = normalizedBlockPos.z;

            Vector2Int chunkPos = GetChunkPosFromBlockPos(blockX, blockY, blockZ);
            chunkPos = GetNormalizedChunkPos(planet, chunkPos.x, chunkPos.y);

            Chunk chunk = planet.chunks[chunkPos.x, chunkPos.y];

            if (chunk == null || !chunk.isDataPopulated) {
                return BlockID.Air;
            }

            int blockXInChunk = blockX % VoxelData.ChunkSize;
            int blockZInChunk = blockZ % VoxelData.ChunkSize;

            return chunk.data.blocks[blockXInChunk, blockY, blockZInChunk];

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions GetBlockID: " + ex.Message); 
            return BlockID.Air;
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
    public static bool IsBlockInPlanet(Planet planet, int y) {
        try {

            return y >= 0 && y < planet.WorldHeight;

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions IsBlockInPlanet: " + ex.Message);
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
    public static bool IsBlockInChunk(Planet planet, int x, int y, int z) {
        try {

            return x >= 0 && x < VoxelData.ChunkSize && y >= 0 && y < planet.WorldHeight && z >= 0 && z < VoxelData.ChunkSize;

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions IsBlockInChunk: " + ex.Message);
            return false;
        }
    }

    public static Chunk GetChunkFromBlockPos(Planet planet, int blockX, int blockY, int blockZ) {
        try {
            
            if (!IsBlockInPlanet(planet, blockY)) {
                return null;
            }

            Vector3Int normalizedBlockPos = GetNormalizedBlockPosInPlanet(planet, blockX, blockY, blockZ);
            Vector2Int chunkPos = GetChunkPosFromBlockPos(normalizedBlockPos.x, normalizedBlockPos.y, normalizedBlockPos.z);
            Chunk chunk = planet.chunks[chunkPos.x, chunkPos.y];
            return chunk;

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions GetChunkFromBlockPos: " + ex.Message);
            return null;
        }
    }

    public static bool CanChunkMeshBeCreated(Planet planet, Vector2Int chunkPos) {
        try {

            foreach (Vector2Int offset in Chunk.ChunksToCheckForMeshCreation) {

                Vector2Int normalizedChunkPos = GetNormalizedChunkPos(planet, chunkPos + offset);
                Chunk chunk = planet.chunks[normalizedChunkPos.x, normalizedChunkPos.y];

                if (chunk == null || !chunk.isDataPopulated) {
                    return false;
                }
            }

            return true;

        } catch (System.Exception ex) {
            Debug.LogError("Error in HelperFunctions CanChunkMeshBeCreated: " + ex.Message);
            return false;
        }
    }
}