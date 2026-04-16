using System.Collections.Generic;
using UnityEngine;


public class BuildingDefinition
{
    public string  Id          { get; }
    public string  Name        { get; }
    public string  Description { get; }

    // What the player pays once to buy this building
    public Dictionary<ResourceType, float> Cost { get; }

    // Resources produced per production tick (every BuildingManager.TickInterval seconds)
    public Dictionary<ResourceType, float> ProductionPerTick { get; }

    public Dictionary<ResourceType, float> ConsumptionPerTick { get; }

    // Flat increase to max storage for all resources (for Warehouse-type buildings)
    public float StorageBonus { get; }

    // Tile colour shown in the empire grid
    public Color TileColor { get; }

    // Short 2-3 char label shown on the tile
    public string TileLabel { get; }

    public BuildingDefinition(
        string  id,
        string  name,
        string  description,
        Dictionary<ResourceType, float> cost,
        Dictionary<ResourceType, float> productionPerTick,
        Dictionary<ResourceType, float> consumptionPerTick = null,
        float   storageBonus = 0f,
        Color?  tileColor    = null,
        string  tileLabel    = null)
    {
        Id                 = id;
        Name               = name;
        Description        = description;
        Cost               = cost;
        ProductionPerTick  = productionPerTick;
        ConsumptionPerTick = consumptionPerTick ?? new Dictionary<ResourceType, float>();
        StorageBonus       = storageBonus;
        TileColor          = tileColor ?? Color.gray;
        TileLabel          = tileLabel ?? name.Substring(0, Mathf.Min(2, name.Length));
    }

    public string CostText()
    {
        if (Cost.Count == 0) return "Free";
        var parts = new List<string>();
        foreach (var (type, amount) in Cost)
            parts.Add($"{(int)amount} {ResourceInfo.GetName(type)}");
        return string.Join("  ", parts);
    }


    public string ProductionText()
    {
        var parts = new List<string>();
        foreach (var (type, amount) in ProductionPerTick)
            parts.Add($"+{amount:F1} {ResourceInfo.GetName(type)}/tick");
        foreach (var (type, amount) in ConsumptionPerTick)
            parts.Add($"-{amount:F1} {ResourceInfo.GetName(type)}/tick");
        if (StorageBonus > 0)
            parts.Add($"+{StorageBonus} storage");
        return parts.Count > 0 ? string.Join("  ", parts) : "No output";
    }
}

/// <summary>
/// All available building types in the game.
/// Add new entries here to extend the building shop without touching other code.
/// </summary>
public static class BuildingDatabase
{
    private static List<BuildingDefinition> _all;

    public static IReadOnlyList<BuildingDefinition> All
    {
        get
        {
            if (_all == null) Init();
            return _all;
        }
    }

    public static BuildingDefinition Get(string id)
    {
        foreach (var b in All)
            if (b.Id == id) return b;
        return null;
    }

    private static void Init()
    {
        _all = new List<BuildingDefinition>
        {
            // ── Tier 1 ── Wood production ─────────────────────────────────
            new BuildingDefinition(
                id:          "lumber_camp",
                name:        "Lumber Camp",
                description: "Chop trees to gather Wood.",
                cost:        new Dictionary<ResourceType, float>(),           // free starter
                productionPerTick: new Dictionary<ResourceType, float>
                    { [ResourceType.Wood] = 2f },
                tileColor:   new Color(0.55f, 0.30f, 0.08f),
                tileLabel:   "LC"),

            new BuildingDefinition(
                id:          "sawmill",
                name:        "Sawmill",
                description: "Processes logs more efficiently.",
                cost:        new Dictionary<ResourceType, float>
                    { [ResourceType.Wood] = 30f },
                productionPerTick: new Dictionary<ResourceType, float>
                    { [ResourceType.Wood] = 5f },
                tileColor:   new Color(0.70f, 0.42f, 0.12f),
                tileLabel:   "SW"),

            // ── Tier 2 ── Stone production ────────────────────────────────
            new BuildingDefinition(
                id:          "quarry",
                name:        "Stone Quarry",
                description: "Mine Stone from the hillside.",
                cost:        new Dictionary<ResourceType, float>
                    { [ResourceType.Wood] = 25f },
                productionPerTick: new Dictionary<ResourceType, float>
                    { [ResourceType.Stone] = 2f },
                tileColor:   new Color(0.55f, 0.58f, 0.60f),
                tileLabel:   "SQ"),

            new BuildingDefinition(
                id:          "mason",
                name:        "Mason's Guild",
                description: "Skilled masons cut Stone faster.",
                cost:        new Dictionary<ResourceType, float>
                    { [ResourceType.Wood] = 20f, [ResourceType.Stone] = 20f },
                productionPerTick: new Dictionary<ResourceType, float>
                    { [ResourceType.Stone] = 5f },
                tileColor:   new Color(0.70f, 0.72f, 0.75f),
                tileLabel:   "MG"),

            // ── Tier 3 ── Coal production ─────────────────────────────────
            new BuildingDefinition(
                id:          "coal_mine",
                name:        "Coal Mine",
                description: "Dig deep for Coal seams.",
                cost:        new Dictionary<ResourceType, float>
                    { [ResourceType.Wood] = 30f, [ResourceType.Stone] = 15f },
                productionPerTick: new Dictionary<ResourceType, float>
                    { [ResourceType.Coal] = 2f },
                tileColor:   new Color(0.22f, 0.22f, 0.28f),
                tileLabel:   "CM"),

            // ── Tier 4 ── Iron production ─────────────────────────────────
            new BuildingDefinition(
                id:          "iron_mine",
                name:        "Iron Mine",
                description: "Extract Iron ore from the earth.",
                cost:        new Dictionary<ResourceType, float>
                    { [ResourceType.Stone] = 30f, [ResourceType.Coal] = 10f },
                productionPerTick: new Dictionary<ResourceType, float>
                    { [ResourceType.Iron] = 1f },
                tileColor:   new Color(0.40f, 0.52f, 0.65f),
                tileLabel:   "IM"),

            new BuildingDefinition(
                id:          "iron_forge",
                name:        "Iron Forge",
                description: "Smelts ore into refined Iron bars. Consumes Coal.",
                cost:        new Dictionary<ResourceType, float>
                    { [ResourceType.Stone] = 40f, [ResourceType.Coal] = 20f },
                productionPerTick: new Dictionary<ResourceType, float>
                    { [ResourceType.Iron] = 3f },
                consumptionPerTick: new Dictionary<ResourceType, float>
                    { [ResourceType.Coal] = 1f },
                tileColor:   new Color(0.28f, 0.42f, 0.58f),
                tileLabel:   "IF"),

            // ── Tier 5 ── Gold production ─────────────────────────────────
            new BuildingDefinition(
                id:          "gold_mine",
                name:        "Gold Mine",
                description: "Rare veins of Gold deep underground.",
                cost:        new Dictionary<ResourceType, float>
                    { [ResourceType.Stone] = 50f, [ResourceType.Iron] = 20f },
                productionPerTick: new Dictionary<ResourceType, float>
                    { [ResourceType.Gold] = 1f },
                tileColor:   new Color(0.85f, 0.70f, 0.05f),
                tileLabel:   "GM"),

            new BuildingDefinition(
                id:          "gold_smelter",
                name:        "Gold Smelter",
                description: "Refines raw Gold into pure ingots. Consumes Coal.",
                cost:        new Dictionary<ResourceType, float>
                    { [ResourceType.Iron] = 40f, [ResourceType.Coal] = 30f },
                productionPerTick: new Dictionary<ResourceType, float>
                    { [ResourceType.Gold] = 3f },
                consumptionPerTick: new Dictionary<ResourceType, float>
                    { [ResourceType.Coal] = 2f },
                tileColor:   new Color(1.00f, 0.80f, 0.10f),
                tileLabel:   "GS"),

            // ── Utility ── Storage ────────────────────────────────────────
            new BuildingDefinition(
                id:          "warehouse",
                name:        "Warehouse",
                description: "Increases maximum storage for all resources by 200.",
                cost:        new Dictionary<ResourceType, float>
                    { [ResourceType.Wood] = 50f, [ResourceType.Stone] = 30f },
                productionPerTick: new Dictionary<ResourceType, float>(),
                storageBonus: 200f,
                tileColor:   new Color(0.35f, 0.60f, 0.35f),
                tileLabel:   "WH"),

            new BuildingDefinition(
                id:          "grand_vault",
                name:        "Grand Vault",
                description: "Massive stone vault. +500 storage for all resources.",
                cost:        new Dictionary<ResourceType, float>
                    { [ResourceType.Stone] = 100f, [ResourceType.Iron] = 50f },
                productionPerTick: new Dictionary<ResourceType, float>(),
                storageBonus: 500f,
                tileColor:   new Color(0.20f, 0.45f, 0.20f),
                tileLabel:   "GV"),

            // ── Luxury ── Multi-resource production ───────────────────────
            new BuildingDefinition(
                id:          "trading_post",
                name:        "Trading Post",
                description: "Converts local goods into Gold through trade.",
                cost:        new Dictionary<ResourceType, float>
                    { [ResourceType.Wood] = 40f, [ResourceType.Stone] = 30f, [ResourceType.Iron] = 15f },
                productionPerTick: new Dictionary<ResourceType, float>
                    { [ResourceType.Gold] = 2f },
                consumptionPerTick: new Dictionary<ResourceType, float>
                    { [ResourceType.Wood] = 1f },
                tileColor:   new Color(0.90f, 0.55f, 0.10f),
                tileLabel:   "TP"),

            new BuildingDefinition(
                id:          "empire_hall",
                name:        "Empire Hall",
                description: "Symbol of your dominion. Produces all resources slowly.",
                cost:        new Dictionary<ResourceType, float>
                    { [ResourceType.Wood] = 100f, [ResourceType.Stone] = 100f,
                      [ResourceType.Iron] = 50f,  [ResourceType.Gold] = 30f },
                productionPerTick: new Dictionary<ResourceType, float>
                    { [ResourceType.Wood]  = 2f, [ResourceType.Stone] = 2f,
                      [ResourceType.Coal]  = 2f, [ResourceType.Iron]  = 2f,
                      [ResourceType.Gold]  = 1f },
                tileColor:   new Color(0.60f, 0.20f, 0.70f),
                tileLabel:   "EH"),
        };
    }
}
