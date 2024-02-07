using UnityEngine;

public class VoxelData {

    public static readonly int ChunkSize = 16;
    public static readonly int TextureAtlasSizeInBlocks = 4;

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
        Vector3Int.right, //Right
        Vector3Int.up, //Top
        Vector3Int.forward, //Front
        Vector3Int.left, //Left
        Vector3Int.down, //Bottom
        Vector3Int.back //Back
    };

    public static readonly int[,] Triangles = new int[6, 4]{
        {1,2,5,6}, //Right
        {3,7,2,6}, //Top
        {5,6,4,7}, //Front
        {4,7,0,3}, //Left
        {1,5,0,4}, //Bottom
        {0,3,1,2} //Back
    };

    public static readonly Vector2Int[] Uvs = new Vector2Int[4]{
        Vector2Int.zero,
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.one
    };

    public enum BlockFace {
        Right = 0,
        Top = 1,
        Front = 2,
        Left = 3,
        Bottom = 4,
        Back = 5
    }
}