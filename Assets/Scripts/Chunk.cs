using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk {

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public GameObject gameObject;
    public Vector2Int pos;
    public Planet planet;
    public ChunkData data;

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    private int vertexIndex = 0;

    public Chunk(Planet planet, int x, int z) {
        try {

            gameObject = new GameObject();
            this.pos = new Vector2Int(x, z);
            this.planet = planet;

            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

            meshRenderer.material = planet.material;
            gameObject.transform.SetParent(planet.gameObject.transform);
            gameObject.transform.position = new Vector3(x * VoxelData.ChunkSize, 0, z * VoxelData.ChunkSize);
            gameObject.name = "Chunk " + x + ", " + z;

            data = planet.data.RequestChunk(x, z);

            CreateMeshData();
            CreateMesh();

        } catch (System.Exception e) {
            Debug.LogError("Error in function Chunk.Constructor: " + e.Message);
        }
    }

    public void CreateMeshData() {
        try {

            for (int y = 0; y < Planet.WorldHeight; y++) {
                for (int x = 0; x < VoxelData.ChunkSize; x++) {
                    for (int z = 0; z < VoxelData.ChunkSize; z++) {

                        byte blockType = data.blocks[x, y, z];

                        if (this.planet.blockTypes[blockType].isSolid) {
                            AddVoxelDataToChunk(new Vector3Int(x, y, z));
                        }
                    }
                }
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function Chunk.CreateMeshData: " + e.Message);
        }
    }

    void AddVoxelDataToChunk(Vector3Int pos) {
        try {

            for (int p = 0; p < 6; p++) {

                Vector3Int posToCheck = pos + VoxelData.FaceChecks[p];

                if (!HelperFunctions.CheckVoxelInChunk(this, posToCheck.x, posToCheck.y, posToCheck.z)) {

                    byte blockID = data.blocks[pos.x, pos.y, pos.z];

                    for (int i = 0; i < 4; i++) {
                        vertices.Add(pos + VoxelData.Vertices[VoxelData.Triangles[p, i]]);
                    }

                    AddTexture(this.planet.blockTypes[blockID].GetTextureID(p));

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
}