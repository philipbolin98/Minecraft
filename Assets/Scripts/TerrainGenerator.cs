using Noise;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {

    private static float tau = 2 * Mathf.PI;

    private OpenSimplex2S OpenSimplex;
    private int seed;

    public int OceanFloorLevel;
    public int SeaLevel;

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
            Random.InitState(seed);

            OpenSimplex = new OpenSimplex2S(seed);

        } catch (System.Exception ex) {
            Debug.LogError("Error in TerrainGenerator Initialize: " + ex.Message);
        }
    }

    public void GenerateTerrain(Chunk chunk, int blockXInChunk, int blockZInChunk) {
        try {

            Planet planet = chunk.planet;

            int blockXInPlanet = (VoxelData.ChunkSize * chunk.pos.x) + blockXInChunk;
            int blockZInPlanet = (VoxelData.ChunkSize * chunk.pos.y) + blockZInChunk;

            float biomeNoiseValue = (float)TorusNoise(planet, blockXInPlanet, blockZInPlanet, this.torusCenterRadius / 4, this.torusTubeRadius / 4);
            int biomeIndex = Mathf.FloorToInt(biomeNoiseValue * planet.biomes.Length);
            Biome biome = planet.biomes[biomeIndex];

            float noiseValue = (float)TorusNoise(planet, blockXInPlanet, blockZInPlanet, this.torusCenterRadius, this.torusTubeRadius);
            int terrainHeight = OceanFloorLevel + Mathf.FloorToInt((biome.maxTerrainHeight - OceanFloorLevel) * noiseValue);

            for (int y = 0; y < planet.WorldHeight; y++) {

                BlockID blockID;

                if (!HelperFunctions.IsBlockInPlanet(planet, y)) {
                    blockID = BlockID.Air;
                } else if (y > terrainHeight) {
                    if (y < SeaLevel) { //Water when above the surface but below sea level
                        blockID = BlockID.Water;
                    } else { //Air when above the surface and above sea level
                        blockID = BlockID.Air;
                    }
                } else if (y < 1) { //Bedrock for y = 0
                    blockID = BlockID.Bedrock;
                } else if (y == terrainHeight) {
                    if (y < SeaLevel + 2) {
                        blockID = BlockID.Sand;
                    } else { //Grass for the top layer
                        blockID = biome.surfaceBlock;
                    }
                } else if (y > terrainHeight - 4) { //Dirt for next 3 layers
                    blockID = BlockID.Dirt;
                } else { //Everything else stone
                    blockID = BlockID.Stone;
                }

                chunk.data.blocks[blockXInChunk, y, blockZInChunk] = blockID;

                if (y == terrainHeight) {

                    //Generate trees

                }
            }

        } catch (System.Exception ex) {
            Debug.LogError("Error in TerrainGenerator GenerateTerrain: " + ex.Message);
        }
    }

    /// <summary>
    /// Takes an x and z coordinate in the world and gets 
    /// See https://mathworld.wolfram.com/Torus.html
    /// </summary>
    /// <param name="blockX">The x coord in the planet</param>
    /// <param name="blockZ">The z coord in the planet</param>
    /// <returns></returns>
    public double TorusNoise(Planet planet, int blockX, int blockZ, float centerRadius, float tubeRadius) {
        try {

            //Normalize the coords to be within 0 and 2pi
            float angleX = tau * blockX / planet.PlanetSizeInVoxels;
            float angleZ = tau * blockZ / planet.PlanetSizeInVoxels;

            //Convert the coords to points on the surface of a torus
            double torusX = (centerRadius + tubeRadius * Mathf.Cos(angleZ)) * Mathf.Cos(angleX);
            double torusY = (centerRadius + tubeRadius * Mathf.Cos(angleZ)) * Mathf.Sin(angleX);
            double torusZ = tubeRadius * Mathf.Sin(angleZ);

            double noiseValue = OpenSimplex.Noise3_Classic(torusX, torusY, torusZ); //Get the noise value
            noiseValue = (noiseValue + 1) / 2; //Normalize the noise value to be between 0 and 1
            return noiseValue;

        } catch (System.Exception ex) {
            Debug.LogError("Error in TerrainGenerator TorusNoise: " + ex.Message);
            return 0;
        }
    }
}