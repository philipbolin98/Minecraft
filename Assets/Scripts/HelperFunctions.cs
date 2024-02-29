using System;
using UnityEngine;

public static class HelperFunctions {
    
    /// <summary>
    /// Gets the chunk position of a block
    /// </summary>
    /// <param name="blockX"></param>
    /// <param name="blockZ"></param>
    /// <returns></returns>
    public static Vector2Int GetChunkPosFromBlockPos(int blockX, int blockZ) {
		try {

            int chunkX = Mathf.FloorToInt(blockX / Chunk.ChunkSize);
            int chunkZ = Mathf.FloorToInt(blockZ / Chunk.ChunkSize);

            return new Vector2Int(chunkX, chunkZ);

		} catch (Exception ex) {
            Debug.LogError("Error in HelperFunctions GetChunkPosFromBlockPos: " + ex.Message);
            return new Vector2Int();
        }
	}

    public static Vector3Int NormalizeBlockPos(int x, int y, int z, int maxVal) {
        try {

            if (x < 0 || x >= maxVal) {
                x = (int)Modulo(x, maxVal);
            }

            if (z < 0 || z >= maxVal) {
                z = (int)Modulo(z, maxVal);
            }

            return new Vector3Int(x, y, z);

        } catch (Exception ex) {
            Debug.LogError("Error in HelperFunctions NormalizeBlockPos: " + ex.Message);
            return new Vector3Int();
        }
    }

    /// <summary>
    /// Gets the position of a chunk in the world in case of wrapping
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static Vector2Int NormalizeChunkPos(Planet planet, int x, int z) {
        try {

            if (x < 0 || x >= planet.PlanetSizeInChunks) {
                x = (int)Modulo(x, planet.PlanetSizeInChunks);
            }

            if (z < 0 || z >= planet.PlanetSizeInChunks) {
                z = (int)Modulo(z, planet.PlanetSizeInChunks);
            }

            return new Vector2Int(x, z);

        } catch (Exception ex) {
            Debug.LogError("Error in HelperFunctions NormalizeChunkPos: " + ex.Message);
            return new Vector2Int();
        }
    }

    /// <summary>
    /// Overload of the previous function that takes a Vector2Int
    /// </summary>
    /// <param name="chunkPos"></param>
    /// <returns></returns>
    public static Vector2Int NormalizeChunkPos(Planet planet, Vector2Int chunkPos) {
        try {

            return NormalizeChunkPos(planet, chunkPos.x, chunkPos.y);

        } catch (Exception ex) {
            Debug.LogError("Error in HelperFunctions NormalizeChunkPos: " + ex.Message);
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

        } catch (Exception ex) {
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

        } catch (Exception ex) {
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

        } catch (Exception ex) {
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

            Vector3Int normalizedBlockPos = NormalizeBlockPos(blockX, blockY, blockZ, planet.PlanetSizeInVoxels);
            blockX = normalizedBlockPos.x;
            blockZ = normalizedBlockPos.z;

            Vector2Int chunkPos = GetChunkPosFromBlockPos(blockX, blockZ);
            chunkPos = NormalizeChunkPos(planet, chunkPos.x, chunkPos.y);

            Chunk chunk = planet.chunks[chunkPos.x, chunkPos.y];

            if (chunk == null || chunk.stage == ChunkStage.Null) {
                return BlockID.Air;
            }

            int blockXInChunk = blockX % Chunk.ChunkSize;
            int blockZInChunk = blockZ % Chunk.ChunkSize;

            return chunk.data.blocks[blockXInChunk, blockY, blockZInChunk];

        } catch (Exception ex) {
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

        } catch (Exception ex) {
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

            return x >= 0 && x < Chunk.ChunkSize && y >= 0 && y < planet.WorldHeight && z >= 0 && z < Chunk.ChunkSize;

        } catch (Exception ex) {
            Debug.LogError("Error in HelperFunctions IsBlockInChunk: " + ex.Message);
            return false;
        }
    }

    public static Chunk GetChunkFromBlockPos(Planet planet, int blockX, int blockY, int blockZ) {
        try {
            
            if (!IsBlockInPlanet(planet, blockY)) {
                return null;
            }

            Vector3Int normalizedBlockPos = NormalizeBlockPos(blockX, blockY, blockZ, planet.PlanetSizeInVoxels);
            Vector2Int chunkPos = GetChunkPosFromBlockPos(normalizedBlockPos.x, normalizedBlockPos.z);
            Chunk chunk = planet.chunks[chunkPos.x, chunkPos.y];
            return chunk;

        } catch (Exception ex) {
            Debug.LogError("Error in HelperFunctions GetChunkFromBlockPos: " + ex.Message);
            return null;
        }
    }

    public static bool CheckNeighboringChunks(Planet planet, Vector2Int chunkPos, Vector2Int[] neighborOffsets, ChunkStage stage) {
        try {

            foreach (Vector2Int offset in neighborOffsets) {

                Vector2Int newChunkPos = chunkPos + offset;

                if (newChunkPos.x == planet.PlanetSizeInChunks) {
                    newChunkPos.x = 0;
                } else if (newChunkPos.x == -1) {
                    newChunkPos.x = planet.PlanetSizeInChunks - 1;
                }

                if (newChunkPos.y == planet.PlanetSizeInChunks) {
                    newChunkPos.y = 0;
                } else if (newChunkPos.y == -1) {
                    newChunkPos.y = planet.PlanetSizeInChunks - 1;
                }

                Chunk chunk = planet.chunks[newChunkPos.x, newChunkPos.y];

                if (chunk == null || chunk.stage < stage) {
                    return false;
                }
            }

            return true;

        } catch (Exception ex) {
            Debug.LogError("Error in HelperFunctions CanChunkStructuresBeGenerated: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Liang Barsky line clipping algorithm extrapolated to 3D
    /// Determines if a ray intersects a block at a given position
    /// See: https://en.wikipedia.org/wiki/Liang%E2%80%93Barsky_algorithm
    /// </summary>
    /// <param name="pos1">The camera position</param>
    /// <param name="pos2">The camera position + reach</param>
    /// <param name="blockPos">The block to check for an intersection</param>
    /// <returns>True/False</returns>
    public static bool LiangBarsky3D(Vector3 pos1, Vector3 pos2, Vector3Int blockPos) {
        try {

            int xMin = blockPos.x;
            int yMin = blockPos.y;
            int zMin = blockPos.z;
            int xMax = xMin + 1;
            int yMax = yMin + 1;
            int zMax = zMin + 1;

            float p1 = -(pos2.x - pos1.x);
            float p2 = -p1;
            float p3 = -(pos2.y - pos1.y);
            float p4 = -p3;
            float p5 = -(pos2.z - pos1.z);
            float p6 = -p5;

            float q1 = pos1.x - xMin;
            float q2 = xMax - pos1.x;
            float q3 = pos1.y - yMin;
            float q4 = yMax - pos1.y;
            float q5 = pos1.z - zMin;
            float q6 = zMax - pos1.z;

            if ((p1 == 0 && q1 < 0) || (p2 == 0 && q2 < 0) || (p3 == 0 && q3 < 0) || (p4 == 0 && q4 < 0) || (p5 == 0 && q5 < 0) || (p6 == 0 && q6 < 0)) {
                return false;
            }

            float min = 1;
            float max = 0;

            LiangBarsky3DHelper(p1, p2, q1, q2, ref min, ref max);
            LiangBarsky3DHelper(p3, p4, q3, q4, ref min, ref max);
            LiangBarsky3DHelper(p5, p6, q5, q6, ref min, ref max);

            return max <= min;

        } catch (Exception ex) {
            Debug.LogError("Error in Player LiangBarsky3D: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Helper function for the repetitive portion of LiangBarsky3D
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="q1"></param>
    /// <param name="q2"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    private static void LiangBarsky3DHelper(float p1, float p2, float q1, float q2, ref float min, ref float max) {
        try {

            if (p1 != 0) {
                float r1 = q1 / p1;
                float r2 = q2 / p2;

                if (p1 < 0) {
                    max = Mathf.Max(max, r1);
                    min = Mathf.Min(min, r2);
                } else {
                    max = Mathf.Max(max, r2);
                    min = Mathf.Min(min, r1);
                }
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Player LiangBarsky3DHelper: " + ex.Message);
        }
    }
}