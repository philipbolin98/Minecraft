using Noise;
using UnityEngine;
using System;

public class TerrainGenerator : MonoBehaviour {

    private static float tau = 2 * Mathf.PI;

    private FastNoiseLite FastNoise;
    private int seed;

    public int oceanFloorLevel;
    public int seaLevel;
    public float treeThreshold;

    /// <summary>
    /// The radius of the torus from the center of the torus to the center of the torus tube
    /// Increasing this value will scrunch the world along the x axis 
    /// </summary>
    public float torusCenterRadius;
    /// <summary>
    /// The radius of the torus tube
    /// Increasing this value will scrunch the world along the z axis
    /// </summary>
    public float torusTubeRadius;

    public void Initialize(int seed) {
        try {

            this.seed = seed;
            UnityEngine.Random.InitState(seed);

            FastNoise = new FastNoiseLite(seed);
            FastNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
            
        } catch (Exception ex) {
            Debug.LogError("Error in TerrainGenerator Initialize: " + ex.Message);
        }
    }

    public void GenerateTerrain(Chunk chunk, int blockXInChunk, int blockZInChunk) {
        try {

            Planet planet = chunk.planet;

            int blockXInPlanet = (VoxelData.ChunkSize * chunk.pos.x) + blockXInChunk;
            int blockZInPlanet = (VoxelData.ChunkSize * chunk.pos.y) + blockZInChunk;

            //FastNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);

            float biomeNoiseValue = TorusNoise(planet, blockXInPlanet, blockZInPlanet, this.torusCenterRadius / 4, this.torusTubeRadius / 4);
            int biomeIndex = Mathf.FloorToInt(biomeNoiseValue * planet.biomes.Length);
            Biome biome = planet.biomes[biomeIndex];

            float noiseValue = TorusNoise(planet, blockXInPlanet, blockZInPlanet, this.torusCenterRadius, this.torusTubeRadius);
            int terrainHeight = oceanFloorLevel + Mathf.FloorToInt((biome.maxTerrainHeight - oceanFloorLevel) * noiseValue);
            
            for (int y = 0; y < planet.WorldHeight; y++) {

                if (chunk.data.blocks[blockXInChunk, y, blockZInChunk] > BlockID.Air) {
                    continue;
                }

                BlockID blockID;

                if (!HelperFunctions.IsBlockInPlanet(planet, y)) {
                    blockID = BlockID.Air;
                } else if (y > terrainHeight) {
                    if (y < seaLevel) { //Water when above the surface but below sea level
                        blockID = BlockID.Water;
                    } else { //Air when above the surface and above sea level
                        blockID = BlockID.Air;
                    }
                } else if (y < 1) { //Bedrock for y = 0
                    blockID = BlockID.Bedrock;
                } else if (y == terrainHeight) {
                    if (y < seaLevel) {
                        blockID = BlockID.Sand;
                    } else {
                        blockID = biome.surfaceBlock;
                    }
                } else if (y > terrainHeight - 4) { //Dirt for next 3 layers
                    blockID = BlockID.Dirt;
                } else { //Everything else stone
                    blockID = BlockID.Stone;
                }

                if (y == terrainHeight + 1 && y >= seaLevel && biome.name != "Plains") {

                    float treeValue = Noise2D(blockXInPlanet * 256, blockZInPlanet * 256);

                    if (treeValue > treeThreshold) {
                        chunk.structures.Add(new Vector3Int(blockXInChunk, y, blockZInChunk), biome.name);
                        //GenerateTree(chunk, biome.name, blockXInChunk, y, blockZInChunk);
                    }
                }

                chunk.data.blocks[blockXInChunk, y, blockZInChunk] = blockID;
            }

        } catch (Exception ex) {
            Debug.LogError("Error in TerrainGenerator GenerateTerrain: " + ex.Message);
        }
    }

    public void GenerateTree(Chunk chunk, string biomeName, int blockXInChunk, int y, int blockZInChunk) {
        try {

            for (int i = 0; i < 6; i++) {

                int treeY = y + i;

                if (treeY < chunk.planet.WorldHeight) {

                    BlockID currentBlockID = chunk.data.blocks[blockXInChunk, treeY, blockZInChunk];

                    if (currentBlockID == BlockID.Air || currentBlockID == BlockID.Leaf) {
                        chunk.data.blocks[blockXInChunk, treeY, blockZInChunk] = BlockID.Wood;
                    }
                }
            }

            for (int i = 4; i < 7; i++) {

                int treeY = y + i;

                if (treeY < chunk.planet.WorldHeight) {

                    int radius = 6 - i;

                    for (int j = blockXInChunk - radius; j <= blockXInChunk + radius; j++) {
                        for (int k = blockZInChunk - radius; k <= blockZInChunk + radius; k++) {

                            Vector2Int chunkPos = chunk.pos;
                            int x = j;
                            int z = k;

                            if (x >= VoxelData.ChunkSize) {
                                x = Mathf.FloorToInt(HelperFunctions.Modulo(x, VoxelData.ChunkSize));
                                chunkPos.x++;

                                if (chunkPos.x == chunk.planet.PlanetSizeInChunks) {
                                    chunkPos.x = 0;
                                }

                            } else if (x < 0) {
                                x = Mathf.FloorToInt(HelperFunctions.Modulo(x, VoxelData.ChunkSize));
                                chunkPos.x--;

                                if (chunkPos.x == -1) {
                                    chunkPos.x = chunk.planet.PlanetSizeInChunks - 1;
                                }
                            }

                            if (z >= VoxelData.ChunkSize) {
                                z = Mathf.FloorToInt(HelperFunctions.Modulo(z, VoxelData.ChunkSize));
                                chunkPos.y++;

                                if (chunkPos.y == chunk.planet.PlanetSizeInChunks) {
                                    chunkPos.y = 0;
                                }

                            } else if (z < 0) {
                                z = Mathf.FloorToInt(HelperFunctions.Modulo(z, VoxelData.ChunkSize));
                                chunkPos.y--;

                                if (chunkPos.y == -1) {
                                    chunkPos.y = chunk.planet.PlanetSizeInChunks - 1;
                                }
                            }

                            Chunk newChunk = chunk.planet.chunks[chunkPos.x, chunkPos.y];

                            BlockID currentBlockID = newChunk.data.blocks[x, treeY, z];

                            if (currentBlockID == BlockID.Air) {
                                newChunk.data.blocks[x, treeY, z] = BlockID.Leaf;
                            }
                        }
                    }
                }
            }

        } catch (Exception ex) {
            Debug.LogError("Error in TerrainGenerator GenerateTree: " + ex.Message);
        }
    }

    public float Noise2D(int blockX, int blockZ) {
        try {

            float noiseValue = FastNoise.GetNoise(blockX, blockZ);
            noiseValue = (noiseValue + 1) / 2;
            return noiseValue;

        } catch (Exception ex) {
            Debug.LogError("Error in TerrainGenerator GenerateTree: " + ex.Message);
            return 0;
        }
    }

    /// <summary>
    /// Takes an x and z coordinate in the world and gets 
    /// See https://mathworld.wolfram.com/Torus.html
    /// </summary>
    /// <param name="blockX">The x coord in the planet</param>
    /// <param name="blockZ">The z coord in the planet</param>
    /// <returns></returns>
    public float TorusNoise(Planet planet, int blockX, int blockZ, float centerRadius, float tubeRadius) {
        try {

            //Normalize the coords to be within 0 and 2pi
            float angleX = tau * blockX / planet.PlanetSizeInVoxels;
            float angleZ = tau * blockZ / planet.PlanetSizeInVoxels;

            //Convert the coords to points on the surface of a torus
            float torusX = (centerRadius + tubeRadius * Mathf.Cos(angleZ)) * Mathf.Cos(angleX);
            float torusY = (centerRadius + tubeRadius * Mathf.Cos(angleZ)) * Mathf.Sin(angleX);
            float torusZ = (centerRadius + tubeRadius * Mathf.Sin(angleZ)) * Mathf.Cos(angleX);

            float noiseValue = FastNoise.GetNoise(torusX, torusY, torusZ); //OpenSimplex.Noise3_Classic(torusX, torusY, torusZ); //Get the noise value
            noiseValue = (noiseValue + 1) / 2; //Normalize the noise value to be between 0 and 1
            return noiseValue;

        } catch (Exception ex) {
            Debug.LogError("Error in TerrainGenerator TorusNoise: " + ex.Message);
            return 0;
        }
    }
}