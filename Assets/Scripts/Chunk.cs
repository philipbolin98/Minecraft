using System;
using System.Collections.Generic;
using UnityEngine;

public class Chunk {

    public static readonly int ChunkSize = 32;

    public static readonly Vector3Int[] NeighboringChunks = new Vector3Int[26]{
        new(-1, -1, -1),
        new(-1, -1, 0),
        new(-1, -1, 1),
        new(0, -1, 1),
        new(1, -1, 1),
        new(1, -1, 0),
        new(1, -1, -1),
        new(0, -1, -1),
        new(0, -1, 0),

        new(-1, 0, -1),
        new(-1, 0, 0),
        new(-1, 0, 1),
        new(0, 0, 1),
        new(1, 0, 1),
        new(1, 0, 0),
        new(1, 0, -1),
        new(0, 0, -1),

        new(-1, 1, -1),
        new(-1, 1, 0),
        new(-1, 1, 1),
        new(0, 1, 1),
        new(1, 1, 1),
        new(1, 1, 0),
        new(1, 1, -1),
        new(0, 1, -1),
        new(0, 1, 0)
    };

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public Mesh mesh = new();
    public GameObject gameObject;
    public Vector3Int pos;
    public Vector3Int normalizedPos;
    public Vector3Int normalizedPosInBlocks;
    public Planet planet;
    public ChunkData data;

    private List<Vector3> vertices = new();
    private List<int> triangles = new();
    private List<int> transparentTriangles = new();
    private List<int> waterTriangles = new();
    private List<Vector2> uvs = new();
    private Material[] materials = new Material[3];
    private int vertexIndex = 0;

    public ChunkStage stage = ChunkStage.Null;
    public bool modified = false;

    /// <summary>
    /// Creates a new chunk object and renders it
    /// </summary>
    /// <param name="planet"></param>
    /// <param name="pos"></param>
    /// <param name="normalizedPos"></param>
    public Chunk(Planet planet, Vector3Int pos, Vector3Int normalizedPos, ChunkData data = null) {
        try {

            this.pos = pos;
            this.normalizedPos = normalizedPos;
            this.normalizedPosInBlocks = normalizedPos * ChunkSize;
            this.planet = planet;

            this.data = data != null ? data : new ChunkData();

            planet.chunks[normalizedPos.x, normalizedPos.y, normalizedPos.z] = this;

            this.materials[0] = planet.material;
            this.materials[1] = planet.waterMaterial;
            this.materials[2] = planet.transparentMaterial;

        } catch (Exception ex) {
            Debug.LogError("Error in function Chunk.Constructor: " + ex.Message);
        }
    }

    /// <summary>
    /// Popuplates the current chunk data with all its voxels
    /// </summary>
    public void Popuplate() {
        try {

            //System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

            for (int x = 0; x < ChunkSize; x++) {
                for (int z = 0; z < ChunkSize; z++) {
                    this.planet.terrainGenerator.GenerateTerrain(this, x, z);
                }
            }

            this.planet.structureGenerator.GenerateStructures(this, this.normalizedPos);
            this.GenerateStructuresInNeighboringChunks();
            this.stage = ChunkStage.Populated;

            //Debug.Log($"Populating chunk: {watch.Elapsed.TotalSeconds}");

        } catch (Exception ex) {
            Debug.LogError("Error in Chunk Popuplate: " + ex.Message);
        }
    }

    public void GenerateStructuresInNeighboringChunks() {
        try {

            foreach (Vector3Int offset in NeighboringChunks) {
                Vector3Int chunkPos = HelperFunctions.NormalizeChunkPos(this.planet, this.normalizedPos + offset);
                this.planet.structureGenerator.GenerateStructures(this, chunkPos);
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Chunk GenerateStructuresInNeighboringChunks: " + ex.Message);
        }
    }

    public void ClearMeshData() {
        try {

            vertexIndex = 0;
            vertices.Clear();
            triangles.Clear();
            transparentTriangles.Clear();
            waterTriangles.Clear();
            uvs.Clear();

        } catch (Exception ex) {
            Debug.LogError("Error in Chunk ClearMeshData: " + ex.Message);
        }
    }

    /// <summary>
    /// Generates the chunk mesh based on its block data
    /// </summary>
    public void CreateMeshData() {
        try {

            //System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

            ClearMeshData();

            for (int x = 0; x < ChunkSize; x++) {
                for (int y = 0; y < ChunkSize; y++) {
                    for (int z = 0; z < ChunkSize; z++) {
                        AddVoxelDataToChunk(x, y, z);
                    }
                }
            }

            this.stage = ChunkStage.MeshCreated;

            //Debug.Log($"Meshing chunk: {watch.Elapsed.TotalSeconds}");

        } catch (Exception ex) {
            Debug.LogError("Error in function Chunk.CreateMeshData: " + ex.Message);
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

                Vector3Int faceCheck = Block.FaceChecks[face];
                Vector3Int neighborPosInChunk = blockPosInChunk + faceCheck;

                BlockID neighborBlockID;

                if (neighborPosInChunk.x == ChunkSize) {

                    Chunk neighborChunk = this.normalizedPos.x == this.planet.PlanetSizeInChunks - 1 ? this.planet.chunks[0, this.normalizedPos.y, this.normalizedPos.z] : this.planet.chunks[this.normalizedPos.x + 1, this.normalizedPos.y, this.normalizedPos.z];

                    if (neighborChunk == null) {
                        continue;
                    }

                    neighborBlockID = neighborChunk.data.blocks[0, neighborPosInChunk.y, neighborPosInChunk.z];

                } else if (neighborPosInChunk.z == ChunkSize) {

                    Chunk neighborChunk = this.normalizedPos.z == this.planet.PlanetSizeInChunks - 1 ? this.planet.chunks[this.normalizedPos.x, this.normalizedPos.y, 0] : this.planet.chunks[this.normalizedPos.x, this.normalizedPos.y, this.normalizedPos.z + 1];

                    if (neighborChunk == null) {
                        continue;
                    }

                    neighborBlockID = neighborChunk.data.blocks[neighborPosInChunk.x, neighborPosInChunk.y, 0];
                    
                } else if (neighborPosInChunk.y == ChunkSize) {

                    if (this.normalizedPos.y == this.planet.PlanetHeightInChunks - 1) {

                        neighborBlockID = BlockID.Air;

                    } else {

                        Chunk neighborChunk = this.planet.chunks[this.normalizedPos.x, this.normalizedPos.y + 1, this.normalizedPos.z];

                        if (neighborChunk == null) {
                            continue;
                        }

                        neighborBlockID = neighborChunk.data.blocks[neighborPosInChunk.x, 0, neighborPosInChunk.z];
                    }

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

                        if (this.planet.blockTypes[(int)neighborBlockID].isTransparent) {
                            AddTexture(blockPosInChunk, blockID, face);
                        }
                    }

                } else if (this.planet.blockTypes[(int)blockID].isTransparent) {

                    if (!this.planet.blockTypes[(int)neighborBlockID].isTransparent) {
                        AddTexture(neighborPosInChunk, neighborBlockID, face + 3);
                    } else if (blockID != neighborBlockID) {
                        AddTexture(blockPosInChunk, blockID, face);

                        if (neighborBlockID != BlockID.Air) {
                            AddTexture(neighborPosInChunk, neighborBlockID, face + 3);
                        }
                    }

                } else {

                    if (this.planet.blockTypes[(int)neighborBlockID].isTransparent) {
                        AddTexture(blockPosInChunk, blockID, face);
                    }
                }
            }
            
        } catch (Exception ex) {
            Debug.LogError("Error in Chunk AddVoxelDataToChunk: " + ex.Message);
        }
    }

    public void CreateMesh() {
        try {

            this.meshFilter.mesh = null;
            this.meshRenderer.materials = this.materials;
            this.mesh.Clear();
            this.mesh.subMeshCount = 3;
            this.mesh.vertices = vertices.ToArray();
            this.mesh.uv = uvs.ToArray();
            this.mesh.SetTriangles(triangles.ToArray(), 0);
            this.mesh.SetTriangles(waterTriangles.ToArray(), 1);
            this.mesh.SetTriangles(transparentTriangles.ToArray(), 2);
            this.mesh.RecalculateNormals();
            this.meshFilter.mesh = this.mesh;

        } catch (Exception ex) {
            Debug.LogError("Error in Chunk CreateMesh: " + ex.Message);
        }
    }

    void AddTexture(Vector3Int blockPosInChunk, BlockID blockID, int face) {
        try {

            for (int i = 0; i < 4; i++) {
                int index = Block.Triangles[face, i];
                Vector3Int vertex = blockPosInChunk + Block.Vertices[index];
                vertices.Add(vertex);
            }

            Block block = this.planet.blockTypes[(int)blockID];
            int textureID = block.GetTextureID(face);

            float y = textureID / Block.TextureAtlasSizeInBlocks;
            float x = textureID - (y * Block.TextureAtlasSizeInBlocks);

            x *= Block.NormalizedBlockTextureSize;
            y *= Block.NormalizedBlockTextureSize;

            y = 1f - y - Block.NormalizedBlockTextureSize;

            uvs.Add(new Vector2(x, y));
            uvs.Add(new Vector2(x, y + Block.NormalizedBlockTextureSize));
            uvs.Add(new Vector2(x + Block.NormalizedBlockTextureSize, y));
            uvs.Add(new Vector2(x + Block.NormalizedBlockTextureSize, y + Block.NormalizedBlockTextureSize));

            if (blockID == BlockID.Water) {

                waterTriangles.Add(vertexIndex);
                waterTriangles.Add(vertexIndex + 1);
                waterTriangles.Add(vertexIndex + 2);
                waterTriangles.Add(vertexIndex + 2);
                waterTriangles.Add(vertexIndex + 1);
                waterTriangles.Add(vertexIndex + 3);

            } else if (block.isTransparent) {

                transparentTriangles.Add(vertexIndex);
                transparentTriangles.Add(vertexIndex + 1);
                transparentTriangles.Add(vertexIndex + 2);
                transparentTriangles.Add(vertexIndex + 2);
                transparentTriangles.Add(vertexIndex + 1);
                transparentTriangles.Add(vertexIndex + 3);

            } else {

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);
            }
            
            vertexIndex += 4;

        } catch (Exception ex) {
            Debug.LogError("Error in Chunk AddTexture: " + ex.Message);
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
            this.stage = ChunkStage.Disabled;

            //if (this.modified) {
            //    this.stage = ChunkStage.Disabled;
            //} else {
            //    this.stage = ChunkStage.Null;
            //    this.ClearMeshData();
            //    this.meshFilter = null;
            //    this.meshRenderer = null;
            //    this.mesh = null;
            //    this.materials = null;
            //    this.modifications.Clear();
            //    this.structures.Clear();
            //    this.data.blocks = null;
            //    this.data = null;
            //    this.planet.chunks[this.normalizedPos.x, this.normalizedPos.y] = null;
            //    this.planet = null;
            //}

        } catch (Exception ex) {
            Debug.LogError("Error in Chunk Disable: " + ex.Message);
        }
    }

    /// <summary>
    /// Renders the chunk into view
    /// </summary>
    public void Enable() {
        try {

            if (this.gameObject == null) {

                if (this.planet.unusedChunkObjects.Count > 0) {
                    this.gameObject = planet.unusedChunkObjects.Dequeue();
                } else {
                    this.gameObject = UnityEngine.Object.Instantiate(this.planet.chunkPrefab);
                }

                this.meshFilter = gameObject.GetComponent<MeshFilter>();
                this.meshRenderer = gameObject.GetComponent<MeshRenderer>();

                this.gameObject.transform.SetParent(planet.gameObject.transform);
                this.gameObject.transform.position = new Vector3(this.pos.x * ChunkSize, this.pos.y * ChunkSize, this.pos.z * ChunkSize);
                this.gameObject.name = $"Chunk {this.normalizedPos.x}, {this.normalizedPos.y}, {this.normalizedPos.z}";
                this.gameObject.SetActive(true);

                this.CreateMesh();
            }
            
            this.stage = ChunkStage.Rendered;

        } catch (Exception ex) {
            Debug.LogError("Error in Chunk Enable: " + ex.Message);
        }
    }
}

[Serializable]
public class ChunkData {

    [HideInInspector]
    public BlockID[,,] blocks;

    public ChunkData() {
        try {

            this.blocks = new BlockID[Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize];

        } catch (Exception ex) {
            Debug.LogError("Error in ChunkData Constructor: " + ex.Message);
        }
    }
}

public enum ChunkStage {
    Null = 0,
    Populating = 1,
    Populated = 2,
    CreatingMesh = 3,
    MeshCreated = 4,
    Rendering = 5,
    Rendered = 6,
    Disabled = 7
}