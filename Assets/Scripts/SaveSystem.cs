using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

public class SaveSystem {

    public static string appPath = Application.persistentDataPath;

    public static void SavePlanet(PlanetData planet) {

        string savePath = $"{appPath}/saves/{planet.planetName}/";

        if (!Directory.Exists(savePath)) {
            Directory.CreateDirectory(savePath);
        }

        Debug.Log($"Saving {planet.planetName}");

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream($"{savePath}planet.dat", FileMode.Create);

        formatter.Serialize(stream, planet);
        stream.Close();

        SaveChunks(planet);
    }

    public static PlanetData LoadPlanet(string planetName, int seed = 0) {

        string loadPath = $"{appPath}/saves/{planetName}/";

        if (Directory.Exists(loadPath)) {

            Debug.Log($"{planetName} found. Loading from save.");

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream($"{loadPath}planet.dat", FileMode.Open);

            PlanetData planet = formatter.Deserialize(stream) as PlanetData;

            stream.Close();
            return new PlanetData(planet);

        } else {

            Debug.Log($"{planetName} not found. Creating new planet.");

            PlanetData planet = new PlanetData(planetName, seed);

            return planet;
        }
    }

    public static void SaveChunks(PlanetData planet) {

        List<ChunkData> chunks = new List<ChunkData>(planet.modifiedChunks);
        planet.modifiedChunks.Clear();

        int count = 0;
        foreach (ChunkData chunk in chunks) {

            SaveChunk(chunk, planet.planetName);
            count++;
        }

        Debug.Log($"{count} chunks saved");
    }

    public static void SaveChunk(ChunkData chunk, string planetName) {

        string chunkName = $"{chunk.chunkCoord.x}-{chunk.chunkCoord.y}-{chunk.chunkCoord.z}";

        string savePath = $"{appPath}/saves/{planetName}/chunks/";

        if (!Directory.Exists(savePath)) {
            Directory.CreateDirectory(savePath);
        }

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream($"{savePath}{chunkName}.chunk", FileMode.Create);

        formatter.Serialize(stream, chunk);
        stream.Close();
    }

    public static ChunkData LoadChunk(string planetName, Vector3Int pos) {

        string chunkName = $"{pos.x}-{pos.y}-{pos.z}";

        string loadPath = $"{appPath}/saves/{planetName}/chunks/{chunkName}.chunk";

        if (File.Exists(loadPath)) {

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath, FileMode.Open);

            ChunkData chunk = formatter.Deserialize(stream) as ChunkData;

            stream.Close();
            return chunk;
        } else {
            return null;
        }
    }
}