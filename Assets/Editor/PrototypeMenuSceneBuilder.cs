using DiceMadness.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class PrototypeMenuSceneBuilder
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    private const string FallbackFontFolderPath = "Assets/UI/Fonts";
    private const string FallbackFontAssetPath = "Assets/UI/Fonts/LegacyRuntime SDF.asset";
    private const int MenuPadding = 64;
    private const int MenuSpacing = 28;
    private const int PanelSpacing = 22;
    private const int InnerCardPadding = 28;
    private const float MainButtonHeight = 92f;
    private const float UtilityButtonHeight = 68f;
    private const float TabButtonHeight = 84f;
    private const int TitleFontSize = 86;
    private const int SubtitleFontSize = 26;
    private const int SectionTitleFontSize = 52;
    private const int BodyFontSize = 30;
    private const int ButtonFontSize = 32;
    private const int TabButtonFontSize = 24;
    private const int UtilityButtonFontSize = 26;

    private static readonly Vector2 MainMenuCardAnchorMin = new Vector2(0.26f, 0.11f);
    private static readonly Vector2 MainMenuCardAnchorMax = new Vector2(0.74f, 0.89f);
    private static readonly Vector2 ScreenCardAnchorMin = new Vector2(0.11f, 0.08f);
    private static readonly Vector2 ScreenCardAnchorMax = new Vector2(0.89f, 0.92f);
    private static readonly Vector2 UtilityBarAnchorMin = new Vector2(0.28f, 1f);
    private static readonly Vector2 UtilityBarAnchorMax = new Vector2(0.72f, 1f);

    private static readonly Color ScreenOverlayColor = new Color(0.03f, 0.05f, 0.08f, 0.82f);
    private static readonly Color CardColor = new Color(0.10f, 0.13f, 0.17f, 0.97f);
    private static readonly Color ContentCardColor = new Color(0.075f, 0.10f, 0.14f, 0.98f);
    private static readonly Color PrimaryButtonColor = new Color(0.22f, 0.53f, 0.85f, 1f);
    private static readonly Color SecondaryButtonColor = new Color(0.18f, 0.22f, 0.28f, 1f);
    private static readonly Color DividerColor = new Color(0.29f, 0.38f, 0.49f, 0.55f);
    private static readonly Color TitleColor = new Color(0.99f, 0.995f, 1f, 1f);
    private static readonly Color SubtitleColor = new Color(0.76f, 0.82f, 0.89f, 1f);
    private static readonly Color BodyTextColor = new Color(0.91f, 0.93f, 0.96f, 1f);
    private static readonly Color AccentTextColor = new Color(0.71f, 0.82f, 0.96f, 1f);
    private static readonly Color CardOutlineColor = new Color(0.44f, 0.58f, 0.75f, 0.18f);
    private static readonly Color ContentOutlineColor = new Color(0.36f, 0.46f, 0.58f, 0.18f);
    private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.24f);

    private sealed class UiRefs
    {
        public Canvas canvas;
        public EventSystem eventSystem;
        public TMP_Text rollText;
        public GameObject mainMenuPanel;
        public GameObject shopPanel;
        public GameObject challengesPanel;
        public GameObject settingsPanel;
        public GameObject roundUtilityBar;
        public Button playButton;
        public Button mainMenuShopButton;
        public Button mainMenuChallengesButton;
        public Button mainMenuSettingsButton;
        public Button shopDiceUnlocksButton;
        public Button shopEfficiencyButton;
        public Button shopScoreMultipliersButton;
        public Button shopReturnButton;
        public TMP_Text shopContextText;
        public TMP_Text shopSectionTitleText;
        public TMP_Text shopContentText;
        public Button challengesBackButton;
        public TMP_Text challengesContentText;
        public Button settingsBackButton;
        public TMP_Text settingsContentText;
        public Button roundChallengesButton;
        public Button roundSettingsButton;
        public Button roundMainMenuButton;
    }

    private enum TextRole
    {
        MainTitle,
        Subtitle,
        SectionTitle,
        CardTitle,
        Body,
        ButtonLabel,
        UtilityLabel,
        RollResult,
        SupportingHint,
    }

    private enum SurfaceRole
    {
        ScreenOverlay,
        Card,
        InsetCard,
        UtilityBar,
    }

    private enum ButtonRole
    {
        Primary,
        Secondary,
    }

    private readonly struct TextStyleDefinition
    {
        public readonly int fontSize;
        public readonly FontStyles fontStyle;
        public readonly TextAlignmentOptions alignment;
        public readonly Color color;
        public readonly TextOverflowModes overflowMode;
        public readonly TextWrappingModes wrappingMode;
        public readonly bool extraPadding;
        public readonly float lineSpacing;
        public readonly bool useShadow;
        public readonly Color shadowColor;
        public readonly Vector2 shadowOffset;

        public TextStyleDefinition(
            int fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Color color,
            TextOverflowModes overflowMode,
            TextWrappingModes wrappingMode,
            bool extraPadding,
            float lineSpacing,
            bool useShadow = false,
            Color shadowColor = default,
            Vector2 shadowOffset = default)
        {
            this.fontSize = fontSize;
            this.fontStyle = fontStyle;
            this.alignment = alignment;
            this.color = color;
            this.overflowMode = overflowMode;
            this.wrappingMode = wrappingMode;
            this.extraPadding = extraPadding;
            this.lineSpacing = lineSpacing;
            this.useShadow = useShadow;
            this.shadowColor = shadowColor;
            this.shadowOffset = shadowOffset;
        }
    }

    private readonly struct SurfaceStyleDefinition
    {
        public readonly Color backgroundColor;
        public readonly bool useOutline;
        public readonly Color outlineColor;
        public readonly Vector2 outlineOffset;
        public readonly bool useShadow;
        public readonly Color shadowColor;
        public readonly Vector2 shadowOffset;

        public SurfaceStyleDefinition(
            Color backgroundColor,
            bool useOutline,
            Color outlineColor,
            Vector2 outlineOffset,
            bool useShadow,
            Color shadowColor,
            Vector2 shadowOffset)
        {
            this.backgroundColor = backgroundColor;
            this.useOutline = useOutline;
            this.outlineColor = outlineColor;
            this.outlineOffset = outlineOffset;
            this.useShadow = useShadow;
            this.shadowColor = shadowColor;
            this.shadowOffset = shadowOffset;
        }
    }

    private readonly struct ButtonStyleDefinition
    {
        public readonly Color backgroundColor;
        public readonly Color textColor;
        public readonly Color highlightedColor;
        public readonly Color pressedColor;
        public readonly Color selectedColor;
        public readonly bool useOutline;
        public readonly Color outlineColor;
        public readonly Vector2 outlineOffset;
        public readonly bool useShadow;
        public readonly Color shadowColor;
        public readonly Vector2 shadowOffset;

        public ButtonStyleDefinition(
            Color backgroundColor,
            Color textColor,
            Color highlightedColor,
            Color pressedColor,
            Color selectedColor,
            bool useOutline,
            Color outlineColor,
            Vector2 outlineOffset,
            bool useShadow,
            Color shadowColor,
            Vector2 shadowOffset)
        {
            this.backgroundColor = backgroundColor;
            this.textColor = textColor;
            this.highlightedColor = highlightedColor;
            this.pressedColor = pressedColor;
            this.selectedColor = selectedColor;
            this.useOutline = useOutline;
            this.outlineColor = outlineColor;
            this.outlineOffset = outlineOffset;
            this.useShadow = useShadow;
            this.shadowColor = shadowColor;
            this.shadowOffset = shadowOffset;
        }
    }

    [MenuItem("Tools/Dice Prototype/Build Menu UI In Scene")]
    public static void BuildMenuUiInScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) != null &&
            EditorSceneManager.GetActiveScene().path != MainScenePath)
        {
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }

        BuildOrRefreshSceneUi(null, null);
    }

    [MenuItem("Tools/Dice Prototype/Refresh Menu TMP Text Styles")]
    public static void RefreshMenuTextStylesInScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) != null &&
            EditorSceneManager.GetActiveScene().path != MainScenePath)
        {
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }

        TMP_FontAsset fontAsset = EnsureMenuFontAsset();
        Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);

        if (canvas == null)
        {
            return;
        }

        Transform root = canvas.transform.Find("MenuUIRoot");
        if (root == null)
        {
            return;
        }

        ApplyStyleInContainer(root, "MainMenuPanel", "TitleText", fontAsset, TextRole.MainTitle);
        ApplyStyleInContainer(root, "ShopPanel", "TitleText", fontAsset, TextRole.SectionTitle);
        ApplyStyleInContainer(root, "ChallengesPanel", "TitleText", fontAsset, TextRole.SectionTitle);
        ApplyStyleInContainer(root, "SettingsPanel", "TitleText", fontAsset, TextRole.SectionTitle);
        ApplyStyleToNamedText(root, "SubtitleText", fontAsset, TextRole.Subtitle);
        ApplyStyleToNamedText(root, "ShopContextText", fontAsset, TextRole.Subtitle, AccentTextColor);
        ApplyStyleToNamedText(root, "ShopSectionTitleText", fontAsset, TextRole.CardTitle);
        ApplyStyleToNamedText(root, "ShopContentText", fontAsset, TextRole.Body);
        ApplyStyleToNamedText(root, "ChallengesContentText", fontAsset, TextRole.Body);
        ApplyStyleToNamedText(root, "SettingsContentText", fontAsset, TextRole.Body);
        ApplyStyleToNamedText(root, "FutureText", fontAsset, TextRole.SupportingHint);
        ApplyStyleToNamedText(root, "RollText", fontAsset, TextRole.RollResult);

        ApplyButtonLabelStyle(root, "PlayButton", fontAsset, TextRole.ButtonLabel);
        ApplyButtonLabelStyle(root, "ShopButton", fontAsset, TextRole.ButtonLabel);
        ApplyButtonLabelStyle(root, "ChallengesButton", fontAsset, TextRole.ButtonLabel);
        ApplyButtonLabelStyle(root, "SettingsButton", fontAsset, TextRole.ButtonLabel);
        ApplyButtonLabelStyle(root, "DiceUnlocksButton", fontAsset, TextRole.ButtonLabel, TabButtonFontSize);
        ApplyButtonLabelStyle(root, "EfficiencyButton", fontAsset, TextRole.ButtonLabel, TabButtonFontSize);
        ApplyButtonLabelStyle(root, "ScoreMultipliersButton", fontAsset, TextRole.ButtonLabel, TabButtonFontSize);
        ApplyButtonLabelStyle(root, "ShopBackButton", fontAsset, TextRole.ButtonLabel);
        ApplyButtonLabelStyle(root, "ChallengesBackButton", fontAsset, TextRole.ButtonLabel);
        ApplyButtonLabelStyle(root, "SettingsBackButton", fontAsset, TextRole.ButtonLabel);
        ApplyButtonLabelStyle(root, "RoundChallengesButton", fontAsset, TextRole.UtilityLabel);
        ApplyButtonLabelStyle(root, "RoundSettingsButton", fontAsset, TextRole.UtilityLabel);
        ApplyButtonLabelStyle(root, "RoundMainMenuButton", fontAsset, TextRole.UtilityLabel);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
    }

    [MenuItem("Tools/Dice Prototype/Refresh All Menu Styles")]
    public static void RefreshAllMenuStylesInScene()
    {
        RefreshMenuVisualStylesInScene();
        RefreshMenuTextStylesInScene();
    }

    [MenuItem("Tools/Dice Prototype/Refresh Menu Visual Styles")]
    public static void RefreshMenuVisualStylesInScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) != null &&
            EditorSceneManager.GetActiveScene().path != MainScenePath)
        {
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            return;
        }

        Transform root = canvas.transform.Find("MenuUIRoot");
        if (root == null)
        {
            return;
        }

        ApplySurfaceStyleInContainer(root, "MainMenuPanel", "MainMenuPanel", SurfaceRole.ScreenOverlay);
        ApplySurfaceStyleInContainer(root, "ShopPanel", "ShopPanel", SurfaceRole.ScreenOverlay);
        ApplySurfaceStyleInContainer(root, "ChallengesPanel", "ChallengesPanel", SurfaceRole.ScreenOverlay);
        ApplySurfaceStyleInContainer(root, "SettingsPanel", "SettingsPanel", SurfaceRole.ScreenOverlay);
        ApplySurfaceStyleInContainer(root, "MainMenuPanel", "MainMenuCard", SurfaceRole.Card);
        ApplySurfaceStyleInContainer(root, "ShopPanel", "ShopCard", SurfaceRole.Card);
        ApplySurfaceStyleInContainer(root, "ChallengesPanel", "ChallengesCard", SurfaceRole.Card);
        ApplySurfaceStyleInContainer(root, "SettingsPanel", "SettingsCard", SurfaceRole.Card);
        ApplySurfaceStyleInContainer(root, "MainMenuPanel", "ActionGroup", SurfaceRole.InsetCard);
        ApplySurfaceStyleInContainer(root, "ShopPanel", "ShopContentCard", SurfaceRole.InsetCard);
        ApplySurfaceStyleInContainer(root, "ChallengesPanel", "ChallengesContentCard", SurfaceRole.InsetCard);
        ApplySurfaceStyleInContainer(root, "SettingsPanel", "SettingsContentCard", SurfaceRole.InsetCard);
        ApplySurfaceStyleToNamedObject(root, "RoundUtilityBar", SurfaceRole.UtilityBar);

        ApplyButtonStyleByName(root, "PlayButton", ButtonRole.Primary);
        ApplyButtonStyleByName(root, "ShopButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "ChallengesButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "SettingsButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "DiceUnlocksButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "EfficiencyButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "ScoreMultipliersButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "ShopBackButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "ChallengesBackButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "SettingsBackButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "RoundChallengesButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "RoundSettingsButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "RoundMainMenuButton", ButtonRole.Secondary);

        ApplyDividerStyle(root);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
    }

    public static void BuildOrRefreshSceneUi(Canvas existingCanvas, DiceManager existingDiceManager)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before rebuilding the scene UI.");
            return;
        }

        Canvas canvas = existingCanvas != null ? existingCanvas : Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        DiceManager diceManager = existingDiceManager != null ? existingDiceManager : Object.FindAnyObjectByType<DiceManager>(FindObjectsInactive.Include);

        if (canvas == null)
        {
            canvas = CreateCanvas();
        }

        TMP_FontAsset fontAsset = EnsureMenuFontAsset();
        EventSystem eventSystem = EnsureEventSystem();
        ClearLegacyUi(canvas.transform);

        UiRefs uiRefs = new UiRefs
        {
            canvas = canvas,
            eventSystem = eventSystem,
        };

        BuildSceneUi(canvas.transform, uiRefs, fontAsset);
        PrototypeGameFlowController controller = EnsureController();
        AssignControllerReferences(controller, diceManager, uiRefs);
        AssignDiceManagerResultText(diceManager, uiRefs.rollText);

        EditorUtility.SetDirty(canvas.gameObject);
        if (diceManager != null)
        {
            EditorUtility.SetDirty(diceManager);
        }

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        if (!string.IsNullOrEmpty(canvas.gameObject.scene.path))
        {
            EditorSceneManager.SaveScene(canvas.gameObject.scene);
        }
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static TMP_FontAsset EnsureMenuFontAsset()
    {
        TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);

        if (settings == null)
        {
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();
            settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        }

        TMP_FontAsset defaultFontAsset = FindDefaultTmpFont(settings);
        if (defaultFontAsset != null)
        {
            return defaultFontAsset;
        }

        if (!AssetDatabase.IsValidFolder(FallbackFontFolderPath))
        {
            AssetDatabase.CreateFolder("Assets/UI", "Fonts");
        }

        TMP_FontAsset fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontAssetPath);
        if (fallbackFont != null)
        {
            return fallbackFont;
        }

        Font sourceFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fallbackFont = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.DynamicOS, true);
        AssetDatabase.CreateAsset(fallbackFont, FallbackFontAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return fallbackFont;
    }

    private static TMP_FontAsset FindDefaultTmpFont(TMP_Settings settings)
    {
        if (settings != null)
        {
            SerializedObject serializedSettings = new SerializedObject(settings);
            SerializedProperty defaultFontProperty = serializedSettings.FindProperty("m_defaultFontAsset");

            if (defaultFontProperty?.objectReferenceValue is TMP_FontAsset fontFromSettings)
            {
                return fontFromSettings;
            }
        }

        string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/TextMesh Pro" });
        for (int i = 0; i < fontGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(fontGuids[i]);
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);

            if (fontAsset != null)
            {
                return fontAsset;
            }
        }

        return null;
    }

    private static EventSystem EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);

        if (eventSystem != null)
        {
            return eventSystem;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystem = eventSystemObject.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif

        return eventSystem;
    }

    private static PrototypeGameFlowController EnsureController()
    {
        PrototypeGameFlowController controller = Object.FindAnyObjectByType<PrototypeGameFlowController>(FindObjectsInactive.Include);

        if (controller != null)
        {
            return controller;
        }

        GameObject controllerObject = new GameObject("GameFlowController");
        return controllerObject.AddComponent<PrototypeGameFlowController>();
    }

    private static void ClearLegacyUi(Transform canvasTransform)
    {
        DestroyChildIfPresent(canvasTransform, "MenuUIRoot");
        DestroyChildIfPresent(canvasTransform, "NavigationRoot");
        DestroyChildIfPresent(canvasTransform, "RollText");
    }

    private static void DestroyChildIfPresent(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);

        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void BuildSceneUi(Transform parent, UiRefs refs, TMP_FontAsset fontAsset)
    {
        RectTransform root = CreateStretchRect("MenuUIRoot", parent);

        refs.mainMenuPanel = CreateMainMenuPanel(root, refs, fontAsset);
        refs.shopPanel = CreateShopPanel(root, refs, fontAsset);
        refs.challengesPanel = CreateChallengesPanel(root, refs, fontAsset);
        refs.settingsPanel = CreateSettingsPanel(root, refs, fontAsset);
        refs.roundUtilityBar = CreateRoundUtilityBar(root, refs, fontAsset);
        refs.rollText = CreateHudText(root, fontAsset);

        refs.shopPanel.SetActive(false);
        refs.challengesPanel.SetActive(false);
        refs.settingsPanel.SetActive(false);
        refs.roundUtilityBar.SetActive(false);
    }

    private static GameObject CreateMainMenuPanel(Transform parent, UiRefs refs, TMP_FontAsset fontAsset)
    {
        RectTransform panel = CreateScreenPanel("MainMenuPanel", parent, GetSurfaceStyle(SurfaceRole.ScreenOverlay).backgroundColor);
        RectTransform card = CreateResponsiveCard("MainMenuCard", panel, MainMenuCardAnchorMin, MainMenuCardAnchorMax, GetSurfaceStyle(SurfaceRole.Card).backgroundColor);
        ConfigureVerticalLayout(card, MenuSpacing, MenuPadding, TextAnchor.UpperCenter);
        ApplySurfaceStyle(card.gameObject, SurfaceRole.Card);

        CreateText(card, "TitleText", "Dice Roguelite", fontAsset, TextRole.MainTitle, 120f);
        CreateText(card, "SubtitleText", "A clean prototype hub for entering runs, browsing meta progression, and expanding the roguelite structure over time.", fontAsset, TextRole.Subtitle, 128f);
        CreateDivider(card);

        RectTransform actionGroup = CreateLayoutPanel("ActionGroup", card, GetSurfaceStyle(SurfaceRole.InsetCard).backgroundColor, 0f);
        ConfigureVerticalLayout(actionGroup, 18, InnerCardPadding, TextAnchor.UpperCenter);
        ApplySurfaceStyle(actionGroup.gameObject, SurfaceRole.InsetCard);

        refs.playButton = CreatePrimaryButton(actionGroup, "PlayButton", "Enter Round / Play", fontAsset);
        refs.mainMenuShopButton = CreateSecondaryButton(actionGroup, "ShopButton", "Shop", MainButtonHeight, fontAsset);
        refs.mainMenuChallengesButton = CreateSecondaryButton(actionGroup, "ChallengesButton", "Challenges", MainButtonHeight, fontAsset);
        refs.mainMenuSettingsButton = CreateSecondaryButton(actionGroup, "SettingsButton", "Settings", MainButtonHeight, fontAsset);

        CreateText(card, "FutureText", "Future room: loadouts, codex, daily runs, profile, cloud save.", fontAsset, TextRole.SupportingHint, 72f);
        return panel.gameObject;
    }

    private static GameObject CreateShopPanel(Transform parent, UiRefs refs, TMP_FontAsset fontAsset)
    {
        RectTransform panel = CreateScreenPanel("ShopPanel", parent, GetSurfaceStyle(SurfaceRole.ScreenOverlay).backgroundColor);
        RectTransform card = CreateResponsiveCard("ShopCard", panel, ScreenCardAnchorMin, ScreenCardAnchorMax, GetSurfaceStyle(SurfaceRole.Card).backgroundColor);
        ConfigureVerticalLayout(card, MenuSpacing, MenuPadding, TextAnchor.UpperCenter);
        ApplySurfaceStyle(card.gameObject, SurfaceRole.Card);

        CreateText(card, "TitleText", "Shop", fontAsset, TextRole.SectionTitle, 78f);
        refs.shopContextText = CreateText(card, "ShopContextText", "Meta Progression", fontAsset, TextRole.Subtitle, 42f, 0f, AccentTextColor);
        CreateDivider(card);

        RectTransform tabs = CreateLayoutRow("ShopTabRow", card, 16, TabButtonHeight);
        refs.shopDiceUnlocksButton = CreateSecondaryButton(tabs, "DiceUnlocksButton", "Dice Unlocks", TabButtonHeight, fontAsset, TabButtonFontSize);
        refs.shopEfficiencyButton = CreateSecondaryButton(tabs, "EfficiencyButton", "Efficiency / Automation", TabButtonHeight, fontAsset, TabButtonFontSize);
        refs.shopScoreMultipliersButton = CreateSecondaryButton(tabs, "ScoreMultipliersButton", "Score Multipliers", TabButtonHeight, fontAsset, TabButtonFontSize);

        RectTransform infoCard = CreateLayoutPanel("ShopContentCard", card, GetSurfaceStyle(SurfaceRole.InsetCard).backgroundColor, 1f);
        ConfigureVerticalLayout(infoCard, PanelSpacing, 34, TextAnchor.UpperLeft);
        ApplySurfaceStyle(infoCard.gameObject, SurfaceRole.InsetCard);
        refs.shopSectionTitleText = CreateText(infoCard, "ShopSectionTitleText", "Dice Unlocks", fontAsset, TextRole.CardTitle, 62f);
        CreateDivider(infoCard, new Color(0.33f, 0.42f, 0.53f, 0.5f));
        refs.shopContentText = CreateText(infoCard, "ShopContentText", string.Empty, fontAsset, TextRole.Body, 420f, 1f);

        refs.shopReturnButton = CreateSecondaryButton(card, "ShopBackButton", "Return to Main Menu", MainButtonHeight, fontAsset);
        return panel.gameObject;
    }

    private static GameObject CreateChallengesPanel(Transform parent, UiRefs refs, TMP_FontAsset fontAsset)
    {
        RectTransform panel = CreateScreenPanel("ChallengesPanel", parent, GetSurfaceStyle(SurfaceRole.ScreenOverlay).backgroundColor);
        RectTransform card = CreateResponsiveCard("ChallengesCard", panel, ScreenCardAnchorMin, ScreenCardAnchorMax, GetSurfaceStyle(SurfaceRole.Card).backgroundColor);
        ConfigureVerticalLayout(card, MenuSpacing, MenuPadding, TextAnchor.UpperCenter);
        ApplySurfaceStyle(card.gameObject, SurfaceRole.Card);

        CreateText(card, "TitleText", "Challenges", fontAsset, TextRole.SectionTitle, 78f);
        CreateText(card, "SubtitleText", "Track milestone goals and achievement hooks here. This panel is shared between meta browsing and in-run reference.", fontAsset, TextRole.Subtitle, 90f);
        CreateDivider(card);

        RectTransform contentCard = CreateLayoutPanel("ChallengesContentCard", card, GetSurfaceStyle(SurfaceRole.InsetCard).backgroundColor, 1f);
        ConfigureVerticalLayout(contentCard, PanelSpacing, 34, TextAnchor.UpperLeft);
        ApplySurfaceStyle(contentCard.gameObject, SurfaceRole.InsetCard);
        refs.challengesContentText = CreateText(contentCard, "ChallengesContentText", string.Empty, fontAsset, TextRole.Body, 420f, 1f);

        refs.challengesBackButton = CreateSecondaryButton(card, "ChallengesBackButton", "Back", MainButtonHeight, fontAsset);
        return panel.gameObject;
    }

    private static GameObject CreateSettingsPanel(Transform parent, UiRefs refs, TMP_FontAsset fontAsset)
    {
        RectTransform panel = CreateScreenPanel("SettingsPanel", parent, GetSurfaceStyle(SurfaceRole.ScreenOverlay).backgroundColor);
        RectTransform card = CreateResponsiveCard("SettingsCard", panel, ScreenCardAnchorMin, ScreenCardAnchorMax, GetSurfaceStyle(SurfaceRole.Card).backgroundColor);
        ConfigureVerticalLayout(card, MenuSpacing, MenuPadding, TextAnchor.UpperCenter);
        ApplySurfaceStyle(card.gameObject, SurfaceRole.Card);

        CreateText(card, "TitleText", "Settings", fontAsset, TextRole.SectionTitle, 78f);
        CreateText(card, "SubtitleText", "Simple structure for future real options. In round, this acts like an overlay reference screen rather than a separate pause menu.", fontAsset, TextRole.Subtitle, 90f);
        CreateDivider(card);

        RectTransform contentCard = CreateLayoutPanel("SettingsContentCard", card, GetSurfaceStyle(SurfaceRole.InsetCard).backgroundColor, 1f);
        ConfigureVerticalLayout(contentCard, PanelSpacing, 34, TextAnchor.UpperLeft);
        ApplySurfaceStyle(contentCard.gameObject, SurfaceRole.InsetCard);
        refs.settingsContentText = CreateText(contentCard, "SettingsContentText", string.Empty, fontAsset, TextRole.Body, 420f, 1f);

        refs.settingsBackButton = CreateSecondaryButton(card, "SettingsBackButton", "Back", MainButtonHeight, fontAsset);
        return panel.gameObject;
    }

    private static GameObject CreateRoundUtilityBar(Transform parent, UiRefs refs, TMP_FontAsset fontAsset)
    {
        RectTransform bar = CreateAnchoredStretchPanel(
            "RoundUtilityBar",
            parent,
            UtilityBarAnchorMin,
            UtilityBarAnchorMax,
            24f,
            UtilityButtonHeight,
            GetSurfaceStyle(SurfaceRole.UtilityBar).backgroundColor);

        ConfigureHorizontalLayout(bar, 16, 16);
        ApplySurfaceStyle(bar.gameObject, SurfaceRole.UtilityBar);
        refs.roundChallengesButton = CreateSecondaryButton(bar, "RoundChallengesButton", "Challenges", UtilityButtonHeight, fontAsset, UtilityButtonFontSize);
        refs.roundSettingsButton = CreateSecondaryButton(bar, "RoundSettingsButton", "Settings", UtilityButtonHeight, fontAsset, UtilityButtonFontSize);
        refs.roundMainMenuButton = CreateSecondaryButton(bar, "RoundMainMenuButton", "Return to Main Menu", UtilityButtonHeight, fontAsset, UtilityButtonFontSize);
        return bar.gameObject;
    }

    private static TMP_Text CreateHudText(Transform parent, TMP_FontAsset fontAsset)
    {
        RectTransform rect = CreateRect("RollText", parent);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
        rect.sizeDelta = new Vector2(600f, 120f);

        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(text, fontAsset, TextRole.RollResult);
        text.text = "Roll: -, -, -\nTotal: -";
        return text;
    }

    private static void AssignControllerReferences(PrototypeGameFlowController controller, DiceManager diceManager, UiRefs refs)
    {
        SerializedObject serializedController = new SerializedObject(controller);
        SetObjectReference(serializedController, "canvas", refs.canvas);
        SetObjectReference(serializedController, "diceManager", diceManager);
        SetObjectReference(serializedController, "eventSystem", refs.eventSystem);
        SetObjectReference(serializedController, "rollText", refs.rollText);
        SetObjectReference(serializedController, "mainMenuPanel", refs.mainMenuPanel);
        SetObjectReference(serializedController, "shopPanel", refs.shopPanel);
        SetObjectReference(serializedController, "challengesPanel", refs.challengesPanel);
        SetObjectReference(serializedController, "settingsPanel", refs.settingsPanel);
        SetObjectReference(serializedController, "roundUtilityBar", refs.roundUtilityBar);
        SetObjectReference(serializedController, "playButton", refs.playButton);
        SetObjectReference(serializedController, "mainMenuShopButton", refs.mainMenuShopButton);
        SetObjectReference(serializedController, "mainMenuChallengesButton", refs.mainMenuChallengesButton);
        SetObjectReference(serializedController, "mainMenuSettingsButton", refs.mainMenuSettingsButton);
        SetObjectReference(serializedController, "shopDiceUnlocksButton", refs.shopDiceUnlocksButton);
        SetObjectReference(serializedController, "shopEfficiencyButton", refs.shopEfficiencyButton);
        SetObjectReference(serializedController, "shopScoreMultipliersButton", refs.shopScoreMultipliersButton);
        SetObjectReference(serializedController, "shopReturnButton", refs.shopReturnButton);
        SetObjectReference(serializedController, "shopContextText", refs.shopContextText);
        SetObjectReference(serializedController, "shopSectionTitleText", refs.shopSectionTitleText);
        SetObjectReference(serializedController, "shopContentText", refs.shopContentText);
        SetObjectReference(serializedController, "challengesBackButton", refs.challengesBackButton);
        SetObjectReference(serializedController, "challengesContentText", refs.challengesContentText);
        SetObjectReference(serializedController, "settingsBackButton", refs.settingsBackButton);
        SetObjectReference(serializedController, "settingsContentText", refs.settingsContentText);
        SetObjectReference(serializedController, "roundChallengesButton", refs.roundChallengesButton);
        SetObjectReference(serializedController, "roundSettingsButton", refs.roundSettingsButton);
        SetObjectReference(serializedController, "roundMainMenuButton", refs.roundMainMenuButton);
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignDiceManagerResultText(DiceManager diceManager, TMP_Text rollText)
    {
        if (diceManager == null)
        {
            return;
        }

        SerializedObject serializedManager = new SerializedObject(diceManager);
        SetObjectReference(serializedManager, "resultText", rollText);
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
        diceManager.SetResultText(rollText);
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static RectTransform CreateScreenPanel(string name, Transform parent, Color backgroundColor)
    {
        RectTransform rect = CreateStretchRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = backgroundColor;
        return rect;
    }

    private static RectTransform CreateResponsiveCard(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color backgroundColor)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = backgroundColor;
        return rect;
    }

    private static RectTransform CreateAnchoredStretchPanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float topInset,
        float height,
        Color backgroundColor)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(0f, -topInset - height);
        rect.offsetMax = new Vector2(0f, -topInset);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = backgroundColor;
        return rect;
    }

    private static RectTransform CreateLayoutPanel(string name, Transform parent, Color backgroundColor, float flexHeight)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = backgroundColor;

        LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 100f;
        layoutElement.flexibleHeight = flexHeight;
        layoutElement.flexibleWidth = 1f;
        return rect;
    }

    private static RectTransform CreateLayoutRow(string name, Transform parent, int spacing, float height)
    {
        RectTransform rect = CreateRect(name, parent);
        ConfigureHorizontalLayout(rect, spacing, 0);

        LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;
        return rect;
    }

    private static RectTransform CreateStretchRect(string name, Transform parent)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        gameObject.layer = parent.gameObject.layer;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    private static void ConfigureVerticalLayout(RectTransform rect, int spacing, int padding, TextAnchor alignment)
    {
        VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void ConfigureHorizontalLayout(RectTransform rect, int spacing, int padding)
    {
        HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string content,
        TMP_FontAsset fontAsset,
        TextRole role,
        float preferredHeight,
        float flexibleHeight = 0f,
        Color? colorOverride = null)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        ApplyTextStyle(text, fontAsset, role, colorOverride);

        LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = preferredHeight;
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleHeight = flexibleHeight;
        layoutElement.flexibleWidth = 1f;
        return text;
    }

    private static Button CreatePrimaryButton(Transform parent, string name, string label, TMP_FontAsset fontAsset)
    {
        return CreateButton(parent, name, label, fontAsset, ButtonRole.Primary, MainButtonHeight, ButtonFontSize, TextRole.ButtonLabel);
    }

    private static Button CreateSecondaryButton(Transform parent, string name, string label, float height, TMP_FontAsset fontAsset)
    {
        return CreateSecondaryButton(parent, name, label, height, fontAsset, ButtonFontSize);
    }

    private static Button CreateSecondaryButton(Transform parent, string name, string label, float height, TMP_FontAsset fontAsset, int labelFontSize)
    {
        TextRole role = labelFontSize == UtilityButtonFontSize ? TextRole.UtilityLabel : TextRole.ButtonLabel;
        return CreateButton(parent, name, label, fontAsset, ButtonRole.Secondary, height, labelFontSize, role);
    }

    private static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset fontAsset, ButtonRole buttonRole, float height, int labelFontSize, TextRole textRole)
    {
        ButtonStyleDefinition buttonStyle = GetButtonStyle(buttonRole);
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = buttonStyle.backgroundColor;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ApplyButtonStyle(button, buttonRole);

        ColorBlock colors = button.colors;
        colors.normalColor = buttonStyle.backgroundColor;
        colors.highlightedColor = buttonStyle.highlightedColor;
        colors.pressedColor = buttonStyle.pressedColor;
        colors.selectedColor = buttonStyle.selectedColor;
        button.colors = colors;

        LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;
        layoutElement.flexibleWidth = 1f;

        RectTransform labelRect = CreateRect("Label", rect);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20f, 12f);
        labelRect.offsetMax = new Vector2(-20f, -12f);

        TextMeshProUGUI labelText = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        ApplyTextStyle(labelText, fontAsset, textRole, buttonStyle.textColor, labelFontSize);

        return button;
    }

    private static void CreateDivider(Transform parent)
    {
        CreateDivider(parent, DividerColor);
    }

    private static void CreateDivider(Transform parent, Color color)
    {
        RectTransform divider = CreateRect("Divider", parent);
        Image image = divider.gameObject.AddComponent<Image>();
        image.color = color;

        LayoutElement layoutElement = divider.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 2f;
        layoutElement.preferredHeight = 2f;
        layoutElement.flexibleWidth = 1f;
    }

    private static SurfaceStyleDefinition GetSurfaceStyle(SurfaceRole role)
    {
        switch (role)
        {
            case SurfaceRole.ScreenOverlay:
                return new SurfaceStyleDefinition(ScreenOverlayColor, false, Color.clear, Vector2.zero, false, Color.clear, Vector2.zero);
            case SurfaceRole.InsetCard:
                return new SurfaceStyleDefinition(ContentCardColor, true, ContentOutlineColor, new Vector2(1f, -1f), true, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -4f));
            case SurfaceRole.UtilityBar:
                return new SurfaceStyleDefinition(new Color(0.08f, 0.1f, 0.14f, 0.94f), true, new Color(0.44f, 0.58f, 0.75f, 0.14f), new Vector2(1f, -1f), true, ShadowColor, new Vector2(0f, -8f));
            case SurfaceRole.Card:
            default:
                return new SurfaceStyleDefinition(CardColor, true, CardOutlineColor, new Vector2(1f, -1f), true, ShadowColor, new Vector2(0f, -8f));
        }
    }

    private static ButtonStyleDefinition GetButtonStyle(ButtonRole role)
    {
        switch (role)
        {
            case ButtonRole.Primary:
                return new ButtonStyleDefinition(
                    PrimaryButtonColor,
                    Color.white,
                    Color.Lerp(PrimaryButtonColor, Color.white, 0.12f),
                    Color.Lerp(PrimaryButtonColor, Color.black, 0.14f),
                    Color.Lerp(PrimaryButtonColor, Color.white, 0.06f),
                    true,
                    new Color(0.72f, 0.84f, 1f, 0.18f),
                    new Vector2(1f, -1f),
                    true,
                    new Color(0f, 0f, 0f, 0.24f),
                    new Vector2(0f, -4f));
            case ButtonRole.Secondary:
            default:
                return new ButtonStyleDefinition(
                    SecondaryButtonColor,
                    new Color(0.96f, 0.97f, 0.98f, 1f),
                    Color.Lerp(SecondaryButtonColor, Color.white, 0.08f),
                    Color.Lerp(SecondaryButtonColor, Color.black, 0.14f),
                    Color.Lerp(SecondaryButtonColor, Color.white, 0.06f),
                    true,
                    new Color(0.49f, 0.58f, 0.68f, 0.14f),
                    new Vector2(1f, -1f),
                    true,
                    new Color(0f, 0f, 0f, 0.2f),
                    new Vector2(0f, -4f));
        }
    }

    private static void ApplySurfaceStyle(GameObject target, SurfaceRole role)
    {
        SurfaceStyleDefinition style = GetSurfaceStyle(role);
        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.color = style.backgroundColor;
        }

        RemoveIfPresent<Outline>(target);
        RemoveIfPresent<Shadow>(target);

        if (style.useOutline)
        {
            AddOutline(target, style.outlineColor, style.outlineOffset);
        }

        if (style.useShadow)
        {
            AddShadow(target, style.shadowColor, style.shadowOffset);
        }
    }

    private static void ApplyButtonStyle(Button button, ButtonRole role)
    {
        if (button == null)
        {
            return;
        }

        ButtonStyleDefinition style = GetButtonStyle(role);
        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            image.color = style.backgroundColor;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = style.backgroundColor;
        colors.highlightedColor = style.highlightedColor;
        colors.pressedColor = style.pressedColor;
        colors.selectedColor = style.selectedColor;
        button.colors = colors;

        RemoveIfPresent<Outline>(button.gameObject);
        RemoveIfPresent<Shadow>(button.gameObject);

        if (style.useOutline)
        {
            AddOutline(button.gameObject, style.outlineColor, style.outlineOffset);
        }

        if (style.useShadow)
        {
            AddShadow(button.gameObject, style.shadowColor, style.shadowOffset);
        }
    }

    private static void ApplyTextStyle(TMP_Text text, TMP_FontAsset fontAsset, TextRole role, Color? colorOverride = null, int? fontSizeOverride = null)
    {
        TextStyleDefinition style = GetTextStyle(role);
        text.font = fontAsset;
        text.fontSharedMaterial = fontAsset.material;
        text.fontSize = fontSizeOverride ?? style.fontSize;
        text.fontStyle = style.fontStyle;
        text.alignment = style.alignment;
        text.color = colorOverride ?? style.color;
        text.textWrappingMode = style.wrappingMode;
        text.overflowMode = style.overflowMode;
        text.extraPadding = style.extraPadding;
        text.enableAutoSizing = false;
        text.raycastTarget = false;
        text.lineSpacing = style.lineSpacing;
        text.richText = true;
        text.margin = Vector4.zero;
        text.isTextObjectScaleStatic = true;

        Shadow existingShadow = text.GetComponent<Shadow>();
        if (existingShadow != null)
        {
            Object.DestroyImmediate(existingShadow);
        }

        if (style.useShadow)
        {
            AddTextShadow(text.gameObject, style.shadowColor, style.shadowOffset);
        }
    }

    private static TextStyleDefinition GetTextStyle(TextRole role)
    {
        switch (role)
        {
            case TextRole.MainTitle:
                return new TextStyleDefinition(TitleFontSize, FontStyles.Bold, TextAlignmentOptions.Center, TitleColor, TextOverflowModes.Overflow, TextWrappingModes.Normal, true, 0.95f, true, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -2f));
            case TextRole.Subtitle:
                return new TextStyleDefinition(SubtitleFontSize, FontStyles.Normal, TextAlignmentOptions.Center, SubtitleColor, TextOverflowModes.Overflow, TextWrappingModes.Normal, true, 1.08f);
            case TextRole.SectionTitle:
                return new TextStyleDefinition(SectionTitleFontSize, FontStyles.Bold, TextAlignmentOptions.Center, TitleColor, TextOverflowModes.Overflow, TextWrappingModes.Normal, true, 1f, true, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -1f));
            case TextRole.CardTitle:
                return new TextStyleDefinition(40, FontStyles.Bold, TextAlignmentOptions.TopLeft, TitleColor, TextOverflowModes.Overflow, TextWrappingModes.Normal, true, 1f);
            case TextRole.Body:
                return new TextStyleDefinition(BodyFontSize, FontStyles.Normal, TextAlignmentOptions.TopLeft, BodyTextColor, TextOverflowModes.Overflow, TextWrappingModes.Normal, true, 1.08f);
            case TextRole.UtilityLabel:
                return new TextStyleDefinition(UtilityButtonFontSize, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.96f, 0.97f, 0.98f, 1f), TextOverflowModes.Overflow, TextWrappingModes.Normal, true, 1f, true, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -1f));
            case TextRole.RollResult:
                return new TextStyleDefinition(36, FontStyles.Bold, TextAlignmentOptions.TopLeft, Color.white, TextOverflowModes.Overflow, TextWrappingModes.Normal, true, 1.04f, true, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -1f));
            case TextRole.SupportingHint:
                return new TextStyleDefinition(22, FontStyles.Italic, TextAlignmentOptions.Center, new Color(0.62f, 0.69f, 0.77f, 1f), TextOverflowModes.Overflow, TextWrappingModes.Normal, true, 1.04f);
            case TextRole.ButtonLabel:
            default:
                return new TextStyleDefinition(ButtonFontSize, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.96f, 0.97f, 0.98f, 1f), TextOverflowModes.Overflow, TextWrappingModes.Normal, true, 1f, true, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -1f));
        }
    }

    private static void ApplyStyleInContainer(Transform root, string containerName, string childName, TMP_FontAsset fontAsset, TextRole role, Color? colorOverride = null)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name != containerName)
            {
                continue;
            }

            Transform child = transforms[i].Find(childName);
            if (child == null)
            {
                continue;
            }

            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text != null)
            {
                ApplyTextStyle(text, fontAsset, role, colorOverride);
            }
        }
    }

    private static void ApplyStyleToNamedText(Transform root, string childName, TMP_FontAsset fontAsset, TextRole role, Color? colorOverride = null)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name != childName)
            {
                continue;
            }

            TMP_Text text = transforms[i].GetComponent<TMP_Text>();
            if (text != null)
            {
                ApplyTextStyle(text, fontAsset, role, colorOverride);
            }
        }
    }

    private static void ApplyButtonLabelStyle(Transform root, string buttonName, TMP_FontAsset fontAsset, TextRole role, int? fontSizeOverride = null)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name != buttonName)
            {
                continue;
            }

            Transform label = transforms[i].Find("Label");
            if (label == null)
            {
                continue;
            }

            TMP_Text text = label.GetComponent<TMP_Text>();
            if (text != null)
            {
                ApplyTextStyle(text, fontAsset, role, null, fontSizeOverride);
            }
        }
    }

    private static void ApplySurfaceStyleInContainer(Transform root, string containerName, string childName, SurfaceRole role)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name != containerName)
            {
                continue;
            }

            Transform child = transforms[i].name == childName ? transforms[i] : transforms[i].Find(childName);
            if (child != null)
            {
                ApplySurfaceStyle(child.gameObject, role);
            }
        }
    }

    private static void ApplySurfaceStyleToNamedObject(Transform root, string objectName, SurfaceRole role)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
            {
                ApplySurfaceStyle(transforms[i].gameObject, role);
            }
        }
    }

    private static void ApplyButtonStyleByName(Transform root, string buttonName, ButtonRole role)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name != buttonName)
            {
                continue;
            }

            Button button = transforms[i].GetComponent<Button>();
            if (button != null)
            {
                ApplyButtonStyle(button, role);
            }
        }
    }

    private static void ApplyDividerStyle(Transform root)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name != "Divider")
            {
                continue;
            }

            Image image = transforms[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = DividerColor;
            }
        }
    }

    private static void RemoveIfPresent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component != null)
        {
            Object.DestroyImmediate(component);
        }
    }

    private static void AddTextShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }
}
