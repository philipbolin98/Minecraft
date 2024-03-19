using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlockOutline {

    public bool enabled = false;
    public Vector3Int pos = Vector3Int.zero;

    public Mesh mesh = new();
    public MeshFilter meshFilter;

    public GameObject gameObject;

    public BlockOutline(GameObject prefab) {
        try {

            this.gameObject = UnityEngine.Object.Instantiate(prefab);
            this.meshFilter = this.gameObject.gameObject.GetComponent<MeshFilter>();
            this.meshFilter.mesh = this.mesh;

        } catch (Exception ex) {
            Debug.LogError("Error in BlockOutline Constructor: " + ex.Message);
        }
    }

    public void Enable(Vector3Int pos, Planet planet) {
        try {

            if (this.pos != pos || !this.enabled) {
                this.enabled = true;
                this.gameObject.transform.position = pos;
                this.gameObject.SetActive(true);
                this.CreateMesh(pos, planet);
            }

        } catch (Exception ex) {
            Debug.LogError("Error in BlockOutline Enable: " + ex.Message);
        }
    }

    public void Disable() {
        try {

            if (this.enabled) {
                this.enabled = false;
                this.gameObject.SetActive(false);
            }

        } catch (Exception ex) {
            Debug.LogError("Error in BlockOutline Enable: " + ex.Message);
        }
    }

    public void CreateMesh(Vector3Int pos, Planet planet) {
        try {

            Dictionary<Vector3, int> dVertices = new();
            List<int> indices = new();

            for (int i = 0; i < Block.FaceChecks.Length; i++) {

                Vector3Int face = Block.FaceChecks[i];
                Vector3Int blockPosInPlanet = HelperFunctions.NormalizeBlockPos(pos + face, planet.PlanetSizeInVoxels);

                if (!HelperFunctions.CheckBlockInPlanet(planet, blockPosInPlanet.x, blockPosInPlanet.y, blockPosInPlanet.z)) {

                    int[] x = new int[4];

                    for (int j = 0; j < 4; j++) {
                        int index = Block.Triangles[i, j];
                        Vector3Int vertex = Block.Vertices[index];

                        if (!dVertices.ContainsKey(vertex)) {
                            dVertices.Add(vertex, dVertices.Count);
                        }

                        x[j] = dVertices[vertex];
                    }

                    indices.Add(x[0]);
                    indices.Add(x[1]);

                    indices.Add(x[1]);
                    indices.Add(x[3]);

                    indices.Add(x[3]);
                    indices.Add(x[2]);

                    indices.Add(x[2]);
                    indices.Add(x[0]);
                }
            }

            this.mesh.Clear();
            this.mesh.vertices = dVertices.Keys.ToList().ToArray();
            this.mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);

        } catch (Exception ex) {
            Debug.LogError("Error in BlockOutline CreateMesh: " + ex.Message);
        }
    }
}