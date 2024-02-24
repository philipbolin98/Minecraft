using System;

[Serializable]
public class Biome {
    public string name;
    public BlockID surfaceBlock;
    public int maxTerrainHeight;
}

public enum BiomeType {
    Plains = 0,
    Forest = 1,
    Desert = 2
}