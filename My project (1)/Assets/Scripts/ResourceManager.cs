using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that owns the player's resource stockpiles.
/// Other systems read from / write to this manager rather than tracking
/// resources themselves, so the UI only needs one place to listen.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    // Fired whenever any resource amount changes: (type, newAmount, maxAmount)
    public event Action<ResourceType, float, float> OnResourceChanged;

    private readonly Dictionary<ResourceType, float> amounts    = new();
    private readonly Dictionary<ResourceType, float> maxAmounts = new();

    // Base storage cap before Warehouse upgrades
    private const float BaseMax = 200f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        foreach (ResourceType rt in Enum.GetValues(typeof(ResourceType)))
        {
            amounts[rt]    = 0f;
            maxAmounts[rt] = BaseMax;
        }

        // Player starts with a little Wood so they can buy their first building
        amounts[ResourceType.Wood] = 30f;
    }

    // ------------------------------------------------------------------ //
    //  Queries
    // ------------------------------------------------------------------ //

    public float Get(ResourceType type)    => amounts[type];
    public float GetMax(ResourceType type) => maxAmounts[type];

    /// <summary>Returns true if every resource in <paramref name="cost"/> is affordable.</summary>
    public bool CanAfford(Dictionary<ResourceType, float> cost)
    {
        foreach (var (type, amount) in cost)
            if (amounts[type] < amount) return false;
        return true;
    }

    // ------------------------------------------------------------------ //
    //  Mutations
    // ------------------------------------------------------------------ //

    /// <summary>Deducts the exact cost. Call <see cref="CanAfford"/> first.</summary>
    public void Spend(Dictionary<ResourceType, float> cost)
    {
        foreach (var (type, amount) in cost)
            SetAmount(type, amounts[type] - amount);
    }

    /// <summary>Adds <paramref name="amount"/> units, clamped to max storage.</summary>
    public void Add(ResourceType type, float amount)
    {
        SetAmount(type, Mathf.Min(amounts[type] + amount, maxAmounts[type]));
    }

    /// <summary>Increases the storage cap for all resources (e.g. Warehouse).</summary>
    public void IncreaseAllMaxBy(float delta)
    {
        foreach (ResourceType rt in Enum.GetValues(typeof(ResourceType)))
            maxAmounts[rt] += delta;
        // Fire events so the UI refreshes the max display
        foreach (ResourceType rt in Enum.GetValues(typeof(ResourceType)))
            OnResourceChanged?.Invoke(rt, amounts[rt], maxAmounts[rt]);
    }

    // ------------------------------------------------------------------ //
    //  Private helpers
    // ------------------------------------------------------------------ //

    private void SetAmount(ResourceType type, float value)
    {
        amounts[type] = Mathf.Max(0f, value);
        OnResourceChanged?.Invoke(type, amounts[type], maxAmounts[type]);
    }
}
