using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk {

    public static readonly Vector2Int[] ChunksToCheckForMeshCreation = new Vector2Int[5]{
        new Vector2Int(0, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0)
    };

    public MeshFilter meshFilter;
    public GameObject gameObject;
    public Vector2Int pos;
    public Planet planet;
    public ChunkData data;

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    private int vertexIndex = 0;

    public bool isDataPopulated = false;
    public bool isMeshGenerated = false;
    public bool isRendered = false;

    /// <summary>
    /// Creates a new chunk object and renders it
    /// </summary>
    /// <param name="planet"></param>
    /// <param name="pos"></param>
    /// <param name="normalizedPos"></param>
    public Chunk(Planet planet, Vector2Int pos) {
        try {

            this.pos = pos;
            this.planet = planet;
            this.data = new ChunkData(planet, pos);
            planet.chunks[pos.x, pos.y] = this;

        } catch (System.Exception e) {
            Debug.LogError("Error in function Chunk.Constructor: " + e.Message);
        }
    }

    /// <summary>
    /// Generates the chunk mesh based on its block data
    /// </summary>
    public void CreateMeshData() {
        try {

            for (int y = 0; y < Planet.WorldHeight; y++) {
                for (int x = 0; x < VoxelData.ChunkSize; x++) {
                    for (int z = 0; z < VoxelData.ChunkSize; z++) {
                        AddVoxelDataToChunk(x, y, z);
                    }
                }
            }

        } catch (System.Exception e) {
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

            if (blockID == BlockID.Air) { //If the block is not solid, there is nothing to render so exit the function
                return;
            }

            Vector3Int blockPosInChunk = new Vector3Int(blockXInChunk, blockYInChunk, blockZInChunk);
            Vector3Int blockPosInPlanet = new Vector3Int(this.pos.x * VoxelData.ChunkSize + blockXInChunk, blockYInChunk, this.pos.y * VoxelData.ChunkSize + blockZInChunk);

            for (int face = 0; face < 6; face++) { //Loop over all 6 faces of the block

                Vector3Int faceCheck = VoxelData.FaceChecks[face];
                Vector3Int neighborPosInPlanet = blockPosInPlanet + faceCheck;

                Chunk chunk = HelperFunctions.GetChunkFromBlockPos(this.planet, neighborPosInPlanet.x, neighborPosInPlanet.y, neighborPosInPlanet.z);

                if (chunk == null) {
                    continue;
                }

                //Since the current block is solid, we need to check if each adjacent block is solid
                //If a neighboring block is also solid, we do not need to render their shared face
                //Otherwise, render the face that is adjacent to the air
                Vector3Int neighborPosInChunk = HelperFunctions.GetNormalizedBlockPosInChunk(blockPosInChunk + faceCheck);
                bool isSolid = HelperFunctions.CheckBlockInChunk(chunk, neighborPosInChunk.x, neighborPosInChunk.y, neighborPosInChunk.z);

                if (!isSolid) {

                    for (int i = 0; i < 4; i++) {
                        vertices.Add(blockPosInChunk + VoxelData.Vertices[VoxelData.Triangles[face, i]]);
                    }

                    Block block = this.planet.blockTypes[(byte)blockID];
                    int textureID = block.GetTextureID(face);
                    AddTexture(textureID);

                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);

                    vertexIndex += 4;
                }
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function Chunk.AddVoxelDataToChunk: " + e.Message);
        }
    }

    void CreateMesh() {
        try {

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();

            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;

        } catch (System.Exception e) {
            Debug.LogError("Error in function Chunk.CreateMesh: " + e.Message);
        }
    }

    void AddTexture(int textureID) {
        try {

            float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
            float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

            x *= VoxelData.NormalizedBlockTextureSize;
            y *= VoxelData.NormalizedBlockTextureSize;

            y = 1f - y - VoxelData.NormalizedBlockTextureSize;

            uvs.Add(new Vector2(x, y));
            uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
            uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
            uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));

        } catch (System.Exception e) {
            Debug.LogError("Error in function Chunk.AddTexture: " + e.Message);
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
            this.isRendered = false;
        
        } catch (System.Exception ex) {
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

            if (this.planet.unusedChunkObjects.Count > 0) {
                this.gameObject = planet.unusedChunkObjects.Dequeue();
            } else {
                this.gameObject = Object.Instantiate(this.planet.chunkPrefab);
            }

            this.meshFilter = gameObject.GetComponent<MeshFilter>();

            this.gameObject.transform.SetParent(planet.gameObject.transform);
            this.gameObject.transform.position = new Vector3(pos.x * VoxelData.ChunkSize, 0, pos.y * VoxelData.ChunkSize);
            this.gameObject.name = $"Chunk {normalizedPos.x}, {normalizedPos.y}";
            this.gameObject.SetActive(true);

            this.CreateMesh();
            this.isRendered = true;

        } catch (System.Exception ex) {
            Debug.LogError("Error in Chunk Enable: " + ex.Message);
        }
    }
}