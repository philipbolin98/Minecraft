using UnityEngine;
using System;

public class TerrainGenerator : MonoBehaviour {

    private static float tau = 2 * Mathf.PI;

    private FastNoiseLite FastNoise;
    public Planet planet;

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

    public float torusCenterRadiusBiomes;
    public float torusTubeRadiusBiomes;

    public void Initialize(Planet planet) {
        try {

            this.planet = planet;
            this.FastNoise = new FastNoiseLite(Planet.seed);
            this.FastNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);

            this.torusCenterRadius = planet.PlanetSizeInChunks * 4;
            this.torusTubeRadius = torusCenterRadius;
            this.torusCenterRadiusBiomes = planet.PlanetSizeInChunks;
            this.torusTubeRadiusBiomes = planet.PlanetSizeInChunks;

        } catch (Exception ex) {
            Debug.LogError("Error in TerrainGenerator Initialize: " + ex.Message);
        }
    }

    public void GenerateTerrain(Chunk chunk, int blockXInChunk, int blockZInChunk) {
        try {

            Planet planet = chunk.planet;

            int blockXInPlanet = chunk.normalizedPosInBlocks.x + blockXInChunk;
            int blockZInPlanet = chunk.normalizedPosInBlocks.y + blockZInChunk;

            Biome biome = GetBiome(blockXInPlanet, blockZInPlanet);
            int terrainHeight = GetTerrainHeight(blockXInPlanet, blockZInPlanet, biome);

            int maxY = Mathf.Max(terrainHeight, seaLevel) + 1;
            maxY = Mathf.Min(maxY, planet.WorldHeight);

            for (int y = 0; y < maxY; y++) {

                BlockID blockID;

                if (y > terrainHeight) {
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

                chunk.data.blocks[blockXInChunk, y, blockZInChunk] = blockID;
            }

        } catch (Exception ex) {
            Debug.LogError("Error in TerrainGenerator GenerateTerrain: " + ex.Message);
        }
    }

    public Biome GetBiome(int blockXInPlanet, int blockZInPlanet) {
        try {

            float biomeNoiseValue = TorusNoise(this.planet, blockXInPlanet, blockZInPlanet, this.torusCenterRadiusBiomes, this.torusTubeRadiusBiomes);
            int biomeIndex = Mathf.FloorToInt(biomeNoiseValue * this.planet.biomes.Length);
            Biome biome = this.planet.biomes[biomeIndex];
            return biome;

        } catch (Exception ex) {
            Debug.LogError("Error in TerrainGenerator GetBiome: " + ex.Message);
            return null;
        }
    }

    public int GetTerrainHeight(int blockXInPlanet, int blockZInPlanet, Biome biome) {
        try {

            float noiseValue = TorusNoise(this.planet, blockXInPlanet, blockZInPlanet, this.torusCenterRadius, this.torusTubeRadius);
            int terrainHeight = oceanFloorLevel + Mathf.FloorToInt((biome.maxTerrainHeight - oceanFloorLevel) * noiseValue);
            return terrainHeight;

        } catch (Exception ex) {
            Debug.LogError("Error in TerrainGenerator GetTerrainHeight: " + ex.Message);
            return 0;
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