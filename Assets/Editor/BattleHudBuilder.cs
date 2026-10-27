using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds the complete in-game battle HUD into the open scene, wired to whatever
/// player is already there, and saves it as a reusable prefab.
///
/// This is an editor tool, not runtime code: it produces real GameObjects with real
/// serialised references that you then tune in the Inspector. Nothing here runs in a
/// build. It exists because the HUD is a large hierarchy that would be miserable to
/// assemble by hand, and because re-running it is how the HUD gets rebuilt once the
/// real art from Figma replaces these placeholder sprites.
/// </summary>
public static class BattleHudBuilder
{
    private const string HudRootName = "UI";
    private const string PrefabPath = "Assets/Prefabs/UI/BattleHUD.prefab";

    // Palette from Docs/UI-Art-Direction-Prompt.md. These are placeholders standing in
    // for the painted art - the shapes and anchors are what matter here.
    private static readonly Color Outline     = new Color32(0x24, 0x1C, 0x1A, 0xFF);
    private static readonly Color Parchment   = new Color32(0xF2, 0xE3, 0xC8, 0xFF);
    private static readonly Color Paprika     = new Color32(0xD6, 0x45, 0x41, 0xFF);
    private static readonly Color PaprikaDark = new Color32(0xA3, 0x2F, 0x2E, 0xFF);
    private static readonly Color Brass       = new Color32(0xE8, 0xB0, 0x4B, 0xFF);
    private static readonly Color Steel       = new Color32(0x8C, 0x9B, 0xA5, 0xFF);
    private static readonly Color Danger      = new Color32(0xC0, 0x39, 0x2B, 0xFF);

    [MenuItem("Paprica/Build Battle HUD", false, 10)]
    public static void Build()
    {
        GameObject existing = GameObject.Find(HudRootName);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Battle HUD",
                $"A GameObject called '{HudRootName}' already exists in this scene.\n\n" +
                "Replace it with a freshly built HUD? Any tweaks made to it will be lost.",
                "Replace", "Cancel");

            if (!replace) return;
            Undo.DestroyObjectImmediate(existing);
        }

        PepperController player = Object.FindFirstObjectByType<PepperController>(FindObjectsInactive.Include);
        HealthComponent playerHealth = player != null ? player.GetComponent<HealthComponent>() : null;

        if (player == null)
            Debug.LogWarning("[BattleHudBuilder] No PepperController in this scene - the HUD is built but left unbound.");

        GameObject canvasGo = BuildCanvas();
        GameObject hud = NewUIObject("HUD", canvasGo.transform, stretch: true);

        BuildPlayerStatus(hud.transform, playerHealth, player);
        BuildBossBar(hud.transform);

        EnsureEventSystem();
        SaveAsPrefab(canvasGo);

        Undo.RegisterCreatedObjectUndo(canvasGo, "Build Battle HUD");
        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Selection.activeGameObject = canvasGo;

        Debug.Log("[BattleHudBuilder] Battle HUD built" +
                  (playerHealth != null ? " and bound to the player." : " (no player found to bind to)."), canvasGo);
    }

    // --- structure ---------------------------------------------------------

    private static GameObject BuildCanvas()
    {
        var go = new GameObject(HudRootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return go;
    }

    private static void BuildPlayerStatus(Transform parent, HealthComponent playerHealth, PepperController player)
    {
        GameObject status = NewUIObject("PlayerStatus", parent);
        RectTransform rt = Rect(status);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);   // top-left
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(40f, -40f);
        rt.sizeDelta = new Vector2(560f, 160f);

        // Portrait
        GameObject portrait = NewImage("PortraitFrame", status.transform, Brass);
        Place(portrait, new Vector2(0f, 0f), new Vector2(96f, 96f), new Vector2(0f, 1f));

        GameObject portraitInner = NewImage("PortraitInner", portrait.transform, Parchment);
        RectTransform pir = Rect(portraitInner);
        pir.anchorMin = Vector2.zero;
        pir.anchorMax = Vector2.one;
        pir.offsetMin = new Vector2(6f, 6f);
        pir.offsetMax = new Vector2(-6f, -6f);

        // Health bar: outline -> track -> delayed fill -> fill
        GameObject bar = NewImage("HealthBar", status.transform, Outline);
        Place(bar, new Vector2(112f, -14f), new Vector2(420f, 40f), new Vector2(0f, 1f));

        GameObject track = NewImage("Track", bar.transform, Parchment);
        Inset(track, 5f);

        GameObject delayed = NewFilled("DelayedFill", track.transform, PaprikaDark);
        Inset(delayed, 0f);

        GameObject fill = NewFilled("Fill", track.transform, Paprika);
        Inset(fill, 0f);

        var healthBar = status.AddComponent<HealthBarUI>();
        BindHealthBar(healthBar, playerHealth, fill.GetComponent<Image>(), delayed.GetComponent<Image>(), null, false);

        // Ability slots
        GameObject slots = NewUIObject("AbilitySlots", status.transform);
        Place(slots, new Vector2(112f, -64f), new Vector2(300f, 64f), new Vector2(0f, 1f));

        for (int i = 0; i < 3; i++)
        {
            bool isDash = i == 0;

            GameObject slot = NewImage(isDash ? "DashSlot" : $"AbilitySlot{i + 1}", slots.transform, Outline);
            Place(slot, new Vector2(i * 76f, 0f), new Vector2(64f, 64f), new Vector2(0f, 1f));

            GameObject icon = NewImage("Icon", slot.transform, isDash ? Parchment : Steel);
            Inset(icon, 5f);

            GameObject cooldown = NewFilled("Cooldown", slot.transform, new Color(0f, 0f, 0f, 0.6f),
                                            Image.FillMethod.Radial360);
            Inset(cooldown, 5f);
            cooldown.GetComponent<Image>().fillAmount = 0f;

            if (!isDash) continue;

            var cd = slot.AddComponent<AbilityCooldownUI>();
            var so = new SerializedObject(cd);
            so.FindProperty("controller").objectReferenceValue = player;
            so.FindProperty("cooldownOverlay").objectReferenceValue = cooldown.GetComponent<Image>();
            so.FindProperty("icon").objectReferenceValue = icon.GetComponent<Image>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void BuildBossBar(Transform parent)
    {
        GameObject boss = NewUIObject("BossBar", parent);
        RectTransform rt = Rect(boss);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);   // bottom-centre
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 72f);
        rt.sizeDelta = new Vector2(900f, 34f);

        GameObject frame = NewImage("Frame", boss.transform, Outline);
        Inset(frame, 0f);

        GameObject track = NewImage("Track", frame.transform, Outline * 0.6f);
        Inset(track, 4f);

        GameObject delayed = NewFilled("DelayedFill", track.transform, Brass);
        Inset(delayed, 0f);

        GameObject fill = NewFilled("Fill", track.transform, Danger);
        Inset(fill, 0f);

        var bar = boss.AddComponent<HealthBarUI>();
        // No target yet - the fight hands it one through HealthBarUI.SetTarget.
        BindHealthBar(bar, null, fill.GetComponent<Image>(), delayed.GetComponent<Image>(), boss, true);

        boss.SetActive(false);
    }

    private static void BindHealthBar(HealthBarUI bar, HealthComponent target, Image fill, Image delayed,
                                      GameObject root, bool hideOnDeath)
    {
        var so = new SerializedObject(bar);
        so.FindProperty("target").objectReferenceValue = target;
        so.FindProperty("fill").objectReferenceValue = fill;
        so.FindProperty("delayedFill").objectReferenceValue = delayed;
        so.FindProperty("root").objectReferenceValue = root;
        so.FindProperty("hideOnDeath").boolValue = hideOnDeath;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // --- helpers -----------------------------------------------------------

    private static RectTransform Rect(GameObject go) => (RectTransform)go.transform;

    private static GameObject NewUIObject(string name, Transform parent, bool stretch = false)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        if (!stretch) return go;

        RectTransform rt = Rect(go);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return go;
    }

    private static GameObject NewImage(string name, Transform parent, Color color)
    {
        var go = NewUIObject(name, parent);
        var image = go.AddComponent<Image>();
        image.sprite = BuiltinUISprite();
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;   // the HUD is not interactive
        return go;
    }

    private static GameObject NewFilled(string name, Transform parent, Color color,
                                        Image.FillMethod method = Image.FillMethod.Horizontal)
    {
        var go = NewImage(name, parent, color);
        var image = go.GetComponent<Image>();
        image.type = Image.Type.Filled;
        image.fillMethod = method;
        image.fillOrigin = method == Image.FillMethod.Radial360
            ? (int)Image.Origin360.Top
            : (int)Image.OriginHorizontal.Left;
        image.fillAmount = 1f;
        return go;
    }

    private static Sprite BuiltinUISprite() =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    /// <summary>Anchor a child to a corner of its parent at a fixed size.</summary>
    private static void Place(GameObject go, Vector2 position, Vector2 size, Vector2 anchor)
    {
        RectTransform rt = Rect(go);
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    /// <summary>Stretch a child to fill its parent, inset by the given margin.</summary>
    private static void Inset(GameObject go, float margin)
    {
        RectTransform rt = Rect(go);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
    }

    private static void SaveAsPrefab(GameObject canvasGo)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(canvasGo, PrefabPath, InteractionMode.AutomatedAction);
        Debug.Log($"[BattleHudBuilder] HUD prefab saved to {PrefabPath}");
    }
}
