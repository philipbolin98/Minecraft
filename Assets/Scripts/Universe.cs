using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Universe : MonoBehaviour {

    public Material material;
    public GameObject player;
    public Vector3 spawnPosition;
    public BlockType[] blockTypes;

    private void Start() {
        try {

            UniverseData.universe = this;

            int seed = Mathf.FloorToInt(Random.Range(0f, 1f) * 100);

            UniverseData.currentPlanet = new Planet(seed);

            spawnPosition = new Vector3(VoxelData.PlanetSizeInVoxels / 2, VoxelData.PlanetSizeInVoxels + 2f, VoxelData.PlanetSizeInVoxels / 2);
            Instantiate(player);
            player.transform.position = spawnPosition;

        } catch (System.Exception e) {
            Debug.LogError("Error in function Universe.Start: " + e.Message);
        }
    }
}