using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class Chunk {

    public static readonly Vector2Int[] NeighboringChunks = new Vector2Int[8]{
        new Vector2Int(-1, 1),
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),
        new Vector2Int(1, 0),
        new Vector2Int(1, -1),
        new Vector2Int(0, -1),
        new Vector2Int(-1, -1),
        new Vector2Int(-1, 0)
    };

    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public Mesh mesh = new();
    public GameObject gameObject;
    public Vector2Int pos;
    public Vector2Int posInBlocks;
    public Planet planet;
    public ChunkData data;

    private List<Vector3> vertices = new();
    private List<int> triangles = new();
    private List<int> waterTriangles = new();
    private List<Vector2> uvs = new();

    private int vertexIndex = 0;

    public Dictionary<Vector3Int, string> structures = new();

    public ChunkStage stage = ChunkStage.Null;

    /// <summary>
    /// Creates a new chunk object and renders it
    /// </summary>
    /// <param name="planet"></param>
    /// <param name="pos"></param>
    /// <param name="normalizedPos"></param>
    public Chunk(Planet planet, Vector2Int pos) {
        try {

            this.pos = pos;
            this.posInBlocks = pos * VoxelData.ChunkSize;
            this.planet = planet;
            this.data = new ChunkData(planet);
            planet.chunks[pos.x, pos.y] = this;

        } catch (Exception e) {
            Debug.LogError("Error in function Chunk.Constructor: " + e.Message);
        }
    }

    /// <summary>
    /// Popuplates the current chunk data with all its voxels
    /// </summary>
    public void Popuplate() {
        try {

            for (int x = 0; x < VoxelData.ChunkSize; x++) {
                for (int z = 0; z < VoxelData.ChunkSize; z++) {
                    this.planet.terrainGenerator.GenerateTerrain(this, x, z);
                }
            }

            this.stage = this.structures.Count == 0 ? ChunkStage.StructuresGenerated : ChunkStage.DataPopulated;

        } catch (Exception e) {
            Debug.LogError("Error in Chunk Popuplate: " + e.Message);
        }
    }

    public void ClearMeshData() {
        try {

            vertexIndex = 0;
            vertices.Clear();
            triangles.Clear();
            waterTriangles.Clear();
            uvs.Clear();

        } catch (Exception ex) {
            Debug.LogError("Error in Chunk ClearMeshData: " + ex.Message);
        }
    }

    public void GenerateStructures() {
        try {

            foreach (KeyValuePair<Vector3Int, string> kvp in this.structures) {
                Vector3Int blockPos = kvp.Key;
                string biomeName = kvp.Value;
                this.planet.terrainGenerator.GenerateTree(this, biomeName, blockPos.x, blockPos.y, blockPos.z);
            }

            this.structures.Clear();
            this.stage = ChunkStage.StructuresGenerated;

        } catch (Exception ex) {
            Debug.LogError("Error in Chunk GenerateStructures: " + ex.Message);
        }
    }

    /// <summary>
    /// Generates the chunk mesh based on its block data
    /// </summary>
    public void CreateMeshData() {
        try {

            ClearMeshData();

            for (int y = 0; y < this.planet.WorldHeight; y++) {
                for (int x = 0; x < VoxelData.ChunkSize; x++) {
                    for (int z = 0; z < VoxelData.ChunkSize; z++) {
                        AddVoxelDataToChunk(x, y, z);
                    }
                }
            }

            this.stage = ChunkStage.MeshCreated;

        } catch (Exception e) {
            Debug.LogError("Error in function Chunk.CreateMeshData: " + e.Message);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="blockXInChunk"></param>
    /// <param name="blockYInChunk"></param>
    /// <param name="blockZInChunk"></param>
    void AddVoxelDataToChunk(int blockXInChunk, int blockYInChunk, int blockZInChunk) {
        try {

            BlockID blockID = this.data.blocks[blockXInChunk, blockYInChunk, blockZInChunk]; //Get the block id

            Vector3Int blockPosInChunk = new(blockXInChunk, blockYInChunk, blockZInChunk);

            for (int face = 0; face < 3; face++) {

                Vector3Int faceCheck = VoxelData.FaceChecks[face];
                Vector3Int neighborPosInChunk = blockPosInChunk + faceCheck;

                BlockID neighborBlockID;

                if (neighborPosInChunk.x == VoxelData.ChunkSize) {

                    Chunk neighborChunk = this.pos.x == this.planet.PlanetSizeInChunks - 1 ? this.planet.chunks[0, this.pos.y] : this.planet.chunks[this.pos.x + 1, this.pos.y];

                    if (neighborChunk == null) {
                        continue;
                    }

                    neighborBlockID = neighborChunk.data.blocks[0, neighborPosInChunk.y, neighborPosInChunk.z];

                } else if (neighborPosInChunk.z == VoxelData.ChunkSize) {

                    Chunk neighborChunk = this.pos.y == this.planet.PlanetSizeInChunks - 1 ? this.planet.chunks[this.pos.x, 0] : this.planet.chunks[this.pos.x, this.pos.y + 1];

                    if (neighborChunk == null) {
                        continue;
                    }

                    neighborBlockID = neighborChunk.data.blocks[neighborPosInChunk.x, neighborPosInChunk.y, 0];

                } else if (neighborPosInChunk.y == this.planet.WorldHeight) {

                    neighborBlockID = BlockID.Air;

                } else {

                    neighborBlockID = this.data.blocks[neighborPosInChunk.x, neighborPosInChunk.y, neighborPosInChunk.z];
                }

                if (blockID == BlockID.Air) {

                    if (neighborBlockID != BlockID.Air) {
                        AddTexture(neighborPosInChunk, neighborBlockID, face + 3);
                    }

                } else if (blockID == BlockID.Water) {

                    if (neighborBlockID == BlockID.Air) {
                        AddTexture(blockPosInChunk, blockID, face);
                    } else if (this.planet.blockTypes[(int)neighborBlockID].isSolid) {
                        AddTexture(neighborPosInChunk, neighborBlockID, face + 3);
                    }

                } else {

                    if (!this.planet.blockTypes[(int)neighborBlockID].isSolid) {
                        AddTexture(blockPosInChunk, blockID, face);
                    }
                }
            }
            
        } catch (Exception e) {
            Debug.LogError("Error in Chunk AddVoxelDataToChunk: " + e.Message);
        }
    }

    public void CreateMesh() {
        try {

            this.meshFilter.mesh = null;
            this.mesh.Clear();
            this.mesh.subMeshCount = 2;
            this.mesh.vertices = vertices.ToArray();
            this.mesh.uv = uvs.ToArray();
            this.mesh.SetTriangles(triangles.ToArray(), 0);
            this.mesh.SetTriangles(waterTriangles.ToArray(), 1);
            this.mesh.RecalculateNormals();
            this.meshFilter.mesh = this.mesh;

        } catch (Exception e) {
            Debug.LogError("Error in Chunk CreateMesh: " + e.Message);
        }
    }

    void AddTexture(Vector3Int blockPosInChunk, BlockID blockID, int face) {
        try {

            for (int i = 0; i < 4; i++) {
                int index = VoxelData.Triangles[face, i];
                Vector3Int vertex = blockPosInChunk + VoxelData.Vertices[index];
                vertices.Add(vertex);
            }

            Block block = this.planet.blockTypes[(byte)blockID];
            int textureID = block.GetTextureID(face);

            float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
            float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

            x *= VoxelData.NormalizedBlockTextureSize;
            y *= VoxelData.NormalizedBlockTextureSize;

            y = 1f - y - VoxelData.NormalizedBlockTextureSize;

            uvs.Add(new Vector2(x, y));
            uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
            uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
            uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));

            if (blockID == BlockID.Water) {

                waterTriangles.Add(vertexIndex);
                waterTriangles.Add(vertexIndex + 1);
                waterTriangles.Add(vertexIndex + 2);
                waterTriangles.Add(vertexIndex + 2);
                waterTriangles.Add(vertexIndex + 1);
                waterTriangles.Add(vertexIndex + 3);

            } else {

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);
            }
            
            vertexIndex += 4;

        } catch (Exception e) {
            Debug.LogError("Error in Chunk AddTexture: " + e.Message);
        }
    }

    /// <summary>
    /// Disables the chunk and adds its game object in the queue to be reused
    /// </summary>
    public void Disable() {
        try {

            this.gameObject.SetActive(false);
            this.planet.unusedChunkObjects.Enqueue(this.gameObject);
            this.gameObject = null;
            this.meshFilter.mesh = null;
            this.stage = ChunkStage.MeshCreated;
        
        } catch (Exception ex) {
            Debug.LogError("Error in Chunk Disable: " + ex.Message);
        }
    }

    /// <summary>
    /// Renders the chunk into view
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="normalizedPos"></param>
    public void Enable(Vector2Int pos, Vector2Int normalizedPos) {
        try {

            if (this.gameObject == null) {
                if (this.planet.unusedChunkObjects.Count > 0) {
                    this.gameObject = planet.unusedChunkObjects.Dequeue();
                } else {
                    this.gameObject = UnityEngine.Object.Instantiate(this.planet.chunkPrefab);
                }
            }

            this.meshFilter = gameObject.GetComponent<MeshFilter>();

            this.gameObject.transform.SetParent(planet.gameObject.transform);
            this.gameObject.transform.position = new Vector3(pos.x * VoxelData.ChunkSize, 0, pos.y * VoxelData.ChunkSize);
            this.gameObject.name = $"Chunk {normalizedPos.x}, {normalizedPos.y}";
            this.gameObject.SetActive(true);

            this.CreateMesh();
            this.stage = ChunkStage.Rendered;

        } catch (Exception ex) {
            Debug.LogError("Error in Chunk Enable: " + ex.Message);
        }
    }
}

[System.Serializable]
public class ChunkData {

    [HideInInspector]
    public BlockID[,,] blocks;

    public ChunkData(Planet planet) {
        try {

            this.blocks = new BlockID[VoxelData.ChunkSize, planet.WorldHeight, VoxelData.ChunkSize];

        } catch (Exception ex) {
            Debug.LogError("Error in ChunkData Constructor: " + ex.Message);
        }
    }
}

public enum ChunkStage {
    Null = 0,
    DataPopulated = 1,
    MeshCreated = 2,
    StructuresGenerated = 3,
    Rendered = 4
}