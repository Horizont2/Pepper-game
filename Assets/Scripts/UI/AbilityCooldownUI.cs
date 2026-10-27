using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One HUD ability slot. Darkens with a radial sweep while the ability is recharging
/// and brightens the moment it is ready, so the player reads availability without
/// counting seconds.
/// </summary>
public class AbilityCooldownUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PepperController controller;
    [Tooltip("Image set to Filled / Radial 360. Covers the slot while recharging.")]
    [SerializeField] private Image cooldownOverlay;
    [Tooltip("Optional. Tinted while the ability is unavailable.")]
    [SerializeField] private Image icon;

    [Header("Appearance")]
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color rechargingColor = new Color(1f, 1f, 1f, 0.45f);

    private bool wasReady = true;

    private void Awake()
    {
        if (controller == null)
            Debug.LogWarning($"[AbilityCooldownUI] controller is not assigned on '{name}'; the slot will always read as ready.", this);
    }

    private void Update()
    {
        float progress = controller != null ? controller.DashCooldownProgress : 1f;

        if (cooldownOverlay != null)
        {
            // fillAmount 1 covers the slot completely, 0 leaves it clear
            cooldownOverlay.fillAmount = 1f - progress;
        }

        bool isReady = progress >= 1f;

        if (icon != null && isReady != wasReady)
        {
            icon.color = isReady ? readyColor : rechargingColor;
        }

        wasReady = isReady;
    }
}
