using System;
using System.Collections.Generic;
using UnityEngine;

public class StructureGenerator : MonoBehaviour {

    public FastNoiseLite FastNoise;
    public Planet planet;
    public Dictionary<StructureID, Structure> StructureMap = new();

    public void Initialize(Planet planet) {
        try {

            this.planet = planet;
            this.FastNoise = new FastNoiseLite(Planet.seed);
            this.FastNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);

            this.StructureMap.Add(StructureID.OakTree, new Structure(Structure.tree, "OakTree"));

        } catch (Exception ex) {
            Debug.LogError("Error in StructureGenerator StructureGenerator: " + ex.Message);
        }
    }

    public void GenerateStructures(Chunk chunkToModify, Vector2Int chunkPos) {
        try {

            Structure tree = this.StructureMap[StructureID.OakTree];

            for (int x = 0; x < Chunk.ChunkSize; x++) {
                for (int z = 0; z < Chunk.ChunkSize; z++) {

                    int blockXInPlanet = chunkPos.x * Chunk.ChunkSize + x;
                    int blockZInPlanet = chunkPos.y * Chunk.ChunkSize + z;

                    float treeValue = FastNoise.GetNoise(blockXInPlanet * 256, blockZInPlanet * 256);
                    treeValue = (treeValue + 1) / 2;

                    if (treeValue > tree.threshold) {

                        Biome biome = this.planet.terrainGenerator.GetBiome(blockXInPlanet, blockZInPlanet);

                        if (biome.name != "Plains") {
                            int terrainHeight = this.planet.terrainGenerator.GetTerrainHeight(blockXInPlanet, blockZInPlanet, biome) + 1;

                            if (terrainHeight > this.planet.terrainGenerator.seaLevel) {
                                this.GenerateStructure(chunkToModify, blockXInPlanet, blockZInPlanet, terrainHeight, tree);
                            }
                        }
                    }
                }
            }

        } catch (Exception ex) {
            Debug.LogError("Error in StructureGenerator GenerateStructures: " + ex.Message);
        }
    }

    public void GenerateStructure(Chunk chunkToModify, int blockXInPlanet, int blockZInPlanet, int terrainHeight, Structure structure) {
        try {

            int minChunkX = chunkToModify.normalizedPos.x * Chunk.ChunkSize;
            int minChunkZ = chunkToModify.normalizedPos.y * Chunk.ChunkSize;
            int maxChunkX = minChunkX + Chunk.ChunkSize - 1;
            int maxChunkZ = minChunkZ + Chunk.ChunkSize - 1;

            foreach (Tuple<Vector3Int, BlockID> block in structure.blocks) {
                
                Vector3Int blockPosInPlanet = HelperFunctions.NormalizeBlockPos(blockXInPlanet + block.Item1.x, terrainHeight + block.Item1.y, blockZInPlanet + block.Item1.z, this.planet.PlanetSizeInVoxels);

                if (blockPosInPlanet.y < this.planet.WorldHeight && blockPosInPlanet.x >= minChunkX && blockPosInPlanet.x <= maxChunkX && blockPosInPlanet.z >= minChunkZ && blockPosInPlanet.z <= maxChunkZ) {

                    Vector3Int blockPosInChunk = HelperFunctions.NormalizeBlockPos(blockPosInPlanet.x, blockPosInPlanet.y, blockPosInPlanet.z, Chunk.ChunkSize);
                    BlockID currentBlockID = chunkToModify.data.blocks[blockPosInChunk.x, blockPosInChunk.y, blockPosInChunk.z];

                    if (currentBlockID == BlockID.Air) {
                        chunkToModify.data.blocks[blockPosInChunk.x, blockPosInChunk.y, blockPosInChunk.z] = block.Item2;
                    }
                }
            }

        } catch (Exception ex) {
            Debug.LogError("Error in StructureGenerator GenerateStructure: " + ex.Message);
        }
    }
}

public class Structure {

    //Not sure if im going to use this method yet
    public static List<Tuple<Vector3Int, BlockID>> tree = new () {
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 0, 0), BlockID.Wood),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 1, 0), BlockID.Wood),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 2, 0), BlockID.Wood),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 3, 0), BlockID.Wood),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 4, 0), BlockID.Wood),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 4, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 4, 0), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 4, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 4, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 4, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 4, 0), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 4, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 4, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-2, 4, -2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-2, 4, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-2, 4, 0), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-2, 4, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-2, 4, 2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 4, 2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 4, 2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 4, 2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(2, 4, 2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(2, 4, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(2, 4, 0), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(2, 4, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(2, 4, -2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 4, -2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 4, -2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 4, -2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 5, 0), BlockID.Wood),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 5, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 5, 0), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 5, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 5, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 5, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 5, 0), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 5, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 5, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 6, 0), BlockID.Leaf)
    };

    public string name;
    public float threshold;
    public List<Tuple<Vector3Int, BlockID>> blocks;

    public Structure(List<Tuple<Vector3Int, BlockID>> blocks, string name) {
        try {

            this.blocks = blocks;
            this.name = name;
            this.threshold = .73f;

        } catch (Exception ex) {
            Debug.LogError("Error in Structure Constructor: " + ex.Message);
        }
    }
}

public enum StructureID {
    OakTree = 0
}