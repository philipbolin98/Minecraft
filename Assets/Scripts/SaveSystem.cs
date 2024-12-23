using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem {

    public static void SavePlanet(Planet planet) {
        try {

            string savePath = $"{Settings.savesPath}/{planet.planetName}";

            if (!Directory.Exists(savePath)) {
                Directory.CreateDirectory(savePath);
            }

            PlanetData data = new PlanetData(planet);

            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream($"{savePath}/planet.dat", FileMode.Create)) {
                formatter.Serialize(stream, data);
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function SaveSystem.SavePlanet: " + e.Message);
        }
    }

    public static void LoadPlanet(Planet planet) {
        try {

            string loadPath = $"{Settings.savesPath}/{planet.planetName}";

            if (Directory.Exists(loadPath)) {

                BinaryFormatter formatter = new BinaryFormatter();

                PlanetData planetData = null;
                using (FileStream stream = new FileStream($"{loadPath}/planet.dat", FileMode.Open)) {
                    planetData = formatter.Deserialize(stream) as PlanetData;
                }

                Planet.seed = planetData.seed;
                planet.PlanetSizeInChunks = planetData.sizeInChunks;
                planet.modifiedChunks = planetData.modifiedChunks;
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function SaveSystem.LoadPlanet: " + e.Message);
        }
    }
}