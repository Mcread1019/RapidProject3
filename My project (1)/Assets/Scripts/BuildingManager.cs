using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks every building the player has purchased and runs production ticks.
/// </summary>
public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    /// <summary>How often (in seconds) each building produces its output.</summary>
    public const float TickInterval = 3f;

    // Fired when a building is successfully purchased: (definition, totalCountOfThatType)
    public event Action<BuildingDefinition> OnBuildingPurchased;
    // Fired every tick with the list of all placed buildings (for UI refresh)
    public event Action<IReadOnlyList<PlacedBuilding>> OnTick;

    // Ordered list of all purchased buildings (newest last = grid fills left-to-right)
    private readonly List<PlacedBuilding> placedBuildings = new();
    private float tickTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        tickTimer += Time.deltaTime;
        if (tickTimer >= TickInterval)
        {
            tickTimer -= TickInterval;
            RunTick();
        }
    }


    public IReadOnlyList<PlacedBuilding> PlacedBuildings => placedBuildings;

    public int TotalBuildingCount => placedBuildings.Count;

    public int CountOf(string buildingId)
    {
        int count = 0;
        foreach (var b in placedBuildings)
            if (b.Definition.Id == buildingId) count++;
        return count;
    }

    /// <summary>
    /// Attempts to purchase one unit of <paramref name="def"/>.
    /// Deducts cost from ResourceManager, applies storage bonus, and registers the building.
    /// Returns true on success.
    /// </summary>
    public bool TryPurchase(BuildingDefinition def)
    {
        var rm = ResourceManager.Instance;
        if (!rm.CanAfford(def.Cost)) return false;

        rm.Spend(def.Cost);

        if (def.StorageBonus > 0f)
            rm.IncreaseAllMaxBy(def.StorageBonus);

        var placed = new PlacedBuilding(def, placedBuildings.Count);
        placedBuildings.Add(placed);

        OnBuildingPurchased?.Invoke(def);
        return true;
    }

    // ------------------------------------------------------------------ //
    //  Empire progress helpers
    // ------------------------------------------------------------------ //

    /// <summary>Numeric empire level based on total buildings purchased.</summary>
    public int EmpireLevel()
    {
        int n = placedBuildings.Count;
        // Thresholds: 1@1, 2@5, 3@12, 4@25, 5@45, 6@70, 7@100, 8@140, 9@190, 10@250
        int[] thresholds = { 1, 5, 12, 25, 45, 70, 100, 140, 190, 250 };
        int level = 0;
        foreach (int t in thresholds)
            if (n >= t) level++;
        return level;
    }

    /// <summary>
    /// Progress towards the next empire level [0..1].
    /// Returns 1.0 if already at max level.
    /// </summary>
    public float EmpireLevelProgress()
    {
        int[] thresholds = { 1, 5, 12, 25, 45, 70, 100, 140, 190, 250 };
        int n     = placedBuildings.Count;
        int level = EmpireLevel();

        if (level == 0)
            return n / (float)thresholds[0];
        if (level >= thresholds.Length)
            return 1f;

        int prev = thresholds[level - 1];
        int next = thresholds[level];
        return Mathf.Clamp01((float)(n - prev) / (next - prev));
    }

    public static string LevelName(int level) => level switch
    {
        0  => "Settlement",
        1  => "Village",
        2  => "Town",
        3  => "City",
        4  => "Metropolis",
        5  => "Kingdom",
        6  => "Empire",
        7  => "Dominion",
        8  => "Dynasty",
        9  => "Legend",
        10 => "Eternal Empire",
        _  => "???"
    };

    // ------------------------------------------------------------------ //
    //  Production tick
    // ------------------------------------------------------------------ //

    private void RunTick()
    {
        var rm = ResourceManager.Instance;

        foreach (var building in placedBuildings)
        {
            var def = building.Definition;

            // Check if this building can consume its required inputs
            bool canConsume = rm.CanAfford(def.ConsumptionPerTick);
            if (!canConsume) continue;   // building is starved – skip production

            // Deduct consumption
            if (def.ConsumptionPerTick.Count > 0)
                rm.Spend(def.ConsumptionPerTick);

            // Add production
            foreach (var (type, amount) in def.ProductionPerTick)
                rm.Add(type, amount);
        }

        OnTick?.Invoke(placedBuildings);
    }
}

/// <summary>
/// Represents a single purchased building instance on the player's land.
/// </summary>
public class PlacedBuilding
{
    public BuildingDefinition Definition { get; }
    /// <summary>Insertion index – used to place the building in the empire grid.</summary>
    public int GridIndex { get; }

    public PlacedBuilding(BuildingDefinition definition, int gridIndex)
    {
        Definition = definition;
        GridIndex  = gridIndex;
    }
}
