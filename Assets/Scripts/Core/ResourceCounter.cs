using System;
using UnityEngine;

/// <summary>
/// What the Pepper has collected. Lives on the player and does nothing but hold a number
/// and announce when it changes - the HUD, shops and gates all read it without any of them
/// needing to know about each other.
/// </summary>
public class ResourceCounter : MonoBehaviour
{
    [SerializeField] private int startingAmount;
    [SerializeField] private int maxAmount = 9999;

    public int Amount { get; private set; }

    /// <summary>Carries the new total. For the HUD counter and pickup audio.</summary>
    public event Action<int> OnAmountChanged;

    private void Awake()
    {
        Amount = Mathf.Clamp(startingAmount, 0, maxAmount);
    }

    public void Add(int value)
    {
        if (value <= 0) return;

        int before = Amount;
        Amount = Mathf.Clamp(Amount + value, 0, maxAmount);

        if (Amount != before) OnAmountChanged?.Invoke(Amount);
    }

    /// <summary>Spends only if there is enough, and says whether it went through.</summary>
    public bool TrySpend(int value)
    {
        if (value <= 0 || Amount < value) return false;

        Amount -= value;
        OnAmountChanged?.Invoke(Amount);
        return true;
    }

    /// <summary>Back to the starting amount. For a full restart rather than a respawn.</summary>
    public void ResetToStart()
    {
        Amount = Mathf.Clamp(startingAmount, 0, maxAmount);
        OnAmountChanged?.Invoke(Amount);
    }
}
