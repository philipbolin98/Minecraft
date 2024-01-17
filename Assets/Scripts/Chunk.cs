using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk {

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public GameObject chunkObject;
    public Vector2Int chunkCoord;
    public Planet planet;
    public ChunkData chunkData;

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    private int vertexIndex = 0;

    public Vector3 position;

    public Chunk(Vector2Int coord, Planet planet) {
        try {

            chunkObject = new GameObject();
            chunkCoord = coord;
            this.planet = planet;

            meshFilter = chunkObject.AddComponent<MeshFilter>();
            meshRenderer = chunkObject.AddComponent<MeshRenderer>();

            meshRenderer.material = planet.material;
            chunkObject.transform.SetParent(planet.gameObject.transform);
            chunkObject.transform.position = new Vector3(chunkCoord.x * VoxelData.ChunkSize, 0, chunkCoord.y * VoxelData.ChunkSize);
            chunkObject.name = "Chunk " + chunkCoord.x + ", " + chunkCoord.y;
            position = chunkObject.transform.position;

            chunkData = planet.planetData.RequestChunk(new Vector2Int(chunkCoord.x, chunkCoord.y));

            CreateMeshData();
            CreateMesh();

        } catch (System.Exception e) {
            Debug.LogError("Error in function Chunk.Constructor: " + e.Message);
        }
    }

    public void CreateMeshData() {
        try {

            for (int y = 0; y < VoxelData.WorldHeight; y++) {
                for (int x = 0; x < VoxelData.ChunkSize; x++) {
                    for (int z = 0; z < VoxelData.ChunkSize; z++) {
                        if (this.planet.blockTypes[chunkData.voxelMap[x, y, z]].isSolid) {
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

                if (!CheckVoxel(pos + VoxelData.FaceChecks[p])) {

                    byte blockID = chunkData.voxelMap[pos.x, pos.y, pos.z];

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

    bool CheckVoxel(Vector3 pos) {
        try {

            int x = Mathf.FloorToInt(pos.x);
            int y = Mathf.FloorToInt(pos.y);
            int z = Mathf.FloorToInt(pos.z);

            if (!IsVoxelInChunk(x, y, z))
                return planet.CheckForVoxel(pos + position);

            return this.planet.blockTypes[chunkData.voxelMap[x, y, z]].isSolid;

        } catch (System.Exception e) {
            Debug.LogError("Error in function Chunk.CheckVoxel: " + e.Message);
            return false;
        }
    }

    bool IsVoxelInChunk(int x, int y, int z) {
        try {

            if (x < 0 || x >= VoxelData.ChunkSize || y < 0 || y >= VoxelData.WorldHeight || z < 0 || z >= VoxelData.ChunkSize)
                return false;

            else return true;

        } catch (System.Exception e) {
            Debug.LogError("Error in function Chunk.IsVoxelInChunk: " + e.Message);
            return false;
        }
    }

    public byte GetVoxelFromGlobalVector3(Vector3 pos) {
        try {

            int xCheck = Mathf.FloorToInt(pos.x);
            int yCheck = Mathf.FloorToInt(pos.y);
            int zCheck = Mathf.FloorToInt(pos.z);

            xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
            zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);

            return chunkData.voxelMap[xCheck, yCheck, zCheck];

        } catch (System.Exception e) {

            Debug.LogError("Error in function Chunk.GetVoxelFromGlobalVector3: " + e.Message);
            return 0;
        }
    }
}