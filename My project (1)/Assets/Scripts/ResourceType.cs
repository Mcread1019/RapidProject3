using UnityEngine;

public enum ResourceType
{
    Wood  = 0,
    Stone = 1,
    Coal  = 2,
    Iron  = 3,
    Gold  = 4
}

public static class ResourceInfo
{
    public static string GetName(ResourceType type) => type switch
    {
        ResourceType.Wood  => "Wood",
        ResourceType.Stone => "Stone",
        ResourceType.Coal  => "Coal",
        ResourceType.Iron  => "Iron",
        ResourceType.Gold  => "Gold",
        _                  => type.ToString()
    };

    // Short label used on building tiles in the empire grid
    public static string GetShortLabel(ResourceType type) => type switch
    {
        ResourceType.Wood  => "W",
        ResourceType.Stone => "S",
        ResourceType.Coal  => "C",
        ResourceType.Iron  => "I",
        ResourceType.Gold  => "G",
        _                  => "?"
    };

    public static Color GetColor(ResourceType type) => type switch
    {
        ResourceType.Wood  => new Color(0.55f, 0.30f, 0.08f),
        ResourceType.Stone => new Color(0.60f, 0.62f, 0.65f),
        ResourceType.Coal  => new Color(0.22f, 0.22f, 0.25f),
        ResourceType.Iron  => new Color(0.45f, 0.58f, 0.72f),
        ResourceType.Gold  => new Color(1.00f, 0.80f, 0.10f),
        _                  => Color.white
    };
}
