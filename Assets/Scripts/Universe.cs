using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Universe : MonoBehaviour {

    public Material material;
    public GameObject player;
    public Vector3 spawnPosition;

    public BlockType[] blockTypes;

    private Planet currentPlanet;

    private void Start() {

        UniverseData.universe = this;

        int seed = Mathf.CeilToInt(Random.Range(0, 100));

        currentPlanet = new Planet(seed);

        spawnPosition = new Vector3(VoxelData.PlanetSizeInVoxels / 2, VoxelData.PlanetSizeInVoxels + 2f, VoxelData.PlanetSizeInVoxels / 2);
        player = Instantiate(player);
        player.transform.position = spawnPosition;
    }

    private void Update() {

        try {

            if (Input.GetKeyDown(KeyCode.F1) && currentPlanet != null) {
                SaveSystem.SavePlanet(currentPlanet.planetData);
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function Planet.Update: " + e.Message);
        }
    }
}