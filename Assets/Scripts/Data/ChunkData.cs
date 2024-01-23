using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

[System.Serializable]
public class ChunkData {

    public Vector2Int pos;

    [HideInInspector]
    public BlockID[,,] blocks = new BlockID[VoxelData.ChunkSize, Planet.WorldHeight, VoxelData.ChunkSize];

    public ChunkData(Planet planet, Vector2Int pos) {
        try {

            this.pos = pos;

        } catch (System.Exception e) {
            Debug.LogError("Error in function ChunkData.Constructor: " + e.Message);
        }
    }

    /// <summary>
    /// Popuplates the current chunk data with all its voxels
    /// </summary>
    public void Popuplate(Planet planet) {
        try {

            for (int x = 0; x < VoxelData.ChunkSize; x++) {
                for (int z = 0; z < VoxelData.ChunkSize; z++) {

                    int voxelX = (VoxelData.ChunkSize * pos.x) + x;
                    int voxelZ = (VoxelData.ChunkSize * pos.y) + z;

                    
                    float noiseValue = (float)planet.TorusNoise(voxelX, voxelZ);

                    for (int y = 0; y < Planet.WorldHeight; y++) {

                        int terrainHeight = Planet.OceanFloorLevel + Mathf.FloorToInt((Planet.MaxTerrainLevel - Planet.OceanFloorLevel) * noiseValue);

                        BlockID blockID;

                        if (!HelperFunctions.IsBlockInPlanet(voxelX, y, voxelZ) || y > terrainHeight) { //Air for everything above the surface
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

                        blocks[x, y, z] = blockID;
                    }
                }
            }

            //planet.data.AddToModifiedChunkList(this);

        } catch (System.Exception e) {
            Debug.LogError("Error in function ChunkData.Popuplate: " + e.Message);
        }
    }
}