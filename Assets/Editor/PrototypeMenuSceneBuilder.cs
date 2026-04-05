using DiceMadness.Core;
using DiceMadness.UI;
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
    private const int UtilityButtonFontSize = 24;
    private const float MainMenuCardAspectRatio = 0.86f;
    private const float ScreenCardAspectRatio = 1.24f;
    private const float MainMenuCardWidthPercent = 0.5f;
    private const float ScreenCardWidthPercent = 0.68f;
    private const float MainMenuCardMaxHeightPercent = 0.9f;
    private const float ScreenCardMaxHeightPercent = 0.86f;
    private const float MainMenuCardMinWidth = 720f;
    private const float MainMenuCardMaxWidth = 920f;
    private const float ScreenCardMinWidth = 860f;
    private const float ScreenCardMaxWidth = 1180f;
    private const int MainMenuPadding = 44;
    private const int MainMenuSpacing = 18;
    private const int MainMenuInnerPadding = 18;
    private const int MainMenuActionSpacing = 12;
    private const float MainMenuButtonHeight = 78f;
    private const float MainMenuTitleHeight = 96f;
    private const float MainMenuSubtitleHeight = 88f;
    private const float MainMenuHintHeight = 42f;
    private const float RoundHudStatWidth = 172f;
    private const float RoundHudStatHeight = 40f;
    private const float RoundInfoPanelWidth = 380f;
    private const float RoundInfoPanelHeight = 560f;
    private const float RoundInfoSummaryHeight = 112f;
    private const float RoundRollSummaryHeight = 92f;

    private static readonly Vector2 UtilityBarAnchorMin = new Vector2(0.14f, 1f);
    private static readonly Vector2 UtilityBarAnchorMax = new Vector2(0.86f, 1f);

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
        public TMP_Text scoreText;
        public TMP_Text coinsText;
        public TMP_Text shardsText;
        public GameObject roundInfoPanel;
        public TMP_Text roundInfoTitleText;
        public TMP_Text roundInfoText;
        public Button roundInfoScoringButton;
        public Button roundInfoCoinsButton;
        public Button roundInfoActiveEffectsButton;
        public Button roundInfoRunInfoButton;
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
        public Button shopResetTabButton;
        public TMP_Text shopContextText;
        public TMP_Text shopSectionTitleText;
        public TMP_Text shopContentText;
        public RectTransform shopTreeRoot;
        public UiTooltipPresenter shopTooltipPresenter;
        public Button challengesBackButton;
        public TMP_Text challengesContentText;
        public Button settingsBackButton;
        public TMP_Text settingsContentText;
        public CanvasScaler canvasScaler;
        public TMP_Text rollKeyValueText;
        public Button rollKeyRebindButton;
        public TMP_Text backKeyValueText;
        public Button backKeyRebindButton;
        public TMP_Dropdown resolutionDropdown;
        public TMP_Dropdown displayModeDropdown;
        public Toggle vSyncToggle;
        public Slider masterVolumeSlider;
        public TMP_Text masterVolumeValueText;
        public Slider musicVolumeSlider;
        public TMP_Text musicVolumeValueText;
        public Slider sfxVolumeSlider;
        public TMP_Text sfxVolumeValueText;
        public Slider uiScaleSlider;
        public TMP_Text uiScaleValueText;
        public Toggle detailedRollBreakdownToggle;
        public Button roundChallengesButton;
        public Button roundSettingsButton;
        public Button roundSpendCoinsButton;
        public Button roundCashOutButton;
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
        HudResource,
        HudScore,
        HudStat,
        CompactCardTitle,
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
        ApplyStyleToNamedText(root, "SettingsContentText", fontAsset, TextRole.SupportingHint);
        ApplyStyleToNamedText(root, "SettingsSectionTitleText", fontAsset, TextRole.CardTitle);
        ApplyStyleToNamedText(root, "SettingsFieldLabelText", fontAsset, TextRole.Body);
        ApplyStyleToNamedText(root, "DropdownArrowText", fontAsset, TextRole.ButtonLabel);
        ApplyStyleToNamedText(root, "RollKeyValueText", fontAsset, TextRole.Subtitle, AccentTextColor);
        ApplyStyleToNamedText(root, "BackKeyValueText", fontAsset, TextRole.Subtitle, AccentTextColor);
        ApplyStyleToNamedText(root, "MasterVolumeValueText", fontAsset, TextRole.Subtitle, AccentTextColor);
        ApplyStyleToNamedText(root, "MusicVolumeValueText", fontAsset, TextRole.Subtitle, AccentTextColor);
        ApplyStyleToNamedText(root, "SfxVolumeValueText", fontAsset, TextRole.Subtitle, AccentTextColor);
        ApplyStyleToNamedText(root, "UiScaleValueText", fontAsset, TextRole.Subtitle, AccentTextColor);
        ApplyStyleToNamedText(root, "ShardsText", fontAsset, TextRole.HudResource, AccentTextColor);
        ApplyStyleToNamedText(root, "CoinsText", fontAsset, TextRole.HudStat);
        ApplyStyleToNamedText(root, "ScoreText", fontAsset, TextRole.HudStat);
        ApplyStyleToNamedText(root, "RoundInfoTitleText", fontAsset, TextRole.CompactCardTitle);
        ApplyStyleToNamedText(root, "RoundInfoText", fontAsset, TextRole.Body);
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
        ApplyButtonLabelStyle(root, "RollKeyRebindButton", fontAsset, TextRole.ButtonLabel, 22);
        ApplyButtonLabelStyle(root, "BackKeyRebindButton", fontAsset, TextRole.ButtonLabel, 22);
        ApplyButtonLabelStyle(root, "RoundChallengesButton", fontAsset, TextRole.UtilityLabel);
        ApplyButtonLabelStyle(root, "RoundSettingsButton", fontAsset, TextRole.UtilityLabel);
        ApplyButtonLabelStyle(root, "RoundSpendCoinsButton", fontAsset, TextRole.UtilityLabel);
        ApplyButtonLabelStyle(root, "RoundCashOutButton", fontAsset, TextRole.UtilityLabel);
        ApplyButtonLabelStyle(root, "RoundMainMenuButton", fontAsset, TextRole.UtilityLabel);
        ApplyButtonLabelStyle(root, "RoundInfoScoringButton", fontAsset, TextRole.ButtonLabel, 12);
        ApplyButtonLabelStyle(root, "RoundInfoCoinsButton", fontAsset, TextRole.ButtonLabel, 12);
        ApplyButtonLabelStyle(root, "RoundInfoActiveEffectsButton", fontAsset, TextRole.ButtonLabel, 12);
        ApplyButtonLabelStyle(root, "RoundInfoRunInfoButton", fontAsset, TextRole.ButtonLabel, 12);

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
        ApplySurfaceStyleToNamedObject(root, "RoundInfoContentCard", SurfaceRole.InsetCard);
        ApplySurfaceStyleToNamedObject(root, "RoundUtilityBar", SurfaceRole.UtilityBar);
        ApplySurfaceStyleToNamedObject(root, "RoundInfoPanel", SurfaceRole.Card);

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
        ApplyButtonStyleByName(root, "RoundSpendCoinsButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "RoundCashOutButton", ButtonRole.Primary);
        ApplyButtonStyleByName(root, "RoundMainMenuButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "RoundInfoScoringButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "RoundInfoCoinsButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "RoundInfoActiveEffectsButton", ButtonRole.Secondary);
        ApplyButtonStyleByName(root, "RoundInfoRunInfoButton", ButtonRole.Secondary);

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

        ApplyCanvasScalerSettings(canvas);

        TMP_FontAsset fontAsset = EnsureMenuFontAsset();
        EventSystem eventSystem = EnsureEventSystem();
        ClearLegacyUi(canvas.transform);

        UiRefs uiRefs = new UiRefs
        {
            canvas = canvas,
            eventSystem = eventSystem,
            canvasScaler = canvas.GetComponent<CanvasScaler>(),
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

        ApplyCanvasScalerSettings(canvas);
        return canvas;
    }

    private static void ApplyCanvasScalerSettings(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
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
        refs.roundInfoPanel = CreateRoundInfoPanel(root, refs, fontAsset);

        refs.shopPanel.SetActive(false);
        refs.challengesPanel.SetActive(false);
        refs.settingsPanel.SetActive(false);
        refs.roundUtilityBar.SetActive(false);
        refs.roundInfoPanel.SetActive(false);
        refs.coinsText.gameObject.SetActive(false);
        refs.scoreText.gameObject.SetActive(false);
        refs.rollText.gameObject.SetActive(false);
    }

    private static GameObject CreateMainMenuPanel(Transform parent, UiRefs refs, TMP_FontAsset fontAsset)
    {
        RectTransform panel = CreateScreenPanel("MainMenuPanel", parent, GetSurfaceStyle(SurfaceRole.ScreenOverlay).backgroundColor);
        RectTransform card = CreateResponsiveCard("MainMenuCard", panel, GetSurfaceStyle(SurfaceRole.Card).backgroundColor);
        ConfigureResponsiveAspectCard(card, MainMenuCardWidthPercent, MainMenuCardMaxHeightPercent, MainMenuCardAspectRatio, MainMenuCardMinWidth, MainMenuCardMaxWidth);
        ConfigureVerticalLayout(card, MainMenuSpacing, MainMenuPadding, TextAnchor.UpperCenter);
        ApplySurfaceStyle(card.gameObject, SurfaceRole.Card);

        refs.shardsText = CreateText(card, "ShardsText", "Shards: 0", fontAsset, TextRole.HudResource, 34f, 0f, AccentTextColor);
        CreateText(card, "TitleText", "Dice Roguelite", fontAsset, TextRole.MainTitle, MainMenuTitleHeight);
        CreateText(card, "SubtitleText", "A clean prototype hub for entering runs, browsing meta progression, and expanding the roguelite structure over time.", fontAsset, TextRole.Subtitle, MainMenuSubtitleHeight);
        CreateDivider(card);

        RectTransform actionGroup = CreateLayoutPanel("ActionGroup", card, GetSurfaceStyle(SurfaceRole.InsetCard).backgroundColor, 0f);
        ConfigureVerticalLayout(actionGroup, MainMenuActionSpacing, MainMenuInnerPadding, TextAnchor.UpperCenter);
        ApplySurfaceStyle(actionGroup.gameObject, SurfaceRole.InsetCard);

        refs.playButton = CreatePrimaryButton(actionGroup, "PlayButton", "Enter Round / Play", MainMenuButtonHeight, fontAsset, ButtonFontSize, TextRole.ButtonLabel);
        refs.mainMenuShopButton = CreateSecondaryButton(actionGroup, "ShopButton", "Shop", MainMenuButtonHeight, fontAsset);
        refs.mainMenuChallengesButton = CreateSecondaryButton(actionGroup, "ChallengesButton", "Challenges", MainMenuButtonHeight, fontAsset);
        refs.mainMenuSettingsButton = CreateSecondaryButton(actionGroup, "SettingsButton", "Settings", MainMenuButtonHeight, fontAsset);

        CreateText(card, "FutureText", "Future room: loadouts, codex, daily runs, profile, cloud save.", fontAsset, TextRole.SupportingHint, MainMenuHintHeight);
        return panel.gameObject;
    }

    private static GameObject CreateShopPanel(Transform parent, UiRefs refs, TMP_FontAsset fontAsset)
    {
        RectTransform panel = CreateScreenPanel("ShopPanel", parent, GetSurfaceStyle(SurfaceRole.ScreenOverlay).backgroundColor);
        RectTransform card = CreateResponsiveCard("ShopCard", panel, GetSurfaceStyle(SurfaceRole.Card).backgroundColor);
        ConfigureResponsiveAspectCard(card, ScreenCardWidthPercent, ScreenCardMaxHeightPercent, ScreenCardAspectRatio, ScreenCardMinWidth, ScreenCardMaxWidth);
        ConfigureVerticalLayout(card, 16, 22, TextAnchor.UpperCenter);
        ApplySurfaceStyle(card.gameObject, SurfaceRole.Card);

        RectTransform topBar = CreateLayoutPanel("ShopTopBar", card, GetSurfaceStyle(SurfaceRole.InsetCard).backgroundColor, 0f);
        ConfigureHorizontalLayout(topBar, 12, 18);
        ApplySurfaceStyle(topBar.gameObject, SurfaceRole.InsetCard);

        refs.shopContextText = CreateText(topBar, "ShopContextText", "Shards: 0", fontAsset, TextRole.HudResource, 42f, 0f, AccentTextColor);
        LayoutElement leftLayout = refs.shopContextText.GetComponent<LayoutElement>();
        if (leftLayout != null)
        {
            leftLayout.minWidth = 220f;
            leftLayout.preferredWidth = 220f;
            leftLayout.flexibleWidth = 0f;
        }

        TMP_Text shopTitle = CreateText(topBar, "TitleText", "Shop", fontAsset, TextRole.CardTitle, 48f);
        LayoutElement titleLayout = shopTitle.GetComponent<LayoutElement>();
        if (titleLayout != null)
        {
            titleLayout.flexibleWidth = 1f;
        }

        refs.shopResetTabButton = CreateSecondaryButton(topBar, "ShopResetTabButton", "Reset Tab", 56f, fontAsset, 22);
        LayoutElement resetLayout = refs.shopResetTabButton.GetComponent<LayoutElement>();
        if (resetLayout != null)
        {
            resetLayout.minWidth = 170f;
            resetLayout.preferredWidth = 170f;
            resetLayout.flexibleWidth = 0f;
        }

        refs.shopReturnButton = CreateSecondaryButton(topBar, "ShopBackButton", "Back to Main Menu", 56f, fontAsset, 24);
        LayoutElement backLayout = refs.shopReturnButton.GetComponent<LayoutElement>();
        if (backLayout != null)
        {
            backLayout.minWidth = 248f;
            backLayout.preferredWidth = 248f;
            backLayout.flexibleWidth = 0f;
        }

        RectTransform tabs = CreateLayoutRow("ShopTabRow", card, 6, 42f);
        refs.shopDiceUnlocksButton = CreateSecondaryButton(tabs, "DiceUnlocksButton", "Dice Unlocks", 42f, fontAsset, 16);
        refs.shopEfficiencyButton = CreateSecondaryButton(tabs, "EfficiencyButton", "Efficiency / Automation", 42f, fontAsset, 16);
        refs.shopScoreMultipliersButton = CreateSecondaryButton(tabs, "ScoreMultipliersButton", "Score Multipliers", 42f, fontAsset, 16);

        RectTransform infoCard = CreateLayoutPanel("ShopContentCard", card, GetSurfaceStyle(SurfaceRole.InsetCard).backgroundColor, 1f);
        ConfigureVerticalLayout(infoCard, 12, 18, TextAnchor.UpperLeft);
        ApplySurfaceStyle(infoCard.gameObject, SurfaceRole.InsetCard);
        refs.shopSectionTitleText = CreateText(infoCard, "ShopSectionTitleText", "Dice Unlocks", fontAsset, TextRole.SupportingHint, 0f);
        LayoutElement sectionLayout = refs.shopSectionTitleText.GetComponent<LayoutElement>();
        if (sectionLayout != null)
        {
            sectionLayout.ignoreLayout = true;
        }

        refs.shopContentText = CreateText(infoCard, "ShopContentText", string.Empty, fontAsset, TextRole.SupportingHint, 0f);
        LayoutElement contentLayout = refs.shopContentText.GetComponent<LayoutElement>();
        if (contentLayout != null)
        {
            contentLayout.ignoreLayout = true;
        }

        refs.shopTreeRoot = CreateLayoutContainer("ShopTreeRoot", infoCard, 1f);
        ConfigureVerticalLayout(refs.shopTreeRoot, 14, 0, TextAnchor.UpperCenter);
        refs.shopTooltipPresenter = CreateShopTooltip(panel, fontAsset);
        return panel.gameObject;
    }

    private static GameObject CreateChallengesPanel(Transform parent, UiRefs refs, TMP_FontAsset fontAsset)
    {
        RectTransform panel = CreateScreenPanel("ChallengesPanel", parent, GetSurfaceStyle(SurfaceRole.ScreenOverlay).backgroundColor);
        RectTransform card = CreateResponsiveCard("ChallengesCard", panel, GetSurfaceStyle(SurfaceRole.Card).backgroundColor);
        ConfigureResponsiveAspectCard(card, ScreenCardWidthPercent, ScreenCardMaxHeightPercent, ScreenCardAspectRatio, ScreenCardMinWidth, ScreenCardMaxWidth);
        ConfigureVerticalLayout(card, MenuSpacing, MenuPadding, TextAnchor.UpperCenter);
        ApplySurfaceStyle(card.gameObject, SurfaceRole.Card);

        CreateText(card, "TitleText", "Challenges", fontAsset, TextRole.SectionTitle, 78f);
        CreateText(card, "SubtitleText", "Track milestone goals and achievement hooks here. This panel is shared between meta browsing and in-run reference.", fontAsset, TextRole.Subtitle, 90f);
        CreateDivider(card);

        RectTransform contentCard = CreateLayoutPanel("ChallengesContentCard", card, GetSurfaceStyle(SurfaceRole.InsetCard).backgroundColor, 1f);
        ConfigureVerticalLayout(contentCard, PanelSpacing, 34, TextAnchor.UpperLeft);
        ApplySurfaceStyle(contentCard.gameObject, SurfaceRole.InsetCard);
        refs.challengesContentText = CreateText(contentCard, "ChallengesContentText", string.Empty, fontAsset, TextRole.Body, 420f, 1f);
        ConfigureFlexibleTextLayout(refs.challengesContentText, 120f);

        refs.challengesBackButton = CreateSecondaryButton(card, "ChallengesBackButton", "Back", MainButtonHeight, fontAsset);
        return panel.gameObject;
    }

    private static GameObject CreateSettingsPanel(Transform parent, UiRefs refs, TMP_FontAsset fontAsset)
    {
        RectTransform panel = CreateScreenPanel("SettingsPanel", parent, GetSurfaceStyle(SurfaceRole.ScreenOverlay).backgroundColor);
        RectTransform card = CreateResponsiveCard("SettingsCard", panel, GetSurfaceStyle(SurfaceRole.Card).backgroundColor);
        ConfigureResponsiveAspectCard(card, ScreenCardWidthPercent, ScreenCardMaxHeightPercent, ScreenCardAspectRatio, ScreenCardMinWidth, ScreenCardMaxWidth);
        ConfigureVerticalLayout(card, MenuSpacing, MenuPadding, TextAnchor.UpperCenter);
        ApplySurfaceStyle(card.gameObject, SurfaceRole.Card);

        CreateText(card, "TitleText", "Settings", fontAsset, TextRole.SectionTitle, 72f);
        CreateText(card, "SubtitleText", "Adjust controls, video, audio, and lightweight gameplay preferences. Changes save automatically.", fontAsset, TextRole.Subtitle, 74f);
        CreateDivider(card);

        RectTransform contentCard = CreateLayoutPanel("SettingsContentCard", card, GetSurfaceStyle(SurfaceRole.InsetCard).backgroundColor, 1f);
        ConfigureVerticalLayout(contentCard, 14, 20, TextAnchor.UpperLeft);
        ApplySurfaceStyle(contentCard.gameObject, SurfaceRole.InsetCard);

        refs.settingsContentText = CreateText(contentCard, "SettingsContentText", "Changes apply immediately and are stored automatically.", fontAsset, TextRole.SupportingHint, 30f);

        RectTransform settingsScrollContent = CreateScrollContentArea("SettingsScrollView", contentCard);

        RectTransform controlsSection = CreateSettingsSection(settingsScrollContent, "SettingsControlsSection", "Controls", fontAsset);
        CreateSettingsRebindRow(controlsSection, "Roll Dice", "RollKeyValueText", out refs.rollKeyValueText, "RollKeyRebindButton", out refs.rollKeyRebindButton, fontAsset);
        CreateSettingsRebindRow(controlsSection, "Back / Close", "BackKeyValueText", out refs.backKeyValueText, "BackKeyRebindButton", out refs.backKeyRebindButton, fontAsset);

        RectTransform videoSection = CreateSettingsSection(settingsScrollContent, "SettingsVideoSection", "Video", fontAsset);
        CreateSettingsDropdownRow(videoSection, "Resolution", "ResolutionDropdown", out refs.resolutionDropdown, fontAsset);
        CreateSettingsDropdownRow(videoSection, "Display Mode", "DisplayModeDropdown", out refs.displayModeDropdown, fontAsset);
        CreateSettingsToggleRow(videoSection, "VSync", "VSyncToggle", out refs.vSyncToggle, fontAsset);

        RectTransform audioSection = CreateSettingsSection(settingsScrollContent, "SettingsAudioSection", "Audio", fontAsset);
        CreateSettingsSliderRow(audioSection, "Master Volume", "MasterVolumeSlider", out refs.masterVolumeSlider, "MasterVolumeValueText", out refs.masterVolumeValueText, fontAsset);
        CreateSettingsSliderRow(audioSection, "Music Volume", "MusicVolumeSlider", out refs.musicVolumeSlider, "MusicVolumeValueText", out refs.musicVolumeValueText, fontAsset);
        CreateSettingsSliderRow(audioSection, "SFX Volume", "SfxVolumeSlider", out refs.sfxVolumeSlider, "SfxVolumeValueText", out refs.sfxVolumeValueText, fontAsset);

        RectTransform gameplaySection = CreateSettingsSection(settingsScrollContent, "SettingsGameplaySection", "Gameplay", fontAsset);
        CreateSettingsSliderRow(gameplaySection, "UI Scale", "UiScaleSlider", out refs.uiScaleSlider, "UiScaleValueText", out refs.uiScaleValueText, fontAsset, 0.75f, 1.35f);

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

        HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14;
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        ApplySurfaceStyle(bar.gameObject, SurfaceRole.UtilityBar);

        RectTransform statsGroup = CreateRect("RoundHudStatsGroup", bar);
        ConfigureHorizontalRowLayout(statsGroup, 12, 0);
        LayoutElement statsLayout = statsGroup.gameObject.AddComponent<LayoutElement>();
        statsLayout.minWidth = 360f;
        statsLayout.preferredWidth = 360f;
        statsLayout.minHeight = UtilityButtonHeight;
        statsLayout.preferredHeight = UtilityButtonHeight;
        statsLayout.flexibleWidth = 0f;

        refs.coinsText = CreateCoinsHudText(statsGroup, fontAsset);
        refs.scoreText = CreateScoreHudText(statsGroup, fontAsset);

        RectTransform spacer = CreateRect("RoundHudSpacer", bar);
        LayoutElement spacerLayout = spacer.gameObject.AddComponent<LayoutElement>();
        spacerLayout.minWidth = 0f;
        spacerLayout.flexibleWidth = 1f;
        spacerLayout.preferredWidth = 0f;

        refs.roundChallengesButton = CreateSecondaryButton(bar, "RoundChallengesButton", "Challenges", UtilityButtonHeight, fontAsset, UtilityButtonFontSize);
        refs.roundSettingsButton = CreateSecondaryButton(bar, "RoundSettingsButton", "Settings", UtilityButtonHeight, fontAsset, UtilityButtonFontSize);
        refs.roundSpendCoinsButton = CreateSecondaryButton(bar, "RoundSpendCoinsButton", "Spend 3 Coins", UtilityButtonHeight, fontAsset, UtilityButtonFontSize);
        refs.roundCashOutButton = CreatePrimaryButton(bar, "RoundCashOutButton", "Cash Out", UtilityButtonHeight, fontAsset, UtilityButtonFontSize, TextRole.UtilityLabel);
        refs.roundMainMenuButton = CreateSecondaryButton(bar, "RoundMainMenuButton", "Return to Main Menu", UtilityButtonHeight, fontAsset, UtilityButtonFontSize);
        return bar.gameObject;
    }

    private static GameObject CreateRoundInfoPanel(Transform parent, UiRefs refs, TMP_FontAsset fontAsset)
    {
        RectTransform panel = CreateRect("RoundInfoPanel", parent);
        panel.anchorMin = new Vector2(0f, 0.5f);
        panel.anchorMax = new Vector2(0f, 0.5f);
        panel.pivot = new Vector2(0f, 0.5f);
        panel.anchoredPosition = new Vector2(28f, 8f);
        panel.sizeDelta = new Vector2(RoundInfoPanelWidth, RoundInfoPanelHeight);

        Image image = panel.gameObject.AddComponent<Image>();
        image.color = GetSurfaceStyle(SurfaceRole.Card).backgroundColor;
        ConfigureVerticalLayout(panel, 14, 24, TextAnchor.UpperLeft);
        ApplySurfaceStyle(panel.gameObject, SurfaceRole.Card);
        panel.gameObject.AddComponent<RectMask2D>();

        RectTransform summaryArea = CreateLayoutPanel("RoundInfoSummaryArea", panel, new Color(1f, 1f, 1f, 0.028f), 0f);
        ConfigureVerticalLayout(summaryArea, 0, 0, TextAnchor.UpperCenter);
        summaryArea.gameObject.AddComponent<RectMask2D>();
        LayoutElement summaryLayout = summaryArea.GetComponent<LayoutElement>();
        if (summaryLayout != null)
        {
            summaryLayout.minHeight = RoundInfoSummaryHeight;
            summaryLayout.preferredHeight = RoundInfoSummaryHeight;
            summaryLayout.flexibleHeight = 0f;
        }

        refs.rollText = CreateText(summaryArea, "RollText", "Press your roll binding to roll.\nStarting Coins: -\nCash out after any resolved roll.", fontAsset, TextRole.RollResult, RoundRollSummaryHeight);
        if (refs.rollText != null)
        {
            refs.rollText.alignment = TextAlignmentOptions.Top;
            LayoutElement rollLayout = refs.rollText.GetComponent<LayoutElement>();
            if (rollLayout != null)
            {
                rollLayout.minHeight = RoundInfoSummaryHeight - 8f;
                rollLayout.preferredHeight = RoundInfoSummaryHeight - 8f;
            }
        }

        RectTransform tabsArea = CreateLayoutPanel("RoundInfoTabsArea", panel, new Color(1f, 1f, 1f, 0.0f), 1f);
        ConfigureVerticalLayout(tabsArea, 12, 0, TextAnchor.UpperLeft);
        LayoutElement tabsLayout = tabsArea.GetComponent<LayoutElement>();
        if (tabsLayout != null)
        {
            tabsLayout.flexibleHeight = 1f;
            tabsLayout.minHeight = 0f;
        }

        refs.roundInfoTitleText = CreateText(tabsArea, "RoundInfoTitleText", "Run Info", fontAsset, TextRole.CompactCardTitle, 34f);
        RectTransform tabRow = CreateLayoutRow("RoundInfoTabRow", tabsArea, 6, 36f);
        refs.roundInfoScoringButton = CreateSecondaryButton(tabRow, "RoundInfoScoringButton", "Scoring", 36f, fontAsset, 12);
        refs.roundInfoCoinsButton = CreateSecondaryButton(tabRow, "RoundInfoCoinsButton", "Coins", 36f, fontAsset, 12);
        refs.roundInfoActiveEffectsButton = CreateSecondaryButton(tabRow, "RoundInfoActiveEffectsButton", "Effects", 36f, fontAsset, 12);
        refs.roundInfoRunInfoButton = CreateSecondaryButton(tabRow, "RoundInfoRunInfoButton", "Run Info", 36f, fontAsset, 12);
        CreateDivider(tabsArea);

        RectTransform content = CreateLayoutPanel("RoundInfoContentCard", tabsArea, GetSurfaceStyle(SurfaceRole.InsetCard).backgroundColor, 1f);
        ConfigureVerticalLayout(content, 12, 22, TextAnchor.UpperLeft);
        ApplySurfaceStyle(content.gameObject, SurfaceRole.InsetCard);
        content.gameObject.AddComponent<RectMask2D>();
        refs.roundInfoText = CreateText(content, "RoundInfoText", "Select a tab to view the current run info.", fontAsset, TextRole.Body, 180f, 1f);

        return panel.gameObject;
    }

    private static TMP_Text CreateCoinsHudText(Transform parent, TMP_FontAsset fontAsset)
    {
        TMP_Text text = CreateText(parent, "CoinsText", "Coins: -", fontAsset, TextRole.HudStat, RoundHudStatHeight, 0f, AccentTextColor);
        ConfigureFixedWidth(text.gameObject, RoundHudStatWidth);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 22f;
        return text;
    }

    private static TMP_Text CreateScoreHudText(Transform parent, TMP_FontAsset fontAsset)
    {
        TMP_Text text = CreateText(parent, "ScoreText", "Score: -", fontAsset, TextRole.HudStat, RoundHudStatHeight);
        ConfigureFixedWidth(text.gameObject, RoundHudStatWidth);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 22f;
        return text;
    }

    private static void AssignControllerReferences(PrototypeGameFlowController controller, DiceManager diceManager, UiRefs refs)
    {
        SerializedObject serializedController = new SerializedObject(controller);
        SetObjectReference(serializedController, "canvas", refs.canvas);
        SetObjectReference(serializedController, "diceManager", diceManager);
        SetObjectReference(serializedController, "eventSystem", refs.eventSystem);
        SetObjectReference(serializedController, "rollText", refs.rollText);
        SetObjectReference(serializedController, "scoreText", refs.scoreText);
        SetObjectReference(serializedController, "coinsText", refs.coinsText);
        SetObjectReference(serializedController, "shardsText", refs.shardsText);
        SetObjectReference(serializedController, "roundInfoPanel", refs.roundInfoPanel);
        SetObjectReference(serializedController, "roundInfoText", refs.roundInfoText);
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
        SetObjectReference(serializedController, "shopResetTabButton", refs.shopResetTabButton);
        SetObjectReference(serializedController, "shopContextText", refs.shopContextText);
        SetObjectReference(serializedController, "shopSectionTitleText", refs.shopSectionTitleText);
        SetObjectReference(serializedController, "shopContentText", refs.shopContentText);
        SetObjectReference(serializedController, "shopTreeRoot", refs.shopTreeRoot);
        SetObjectReference(serializedController, "shopTooltipPresenter", refs.shopTooltipPresenter);
        SetObjectReference(serializedController, "challengesBackButton", refs.challengesBackButton);
        SetObjectReference(serializedController, "challengesContentText", refs.challengesContentText);
        SetObjectReference(serializedController, "settingsBackButton", refs.settingsBackButton);
        SetObjectReference(serializedController, "settingsContentText", refs.settingsContentText);
        SetObjectReference(serializedController, "canvasScaler", refs.canvasScaler);
        SetObjectReference(serializedController, "roundInfoTitleText", refs.roundInfoTitleText);
        SetObjectReference(serializedController, "roundInfoScoringButton", refs.roundInfoScoringButton);
        SetObjectReference(serializedController, "roundInfoCoinsButton", refs.roundInfoCoinsButton);
        SetObjectReference(serializedController, "roundInfoActiveEffectsButton", refs.roundInfoActiveEffectsButton);
        SetObjectReference(serializedController, "roundInfoRunInfoButton", refs.roundInfoRunInfoButton);
        SetObjectReference(serializedController, "rollKeyValueText", refs.rollKeyValueText);
        SetObjectReference(serializedController, "rollKeyRebindButton", refs.rollKeyRebindButton);
        SetObjectReference(serializedController, "backKeyValueText", refs.backKeyValueText);
        SetObjectReference(serializedController, "backKeyRebindButton", refs.backKeyRebindButton);
        SetObjectReference(serializedController, "resolutionDropdown", refs.resolutionDropdown);
        SetObjectReference(serializedController, "displayModeDropdown", refs.displayModeDropdown);
        SetObjectReference(serializedController, "vSyncToggle", refs.vSyncToggle);
        SetObjectReference(serializedController, "masterVolumeSlider", refs.masterVolumeSlider);
        SetObjectReference(serializedController, "masterVolumeValueText", refs.masterVolumeValueText);
        SetObjectReference(serializedController, "musicVolumeSlider", refs.musicVolumeSlider);
        SetObjectReference(serializedController, "musicVolumeValueText", refs.musicVolumeValueText);
        SetObjectReference(serializedController, "sfxVolumeSlider", refs.sfxVolumeSlider);
        SetObjectReference(serializedController, "sfxVolumeValueText", refs.sfxVolumeValueText);
        SetObjectReference(serializedController, "uiScaleSlider", refs.uiScaleSlider);
        SetObjectReference(serializedController, "uiScaleValueText", refs.uiScaleValueText);
        SetObjectReference(serializedController, "detailedRollBreakdownToggle", refs.detailedRollBreakdownToggle);
        SetObjectReference(serializedController, "roundChallengesButton", refs.roundChallengesButton);
        SetObjectReference(serializedController, "roundSettingsButton", refs.roundSettingsButton);
        SetObjectReference(serializedController, "roundSpendCoinsButton", refs.roundSpendCoinsButton);
        SetObjectReference(serializedController, "roundCashOutButton", refs.roundCashOutButton);
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

    private static RectTransform CreateResponsiveCard(string name, Transform parent, Color backgroundColor)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = backgroundColor;
        return rect;
    }

    private static void ConfigureResponsiveAspectCard(
        RectTransform rect,
        float widthPercent,
        float maxHeightPercent,
        float aspectRatio,
        float minWidth,
        float maxWidth)
    {
        if (rect == null)
        {
            return;
        }

        AspectRatioFitter fitter = rect.GetComponent<AspectRatioFitter>();
        if (fitter != null)
        {
            Object.DestroyImmediate(fitter);
        }

        ResponsiveAspectCard responsiveCard = rect.GetComponent<ResponsiveAspectCard>();
        if (responsiveCard == null)
        {
            responsiveCard = rect.gameObject.AddComponent<ResponsiveAspectCard>();
        }

        responsiveCard.Configure(widthPercent, maxHeightPercent, aspectRatio, minWidth, maxWidth);
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

    private static RectTransform CreateLayoutContainer(string name, Transform parent, float flexHeight)
    {
        RectTransform rect = CreateRect(name, parent);

        LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 160f;
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

    private static RectTransform CreateScrollContentArea(string name, Transform parent)
    {
        RectTransform scrollView = CreateRect(name, parent);
        LayoutElement scrollLayout = scrollView.gameObject.AddComponent<LayoutElement>();
        scrollLayout.minHeight = 320f;
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.flexibleWidth = 1f;

        Image scrollImage = scrollView.gameObject.AddComponent<Image>();
        scrollImage.color = new Color(1f, 1f, 1f, 0.015f);

        ScrollRect scrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.12f;

        RectTransform viewport = CreateStretchRect("Viewport", scrollView);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(0f, 0f);
        content.offsetMax = new Vector2(0f, 0f);
        ConfigureVerticalLayout(content, 14, 0, TextAnchor.UpperLeft);

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        return content;
    }

    private static RectTransform CreateSettingsSection(Transform parent, string name, string title, TMP_FontAsset fontAsset)
    {
        RectTransform section = CreateLayoutPanel(name, parent, new Color(1f, 1f, 1f, 0.028f), 0f);
        ConfigureVerticalLayout(section, 10, 16, TextAnchor.UpperLeft);
        AddOutline(section.gameObject, new Color(0.36f, 0.46f, 0.58f, 0.12f), new Vector2(1f, -1f));
        CreateText(section, "SettingsSectionTitleText", title, fontAsset, TextRole.CardTitle, 30f);
        return section;
    }

    private static void CreateSettingsRebindRow(
        Transform parent,
        string label,
        string valueName,
        out TMP_Text valueText,
        string buttonName,
        out Button button,
        TMP_FontAsset fontAsset)
    {
        RectTransform row = CreateSettingsRow(parent, $"{buttonName}Row");
        CreateSettingsFieldLabel(row, label, fontAsset);
        valueText = CreateSettingsValueText(row, valueName, "Space", fontAsset, 120f);
        button = CreateSecondaryButton(row, buttonName, "Rebind", 48f, fontAsset, 22);
        ConfigureFixedWidth(button.gameObject, 130f);
    }

    private static void CreateSettingsDropdownRow(
        Transform parent,
        string label,
        string dropdownName,
        out TMP_Dropdown dropdown,
        TMP_FontAsset fontAsset)
    {
        RectTransform row = CreateSettingsRow(parent, $"{dropdownName}Row");
        CreateSettingsFieldLabel(row, label, fontAsset);
        dropdown = CreateSettingsDropdown(row, dropdownName, fontAsset);
    }

    private static void CreateSettingsToggleRow(
        Transform parent,
        string label,
        string toggleName,
        out Toggle toggle,
        TMP_FontAsset fontAsset)
    {
        RectTransform row = CreateSettingsRow(parent, $"{toggleName}Row");
        CreateSettingsFieldLabel(row, label, fontAsset);
        toggle = CreateSettingsToggle(row, toggleName);
    }

    private static void CreateSettingsSliderRow(
        Transform parent,
        string label,
        string sliderName,
        out Slider slider,
        string valueName,
        out TMP_Text valueText,
        TMP_FontAsset fontAsset,
        float minValue = 0f,
        float maxValue = 1f)
    {
        RectTransform row = CreateSettingsRow(parent, $"{sliderName}Row");
        CreateSettingsFieldLabel(row, label, fontAsset);
        slider = CreateSettingsSlider(row, sliderName, minValue, maxValue);
        valueText = CreateSettingsValueText(row, valueName, "100%", fontAsset, 84f);
    }

    private static UiTooltipPresenter CreateShopTooltip(Transform parent, TMP_FontAsset fontAsset)
    {
        RectTransform layer = CreateStretchRect("ShopTooltipLayer", parent);
        LayoutElement layerLayout = layer.gameObject.AddComponent<LayoutElement>();
        layerLayout.ignoreLayout = true;

        RectTransform panel = CreateRect("ShopTooltipPanel", layer);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(500f, 280f);

        LayoutElement panelLayout = panel.gameObject.AddComponent<LayoutElement>();
        panelLayout.minWidth = 500f;
        panelLayout.preferredWidth = 500f;
        panelLayout.minHeight = 280f;
        panelLayout.preferredHeight = 280f;

        CanvasGroup canvasGroup = layer.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Image background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

        Shadow shadow = panel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.26f);
        shadow.effectDistance = new Vector2(0f, -4f);
        shadow.useGraphicAlpha = true;

        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.44f, 0.58f, 0.75f, 0.18f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;

        ConfigureVerticalLayout(panel, 10, 20, TextAnchor.UpperLeft);

        TMP_Text title = CreateText(panel, "ShopTooltipTitleText", "Tooltip", fontAsset, TextRole.CardTitle, 38f);
        TMP_Text body = CreateText(panel, "ShopTooltipBodyText", "Tooltip body", fontAsset, TextRole.Body, 150f, 1f);
        if (body != null)
        {
            body.fontSize = 19;
            body.lineSpacing = 1.06f;
        }

        UiTooltipPresenter presenter = panel.gameObject.AddComponent<UiTooltipPresenter>();
        presenter.Configure(layer, panel, title, body);
        layer.gameObject.SetActive(false);
        return presenter;
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

    private static RectTransform CreateSettingsRow(Transform parent, string name)
    {
        RectTransform row = CreateRect(name, parent);
        ConfigureHorizontalRowLayout(row, 14, 0);

        LayoutElement layoutElement = row.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 56f;
        layoutElement.preferredHeight = 56f;
        layoutElement.flexibleWidth = 1f;
        return row;
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

    private static void ConfigureHorizontalRowLayout(RectTransform rect, int spacing, int padding)
    {
        HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
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

        if (role == TextRole.HudStat)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 22f;
        }

        return text;
    }

    private static void ConfigureFlexibleTextLayout(TMP_Text text, float minimumHeight)
    {
        if (text == null)
        {
            return;
        }

        LayoutElement layoutElement = text.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            return;
        }

        layoutElement.minHeight = minimumHeight;
        layoutElement.preferredHeight = -1f;
        layoutElement.flexibleHeight = 1f;
    }

    private static TMP_Text CreateSettingsFieldLabel(Transform parent, string content, TMP_FontAsset fontAsset)
    {
        TMP_Text label = CreateText(parent, "SettingsFieldLabelText", content, fontAsset, TextRole.Body, 34f);
        ConfigureFixedWidth(label.gameObject, 280f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.fontSize = 24f;
        return label;
    }

    private static TMP_Text CreateSettingsValueText(Transform parent, string name, string content, TMP_FontAsset fontAsset, float width)
    {
        TMP_Text valueText = CreateText(parent, name, content, fontAsset, TextRole.Subtitle, 34f, 0f, AccentTextColor);
        ConfigureFixedWidth(valueText.gameObject, width);
        valueText.name = name;
        valueText.alignment = TextAlignmentOptions.MidlineRight;
        valueText.fontSize = 22f;
        return valueText;
    }

    private static TMP_Dropdown CreateSettingsDropdown(Transform parent, string name, TMP_FontAsset fontAsset)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = SecondaryButtonColor;

        TMP_Dropdown dropdown = rect.gameObject.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = image;
        ApplyButtonStyleGraphic(rect.gameObject, image, ButtonRole.Secondary);

        LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 48f;
        layoutElement.preferredHeight = 48f;
        layoutElement.preferredWidth = 340f;
        layoutElement.flexibleWidth = 1f;

        RectTransform labelRect = CreateRect("Label", rect);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(16f, 8f);
        labelRect.offsetMax = new Vector2(-40f, -8f);
        TextMeshProUGUI labelText = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(labelText, fontAsset, TextRole.Body, null, 22);
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.text = "Option";

        RectTransform arrowRect = CreateRect("Arrow", rect);
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-14f, 0f);
        arrowRect.sizeDelta = new Vector2(22f, 22f);
        TextMeshProUGUI arrowText = arrowRect.gameObject.AddComponent<TextMeshProUGUI>();
        arrowText.name = "DropdownArrowText";
        ApplyTextStyle(arrowText, fontAsset, TextRole.ButtonLabel, null, 20);
        arrowText.text = "v";
        arrowText.alignment = TextAlignmentOptions.Center;

        RectTransform template = CreateRect("Template", rect);
        template.anchorMin = new Vector2(0f, 0f);
        template.anchorMax = new Vector2(1f, 0f);
        template.pivot = new Vector2(0.5f, 1f);
        template.anchoredPosition = new Vector2(0f, -4f);
        template.sizeDelta = new Vector2(0f, 180f);
        template.gameObject.SetActive(false);

        Image templateImage = template.gameObject.AddComponent<Image>();
        templateImage.color = new Color(0.11f, 0.14f, 0.19f, 0.98f);
        ScrollRect scrollRect = template.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        RectTransform viewport = CreateStretchRect("Viewport", template);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        ConfigureVerticalLayout(content, 0, 0, TextAnchor.UpperLeft);
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform item = CreateRect("Item", content);
        Image itemImage = item.gameObject.AddComponent<Image>();
        itemImage.color = new Color(0.14f, 0.18f, 0.24f, 1f);
        Toggle toggle = item.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = itemImage;
        LayoutElement itemLayout = item.gameObject.AddComponent<LayoutElement>();
        itemLayout.minHeight = 42f;
        itemLayout.preferredHeight = 42f;
        itemLayout.flexibleWidth = 1f;

        RectTransform checkmark = CreateRect("Checkmark", item);
        checkmark.anchorMin = new Vector2(0f, 0.5f);
        checkmark.anchorMax = new Vector2(0f, 0.5f);
        checkmark.pivot = new Vector2(0.5f, 0.5f);
        checkmark.anchoredPosition = new Vector2(16f, 0f);
        checkmark.sizeDelta = new Vector2(16f, 16f);
        Image checkmarkImage = checkmark.gameObject.AddComponent<Image>();
        checkmarkImage.color = AccentTextColor;
        toggle.graphic = checkmarkImage;

        RectTransform itemLabelRect = CreateRect("Item Label", item);
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(36f, 6f);
        itemLabelRect.offsetMax = new Vector2(-12f, -6f);
        TextMeshProUGUI itemLabel = itemLabelRect.gameObject.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(itemLabel, fontAsset, TextRole.Body, null, 20);
        itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
        itemLabel.text = "Option";

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        dropdown.template = template;
        dropdown.captionText = labelText;
        dropdown.itemText = itemLabel;
        dropdown.alphaFadeSpeed = 0.12f;
        return dropdown;
    }

    private static Toggle CreateSettingsToggle(Transform parent, string name)
    {
        RectTransform rect = CreateRect(name, parent);
        LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        layoutElement.minWidth = 54f;
        layoutElement.preferredWidth = 54f;
        layoutElement.minHeight = 48f;
        layoutElement.preferredHeight = 48f;
        layoutElement.flexibleWidth = 0f;

        Toggle toggle = rect.gameObject.AddComponent<Toggle>();

        RectTransform background = CreateRect("Background", rect);
        background.anchorMin = new Vector2(0.5f, 0.5f);
        background.anchorMax = new Vector2(0.5f, 0.5f);
        background.pivot = new Vector2(0.5f, 0.5f);
        background.sizeDelta = new Vector2(28f, 28f);
        Image backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.color = SecondaryButtonColor;
        AddOutline(background.gameObject, new Color(0.49f, 0.58f, 0.68f, 0.18f), new Vector2(1f, -1f));

        RectTransform checkmark = CreateRect("Checkmark", background);
        checkmark.anchorMin = Vector2.zero;
        checkmark.anchorMax = Vector2.one;
        checkmark.offsetMin = new Vector2(5f, 5f);
        checkmark.offsetMax = new Vector2(-5f, -5f);
        Image checkmarkImage = checkmark.gameObject.AddComponent<Image>();
        checkmarkImage.color = AccentTextColor;

        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkmarkImage;
        return toggle;
    }

    private static Slider CreateSettingsSlider(Transform parent, string name, float minValue, float maxValue)
    {
        RectTransform rect = CreateRect(name, parent);
        LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 40f;
        layoutElement.preferredHeight = 40f;
        layoutElement.preferredWidth = 320f;
        layoutElement.flexibleWidth = 1f;

        Slider slider = rect.gameObject.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;

        RectTransform background = CreateRect("Background", rect);
        background.anchorMin = new Vector2(0f, 0.5f);
        background.anchorMax = new Vector2(1f, 0.5f);
        background.pivot = new Vector2(0.5f, 0.5f);
        background.offsetMin = new Vector2(0f, -5f);
        background.offsetMax = new Vector2(0f, 5f);
        Image backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.color = new Color(0.19f, 0.24f, 0.30f, 1f);

        RectTransform fillArea = CreateRect("Fill Area", rect);
        fillArea.anchorMin = new Vector2(0f, 0f);
        fillArea.anchorMax = new Vector2(1f, 1f);
        fillArea.offsetMin = new Vector2(0f, 0f);
        fillArea.offsetMax = new Vector2(0f, 0f);

        RectTransform fill = CreateRect("Fill", fillArea);
        fill.anchorMin = new Vector2(0f, 0.5f);
        fill.anchorMax = new Vector2(1f, 0.5f);
        fill.pivot = new Vector2(0f, 0.5f);
        fill.offsetMin = new Vector2(0f, -5f);
        fill.offsetMax = new Vector2(0f, 5f);
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = AccentTextColor;

        RectTransform handleSlideArea = CreateRect("Handle Slide Area", rect);
        handleSlideArea.anchorMin = Vector2.zero;
        handleSlideArea.anchorMax = Vector2.one;
        handleSlideArea.offsetMin = Vector2.zero;
        handleSlideArea.offsetMax = Vector2.zero;

        RectTransform handle = CreateRect("Handle", handleSlideArea);
        handle.anchorMin = new Vector2(0f, 0.5f);
        handle.anchorMax = new Vector2(0f, 0.5f);
        handle.pivot = new Vector2(0.5f, 0.5f);
        handle.sizeDelta = new Vector2(20f, 20f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = Color.white;
        AddShadow(handle.gameObject, new Color(0f, 0f, 0f, 0.16f), new Vector2(0f, -2f));

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        return slider;
    }

    private static void ConfigureFixedWidth(GameObject target, float width)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = target.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = width;
        layoutElement.preferredWidth = width;
        layoutElement.flexibleWidth = 0f;
    }

    private static Button CreatePrimaryButton(Transform parent, string name, string label, TMP_FontAsset fontAsset)
    {
        return CreateButton(parent, name, label, fontAsset, ButtonRole.Primary, MainButtonHeight, ButtonFontSize, TextRole.ButtonLabel);
    }

    private static Button CreatePrimaryButton(Transform parent, string name, string label, float height, TMP_FontAsset fontAsset, int labelFontSize, TextRole textRole)
    {
        return CreateButton(parent, name, label, fontAsset, ButtonRole.Primary, height, labelFontSize, textRole);
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
        if (textRole == TextRole.UtilityLabel)
        {
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 18f;
            labelText.fontSizeMax = labelFontSize;
        }
        else if (textRole == TextRole.HudStat)
        {
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 18f;
            labelText.fontSizeMax = 22f;
        }
        labelText.raycastTarget = false;

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

    private static void ApplyButtonStyleGraphic(GameObject target, Image image, ButtonRole role)
    {
        if (target == null || image == null)
        {
            return;
        }

        ButtonStyleDefinition style = GetButtonStyle(role);
        image.color = style.backgroundColor;
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

        if (role == TextRole.HudStat)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 22f;
        }
        else if (role == TextRole.UtilityLabel)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = style.fontSize;
        }

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
                return new TextStyleDefinition(24, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.96f, 0.97f, 0.98f, 1f), TextOverflowModes.Overflow, TextWrappingModes.NoWrap, true, 1f, true, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -1f));
            case TextRole.HudResource:
                return new TextStyleDefinition(28, FontStyles.Bold, TextAlignmentOptions.TopLeft, BodyTextColor, TextOverflowModes.Overflow, TextWrappingModes.Normal, true, 1f, true, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -1f));
            case TextRole.HudScore:
                return new TextStyleDefinition(40, FontStyles.Bold, TextAlignmentOptions.Center, TitleColor, TextOverflowModes.Overflow, TextWrappingModes.Normal, true, 1.02f, true, new Color(0f, 0f, 0f, 0.24f), new Vector2(0f, -1f));
            case TextRole.HudStat:
                return new TextStyleDefinition(22, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, BodyTextColor, TextOverflowModes.Overflow, TextWrappingModes.NoWrap, true, 1f, true, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -1f));
            case TextRole.CompactCardTitle:
                return new TextStyleDefinition(30, FontStyles.Bold, TextAlignmentOptions.TopLeft, TitleColor, TextOverflowModes.Overflow, TextWrappingModes.NoWrap, true, 1f, true, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -1f));
            case TextRole.RollResult:
                return new TextStyleDefinition(23, FontStyles.Bold, TextAlignmentOptions.Top, Color.white, TextOverflowModes.Overflow, TextWrappingModes.Normal, true, 1.04f, true, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -1f));
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
