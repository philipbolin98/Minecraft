using Noise;
using System;
using UnityEditor.UI;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {

    private static float tau = 2 * Mathf.PI;

    private OpenSimplex2S OpenSimplex;
    private int seed;

    public int OceanFloorLevel;
    public int SeaLevel;
    public int MaxTerrainLevel;
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

            float noiseValue = (float)TorusNoise(planet, blockXInPlanet, blockZInPlanet);

            for (int y = 0; y < planet.WorldHeight; y++) {

                int terrainHeight = OceanFloorLevel + Mathf.FloorToInt((MaxTerrainLevel - OceanFloorLevel) * noiseValue);

                BlockID blockID;

                if (!HelperFunctions.IsBlockInPlanet(planet, y) || y > terrainHeight) { //Air for everything above the surface
                    blockID = BlockID.Air;
                } else if (y < 1) { //Bedrock for y = 0
                    blockID = BlockID.Bedrock;
                } else if (y == terrainHeight) { //Grass for the top layer
                    blockID = BlockID.Grass;
                } else if (y > terrainHeight - 4) { //Dirt for next 3 layers
                    blockID = BlockID.Dirt;
                } else { //Everything else stone
                    blockID = BlockID.Stone;
                }

                chunk.data.blocks[blockXInChunk, y, blockZInChunk] = blockID;
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
    public double TorusNoise(Planet planet, int blockX, int blockZ) {
        try {

            //Normalize the coords to be within 0 and 2pi
            float angleX = tau * blockX / planet.PlanetSizeInVoxels;
            float angleZ = tau * blockZ / planet.PlanetSizeInVoxels;

            //Convert the coords to points on the surface of a torus
            double torusX = (this.torusCenterRadius + this.torusTubeRadius * Mathf.Cos(angleZ)) * Mathf.Cos(angleX);
            double torusY = (this.torusCenterRadius + this.torusTubeRadius * Mathf.Cos(angleZ)) * Mathf.Sin(angleX);
            double torusZ = this.torusTubeRadius * Mathf.Sin(angleZ);

            double noiseValue = OpenSimplex.Noise3_Classic(torusX, torusY, torusZ); //Get the noise value
            noiseValue = (noiseValue + 1) / 2; //Normalize the noise value to be between 0 and 1
            return noiseValue;

        } catch (System.Exception ex) {
            Debug.LogError("Error in TerrainGenerator TorusNoise: " + ex.Message);
            return 0;
        }
    }
}