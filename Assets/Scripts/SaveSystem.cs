using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.IO.IsolatedStorage;

public class SaveSystem {

    public static string appPath = Application.persistentDataPath;

    public static void SavePlanet(PlanetData planet) {
        try {

            string savePath = $"{appPath}/saves/{planet.planetName}/";

            if (!Directory.Exists(savePath)) {
                Directory.CreateDirectory(savePath);
            }

            Debug.Log($"Saving {planet.planetName}");

            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream($"{savePath}planet.dat", FileMode.Create)) {
                formatter.Serialize(stream, planet);
            }

            SaveChunks(planet);

        } catch (System.Exception e) {
            Debug.LogError("Error in function SaveSystem.SavePlanet: " + e.Message);
        }
    }

    public static PlanetData LoadPlanet(string planetName, int seed) {
        try {

            string loadPath = $"{appPath}/saves/{planetName}/";

            if (Directory.Exists(loadPath)) {

                Debug.Log($"{planetName} found. Loading from save.");

                BinaryFormatter formatter = new BinaryFormatter();

                PlanetData planetData = null;
                using (FileStream stream = new FileStream($"{loadPath}planet.dat", FileMode.Open)) {
                    planetData = formatter.Deserialize(stream) as PlanetData;
                }
                
                return planetData;

            } else {

                Debug.Log($"{planetName} not found. Creating new planet.");

                return new PlanetData(planetName, seed);
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function SaveSystem.LoadPlanet: " + e.Message);
            return null;
        }
    }

    public static void SaveChunks(PlanetData planet) {
        try {

            List<ChunkData> chunks = new List<ChunkData>(planet.modifiedChunks);
            planet.modifiedChunks.Clear();

            int count = 0;
            foreach (ChunkData chunk in chunks) {

                SaveChunk(chunk, planet.planetName);
                count++;
            }

            Debug.Log($"{count} chunks saved");

        } catch (System.Exception e) {
            Debug.LogError("Error in function SaveSystem.SaveChunks: " + e.Message);
        }
    }

    public static void SaveChunk(ChunkData chunk, string planetName) {
        try {

            string chunkName = $"{chunk.chunkCoord.x}-{chunk.chunkCoord.y}-{chunk.chunkCoord.z}";

            string savePath = $"{appPath}/saves/{planetName}/chunks/";

            if (!Directory.Exists(savePath)) {
                Directory.CreateDirectory(savePath);
            }

            BinaryFormatter formatter = new BinaryFormatter();

            using (FileStream stream = new FileStream($"{savePath}{chunkName}.chunk", FileMode.Create)) {
                formatter.Serialize(stream, chunk);
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function SaveSystem.SaveChunk: " + e.Message);
        }
    }

    public static ChunkData LoadChunk(string planetName, Vector3Int pos) {
        try {

            string chunkName = $"{pos.x}-{pos.y}-{pos.z}";

            string loadPath = $"{appPath}/saves/{planetName}/chunks/{chunkName}.chunk";

            if (File.Exists(loadPath)) {

                BinaryFormatter formatter = new BinaryFormatter();

                ChunkData chunk = null;

                using (FileStream stream = new FileStream(loadPath, FileMode.Open)) {
                    chunk = formatter.Deserialize(stream) as ChunkData;
                }

                return chunk;
            } else {
                return null;
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function SaveSystem.LoadChunk: " + e.Message);
            return null;
        }
    }
}