using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Block {

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

    public string blockName;
    public bool isSolid;
    public Sprite icon;

    [Header("Texture Values")]
    public int rightFaceTexture;
    public int topFaceTexture;
    public int frontFaceTexture;
    public int leftFaceTexture;
    public int bottomFaceTexture;
    public int backFaceTexture;

    public int GetTextureID(int faceIndex) {

        int index = 0;

        try {

            switch (faceIndex) {
                case 0:
                    index = this.rightFaceTexture;
                    break;
                case 1:
                    index = this.topFaceTexture;
                    break;
                case 2:
                    index = this.frontFaceTexture;
                    break;
                case 3:
                    index = this.leftFaceTexture;
                    break;
                case 4:
                    index = this.bottomFaceTexture;
                    break;
                case 5:
                    index = this.backFaceTexture;
                    break;
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Block GetTextureID: " + ex.Message);
        }

        return index;
    }
}

public enum BlockID {
    Air = 0,
    Bedrock = 1,
    Stone = 2,
    Grass = 3,
    Dirt = 4,
    Water = 5,
    Sand = 6,
    Wood = 7,
    Leaf = 8
}