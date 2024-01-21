using UnityEngine;

public class VoxelData {

    public static readonly int ChunkSize = 16;
    public static readonly int TextureAtlasSizeInBlocks = 4;
    public static readonly float TAU = 2 * Mathf.PI;

    public static float NormalizedBlockTextureSize {
        get { return 1f / TextureAtlasSizeInBlocks; }
    }

    public static readonly Vector3Int[] Vertices = new Vector3Int[8]{
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(1, 0, 1),
        new Vector3Int(1, 1, 1),
        new Vector3Int(0, 1, 1)
    };

    public static readonly Vector3Int[] FaceChecks = new Vector3Int[6] {
        new Vector3Int(0, 0, -1),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(1, 0, 0)
    };

    public static readonly int[,] Triangles = new int[6, 4]{
        {0,3,1,2}, //Back
        {5,6,4,7}, //Front
        {3,7,2,6}, //Top
        {1,5,0,4}, //Bottom
        {4,7,0,3}, //Left
        {1,2,5,6} //Right
    };

    public static readonly Vector2Int[] Uvs = new Vector2Int[4]{
        new Vector2Int(0, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(1, 1)
    };
}