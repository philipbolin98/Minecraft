using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlanetData {

    public string planetName;
    public int seed;

    /// <summary>
    /// Creates new planet data when save file does not exist
    /// </summary>
    /// <param name="planetName">Name of the planet to create</param>
    /// <param name="seed">Seed of the planet</param>
    public PlanetData(string planetName, int seed) {
        try {

            this.planetName = planetName;
            this.seed = seed;

        } catch (System.Exception e) {
            Debug.LogError("Error in function PlanetData.Constructor1: " + e.Message);
        }
    }
}