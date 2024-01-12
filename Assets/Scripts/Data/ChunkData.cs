using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

[System.Serializable]
public class ChunkData {

    private int x;
    private int z;

    public Vector2Int chunkCoord {

        get { 
            return new Vector2Int(x, z);
        }
        set {
            x = value.x;
            z = value.y;
        }
    }

    [HideInInspector]
    public byte[,,] voxelMap = new byte[VoxelData.ChunkSize, VoxelData.WorldHeight, VoxelData.ChunkSize];

    public ChunkData(Vector2Int pos, Planet planet) {
        try {

            chunkCoord = pos;
            Popuplate(planet);

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

                    int voxelX = (VoxelData.ChunkSize * chunkCoord.x) + x;
                    int voxelZ = (VoxelData.ChunkSize * chunkCoord.y) + z;

                    byte voxelValue;
                    float noiseValue = (float)planet.TorusNoise(voxelX, voxelZ);

                    for (int y = 0; y < VoxelData.WorldHeight; y++) {

                        Vector3Int voxelPosInWorld = new Vector3Int(voxelX, y, voxelZ);

                        int terrainHeight = VoxelData.OceanFloorLevel + Mathf.FloorToInt((VoxelData.MaxTerrainLevel - VoxelData.OceanFloorLevel) * noiseValue);
                        if (!PlanetData.IsVoxelInPlanet(voxelPosInWorld) || y > terrainHeight) //Air for everything above the surface
                            voxelValue = 0;

                        else if (y < 1) //Bedrock for y = 0
                            voxelValue = 1;
                        else if (y == terrainHeight) //Grass for the top layer
                            voxelValue = 3;
                        else if (y > terrainHeight - 4) //Dirt for next 3 layers
                            voxelValue = 4;
                        else //Everything else stone
                            voxelValue = 2;

                        //return voxelValue;


                        voxelMap[x, y, z] = voxelValue;
                    }
                }
            }

            planet.planetData.AddToModifiedChunkList(this);

        } catch (System.Exception e) {
            Debug.LogError("Error in function ChunkData.Popuplate: " + e.Message);
        }
    }
}