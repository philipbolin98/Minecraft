using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkData {

    private int x;
    private int y;
    private int z;

    public Vector3Int chunkCoord {

        get { return new Vector3Int(x, y, z); }
        set {
            x = value.x;
            y = value.y;
            z = value.z;
        }
    }

    [HideInInspector]
    public byte[,,] voxelMap = new byte[VoxelData.ChunkSize, VoxelData.ChunkSize, VoxelData.ChunkSize];

    public ChunkData(Vector3Int pos) {

        try {

            chunkCoord = pos;
            Popuplate();

        } catch (System.Exception e) {
            Debug.LogError("Error in function ChunkData.Constructor: " + e.Message);
        }
    }

    /// <summary>
    /// Popuplates the current chunk data with all its voxels
    /// </summary>
    public void Popuplate() {

        try {

            for (int x = 0; x < VoxelData.ChunkSize; x++) {
                for (int z = 0; z < VoxelData.ChunkSize; z++) {
                    for (int y = 0; y < VoxelData.ChunkSize; y++) {

                        Vector3Int voxelPosInWorld = new Vector3Int((VoxelData.ChunkSize * chunkCoord.x) + x, (VoxelData.ChunkSize * chunkCoord.y) + y, (VoxelData.ChunkSize * chunkCoord.z) + z);
                        voxelMap[x, y, z] = UniverseData.currentPlanet.GetVoxel(voxelPosInWorld);
                    }
                }
            }

            UniverseData.currentPlanet.planetData.AddToModifiedChunkList(this);

        } catch (System.Exception e) {
            Debug.LogError("Error in function ChunkData.Popuplate: " + e.Message);
        }
    }
}