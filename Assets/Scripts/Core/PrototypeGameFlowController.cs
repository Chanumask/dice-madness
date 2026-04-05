using System.Collections.Generic;
using DiceMadness.Dice;
using DiceMadness.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace DiceMadness.Core
{
    [DefaultExecutionOrder(-150)]
    public class PrototypeGameFlowController : MonoBehaviour
    {
        private const string ShardsPlayerPrefsKey = "DiceMadness.Shards";
        private const string ShopUpgradeUnlockedKeyPrefix = "DiceMadness.ShopUpgrade.";

        private static readonly string[] ChallengeLines =
        {
            "Roll three even results in one round.",
            "Finish a round with a total of 15 or more.",
            "Win a future challenge run without buying an upgrade.",
        };

        private enum ViewState
        {
            MainMenu,
            Shop,
            Challenges,
            Settings,
            Round,
            RoundChallenges,
            RoundSettings,
        }

        private enum ShopSection
        {
            DiceUnlocks,
            Efficiency,
            ScoreMultipliers,
        }

        private enum ShopNodeState
        {
            Locked,
            Available,
            InsufficientShards,
            Unlocked,
        }

        private enum RoundInfoTab
        {
            Scoring,
            Coins,
            ActiveEffects,
            RunInfo,
        }

        private enum PendingRebindAction
        {
            None,
            Roll,
            Back,
        }

        private sealed class ShopUpgradeDefinition
        {
            public string id;
            public string title;
            public string description;
            public string effectText;
            public string iconLabel;
            public int shardCost;
            public ShopSection section;
            public string[] prerequisites;
            public int depth;
            public int order;
        }

        private sealed class ShopSectionDefinition
        {
            public string title;
            public string summary;
            public List<ShopUpgradeDefinition> upgrades;
        }

        [System.Serializable]
        private struct ScoreSettings
        {
            public int pointsPerPip;
            public int pairBonus;
            public int tripleBonus;
            public int straightBonus;
            public int highFaceBonusPerDie;
            public float scoreMultiplier;
        }

        [System.Serializable]
        private struct CoinSettings
        {
            public int startingCoins;
            public int baseCoinReward;
            public int minimumCoinsPerRoll;
            public int highFaceBonusPerDie;
            public int pairBonus;
            public int tripleBonus;
            public int straightBonus;
            public float coinRewardMultiplier;
            public int spendCoinsCost;
        }

        [System.Serializable]
        private struct ShardSettings
        {
            public float highestScoreFactor;
            public float totalCoinsEarnedFactor;
            public int baseCashOutReward;
        }

        [Header("Scene References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private DiceManager diceManager;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private TMP_Text rollText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text shardsText;
        [SerializeField] private GameObject roundInfoPanel;
        [SerializeField] private TMP_Text roundInfoText;

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject challengesPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject roundUtilityBar;

        [Header("Round Info")]
        [SerializeField] private TMP_Text roundInfoTitleText;
        [SerializeField] private Button roundInfoScoringButton;
        [SerializeField] private Button roundInfoCoinsButton;
        [SerializeField] private Button roundInfoActiveEffectsButton;
        [SerializeField] private Button roundInfoRunInfoButton;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button mainMenuShopButton;
        [SerializeField] private Button mainMenuChallengesButton;
        [SerializeField] private Button mainMenuSettingsButton;

        [Header("Shop")]
        [SerializeField] private float shopNodeWidth = 185f;
        [SerializeField] private Button shopDiceUnlocksButton;
        [SerializeField] private Button shopEfficiencyButton;
        [SerializeField] private Button shopScoreMultipliersButton;
        [SerializeField] private Button shopReturnButton;
        [SerializeField] private Button shopResetTabButton;
        [SerializeField] private TMP_Text shopContextText;
        [SerializeField] private TMP_Text shopSectionTitleText;
        [SerializeField] private TMP_Text shopContentText;
        [SerializeField] private RectTransform shopTreeRoot;
        [SerializeField] private UiTooltipPresenter shopTooltipPresenter;

        [Header("Challenges")]
        [SerializeField] private Button challengesBackButton;
        [SerializeField] private TMP_Text challengesContentText;

        [Header("Settings")]
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private TMP_Text settingsContentText;
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private TMP_Text rollKeyValueText;
        [SerializeField] private Button rollKeyRebindButton;
        [SerializeField] private TMP_Text backKeyValueText;
        [SerializeField] private Button backKeyRebindButton;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown displayModeDropdown;
        [SerializeField] private Toggle vSyncToggle;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeValueText;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private TMP_Text musicVolumeValueText;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Text sfxVolumeValueText;
        [SerializeField] private Slider uiScaleSlider;
        [SerializeField] private TMP_Text uiScaleValueText;
        [SerializeField] private Toggle detailedRollBreakdownToggle;

        [Header("In-Round Utility")]
        [SerializeField] private Button roundChallengesButton;
        [SerializeField] private Button roundSettingsButton;
        [SerializeField] private Button roundSpendCoinsButton;
        [SerializeField] private Button roundCashOutButton;
        [SerializeField] private Button roundMainMenuButton;

        [Header("Score")]
        [SerializeField] private ScoreSettings scoreSettings = new ScoreSettings
        {
            pointsPerPip = 12,
            pairBonus = 24,
            tripleBonus = 90,
            straightBonus = 48,
            highFaceBonusPerDie = 8,
            scoreMultiplier = 1f,
        };

        [Header("Run Coins")]
        [SerializeField] private CoinSettings coinSettings = new CoinSettings
        {
            startingCoins = 12,
            baseCoinReward = 2,
            minimumCoinsPerRoll = 1,
            highFaceBonusPerDie = 1,
            pairBonus = 2,
            tripleBonus = 5,
            straightBonus = 4,
            coinRewardMultiplier = 1f,
            spendCoinsCost = 3,
        };

        [Header("Shards")]
        [SerializeField] private ShardSettings shardSettings = new ShardSettings
        {
            highestScoreFactor = 0.08f,
            totalCoinsEarnedFactor = 0.35f,
            baseCashOutReward = 1,
        };

        [Header("Debug")]
        [SerializeField] private bool debugEconomyLogs = true;

        private static readonly Color ShopNodeUnlockedColor = new Color(0.16f, 0.36f, 0.26f, 0.98f);
        private static readonly Color ShopNodeAvailableColor = new Color(0.18f, 0.32f, 0.52f, 0.98f);
        private static readonly Color ShopNodeInsufficientColor = new Color(0.36f, 0.27f, 0.16f, 0.98f);
        private static readonly Color ShopNodeLockedColor = new Color(0.10f, 0.13f, 0.18f, 0.98f);
        private static readonly Color ShopNodeOutlineColor = new Color(0.45f, 0.57f, 0.72f, 0.18f);
        private static readonly Color ShopNodeUnlockedOutlineColor = new Color(0.50f, 0.82f, 0.64f, 0.28f);
        private static readonly Color ShopNodeTitleColor = new Color(0.98f, 0.99f, 1f, 1f);
        private static readonly Color ShopNodeBodyColor = new Color(0.86f, 0.90f, 0.95f, 1f);
        private static readonly Color ShopNodeMutedColor = new Color(0.63f, 0.70f, 0.79f, 1f);
        private static readonly Color ShopNodeAccentColor = new Color(0.74f, 0.84f, 0.98f, 1f);
        private static readonly Color ShopNodeIconColor = new Color(0.18f, 0.23f, 0.32f, 1f);

        private readonly List<GameObject> roundRoots = new List<GameObject>();
        private readonly Dictionary<ShopSection, ShopSectionDefinition> shopSections = new Dictionary<ShopSection, ShopSectionDefinition>();
        private readonly Dictionary<string, ShopUpgradeDefinition> shopUpgradesById = new Dictionary<string, ShopUpgradeDefinition>();
        private readonly HashSet<string> unlockedUpgradeIds = new HashSet<string>();
        private ViewState currentState;
        private ShopSection currentShopSection = ShopSection.DiceUnlocks;
        private bool listenersBound;
        private bool shopResetConfirmationArmed;
        private ShopSection shopResetConfirmationSection;
        private bool settingsListenersBound;
        private bool suppressSettingsUiCallbacks;
        private PendingRebindAction pendingRebindAction;
        private int pendingRebindStartedFrame = -1;
        private RoundInfoTab currentRoundInfoTab = RoundInfoTab.Scoring;
        private bool roundInfoUiBuilt;
        private bool settingsUiBuilt;
        private RectTransform settingsScrollContentRoot;
        private bool runActive;
        private int runCoins;
        private int currentScore;
        private int highestScoreReached;
        private int totalCoinsEarned;
        private int completedRolls;
        private int shards;

        private void Reset()
        {
            AutoWireSceneReferences();
        }

        private void Awake()
        {
            AutoWireSceneReferences();
            InitializeShopData();
            GameSettingsService.EnsureLoaded();
            EnsureEventSystem();
            DiscoverRoundRoots();
            BindButtonListeners();
            EnsureSettingsUiBuilt();
            BindSettingsListeners();
            EnsureRoundInfoUiBuilt();
            LoadShards();
            LoadUnlockedUpgrades();
            PopulateStaticPanelText();
            PushRollTextReference();
            ApplyRunHudLayout();
            GameSettingsService.SettingsChanged -= HandleSettingsChanged;
            GameSettingsService.SettingsChanged += HandleSettingsChanged;
            GameSettingsService.ApplyToCanvasScaler(canvasScaler);
            RefreshShardsText();
            RefreshRoundInfoPanelText();
            RefreshRunHud();
            RefreshSettingsUi();

            if (diceManager != null)
            {
                diceManager.RollResolved -= HandleRollResolved;
                diceManager.RollResolved += HandleRollResolved;
            }
        }

        private void Start()
        {
            if (!HasRequiredReferences())
            {
                enabled = false;
                return;
            }

            EnterMainMenu();
        }

        private void Update()
        {
            if (pendingRebindAction != PendingRebindAction.None)
            {
                PollPendingRebind();
                return;
            }

            if (WasBackPressed())
            {
                HandleBackAction();
            }

            RefreshRoundActionInteractivity();
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            GameSettingsService.SettingsChanged -= HandleSettingsChanged;

            if (diceManager != null)
            {
                diceManager.RollResolved -= HandleRollResolved;
            }
        }

        [ContextMenu("Auto Wire Scene References")]
        private void AutoWireSceneReferences()
        {
            canvas ??= FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            diceManager ??= FindAnyObjectByType<DiceManager>(FindObjectsInactive.Include);
            eventSystem ??= FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
            canvasScaler ??= canvas != null ? canvas.GetComponent<CanvasScaler>() : null;

            Transform uiRoot = canvas != null ? FindChildTransform(canvas.transform, "MenuUIRoot") : null;
            Transform searchRoot = uiRoot != null ? uiRoot : canvas != null ? canvas.transform : null;

            if (searchRoot == null)
            {
                return;
            }

            rollText ??= FindChildComponent<TMP_Text>(searchRoot, "RollText");
            scoreText ??= FindChildComponent<TMP_Text>(searchRoot, "ScoreText");
            coinsText ??= FindChildComponent<TMP_Text>(searchRoot, "CoinsText");
            shardsText ??= FindChildComponent<TMP_Text>(searchRoot, "ShardsText");
            roundInfoPanel ??= FindChildGameObject(searchRoot, "RoundInfoPanel");
            roundInfoTitleText ??= FindChildComponent<TMP_Text>(searchRoot, "RoundInfoTitleText");
            roundInfoText ??= FindChildComponent<TMP_Text>(searchRoot, "RoundInfoText");
            roundInfoScoringButton ??= FindChildComponent<Button>(searchRoot, "RoundInfoScoringButton");
            roundInfoCoinsButton ??= FindChildComponent<Button>(searchRoot, "RoundInfoCoinsButton");
            roundInfoActiveEffectsButton ??= FindChildComponent<Button>(searchRoot, "RoundInfoActiveEffectsButton");
            roundInfoRunInfoButton ??= FindChildComponent<Button>(searchRoot, "RoundInfoRunInfoButton");
            mainMenuPanel ??= FindChildGameObject(searchRoot, "MainMenuPanel");
            shopPanel ??= FindChildGameObject(searchRoot, "ShopPanel");
            challengesPanel ??= FindChildGameObject(searchRoot, "ChallengesPanel");
            settingsPanel ??= FindChildGameObject(searchRoot, "SettingsPanel");
            roundUtilityBar ??= FindChildGameObject(searchRoot, "RoundUtilityBar");

            playButton ??= FindChildComponent<Button>(searchRoot, "PlayButton");
            mainMenuShopButton ??= FindChildComponent<Button>(searchRoot, "ShopButton");
            mainMenuChallengesButton ??= FindChildComponent<Button>(searchRoot, "ChallengesButton");
            mainMenuSettingsButton ??= FindChildComponent<Button>(searchRoot, "SettingsButton");

            shopDiceUnlocksButton ??= FindChildComponent<Button>(searchRoot, "DiceUnlocksButton");
            shopEfficiencyButton ??= FindChildComponent<Button>(searchRoot, "EfficiencyButton");
            shopScoreMultipliersButton ??= FindChildComponent<Button>(searchRoot, "ScoreMultipliersButton");
            shopReturnButton ??= FindChildComponent<Button>(searchRoot, "ShopBackButton");
            shopResetTabButton ??= FindChildComponent<Button>(searchRoot, "ShopResetTabButton");
            shopContextText ??= FindChildComponent<TMP_Text>(searchRoot, "ShopContextText");
            shopSectionTitleText ??= FindChildComponent<TMP_Text>(searchRoot, "ShopSectionTitleText");
            shopContentText ??= FindChildComponent<TMP_Text>(searchRoot, "ShopContentText");
            shopTreeRoot ??= FindChildComponent<RectTransform>(searchRoot, "ShopTreeRoot");
            shopTooltipPresenter ??= FindChildComponent<UiTooltipPresenter>(searchRoot, "ShopTooltipPanel");

            challengesBackButton ??= FindChildComponent<Button>(searchRoot, "ChallengesBackButton");
            challengesContentText ??= FindChildComponent<TMP_Text>(searchRoot, "ChallengesContentText");

            settingsBackButton ??= FindChildComponent<Button>(searchRoot, "SettingsBackButton");
            settingsContentText ??= FindChildComponent<TMP_Text>(searchRoot, "SettingsContentText");
            rollKeyValueText ??= FindChildComponent<TMP_Text>(searchRoot, "RollKeyValueText");
            rollKeyRebindButton ??= FindChildComponent<Button>(searchRoot, "RollKeyRebindButton");
            backKeyValueText ??= FindChildComponent<TMP_Text>(searchRoot, "BackKeyValueText");
            backKeyRebindButton ??= FindChildComponent<Button>(searchRoot, "BackKeyRebindButton");
            resolutionDropdown ??= FindChildComponent<TMP_Dropdown>(searchRoot, "ResolutionDropdown");
            displayModeDropdown ??= FindChildComponent<TMP_Dropdown>(searchRoot, "DisplayModeDropdown");
            vSyncToggle ??= FindChildComponent<Toggle>(searchRoot, "VSyncToggle");
            masterVolumeSlider ??= FindChildComponent<Slider>(searchRoot, "MasterVolumeSlider");
            masterVolumeValueText ??= FindChildComponent<TMP_Text>(searchRoot, "MasterVolumeValueText");
            musicVolumeSlider ??= FindChildComponent<Slider>(searchRoot, "MusicVolumeSlider");
            musicVolumeValueText ??= FindChildComponent<TMP_Text>(searchRoot, "MusicVolumeValueText");
            sfxVolumeSlider ??= FindChildComponent<Slider>(searchRoot, "SfxVolumeSlider");
            sfxVolumeValueText ??= FindChildComponent<TMP_Text>(searchRoot, "SfxVolumeValueText");
            uiScaleSlider ??= FindChildComponent<Slider>(searchRoot, "UiScaleSlider");
            uiScaleValueText ??= FindChildComponent<TMP_Text>(searchRoot, "UiScaleValueText");
            detailedRollBreakdownToggle ??= FindChildComponent<Toggle>(searchRoot, "DetailedRollBreakdownToggle");

            roundChallengesButton ??= FindChildComponent<Button>(searchRoot, "RoundChallengesButton");
            roundSettingsButton ??= FindChildComponent<Button>(searchRoot, "RoundSettingsButton");
            roundSpendCoinsButton ??= FindChildComponent<Button>(searchRoot, "RoundSpendCoinsButton");
            roundCashOutButton ??= FindChildComponent<Button>(searchRoot, "RoundCashOutButton");
            roundMainMenuButton ??= FindChildComponent<Button>(searchRoot, "RoundMainMenuButton");

            NormalizeDropdownArrow(resolutionDropdown);
            NormalizeDropdownArrow(displayModeDropdown);
        }

        private void EnsureEventSystem()
        {
            if (eventSystem != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private void DiscoverRoundRoots()
        {
            roundRoots.Clear();
            GameObject[] roots = gameObject.scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];

                if (root == null ||
                    root == canvas?.gameObject ||
                    root == gameObject ||
                    root == eventSystem?.gameObject)
                {
                    continue;
                }

                Camera camera = root.GetComponent<Camera>();
                Light light = root.GetComponent<Light>();

                if (camera != null || light != null)
                {
                    continue;
                }

                roundRoots.Add(root);
            }
        }

        private void BindButtonListeners()
        {
            if (listenersBound)
            {
                return;
            }

            BindButton(playButton, StartRound);
            BindButton(mainMenuShopButton, EnterMetaShop);
            BindButton(mainMenuChallengesButton, EnterChallenges);
            BindButton(mainMenuSettingsButton, EnterMetaSettings);

            BindButton(shopDiceUnlocksButton, () => SetShopSection(ShopSection.DiceUnlocks));
            BindButton(shopEfficiencyButton, () => SetShopSection(ShopSection.Efficiency));
            BindButton(shopScoreMultipliersButton, () => SetShopSection(ShopSection.ScoreMultipliers));
            BindButton(shopResetTabButton, HandleResetCurrentShopTab);
            BindButton(shopReturnButton, EnterMainMenu);

            BindButton(challengesBackButton, HandleBackAction);
            BindButton(settingsBackButton, HandleBackAction);

            BindButton(roundChallengesButton, EnterRoundChallenges);
            BindButton(roundSettingsButton, EnterRoundSettings);
            BindButton(roundSpendCoinsButton, SpendCoinsPlaceholder);
            BindButton(roundCashOutButton, CashOutRun);
            BindButton(roundMainMenuButton, EnterMainMenu);

            BindRoundInfoTabButtons();

            listenersBound = true;
        }

        private void BindSettingsListeners()
        {
            if (settingsListenersBound)
            {
                return;
            }

            BindButton(rollKeyRebindButton, () => BeginRebind(PendingRebindAction.Roll));
            BindButton(backKeyRebindButton, () => BeginRebind(PendingRebindAction.Back));

            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.RemoveAllListeners();
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            if (displayModeDropdown != null)
            {
                displayModeDropdown.onValueChanged.RemoveAllListeners();
                displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);
            }

            if (vSyncToggle != null)
            {
                vSyncToggle.onValueChanged.RemoveAllListeners();
                vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveAllListeners();
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveAllListeners();
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveAllListeners();
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            if (uiScaleSlider != null)
            {
                uiScaleSlider.onValueChanged.RemoveAllListeners();
                uiScaleSlider.onValueChanged.AddListener(OnUiScaleChanged);
            }

            if (detailedRollBreakdownToggle != null)
            {
                detailedRollBreakdownToggle.onValueChanged.RemoveAllListeners();
                detailedRollBreakdownToggle.onValueChanged.AddListener(OnDetailedRollBreakdownChanged);
            }

            settingsListenersBound = true;
        }

        private void PopulateStaticPanelText()
        {
            RefreshShopSection(currentShopSection);

            if (challengesContentText != null)
            {
                challengesContentText.text = string.Join("\n\n", ChallengeLines);
            }

            if (roundInfoTitleText != null)
            {
                roundInfoTitleText.text = "Run Info";
            }
        }

        private void HandleSettingsChanged()
        {
            GameSettingsService.ApplyToCanvasScaler(canvasScaler);
            EnsureSettingsUiBuilt();
            RefreshSettingsUi();
            EnsureRoundInfoUiBuilt();
            RefreshRoundInfoPanelText();

            if (runActive && diceManager != null && !diceManager.IsRolling && completedRolls == 0)
            {
                ShowRunReadySummary();
            }
        }

        private void RefreshSettingsUi()
        {
            EnsureSettingsUiBuilt();
            GameSettingsData settings = GameSettingsService.Current;
            suppressSettingsUiCallbacks = true;

            try
            {
                if (rollKeyValueText != null)
                {
                    rollKeyValueText.text = pendingRebindAction == PendingRebindAction.Roll
                        ? "Press any key..."
                        : GameSettingsService.GetBindingDisplayName(settings.rollKey);
                }

                if (backKeyValueText != null)
                {
                    backKeyValueText.text = pendingRebindAction == PendingRebindAction.Back
                        ? "Press any key..."
                        : GameSettingsService.GetBindingDisplayName(settings.backKey);
                }

            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                List<string> options = new List<string>();
                IReadOnlyList<ResolutionOption> resolutions = GameSettingsService.AvailableResolutions;
                    for (int i = 0; i < resolutions.Count; i++)
                    {
                        options.Add(resolutions[i].ToString());
                    }

                resolutionDropdown.AddOptions(options);
                resolutionDropdown.SetValueWithoutNotify(GameSettingsService.GetSelectedResolutionIndex());
                NormalizeDropdownArrow(resolutionDropdown);
                resolutionDropdown.RefreshShownValue();
            }

            if (displayModeDropdown != null)
            {
                displayModeDropdown.ClearOptions();
                displayModeDropdown.AddOptions(new List<string> { "Windowed", "Borderless", "Fullscreen" });
                displayModeDropdown.SetValueWithoutNotify((int)settings.displayMode);
                NormalizeDropdownArrow(displayModeDropdown);
                displayModeDropdown.RefreshShownValue();
            }

                if (vSyncToggle != null)
                {
                    vSyncToggle.SetIsOnWithoutNotify(settings.vSyncEnabled);
                }

                if (masterVolumeSlider != null)
                {
                    masterVolumeSlider.SetValueWithoutNotify(settings.masterVolume);
                }

                if (musicVolumeSlider != null)
                {
                    musicVolumeSlider.SetValueWithoutNotify(settings.musicVolume);
                }

                if (sfxVolumeSlider != null)
                {
                    sfxVolumeSlider.SetValueWithoutNotify(settings.sfxVolume);
                }

                if (uiScaleSlider != null)
                {
                    uiScaleSlider.SetValueWithoutNotify(settings.uiScale);
                }

                if (detailedRollBreakdownToggle != null)
                {
                    detailedRollBreakdownToggle.SetIsOnWithoutNotify(settings.showDetailedRollBreakdown);
                }

                if (masterVolumeValueText != null)
                {
                    masterVolumeValueText.text = $"{Mathf.RoundToInt(settings.masterVolume * 100f)}%";
                }

                if (musicVolumeValueText != null)
                {
                    musicVolumeValueText.text = $"{Mathf.RoundToInt(settings.musicVolume * 100f)}%";
                }

                if (sfxVolumeValueText != null)
                {
                    sfxVolumeValueText.text = $"{Mathf.RoundToInt(settings.sfxVolume * 100f)}%";
                }

                if (uiScaleValueText != null)
                {
                    uiScaleValueText.text = $"{settings.uiScale:0.00}x";
                }

                if (settingsContentText != null)
                {
                    settingsContentText.text = "Settings apply immediately and are saved automatically.";
                }
            }
            finally
            {
                suppressSettingsUiCallbacks = false;
            }
        }

        private void PushRollTextReference()
        {
            if (diceManager != null && rollText != null)
            {
                diceManager.SetResultText(rollText);
            }
        }

        private void BeginRebind(PendingRebindAction action)
        {
            pendingRebindAction = action;
            pendingRebindStartedFrame = Time.frameCount;
            RefreshSettingsUi();
        }

        private void PollPendingRebind()
        {
            if (Time.frameCount <= pendingRebindStartedFrame)
            {
                return;
            }

            if (!GameSettingsService.TryCaptureNextBinding(out KeyCode key))
            {
                return;
            }

            switch (pendingRebindAction)
            {
                case PendingRebindAction.Roll:
                    GameSettingsService.SetRollKey(key);
                    break;
                case PendingRebindAction.Back:
                    GameSettingsService.SetBackKey(key);
                    break;
            }

            pendingRebindAction = PendingRebindAction.None;
            pendingRebindStartedFrame = -1;
            RefreshSettingsUi();
        }

        private void OnResolutionChanged(int selectedIndex)
        {
            if (suppressSettingsUiCallbacks)
            {
                return;
            }

            GameSettingsService.SetResolutionByIndex(selectedIndex);
        }

        private void OnDisplayModeChanged(int selectedIndex)
        {
            if (suppressSettingsUiCallbacks)
            {
                return;
            }

            GameSettingsService.SetDisplayMode((DisplayModeOption)Mathf.Clamp(selectedIndex, 0, 2));
        }

        private void OnVSyncChanged(bool enabled)
        {
            if (suppressSettingsUiCallbacks)
            {
                return;
            }

            GameSettingsService.SetVSync(enabled);
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (suppressSettingsUiCallbacks)
            {
                return;
            }

            GameSettingsService.SetMasterVolume(value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (suppressSettingsUiCallbacks)
            {
                return;
            }

            GameSettingsService.SetMusicVolume(value);
            RefreshSettingsUi();
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (suppressSettingsUiCallbacks)
            {
                return;
            }

            GameSettingsService.SetSfxVolume(value);
            RefreshSettingsUi();
        }

        private void OnUiScaleChanged(float value)
        {
            if (suppressSettingsUiCallbacks)
            {
                return;
            }

            GameSettingsService.SetUiScale(value);
        }

        private void OnDetailedRollBreakdownChanged(bool enabled)
        {
            if (suppressSettingsUiCallbacks)
            {
                return;
            }

            GameSettingsService.SetDetailedRollBreakdown(enabled);
        }

        private void EnsureSettingsUiBuilt()
        {
            if (settingsUiBuilt)
            {
                return;
            }

            if (settingsPanel == null)
            {
                return;
            }

            Transform settingsContentCard = FindChildTransform(settingsPanel.transform, "SettingsContentCard");
            if (settingsContentCard == null)
            {
                return;
            }

            if (rollKeyRebindButton == null ||
                backKeyRebindButton == null ||
                resolutionDropdown == null ||
                displayModeDropdown == null ||
                masterVolumeSlider == null ||
                musicVolumeSlider == null ||
                sfxVolumeSlider == null ||
                uiScaleSlider == null)
            {
                BuildSettingsUiRuntime(settingsContentCard);
            }

            settingsUiBuilt = true;
        }

        private void BuildSettingsUiRuntime(Transform settingsContentCard)
        {
            if (settingsContentCard == null)
            {
                return;
            }

            if (settingsContentText == null)
            {
                settingsContentText = FindChildComponent<TMP_Text>(settingsContentCard, "SettingsContentText");
            }

            if (settingsContentText != null)
            {
                settingsContentText.text = "Settings apply immediately and are saved automatically.";
            }

            settingsScrollContentRoot = FindChildTransform(settingsContentCard, "SettingsScrollContent") as RectTransform;
            if (settingsScrollContentRoot == null)
            {
                settingsScrollContentRoot = CreateRuntimeScrollContentArea(settingsContentCard, "SettingsScrollContent");
            }

            Transform controlsSection = CreateRuntimeSettingsSection(settingsScrollContentRoot, "SettingsControlsSection", "Controls");
            CreateRuntimeRebindRow(controlsSection, "Roll Dice", "RollKeyValueText", "RollKeyRebindButton", out rollKeyValueText, out rollKeyRebindButton);
            CreateRuntimeRebindRow(controlsSection, "Back / Close", "BackKeyValueText", "BackKeyRebindButton", out backKeyValueText, out backKeyRebindButton);

            Transform videoSection = CreateRuntimeSettingsSection(settingsScrollContentRoot, "SettingsVideoSection", "Video");
            CreateRuntimeDropdownRow(videoSection, "Resolution", "ResolutionDropdown", out resolutionDropdown);
            CreateRuntimeDropdownRow(videoSection, "Display Mode", "DisplayModeDropdown", out displayModeDropdown);

            Transform audioSection = CreateRuntimeSettingsSection(settingsScrollContentRoot, "SettingsAudioSection", "Audio");
            CreateRuntimeSliderRow(audioSection, "Master Volume", "MasterVolumeSlider", "MasterVolumeValueText", out masterVolumeSlider, out masterVolumeValueText, 0f, 1f);
            CreateRuntimeSliderRow(audioSection, "Music Volume", "MusicVolumeSlider", "MusicVolumeValueText", out musicVolumeSlider, out musicVolumeValueText, 0f, 1f);
            CreateRuntimeSliderRow(audioSection, "SFX Volume", "SfxVolumeSlider", "SfxVolumeValueText", out sfxVolumeSlider, out sfxVolumeValueText, 0f, 1f);

            Transform gameplaySection = CreateRuntimeSettingsSection(settingsScrollContentRoot, "SettingsGameplaySection", "Gameplay");
            CreateRuntimeSliderRow(gameplaySection, "UI Scale", "UiScaleSlider", "UiScaleValueText", out uiScaleSlider, out uiScaleValueText, 0.75f, 1.35f);

            if (settingsContentText != null)
            {
                LayoutElement layoutElement = settingsContentText.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.minHeight = 28f;
                    layoutElement.preferredHeight = 28f;
                    layoutElement.flexibleHeight = 0f;
                }
            }
        }

        private RectTransform CreateRuntimeScrollContentArea(Transform parent, string name)
        {
            RectTransform scrollView = CreateRuntimeRect(name, parent);
            LayoutElement scrollLayout = scrollView.gameObject.AddComponent<LayoutElement>();
            scrollLayout.minHeight = 320f;
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.flexibleWidth = 1f;

            Image image = scrollView.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.01f);

            ScrollRect scrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            RectTransform viewport = CreateRuntimeStretchRect("Viewport", scrollView);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform content = CreateRuntimeRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            ConfigureRuntimeVerticalLayout(content, 14, 0, TextAnchor.UpperLeft);

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        private Transform CreateRuntimeSettingsSection(Transform parent, string name, string title)
        {
            RectTransform section = CreateRuntimeRect(name, parent);
            Image image = section.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.028f);
            Outline outline = section.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.36f, 0.46f, 0.58f, 0.12f);
            outline.effectDistance = new Vector2(1f, -1f);

            LayoutElement layoutElement = section.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 100f;
            layoutElement.flexibleWidth = 1f;

            ConfigureRuntimeVerticalLayout(section, 10, 16, TextAnchor.UpperLeft);
            CreateRuntimeText(section, "SettingsSectionTitleText", title, 28, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0.98f, 0.99f, 1f, 1f), 32f);
            return section;
        }

        private void CreateRuntimeRebindRow(Transform parent, string label, string valueName, string buttonName, out TMP_Text valueText, out Button button)
        {
            RectTransform row = CreateRuntimeRow(parent, buttonName + "Row");
            CreateRuntimeText(row, "SettingsFieldLabelText", label, 24, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.91f, 0.93f, 0.96f, 1f), 280f);
            valueText = CreateRuntimeText(row, valueName, "Space", 22, FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Color(0.71f, 0.82f, 0.96f, 1f), 120f);
            button = CreateRuntimeButton(row, buttonName, "Rebind", 48f, 140f, 22f);
        }

        private void CreateRuntimeDropdownRow(Transform parent, string label, string dropdownName, out TMP_Dropdown dropdown)
        {
            RectTransform row = CreateRuntimeRow(parent, dropdownName + "Row");
            CreateRuntimeText(row, "SettingsFieldLabelText", label, 24, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.91f, 0.93f, 0.96f, 1f), 280f);
            dropdown = CreateRuntimeDropdown(row, dropdownName);
        }

        private void CreateRuntimeSliderRow(Transform parent, string label, string sliderName, string valueName, out Slider slider, out TMP_Text valueText, float minValue, float maxValue)
        {
            RectTransform row = CreateRuntimeRow(parent, sliderName + "Row");
            CreateRuntimeText(row, "SettingsFieldLabelText", label, 24, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.91f, 0.93f, 0.96f, 1f), 280f);
            slider = CreateRuntimeSlider(row, sliderName, minValue, maxValue);
            valueText = CreateRuntimeText(row, valueName, "100%", 22, FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Color(0.71f, 0.82f, 0.96f, 1f), 84f);
        }

        private void CreateRuntimeToggleRow(Transform parent, string label, string toggleName, out Toggle toggle)
        {
            RectTransform row = CreateRuntimeRow(parent, toggleName + "Row");
            CreateRuntimeText(row, "SettingsFieldLabelText", label, 24, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.91f, 0.93f, 0.96f, 1f), 280f);
            toggle = CreateRuntimeToggle(row, toggleName);
        }

        private RectTransform CreateRuntimeRow(Transform parent, string name)
        {
            RectTransform row = CreateRuntimeRect(name, parent);
            ConfigureRuntimeHorizontalLayout(row, 12, 0);

            LayoutElement layoutElement = row.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 52f;
            layoutElement.preferredHeight = 52f;
            layoutElement.flexibleWidth = 1f;
            return row;
        }

        private TMP_Text CreateRuntimeText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Color color,
            float preferredWidth)
        {
            RectTransform rect = CreateRuntimeRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset fontAsset = ResolveRuntimeFontAsset();
            if (fontAsset != null)
            {
                text.font = fontAsset;
                text.fontSharedMaterial = fontAsset.material;
            }

            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.extraPadding = true;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
            text.lineSpacing = 1.02f;
            text.margin = Vector4.zero;

            LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            layoutElement.minWidth = preferredWidth;
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.flexibleWidth = 0f;
            layoutElement.minHeight = 30f;
            layoutElement.preferredHeight = 30f;
            return text;
        }

        private TMP_Text CreateRuntimeTextOnRect(
            RectTransform rect,
            string content,
            int fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Color color)
        {
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset fontAsset = ResolveRuntimeFontAsset();
            if (fontAsset != null)
            {
                text.font = fontAsset;
                text.fontSharedMaterial = fontAsset.material;
            }

            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.extraPadding = true;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
            text.lineSpacing = 1.02f;
            text.margin = Vector4.zero;
            return text;
        }

        private Button CreateRuntimeButton(Transform parent, string name, string label, float height, float preferredWidth, float fontSize)
        {
            RectTransform rect = CreateRuntimeRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.22f, 0.28f, 1f);

            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.2f, 0.25f, 0.32f, 1f);
            colors.pressedColor = new Color(0.13f, 0.16f, 0.21f, 1f);
            colors.selectedColor = new Color(0.2f, 0.25f, 0.32f, 1f);
            button.colors = colors;
            button.targetGraphic = image;

            LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.flexibleWidth = 0f;

            RectTransform labelRect = CreateRuntimeRect("Label", rect);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 8f);
            labelRect.offsetMax = new Vector2(-12f, -8f);
            TMP_Text labelText = CreateRuntimeTextOnRect(labelRect, label, (int)fontSize, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.raycastTarget = false;
            return button;
        }

        private TMP_Dropdown CreateRuntimeDropdown(Transform parent, string name)
        {
            RectTransform rect = CreateRuntimeRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.22f, 0.28f, 1f);

            TMP_Dropdown dropdown = rect.gameObject.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = image;

            LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 48f;
            layoutElement.preferredHeight = 48f;
            layoutElement.preferredWidth = 340f;
            layoutElement.flexibleWidth = 1f;

            TMP_FontAsset fontAsset = ResolveRuntimeFontAsset();

            RectTransform captionRect = CreateRuntimeRect("Label", rect);
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(14f, 8f);
            captionRect.offsetMax = new Vector2(-36f, -8f);
            TMP_Text caption = CreateRuntimeTextOnRect(captionRect, "Option", 22, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.white);
            dropdown.captionText = caption;

            RectTransform arrowRect = CreateRuntimeRect("Arrow", rect);
            arrowRect.anchorMin = new Vector2(1f, 0.5f);
            arrowRect.anchorMax = new Vector2(1f, 0.5f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-12f, 0f);
            arrowRect.sizeDelta = new Vector2(20f, 20f);
            TMP_Text arrow = CreateRuntimeTextOnRect(arrowRect, "v", 20, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);

            RectTransform template = CreateRuntimeRect("Template", rect);
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

            RectTransform viewport = CreateRuntimeStretchRect("Viewport", template);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform content = CreateRuntimeRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            ConfigureRuntimeVerticalLayout(content, 0, 0, TextAnchor.UpperLeft);
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform item = CreateRuntimeRect("Item", content);
            Image itemImage = item.gameObject.AddComponent<Image>();
            itemImage.color = new Color(0.14f, 0.18f, 0.24f, 1f);
            Toggle itemToggle = item.gameObject.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemImage;
            LayoutElement itemLayout = item.gameObject.AddComponent<LayoutElement>();
            itemLayout.minHeight = 42f;
            itemLayout.preferredHeight = 42f;
            itemLayout.flexibleWidth = 1f;

            RectTransform itemTextRect = CreateRuntimeRect("Item Label", item);
            itemTextRect.anchorMin = Vector2.zero;
            itemTextRect.anchorMax = Vector2.one;
            itemTextRect.offsetMin = new Vector2(34f, 6f);
            itemTextRect.offsetMax = new Vector2(-12f, -6f);
            TMP_Text itemText = CreateRuntimeTextOnRect(itemTextRect, "Option", 20, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.white);
            dropdown.itemText = itemText;

            RectTransform checkmark = CreateRuntimeRect("Checkmark", item);
            checkmark.anchorMin = new Vector2(0f, 0.5f);
            checkmark.anchorMax = new Vector2(0f, 0.5f);
            checkmark.pivot = new Vector2(0.5f, 0.5f);
            checkmark.anchoredPosition = new Vector2(16f, 0f);
            checkmark.sizeDelta = new Vector2(14f, 14f);
            Image checkmarkImage = checkmark.gameObject.AddComponent<Image>();
            checkmarkImage.color = new Color(0.71f, 0.82f, 0.96f, 1f);
            itemToggle.graphic = checkmarkImage;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            dropdown.template = template;
            return dropdown;
        }

        private Toggle CreateRuntimeToggle(Transform parent, string name)
        {
            RectTransform rect = CreateRuntimeRect(name, parent);
            LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            layoutElement.minWidth = 54f;
            layoutElement.preferredWidth = 54f;
            layoutElement.minHeight = 48f;
            layoutElement.preferredHeight = 48f;
            layoutElement.flexibleWidth = 0f;

            Toggle toggle = rect.gameObject.AddComponent<Toggle>();
            RectTransform background = CreateRuntimeRect("Background", rect);
            background.anchorMin = new Vector2(0.5f, 0.5f);
            background.anchorMax = new Vector2(0.5f, 0.5f);
            background.sizeDelta = new Vector2(28f, 28f);
            Image backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.18f, 0.22f, 0.28f, 1f);

            RectTransform checkmark = CreateRuntimeRect("Checkmark", background);
            checkmark.anchorMin = Vector2.zero;
            checkmark.anchorMax = Vector2.one;
            checkmark.offsetMin = new Vector2(5f, 5f);
            checkmark.offsetMax = new Vector2(-5f, -5f);
            Image checkmarkImage = checkmark.gameObject.AddComponent<Image>();
            checkmarkImage.color = new Color(0.71f, 0.82f, 0.96f, 1f);
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;
            return toggle;
        }

        private Slider CreateRuntimeSlider(Transform parent, string name, float minValue, float maxValue)
        {
            RectTransform rect = CreateRuntimeRect(name, parent);
            LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 40f;
            layoutElement.preferredHeight = 40f;
            layoutElement.preferredWidth = 320f;
            layoutElement.flexibleWidth = 1f;

            Slider slider = rect.gameObject.AddComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.direction = Slider.Direction.LeftToRight;

            RectTransform background = CreateRuntimeRect("Background", rect);
            background.anchorMin = new Vector2(0f, 0.5f);
            background.anchorMax = new Vector2(1f, 0.5f);
            background.offsetMin = new Vector2(0f, -5f);
            background.offsetMax = new Vector2(0f, 5f);
            Image backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.19f, 0.24f, 0.30f, 1f);

            RectTransform fillArea = CreateRuntimeRect("Fill Area", rect);
            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = Vector2.zero;
            fillArea.offsetMax = Vector2.zero;

            RectTransform fill = CreateRuntimeRect("Fill", fillArea);
            fill.anchorMin = new Vector2(0f, 0.5f);
            fill.anchorMax = new Vector2(1f, 0.5f);
            fill.offsetMin = new Vector2(0f, -5f);
            fill.offsetMax = new Vector2(0f, 5f);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.71f, 0.82f, 0.96f, 1f);

            RectTransform handleSlideArea = CreateRuntimeRect("Handle Slide Area", rect);
            handleSlideArea.anchorMin = Vector2.zero;
            handleSlideArea.anchorMax = Vector2.one;
            handleSlideArea.offsetMin = Vector2.zero;
            handleSlideArea.offsetMax = Vector2.zero;

            RectTransform handle = CreateRuntimeRect("Handle", handleSlideArea);
            handle.anchorMin = new Vector2(0f, 0.5f);
            handle.anchorMax = new Vector2(0f, 0.5f);
            handle.sizeDelta = new Vector2(20f, 20f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Color.white;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private static RectTransform CreateRuntimeRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            gameObject.layer = parent.gameObject.layer;
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static RectTransform CreateRuntimeStretchRect(string name, Transform parent)
        {
            RectTransform rect = CreateRuntimeRect(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private static void ConfigureRuntimeVerticalLayout(RectTransform rect, int spacing, int padding, TextAnchor alignment)
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

        private static void ConfigureRuntimeHorizontalLayout(RectTransform rect, int spacing, int padding)
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

        private TMP_FontAsset ResolveRuntimeFontAsset()
        {
            if (rollText != null && rollText.font != null)
            {
                return rollText.font;
            }

            if (shardsText != null && shardsText.font != null)
            {
                return shardsText.font;
            }

            if (settingsContentText != null && settingsContentText.font != null)
            {
                return settingsContentText.font;
            }

            if (shopContentText != null && shopContentText.font != null)
            {
                return shopContentText.font;
            }

            return null;
        }

        private bool HasRequiredReferences()
        {
            return canvas != null &&
                   diceManager != null &&
                   mainMenuPanel != null &&
                   shopPanel != null &&
                   shopTreeRoot != null &&
                   challengesPanel != null &&
                   settingsPanel != null &&
                   roundUtilityBar != null;
        }

        private void StartRound()
        {
            Time.timeScale = 1f;
            currentState = ViewState.Round;
            currentRoundInfoTab = RoundInfoTab.Scoring;
            SetRoundRootsActive(true);
            diceManager.ResetRoundState();
            BeginRun();
            diceManager.SetRollInputEnabled(true);
            SetHudVisible(true);
            SetOnlyPanelActive(roundUtilityBar);
            RefreshRoundInfoPanelText();
            RefreshRoundActionInteractivity();
        }

        private void ResumeRound()
        {
            Time.timeScale = 1f;
            currentState = ViewState.Round;
            diceManager.SetRollInputEnabled(true);
            SetHudVisible(true);
            SetOnlyPanelActive(roundUtilityBar);
            RefreshRoundInfoPanelText();
            RefreshRoundActionInteractivity();
        }

        private void EnterMainMenu()
        {
            Time.timeScale = 1f;
            currentState = ViewState.MainMenu;

            if (diceManager != null)
            {
                diceManager.SetRollInputEnabled(false);
                diceManager.ResetRoundState();
            }

            ClearRun();
            SetRoundRootsActive(false);
            SetHudVisible(false);
            SetOnlyPanelActive(mainMenuPanel);
            RefreshShardsText();
        }

        private void EnterMetaShop()
        {
            Time.timeScale = 1f;
            currentState = ViewState.Shop;
            diceManager.SetRollInputEnabled(false);
            SetRoundRootsActive(false);
            SetHudVisible(false);
            RefreshShopSection(currentShopSection);
            SetOnlyPanelActive(shopPanel);
        }

        private void EnterChallenges()
        {
            Time.timeScale = 1f;
            currentState = ViewState.Challenges;
            diceManager.SetRollInputEnabled(false);
            SetRoundRootsActive(false);
            SetHudVisible(false);
            SetOnlyPanelActive(challengesPanel);
        }

        private void EnterMetaSettings()
        {
            Time.timeScale = 1f;
            currentState = ViewState.Settings;
            diceManager.SetRollInputEnabled(false);
            SetRoundRootsActive(false);
            SetHudVisible(false);
            EnsureSettingsUiBuilt();
            RefreshSettingsUi();
            SetOnlyPanelActive(settingsPanel);
        }

        private void EnterRoundChallenges()
        {
            Time.timeScale = 0f;
            currentState = ViewState.RoundChallenges;
            diceManager.SetRollInputEnabled(false);
            SetHudVisible(false);
            SetOnlyPanelActive(challengesPanel);
        }

        private void EnterRoundSettings()
        {
            Time.timeScale = 0f;
            currentState = ViewState.RoundSettings;
            diceManager.SetRollInputEnabled(false);
            SetHudVisible(false);
            EnsureSettingsUiBuilt();
            RefreshSettingsUi();
            SetOnlyPanelActive(settingsPanel);
        }

        private void HandleBackAction()
        {
            switch (currentState)
            {
                case ViewState.Shop:
                case ViewState.Challenges:
                case ViewState.Settings:
                    EnterMainMenu();
                    break;
                case ViewState.RoundChallenges:
                case ViewState.RoundSettings:
                    ResumeRound();
                    break;
                case ViewState.Round:
                    EnterRoundSettings();
                    break;
            }
        }

        private void SetShopSection(ShopSection section)
        {
            currentShopSection = section;
            shopResetConfirmationArmed = false;
            RefreshShopSection(section);
        }

        private void InitializeShopData()
        {
            if (shopSections.Count > 0)
            {
                return;
            }

            RegisterShopSection(
                ShopSection.DiceUnlocks,
                "Dice Unlocks",
                "Unlock the root die license first, then choose which experimental dice paths to expand.",
                CreateUpgrade("dice_root_license", ShopSection.DiceUnlocks, "Foundry Permit", "Authorize your first alternate die branch and open the dice workshop.", "Unlocks the first split in the dice workshop tree.", "D6", 4, 0, 0),
                CreateUpgrade("dice_loaded_d6", ShopSection.DiceUnlocks, "Loaded D6", "Prototype weighted die branch for future custom probability experiments.", "Will later unlock a biased custom die variant.", "LD", 8, 1, 0, "dice_root_license"),
                CreateUpgrade("dice_frost_die", ShopSection.DiceUnlocks, "Frost Die", "Unlock a cold-element die concept for future status and combo systems.", "Will later unlock ice-themed dice effects.", "FR", 8, 1, 1, "dice_root_license"),
                CreateUpgrade("dice_twin_face", ShopSection.DiceUnlocks, "Twin Face Die", "Repeated-value die concept for future duplicated-face builds.", "Will later enable repeated-face dice prototypes.", "TF", 11, 2, 0, "dice_loaded_d6"),
                CreateUpgrade("dice_prism_die", ShopSection.DiceUnlocks, "Prism Die", "Hybrid end-branch for future symbol and custom-face experiments.", "Will later unlock symbol-heavy custom dice.", "PR", 14, 2, 1, "dice_frost_die"));

            RegisterShopSection(
                ShopSection.Efficiency,
                "Efficiency / Automation",
                "Unlock the workshop backbone first, then branch into faster cleanup or more automation.",
                CreateUpgrade("eff_root_ledger", ShopSection.Efficiency, "Workshop Ledger", "Establish the basic infrastructure needed for automation upgrades.", "Opens the efficiency tree for future utility systems.", "WG", 4, 0, 0),
                CreateUpgrade("eff_tray_magnet", ShopSection.Efficiency, "Tray Magnet", "Future branch for faster tray cleanup and post-roll reset handling.", "Will later improve tray cleanup and reset cadence.", "MG", 7, 1, 0, "eff_root_ledger"),
                CreateUpgrade("eff_quick_evaluate", ShopSection.Efficiency, "Quick Evaluate", "Future branch for shortening readout and reveal steps.", "Will later shorten result presentation timing.", "QV", 7, 1, 1, "eff_root_ledger"),
                CreateUpgrade("eff_magnetic_sweep", ShopSection.Efficiency, "Magnetic Sweep", "Placeholder for stronger cleanup between rolls.", "Will later strengthen cleanup automation.", "SW", 10, 2, 0, "eff_tray_magnet"),
                CreateUpgrade("eff_auto_roll", ShopSection.Efficiency, "Auto Roll I", "Placeholder passive roll automation branch.", "Will later unlock passive roll automation hooks.", "AR", 11, 2, 1, "eff_quick_evaluate"));

            RegisterShopSection(
                ShopSection.ScoreMultipliers,
                "Score Multipliers",
                "Start with a scoring primer, then branch into combo-focused or high-face-focused score upgrades.",
                CreateUpgrade("score_root_primer", ShopSection.ScoreMultipliers, "Scoring Primer", "Unlock the first scoring specialization branch.", "Opens the score multiplier tree.", "SC", 4, 0, 0),
                CreateUpgrade("score_combo_lens", ShopSection.ScoreMultipliers, "Combo Lens", "Future branch for stronger pair, straight, and combo rewards.", "Will later amplify combo scoring bonuses.", "CL", 8, 1, 0, "score_root_primer"),
                CreateUpgrade("score_crown_weight", ShopSection.ScoreMultipliers, "Crown Weight", "Future branch for higher-value face scaling.", "Will later reward higher-value faces more strongly.", "CW", 8, 1, 1, "score_root_primer"),
                CreateUpgrade("score_streak_register", ShopSection.ScoreMultipliers, "Streak Register", "Placeholder for momentum-based score bonuses.", "Will later reward streak-based scoring chains.", "SR", 11, 2, 0, "score_combo_lens"),
                CreateUpgrade("score_echo_multiplier", ShopSection.ScoreMultipliers, "Echo Multiplier", "Placeholder for end-of-roll multiplier stacking.", "Will later add layered multiplier stacking.", "XM", 13, 2, 1, "score_crown_weight"));
        }

        private void RegisterShopSection(ShopSection section, string title, string summary, params ShopUpgradeDefinition[] upgrades)
        {
            ShopSectionDefinition definition = new ShopSectionDefinition
            {
                title = title,
                summary = summary,
                upgrades = new List<ShopUpgradeDefinition>(upgrades),
            };

            definition.upgrades.Sort((left, right) =>
            {
                int depthCompare = left.depth.CompareTo(right.depth);
                return depthCompare != 0 ? depthCompare : left.order.CompareTo(right.order);
            });

            shopSections[section] = definition;

            for (int i = 0; i < definition.upgrades.Count; i++)
            {
                shopUpgradesById[definition.upgrades[i].id] = definition.upgrades[i];
            }
        }

        private static ShopUpgradeDefinition CreateUpgrade(
            string id,
            ShopSection section,
            string title,
            string description,
            string effectText,
            string iconLabel,
            int shardCost,
            int depth,
            int order,
            params string[] prerequisites)
        {
            return new ShopUpgradeDefinition
            {
                id = id,
                section = section,
                title = title,
                description = description,
                effectText = effectText,
                iconLabel = iconLabel,
                shardCost = shardCost,
                depth = depth,
                order = order,
                prerequisites = prerequisites ?? System.Array.Empty<string>(),
            };
        }

        private void LoadUnlockedUpgrades()
        {
            unlockedUpgradeIds.Clear();

            foreach (KeyValuePair<string, ShopUpgradeDefinition> pair in shopUpgradesById)
            {
                if (PlayerPrefs.GetInt(GetShopUpgradePlayerPrefsKey(pair.Key), 0) == 1)
                {
                    unlockedUpgradeIds.Add(pair.Key);
                }
            }
        }

        private void RefreshShopSection(ShopSection section)
        {
            if (!shopSections.TryGetValue(section, out ShopSectionDefinition definition))
            {
                return;
            }

            if (shopContextText != null)
            {
                shopContextText.text = $"Shards: {shards}";
            }

            if (shopSectionTitleText != null)
            {
                shopSectionTitleText.text = string.Empty;
            }

            if (shopContentText != null)
            {
                shopContentText.text = string.Empty;
            }

            RefreshShardsText();
            UpdateShopTabInteractivity();
            RefreshShopTabSizing();
            RefreshShopTabTooltips();
            shopTooltipPresenter?.Hide();
            RebuildShopTreeUi(definition);
        }

        private void UpdateShopTabInteractivity()
        {
            if (shopDiceUnlocksButton != null)
            {
                shopDiceUnlocksButton.interactable = currentShopSection != ShopSection.DiceUnlocks;
            }

            if (shopEfficiencyButton != null)
            {
                shopEfficiencyButton.interactable = currentShopSection != ShopSection.Efficiency;
            }

            if (shopScoreMultipliersButton != null)
            {
                shopScoreMultipliersButton.interactable = currentShopSection != ShopSection.ScoreMultipliers;
            }

            RefreshShopResetButton();
        }

        private void RefreshShopTabSizing()
        {
            RectTransform row = shopDiceUnlocksButton != null ? shopDiceUnlocksButton.transform.parent as RectTransform : null;
            if (row != null)
            {
                LayoutElement rowLayout = row.GetComponent<LayoutElement>();
                if (rowLayout != null)
                {
                    rowLayout.minHeight = 42f;
                    rowLayout.preferredHeight = 42f;
                }

                HorizontalLayoutGroup rowGroup = row.GetComponent<HorizontalLayoutGroup>();
                if (rowGroup != null)
                {
                    rowGroup.spacing = 6f;
                    rowGroup.padding = new RectOffset(0, 0, 0, 0);
                    rowGroup.childAlignment = TextAnchor.MiddleCenter;
                    rowGroup.childControlWidth = true;
                    rowGroup.childControlHeight = true;
                    rowGroup.childForceExpandWidth = true;
                    rowGroup.childForceExpandHeight = false;
                }
            }

            TuneShopTabButton(shopDiceUnlocksButton);
            TuneShopTabButton(shopEfficiencyButton);
            TuneShopTabButton(shopScoreMultipliersButton);
        }

        private static void TuneShopTabButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minHeight = 42f;
                layout.preferredHeight = 42f;
                layout.flexibleWidth = 1f;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = 16f;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
            }
        }

        private void RefreshShopTabTooltips()
        {
            ConfigureTooltip(shopDiceUnlocksButton, "Dice Unlocks", shopSections.TryGetValue(ShopSection.DiceUnlocks, out ShopSectionDefinition diceSection) ? diceSection.summary : "Unlock future custom dice variants.");
            ConfigureTooltip(shopEfficiencyButton, "Efficiency / Automation", shopSections.TryGetValue(ShopSection.Efficiency, out ShopSectionDefinition efficiencySection) ? efficiencySection.summary : "Unlock future cleanup and automation tools.");
            ConfigureTooltip(shopScoreMultipliersButton, "Score Multipliers", shopSections.TryGetValue(ShopSection.ScoreMultipliers, out ShopSectionDefinition scoreSection) ? scoreSection.summary : "Unlock future scoring specialization paths.");
            ConfigureTooltip(shopResetTabButton, "Reset Current Tab", GetResetTooltipText());
        }

        private void RefreshShopResetButton()
        {
            if (shopResetTabButton == null)
            {
                return;
            }

            int refundAmount = CalculateTabRefund(currentShopSection);
            TMP_Text label = shopResetTabButton.GetComponentInChildren<TMP_Text>(true);
            bool hasPurchases = refundAmount > 0;

            if (label != null)
            {
                label.text = shopResetConfirmationArmed && shopResetConfirmationSection == currentShopSection
                    ? $"Confirm Reset (+{refundAmount})"
                    : "Reset Tab";
            }

            shopResetTabButton.interactable = hasPurchases;
        }

        private void RebuildShopTreeUi(ShopSectionDefinition sectionDefinition)
        {
            if (shopTreeRoot == null)
            {
                return;
            }

            ClearChildren(shopTreeRoot);

            int currentDepth = -1;
            RectTransform currentRow = null;

            for (int i = 0; i < sectionDefinition.upgrades.Count; i++)
            {
                ShopUpgradeDefinition upgrade = sectionDefinition.upgrades[i];

                if (upgrade.depth != currentDepth || currentRow == null)
                {
                    currentDepth = upgrade.depth;
                    currentRow = CreateShopTreeRow(shopTreeRoot, $"Depth{currentDepth}Row");
                }

                CreateShopUpgradeNode(currentRow, upgrade);
            }
        }

        private RectTransform CreateShopTreeRow(Transform parent, string name)
        {
            RectTransform row = CreateUiRect(name, parent);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 0, 0);

            LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 112f;
            element.preferredHeight = 112f;
            element.flexibleWidth = 1f;

            return row;
        }

        private void CreateShopUpgradeNode(Transform parent, ShopUpgradeDefinition upgrade)
        {
            ShopNodeState nodeState = GetShopNodeState(upgrade);
            bool canPurchase = nodeState == ShopNodeState.Available;

            RectTransform card = CreateUiRect(upgrade.id, parent);
            Image background = card.gameObject.AddComponent<Image>();
            background.color = GetShopNodeBackgroundColor(nodeState);

            Outline outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = nodeState == ShopNodeState.Unlocked ? ShopNodeUnlockedOutlineColor : ShopNodeOutlineColor;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;

            Shadow shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            shadow.effectDistance = new Vector2(0f, -3f);
            shadow.useGraphicAlpha = true;

            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.interactable = true;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ApplyShopNodeButtonColors(button, nodeState, background.color);

            LayoutElement element = card.gameObject.AddComponent<LayoutElement>();
            element.minWidth = shopNodeWidth;
            element.preferredWidth = shopNodeWidth;
            element.minHeight = 84f;
            element.preferredHeight = 84f;
            element.flexibleWidth = 0f;

            HorizontalLayoutGroup layout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateShopNodeIcon(card, upgrade.iconLabel, nodeState);
            CreateShopNodeText(card, "Title", upgrade.title, 14f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ShopNodeTitleColor, 34f, 0f, true);

            ConfigureTooltip(card.GetComponent<Button>(), upgrade.title, BuildUpgradeTooltip(upgrade, nodeState));

            if (canPurchase)
            {
                string upgradeId = upgrade.id;
                button.onClick.AddListener(() => PurchaseUpgrade(upgradeId));
            }
        }

        private TMP_Text CreateShopNodeText(
            Transform parent,
            string name,
            string content,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Color color,
            float preferredHeight,
            float flexibleHeight = 0f,
            bool allowWrap = false)
        {
            RectTransform rect = CreateUiRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.font = shopContentText != null ? shopContentText.font : null;
            text.fontSharedMaterial = shopContentText != null ? shopContentText.fontSharedMaterial : null;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = false;
            text.textWrappingMode = allowWrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = allowWrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
            text.extraPadding = true;
            text.lineSpacing = 1.04f;
            text.isTextObjectScaleStatic = true;
            text.raycastTarget = false;

            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = preferredHeight;
            element.preferredHeight = preferredHeight;
            element.flexibleHeight = flexibleHeight;
            element.flexibleWidth = 1f;
            if (allowWrap)
            {
                element.minWidth = 0f;
                element.preferredWidth = -1f;
                element.flexibleWidth = 1f;
            }

            return text;
        }

        private void CreateShopNodeIcon(Transform parent, string iconLabel, ShopNodeState state)
        {
            RectTransform icon = CreateUiRect("Icon", parent);
            LayoutElement element = icon.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 52f;
            element.preferredHeight = 52f;
            element.minWidth = 52f;
            element.preferredWidth = 52f;
            element.flexibleWidth = 0f;

            Image background = icon.gameObject.AddComponent<Image>();
            background.color = state == ShopNodeState.Unlocked
                ? new Color(0.22f, 0.48f, 0.34f, 1f)
                : ShopNodeIconColor;
            background.raycastTarget = false;

            Outline outline = icon.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.82f, 0.88f, 0.96f, 0.12f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;

            TMP_Text label = CreateShopNodeText(icon, "IconLabel", iconLabel, 14, FontStyles.Bold, TextAlignmentOptions.Center, ShopNodeAccentColor, 52f, 0f, false);
            LayoutElement labelLayout = label.GetComponent<LayoutElement>();
            if (labelLayout != null)
            {
                labelLayout.ignoreLayout = true;
            }

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private void PurchaseUpgrade(string upgradeId)
        {
            if (!shopUpgradesById.TryGetValue(upgradeId, out ShopUpgradeDefinition upgrade))
            {
                return;
            }

            if (unlockedUpgradeIds.Contains(upgradeId) || !ArePrerequisitesUnlocked(upgrade))
            {
                return;
            }

            if (shards < upgrade.shardCost)
            {
                return;
            }

            shards -= upgrade.shardCost;
            unlockedUpgradeIds.Add(upgradeId);
            shopResetConfirmationArmed = false;
            PlayerPrefs.SetInt(GetShopUpgradePlayerPrefsKey(upgradeId), 1);
            PlayerPrefs.Save();
            RefreshShopSection(currentShopSection);

            if (debugEconomyLogs)
            {
                Debug.Log($"Shop purchase: {upgradeId} for {upgrade.shardCost} Shards. Shards left={shards}", this);
            }
        }

        public bool IsUpgradeUnlocked(string upgradeId)
        {
            return !string.IsNullOrEmpty(upgradeId) && unlockedUpgradeIds.Contains(upgradeId);
        }

        private void HandleResetCurrentShopTab()
        {
            int refundAmount = CalculateTabRefund(currentShopSection);
            if (refundAmount <= 0)
            {
                shopResetConfirmationArmed = false;
                RefreshShopResetButton();
                return;
            }

            if (!shopResetConfirmationArmed || shopResetConfirmationSection != currentShopSection)
            {
                shopResetConfirmationArmed = true;
                shopResetConfirmationSection = currentShopSection;
                RefreshShopResetButton();
                return;
            }

            ResetCurrentShopTab();
        }

        private void ResetCurrentShopTab()
        {
            if (!shopSections.TryGetValue(currentShopSection, out ShopSectionDefinition definition))
            {
                return;
            }

            int refundAmount = 0;

            for (int i = 0; i < definition.upgrades.Count; i++)
            {
                ShopUpgradeDefinition upgrade = definition.upgrades[i];
                if (!unlockedUpgradeIds.Contains(upgrade.id))
                {
                    continue;
                }

                refundAmount += upgrade.shardCost;
                unlockedUpgradeIds.Remove(upgrade.id);
                PlayerPrefs.DeleteKey(GetShopUpgradePlayerPrefsKey(upgrade.id));
            }

            shards += refundAmount;
            shopResetConfirmationArmed = false;
            PlayerPrefs.SetInt(ShardsPlayerPrefsKey, shards);
            PlayerPrefs.Save();
            RefreshShopSection(currentShopSection);

            if (debugEconomyLogs)
            {
                Debug.Log($"Shop tab reset: {currentShopSection}, refunded {refundAmount} Shards, shards={shards}", this);
            }
        }

        private int CalculateTabRefund(ShopSection section)
        {
            if (!shopSections.TryGetValue(section, out ShopSectionDefinition definition))
            {
                return 0;
            }

            int refundAmount = 0;

            for (int i = 0; i < definition.upgrades.Count; i++)
            {
                if (unlockedUpgradeIds.Contains(definition.upgrades[i].id))
                {
                    refundAmount += definition.upgrades[i].shardCost;
                }
            }

            return refundAmount;
        }

        private ShopNodeState GetShopNodeState(ShopUpgradeDefinition upgrade)
        {
            if (unlockedUpgradeIds.Contains(upgrade.id))
            {
                return ShopNodeState.Unlocked;
            }

            if (!ArePrerequisitesUnlocked(upgrade))
            {
                return ShopNodeState.Locked;
            }

            return shards >= upgrade.shardCost ? ShopNodeState.Available : ShopNodeState.InsufficientShards;
        }

        private bool ArePrerequisitesUnlocked(ShopUpgradeDefinition upgrade)
        {
            if (upgrade.prerequisites == null || upgrade.prerequisites.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < upgrade.prerequisites.Length; i++)
            {
                if (!unlockedUpgradeIds.Contains(upgrade.prerequisites[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetShopUpgradePlayerPrefsKey(string upgradeId)
        {
            return $"{ShopUpgradeUnlockedKeyPrefix}{upgradeId}";
        }

        private string GetResetTooltipText()
        {
            int refundAmount = CalculateTabRefund(currentShopSection);
            return refundAmount > 0
                ? $"Reset only the currently selected shop tab.\nRefund: {refundAmount} Shards in full.\nOther tabs stay untouched."
                : "No purchases in this tab yet.\nOnly the currently selected tab would be reset.";
        }

        private string BuildUpgradeTooltip(ShopUpgradeDefinition upgrade, ShopNodeState state)
        {
            return
                $"{upgrade.description}\n\n" +
                $"Cost: {upgrade.shardCost} Shards\n" +
                $"Effect: {upgrade.effectText}\n" +
                $"State: {GetShopNodeStatusText(upgrade, state)}";
        }

        private string GetShopNodeStatusText(ShopUpgradeDefinition upgrade, ShopNodeState state)
        {
            switch (state)
            {
                case ShopNodeState.Unlocked:
                    return "Unlocked";
                case ShopNodeState.Available:
                    return "Available now";
                case ShopNodeState.InsufficientShards:
                    return $"Need {Mathf.Max(0, upgrade.shardCost - shards)} more Shards";
                default:
                    return upgrade.prerequisites != null && upgrade.prerequisites.Length > 0
                        ? "Requires parent unlock"
                        : "Root node";
            }
        }

        private static Color GetShopNodeBackgroundColor(ShopNodeState state)
        {
            switch (state)
            {
                case ShopNodeState.Unlocked:
                    return ShopNodeUnlockedColor;
                case ShopNodeState.Available:
                    return ShopNodeAvailableColor;
                case ShopNodeState.InsufficientShards:
                    return ShopNodeInsufficientColor;
                default:
                    return ShopNodeLockedColor;
            }
        }

        private static Color GetShopNodeStatusColor(ShopNodeState state)
        {
            switch (state)
            {
                case ShopNodeState.Unlocked:
                    return new Color(0.76f, 0.94f, 0.85f, 1f);
                case ShopNodeState.Available:
                    return ShopNodeAccentColor;
                case ShopNodeState.InsufficientShards:
                    return new Color(0.98f, 0.84f, 0.58f, 1f);
                default:
                    return ShopNodeMutedColor;
            }
        }

        private static void ApplyShopNodeButtonColors(Button button, ShopNodeState state, Color baseColor)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = state == ShopNodeState.Available ? Color.Lerp(baseColor, Color.white, 0.08f) : baseColor;
            colors.pressedColor = state == ShopNodeState.Available ? Color.Lerp(baseColor, Color.black, 0.12f) : baseColor;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = baseColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private void ConfigureTooltip(Component target, string title, string body)
        {
            if (target == null || shopTooltipPresenter == null)
            {
                return;
            }

            UiHoverTooltipTrigger trigger = target.GetComponent<UiHoverTooltipTrigger>();
            if (trigger == null)
            {
                trigger = target.gameObject.AddComponent<UiHoverTooltipTrigger>();
            }

            trigger.Configure(shopTooltipPresenter, title, body);
        }

        private static RectTransform CreateUiRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void ClearChildren(Transform target)
        {
            if (target == null)
            {
                return;
            }

            for (int i = target.childCount - 1; i >= 0; i--)
            {
                Transform childTransform = target.GetChild(i);
                GameObject child = childTransform.gameObject;
                childTransform.SetParent(null, false);

                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private void SetRoundRootsActive(bool active)
        {
            for (int i = 0; i < roundRoots.Count; i++)
            {
                if (roundRoots[i] != null)
                {
                    roundRoots[i].SetActive(active);
                }
            }
        }

        private void SetHudVisible(bool visible)
        {
            bool showRoundHud = visible && currentState == ViewState.Round;

            SetHudObjectActive(rollText, showRoundHud);
            SetHudObjectActive(scoreText, showRoundHud);
            SetHudObjectActive(coinsText, showRoundHud);
            SetActive(roundInfoPanel, showRoundHud);

            if (roundUtilityBar != null)
            {
                roundUtilityBar.SetActive(showRoundHud);
            }
        }

        private void SetOnlyPanelActive(GameObject activePanel)
        {
            shopTooltipPresenter?.Hide();
            SetActive(mainMenuPanel, activePanel == mainMenuPanel);
            SetActive(shopPanel, activePanel == shopPanel);
            SetActive(challengesPanel, activePanel == challengesPanel);
            SetActive(settingsPanel, activePanel == settingsPanel);
            SetActive(roundUtilityBar, activePanel == roundUtilityBar);
        }

        private void BeginRun()
        {
            runActive = true;
            runCoins = coinSettings.startingCoins;
            currentScore = 0;
            highestScoreReached = 0;
            totalCoinsEarned = 0;
            completedRolls = 0;
            RefreshRunHud();
            ShowRunReadySummary();
            RefreshRoundInfoPanelText();
        }

        private void ClearRun()
        {
            runActive = false;
            runCoins = 0;
            currentScore = 0;
            highestScoreReached = 0;
            totalCoinsEarned = 0;
            completedRolls = 0;
            RefreshRunHud();
        }

        private void HandleRollResolved(DiceManager.RollOutcome outcome)
        {
            if (!runActive)
            {
                return;
            }

            completedRolls++;

            int scoreGain = CalculateScoreGain(outcome);
            int coinGain = CalculateCoinGain(outcome);
            int coinLoss = 0;
            int netCoinDelta = coinGain;

            currentScore += scoreGain;
            highestScoreReached = Mathf.Max(highestScoreReached, currentScore);
            runCoins += netCoinDelta;
            totalCoinsEarned += Mathf.Max(0, coinGain);

            if (debugEconomyLogs)
            {
                Debug.Log(
                    $"Roll {completedRolls}: [{string.Join(", ", outcome.Values)}] total={outcome.Total}, score+={scoreGain}, coins+={coinGain}, coins-={coinLoss}, runCoins={runCoins}, runScore={currentScore}",
                    this);
            }

            ShowResolvedRollSummary(outcome, scoreGain, coinGain, coinLoss, netCoinDelta);
            RefreshRunHud();
            RefreshRoundInfoPanelText();

            if (runCoins <= 0)
            {
                HandleRunFailure();
                return;
            }

            RefreshRoundActionInteractivity();
        }

        private void SpendCoinsPlaceholder()
        {
            if (!CanUseRunAction())
            {
                return;
            }

            runCoins -= coinSettings.spendCoinsCost;

            if (rollText != null)
            {
                rollText.text =
                    $"Spent {coinSettings.spendCoinsCost} coins.\n" +
                    "Placeholder spend action for future in-run upgrades.\n" +
                    $"Coins Left: {runCoins}";
            }

            if (debugEconomyLogs)
            {
                Debug.Log($"Placeholder spend used: -{coinSettings.spendCoinsCost} coins, runCoins={runCoins}", this);
            }

            RefreshRunHud();

            if (runCoins <= 0)
            {
                HandleRunFailure();
                return;
            }

            RefreshRoundActionInteractivity();
        }

        private void CashOutRun()
        {
            if (!CanCashOut())
            {
                return;
            }

            int shardAward = CalculateShardAward();
            shards += shardAward;
            SaveShards();

            if (debugEconomyLogs)
            {
                Debug.Log(
                    $"Cash out: shards+={shardAward}, highestScore={highestScoreReached}, totalCoinsEarned={totalCoinsEarned}, shards={shards}",
                    this);
            }

            EnterMainMenu();
        }

        private void HandleRunFailure()
        {
            if (debugEconomyLogs)
            {
                Debug.Log(
                    $"Run failed: coins depleted. HighestScore={highestScoreReached}, totalCoinsEarned={totalCoinsEarned}",
                    this);
            }

            EnterMainMenu();
        }

        private int CalculateScoreGain(DiceManager.RollOutcome outcome)
        {
            int pairCount = CountDistinctPairs(outcome.Values);
            bool triple = IsTriple(outcome.Values);
            bool straight = IsStraight(outcome.Values);
            int highFaceCount = CountHighFaces(outcome.Values);

            int rawScore =
                outcome.Total * scoreSettings.pointsPerPip +
                highFaceCount * scoreSettings.highFaceBonusPerDie +
                pairCount * scoreSettings.pairBonus;

            if (triple)
            {
                rawScore += scoreSettings.tripleBonus;
            }

            if (straight)
            {
                rawScore += scoreSettings.straightBonus;
            }

            return Mathf.Max(1, Mathf.RoundToInt(rawScore * Mathf.Max(0f, scoreSettings.scoreMultiplier)));
        }

        private int CalculateCoinGain(DiceManager.RollOutcome outcome)
        {
            int pairCount = CountDistinctPairs(outcome.Values);
            bool triple = IsTriple(outcome.Values);
            bool straight = IsStraight(outcome.Values);
            int highFaceCount = CountHighFaces(outcome.Values);

            int rawCoins =
                coinSettings.baseCoinReward +
                highFaceCount * coinSettings.highFaceBonusPerDie +
                pairCount * coinSettings.pairBonus;

            if (triple)
            {
                rawCoins += coinSettings.tripleBonus;
            }

            if (straight)
            {
                rawCoins += coinSettings.straightBonus;
            }

            int scaledCoins = Mathf.RoundToInt(rawCoins * Mathf.Max(0f, coinSettings.coinRewardMultiplier));
            return Mathf.Max(coinSettings.minimumCoinsPerRoll, scaledCoins);
        }

        private int CalculateShardAward()
        {
            float shardValue =
                highestScoreReached * Mathf.Max(0f, shardSettings.highestScoreFactor) +
                totalCoinsEarned * Mathf.Max(0f, shardSettings.totalCoinsEarnedFactor);

            return Mathf.Max(0, shardSettings.baseCashOutReward + Mathf.FloorToInt(shardValue));
        }

        private void RefreshRunHud()
        {
            if (scoreText != null)
            {
                scoreText.text = runActive ? $"Score: {currentScore}" : "Score: -";
            }

            if (coinsText != null)
            {
                coinsText.text = runActive ? $"Coins: {runCoins}" : "Coins: -";
            }
        }

        private void RefreshShardsText()
        {
            if (shardsText == null)
            {
                return;
            }

            shardsText.text = $"Shards: {shards}";
        }

        private void ShowRunReadySummary()
        {
            if (rollText == null)
            {
                return;
            }

            string rollBinding = GameSettingsService.GetBindingDisplayName(GameSettingsService.Current.rollKey);

            rollText.text =
                $"Press {rollBinding} to roll.\n" +
                $"Starting Coins: {runCoins}\n" +
                "Cash out after any resolved roll.";
        }

        private void ShowResolvedRollSummary(DiceManager.RollOutcome outcome, int scoreGain, int coinGain, int coinLoss, int netCoinDelta)
        {
            if (rollText == null)
            {
                return;
            }

            bool detailedBreakdown = GameSettingsService.Current.showDetailedRollBreakdown;
            string coinDeltaPrefix = netCoinDelta >= 0 ? "+" : string.Empty;
            string coinSummary = coinLoss > 0
                ? $"Coins: {coinDeltaPrefix}{netCoinDelta} ({coinGain} gain / {coinLoss} loss)"
                : $"Coins: +{coinGain} reward";

            if (detailedBreakdown)
            {
                rollText.text =
                    $"Roll: {string.Join(", ", outcome.Values)}\n" +
                    $"Total: {outcome.Total} | Score +{scoreGain}\n" +
                    coinSummary;
            }
            else
            {
                rollText.text =
                    $"Total: {outcome.Total}\n" +
                    $"Score +{scoreGain} | {coinSummary}";
            }
        }

        private void RefreshRoundInfoPanelText()
        {
            EnsureRoundInfoUiBuilt();

            if (roundInfoTitleText != null)
            {
                roundInfoTitleText.text = "Run Info";
                roundInfoTitleText.fontSize = 30f;
            }

            if (roundInfoText == null)
            {
                RefreshRoundInfoTabButtons();
                return;
            }

            roundInfoText.text = GetRoundInfoTabContent(currentRoundInfoTab);
            roundInfoText.fontSize = 16f;
            roundInfoText.lineSpacing = 0.92f;
            RefreshRoundInfoTabButtons();
        }

        private void EnsureRoundInfoUiBuilt()
        {
            if (roundInfoUiBuilt)
            {
                return;
            }

            if (roundInfoPanel == null)
            {
                return;
            }

            if (roundInfoTitleText == null)
            {
                roundInfoTitleText = FindChildComponent<TMP_Text>(roundInfoPanel.transform, "RoundInfoTitleText");
            }

            if (roundInfoText == null)
            {
                roundInfoText = FindChildComponent<TMP_Text>(roundInfoPanel.transform, "RoundInfoText");
            }

            if (roundInfoScoringButton == null ||
                roundInfoCoinsButton == null ||
                roundInfoActiveEffectsButton == null ||
                roundInfoRunInfoButton == null)
            {
                BuildRoundInfoUiRuntime(roundInfoPanel.transform);
            }

            roundInfoUiBuilt = true;
        }

        private void BuildRoundInfoUiRuntime(Transform roundInfoPanelTransform)
        {
            if (roundInfoPanelTransform == null)
            {
                return;
            }

            RectTransform panelRect = roundInfoPanelTransform as RectTransform;
            if (panelRect != null && panelRect.GetComponent<RectMask2D>() == null)
            {
                panelRect.gameObject.AddComponent<RectMask2D>();
            }

            if (roundInfoTitleText == null)
            {
                roundInfoTitleText = FindChildComponent<TMP_Text>(roundInfoPanelTransform, "RoundInfoTitleText");
            }

            if (roundInfoText == null)
            {
                roundInfoText = FindChildComponent<TMP_Text>(roundInfoPanelTransform, "RoundInfoText");
            }

            Transform summaryArea = FindChildTransform(roundInfoPanelTransform, "RoundInfoSummaryArea");
            if (summaryArea == null)
            {
                summaryArea = CreateRuntimeRect("RoundInfoSummaryArea", roundInfoPanelTransform);
                summaryArea.SetSiblingIndex(0);
                ConfigureRuntimeVerticalLayout(summaryArea as RectTransform, 0, 0, TextAnchor.UpperCenter);
                LayoutElement summaryLayout = summaryArea.gameObject.AddComponent<LayoutElement>();
                summaryLayout.minHeight = 112f;
                summaryLayout.preferredHeight = 112f;
                summaryLayout.flexibleHeight = 0f;
            }

            if (summaryArea.GetComponent<RectMask2D>() == null)
            {
                summaryArea.gameObject.AddComponent<RectMask2D>();
            }

            Transform tabsArea = FindChildTransform(roundInfoPanelTransform, "RoundInfoTabsArea");
            if (tabsArea == null)
            {
                tabsArea = CreateRuntimeRect("RoundInfoTabsArea", roundInfoPanelTransform);
                tabsArea.SetSiblingIndex(Mathf.Min(1, roundInfoPanelTransform.childCount - 1));
                ConfigureRuntimeVerticalLayout(tabsArea as RectTransform, 12, 0, TextAnchor.UpperLeft);
                LayoutElement tabsLayout = tabsArea.gameObject.AddComponent<LayoutElement>();
                tabsLayout.minHeight = 0f;
                tabsLayout.preferredHeight = 0f;
                tabsLayout.flexibleHeight = 1f;
                tabsLayout.flexibleWidth = 1f;
            }

            if (rollText != null && rollText.transform.parent != summaryArea)
            {
                rollText.transform.SetParent(summaryArea, false);
            }

            if (rollText != null)
            {
                rollText.transform.SetSiblingIndex(0);
                ConfigureRuntimeRollText(rollText);
            }

            if (roundInfoTitleText != null && roundInfoTitleText.transform.parent != tabsArea)
            {
                roundInfoTitleText.transform.SetParent(tabsArea, false);
            }

            Transform tabRow = FindChildTransform(tabsArea, "RoundInfoTabRow");
            if (tabRow == null)
            {
                tabRow = CreateRuntimeRect("RoundInfoTabRow", tabsArea);
                tabRow.SetSiblingIndex(Mathf.Min(1, tabsArea.childCount - 1));
                ConfigureRuntimeHorizontalLayout(tabRow as RectTransform, 8, 0);

                LayoutElement rowLayout = tabRow.gameObject.AddComponent<LayoutElement>();
                rowLayout.minHeight = 36f;
                rowLayout.preferredHeight = 36f;
                rowLayout.flexibleWidth = 1f;
            }

            roundInfoScoringButton = roundInfoScoringButton ?? FindChildComponent<Button>(tabsArea, "RoundInfoScoringButton");
            roundInfoCoinsButton = roundInfoCoinsButton ?? FindChildComponent<Button>(tabsArea, "RoundInfoCoinsButton");
            roundInfoActiveEffectsButton = roundInfoActiveEffectsButton ?? FindChildComponent<Button>(tabsArea, "RoundInfoActiveEffectsButton");
            roundInfoRunInfoButton = roundInfoRunInfoButton ?? FindChildComponent<Button>(tabsArea, "RoundInfoRunInfoButton");

            if (roundInfoScoringButton == null)
            {
                roundInfoScoringButton = CreateRuntimeRoundInfoTabButton(tabRow, "RoundInfoScoringButton", "Scoring");
            }

            if (roundInfoCoinsButton == null)
            {
                roundInfoCoinsButton = CreateRuntimeRoundInfoTabButton(tabRow, "RoundInfoCoinsButton", "Coins");
            }

            if (roundInfoActiveEffectsButton == null)
            {
                roundInfoActiveEffectsButton = CreateRuntimeRoundInfoTabButton(tabRow, "RoundInfoActiveEffectsButton", "Effects");
            }

            if (roundInfoRunInfoButton == null)
            {
                roundInfoRunInfoButton = CreateRuntimeRoundInfoTabButton(tabRow, "RoundInfoRunInfoButton", "Run Info");
            }

            if (roundInfoScoringButton != null && roundInfoScoringButton.transform.parent != tabRow)
            {
                roundInfoScoringButton.transform.SetParent(tabRow, false);
            }
            if (roundInfoCoinsButton != null && roundInfoCoinsButton.transform.parent != tabRow)
            {
                roundInfoCoinsButton.transform.SetParent(tabRow, false);
            }
            if (roundInfoActiveEffectsButton != null && roundInfoActiveEffectsButton.transform.parent != tabRow)
            {
                roundInfoActiveEffectsButton.transform.SetParent(tabRow, false);
            }
            if (roundInfoRunInfoButton != null && roundInfoRunInfoButton.transform.parent != tabRow)
            {
                roundInfoRunInfoButton.transform.SetParent(tabRow, false);
            }

            Transform divider = FindChildTransform(tabsArea, "Divider");
            if (divider == null)
            {
                Transform orphanedDivider = FindChildTransform(roundInfoPanelTransform, "Divider");
                if (orphanedDivider != null)
                {
                    orphanedDivider.SetParent(tabsArea, false);
                    divider = orphanedDivider;
                }
            }

            if (divider == null)
            {
                divider = CreateRuntimeRect("Divider", tabsArea);
                LayoutElement dividerLayout = divider.gameObject.AddComponent<LayoutElement>();
                dividerLayout.minHeight = 2f;
                dividerLayout.preferredHeight = 2f;
                dividerLayout.flexibleWidth = 1f;
            }

            Transform contentCard = FindChildTransform(tabsArea, "RoundInfoContentCard");
            if (contentCard == null)
            {
                Transform orphanedContentCard = FindChildTransform(roundInfoPanelTransform, "RoundInfoContentCard");
                if (orphanedContentCard != null)
                {
                    orphanedContentCard.SetParent(tabsArea, false);
                    contentCard = orphanedContentCard;
                }
            }

            if (contentCard == null)
            {
                contentCard = CreateRuntimeRect("RoundInfoContentCard", tabsArea);
                LayoutElement contentLayout = contentCard.gameObject.AddComponent<LayoutElement>();
                contentLayout.minHeight = 180f;
                contentLayout.flexibleHeight = 1f;
                contentLayout.flexibleWidth = 1f;
            }

            if (contentCard.GetComponent<RectMask2D>() == null)
            {
                contentCard.gameObject.AddComponent<RectMask2D>();
            }

            if (roundInfoText != null && roundInfoText.transform.parent != contentCard)
            {
                roundInfoText.transform.SetParent(contentCard, false);
            }

            BindRoundInfoTabButtons();
            RefreshRoundInfoTabButtons();
        }

        private Button CreateRuntimeRoundInfoTabButton(Transform parent, string name, string label)
        {
            Button button = CreateRuntimeButton(parent, name, label, 36f, 0f, 12f);
            TMP_Text labelText = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (labelText != null)
            {
                labelText.textWrappingMode = TextWrappingModes.NoWrap;
                labelText.overflowMode = TextOverflowModes.Ellipsis;
                labelText.margin = Vector4.zero;
            }

            LayoutElement layoutElement = button != null ? button.GetComponent<LayoutElement>() : null;
            if (layoutElement != null)
            {
                layoutElement.flexibleWidth = 1f;
                layoutElement.preferredWidth = 0f;
            }

            return button;
        }

        private void BindRoundInfoTabButtons()
        {
            BindButton(roundInfoScoringButton, () => SelectRoundInfoTab(RoundInfoTab.Scoring));
            BindButton(roundInfoCoinsButton, () => SelectRoundInfoTab(RoundInfoTab.Coins));
            BindButton(roundInfoActiveEffectsButton, () => SelectRoundInfoTab(RoundInfoTab.ActiveEffects));
            BindButton(roundInfoRunInfoButton, () => SelectRoundInfoTab(RoundInfoTab.RunInfo));
        }

        private void SelectRoundInfoTab(RoundInfoTab tab)
        {
            if (currentRoundInfoTab == tab)
            {
                RefreshRoundInfoPanelText();
                return;
            }

            currentRoundInfoTab = tab;
            RefreshRoundInfoPanelText();
        }

        private void RefreshRoundInfoTabButtons()
        {
            SetRoundInfoTabButtonState(roundInfoScoringButton, currentRoundInfoTab == RoundInfoTab.Scoring);
            SetRoundInfoTabButtonState(roundInfoCoinsButton, currentRoundInfoTab == RoundInfoTab.Coins);
            SetRoundInfoTabButtonState(roundInfoActiveEffectsButton, currentRoundInfoTab == RoundInfoTab.ActiveEffects);
            SetRoundInfoTabButtonState(roundInfoRunInfoButton, currentRoundInfoTab == RoundInfoTab.RunInfo);
        }

        private static void SetRoundInfoTabButtonState(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = selected
                    ? new Color(0.22f, 0.53f, 0.85f, 1f)
                    : new Color(0.18f, 0.22f, 0.28f, 1f);
            }

            ColorBlock colors = button.colors;
            colors.normalColor = selected
                ? new Color(0.22f, 0.53f, 0.85f, 1f)
                : new Color(0.18f, 0.22f, 0.28f, 1f);
            colors.highlightedColor = selected
                ? new Color(0.26f, 0.58f, 0.90f, 1f)
                : new Color(0.24f, 0.29f, 0.36f, 1f);
            colors.pressedColor = selected
                ? new Color(0.17f, 0.43f, 0.70f, 1f)
                : new Color(0.15f, 0.18f, 0.24f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            TMP_Text labelText = button.GetComponentInChildren<TMP_Text>(true);
            if (labelText != null)
            {
                labelText.color = selected ? Color.white : new Color(0.91f, 0.93f, 0.96f, 1f);
            }
        }

        private string GetRoundInfoTabContent(RoundInfoTab tab)
        {
            switch (tab)
            {
                case RoundInfoTab.Coins:
                    return
                        "Coins\n" +
                        $"- Base reward: +{coinSettings.baseCoinReward}\n" +
                        $"- Minimum per roll: +{coinSettings.minimumCoinsPerRoll}\n" +
                        $"- High-face bonus: +{coinSettings.highFaceBonusPerDie}\n" +
                        $"- Combo bonuses apply\n" +
                        $"- Spend action: -{coinSettings.spendCoinsCost}\n\n" +
                        $"Run Coins: {runCoins}\n" +
                        $"Coins Earned: {totalCoinsEarned}";
                case RoundInfoTab.ActiveEffects:
                    return
                        "Active Effects\n" +
                        "- No active modifiers yet.\n\n" +
                        "Future hooks:\n" +
                        "- upgrades\n" +
                        "- debuffs\n" +
                        "- special dice";
                case RoundInfoTab.RunInfo:
                    return
                        "Run Info\n" +
                        $"- Roll with your bound roll key.\n" +
                        $"- Cash Out converts the run into Shards.\n" +
                        $"- Shards scale with score and coins.\n" +
                        $"- Spend Coins is a placeholder action.\n\n" +
                        $"Rolls: {completedRolls}\n" +
                        $"Score: {currentScore}\n" +
                        $"Best Score: {highestScoreReached}\n" +
                        $"Shards: {shards}";
                case RoundInfoTab.Scoring:
                default:
                    return
                        "Scoring\n" +
                        $"- Pips x {scoreSettings.pointsPerPip}\n" +
                        $"- High faces: +{scoreSettings.highFaceBonusPerDie}\n" +
                        $"- Pair bonus: +{scoreSettings.pairBonus}\n" +
                        $"- Triple bonus: +{scoreSettings.tripleBonus}\n" +
                        $"- Straight bonus: +{scoreSettings.straightBonus}\n" +
                        $"- Final score x{scoreSettings.scoreMultiplier:0.##}\n\n" +
                        $"Score: {currentScore}\n" +
                        $"Best Score: {highestScoreReached}";
            }
        }

        private void NormalizeDropdownArrow(TMP_Dropdown dropdown)
        {
            if (dropdown == null)
            {
                return;
            }

            Transform arrowTransform = dropdown.transform.Find("Arrow");
            if (arrowTransform == null)
            {
                return;
            }

            TMP_Text arrowText = arrowTransform.GetComponent<TMP_Text>();
            if (arrowText != null)
            {
                arrowText.text = "v";
                arrowText.raycastTarget = false;
            }
        }

        private void LoadShards()
        {
            shards = Mathf.Max(0, PlayerPrefs.GetInt(ShardsPlayerPrefsKey, 0));
        }

        private void SaveShards()
        {
            PlayerPrefs.SetInt(ShardsPlayerPrefsKey, shards);
            PlayerPrefs.Save();
            RefreshShardsText();
        }

        private void RefreshRoundActionInteractivity()
        {
            bool canUseRunAction = CanUseRunAction();

            if (roundSpendCoinsButton != null)
            {
                roundSpendCoinsButton.interactable = canUseRunAction && runCoins >= coinSettings.spendCoinsCost;
            }

            if (roundCashOutButton != null)
            {
                roundCashOutButton.interactable = CanCashOut();
            }
        }

        private bool CanUseRunAction()
        {
            return runActive &&
                   currentState == ViewState.Round &&
                   diceManager != null &&
                   !diceManager.IsRolling;
        }

        private bool CanCashOut()
        {
            return CanUseRunAction() && completedRolls > 0;
        }

        private static int CountDistinctPairs(int[] values)
        {
            if (values == null || values.Length < 2)
            {
                return 0;
            }

            if (IsTriple(values))
            {
                return 0;
            }

            return values[0] == values[1] || values[0] == values[2] || values[1] == values[2] ? 1 : 0;
        }

        private static bool IsTriple(int[] values)
        {
            return values != null &&
                   values.Length >= 3 &&
                   values[0] == values[1] &&
                   values[1] == values[2];
        }

        private static bool IsStraight(int[] values)
        {
            if (values == null || values.Length < 3)
            {
                return false;
            }

            int[] sorted = (int[])values.Clone();
            System.Array.Sort(sorted);
            return sorted[0] + 1 == sorted[1] && sorted[1] + 1 == sorted[2];
        }

        private static int CountHighFaces(int[] values)
        {
            if (values == null)
            {
                return 0;
            }

            int count = 0;

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] >= 5)
                {
                    count++;
                }
            }

            return count;
        }

        private static void SetHudObjectActive(Component target, bool active)
        {
            if (target != null)
            {
                target.gameObject.SetActive(active);
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private void ApplyRunHudLayout()
        {
            if (roundUtilityBar != null)
            {
                RectTransform barRect = roundUtilityBar.transform as RectTransform;
                if (barRect != null)
                {
                    barRect.anchorMin = new Vector2(0.14f, 1f);
                    barRect.anchorMax = new Vector2(0.86f, 1f);
                    barRect.pivot = new Vector2(0.5f, 1f);
                    barRect.anchoredPosition = new Vector2(0f, -24f);
                    barRect.sizeDelta = new Vector2(0f, 68f);
                }

                HorizontalLayoutGroup layout = roundUtilityBar.GetComponent<HorizontalLayoutGroup>();
                if (layout != null)
                {
                    layout.spacing = 14f;
                    layout.padding = new RectOffset(16, 16, 16, 16);
                    layout.childAlignment = TextAnchor.MiddleLeft;
                    layout.childControlWidth = true;
                    layout.childControlHeight = true;
                    layout.childForceExpandWidth = false;
                    layout.childForceExpandHeight = false;
                }

                Transform statsGroup = FindChildTransform(roundUtilityBar.transform, "RoundHudStatsGroup");
                if (statsGroup == null)
                {
                    statsGroup = CreateRunHudStatsGroup(roundUtilityBar.transform);
                }

                Transform spacer = FindChildTransform(roundUtilityBar.transform, "RoundHudSpacer");
                if (spacer == null)
                {
                    spacer = CreateRunHudSpacer(roundUtilityBar.transform);
                }

                if (statsGroup != null)
                {
                    statsGroup.SetAsFirstSibling();
                }

                if (spacer != null)
                {
                    spacer.SetSiblingIndex(1);
                }

                if (coinsText != null && coinsText.transform.parent != statsGroup)
                {
                    coinsText.transform.SetParent(statsGroup, false);
                }

                if (scoreText != null && scoreText.transform.parent != statsGroup)
                {
                    scoreText.transform.SetParent(statsGroup, false);
                }

                if (coinsText != null)
                {
                    coinsText.text = runActive ? $"Coins: {runCoins}" : "Coins: -";
                    ConfigureRuntimeHudStatText(coinsText, 172f);
                }

                if (scoreText != null)
                {
                    scoreText.text = runActive ? $"Score: {currentScore}" : "Score: -";
                    ConfigureRuntimeHudStatText(scoreText, 172f);
                }

                ConfigureRuntimeButtonLabelFit(roundChallengesButton, 18f, 24f);
                ConfigureRuntimeButtonLabelFit(roundSettingsButton, 18f, 24f);
                ConfigureRuntimeButtonLabelFit(roundSpendCoinsButton, 18f, 24f);
                ConfigureRuntimeButtonLabelFit(roundCashOutButton, 18f, 24f);
                ConfigureRuntimeButtonLabelFit(roundMainMenuButton, 18f, 24f);
            }

            if (roundInfoPanel != null)
            {
                RectTransform panelRect = roundInfoPanel.transform as RectTransform;
                if (panelRect != null)
                {
                    panelRect.anchorMin = new Vector2(0f, 0.5f);
                    panelRect.anchorMax = new Vector2(0f, 0.5f);
                    panelRect.pivot = new Vector2(0f, 0.5f);
                    panelRect.anchoredPosition = new Vector2(28f, 8f);
                    panelRect.sizeDelta = new Vector2(380f, 560f);
                }

                if (roundInfoPanel.GetComponent<RectMask2D>() == null)
                {
                    roundInfoPanel.AddComponent<RectMask2D>();
                }

                VerticalLayoutGroup layout = roundInfoPanel.GetComponent<VerticalLayoutGroup>();
                if (layout != null)
                {
                    layout.spacing = 14f;
                    layout.padding = new RectOffset(24, 24, 24, 24);
                    layout.childAlignment = TextAnchor.UpperLeft;
                    layout.childControlWidth = true;
                    layout.childControlHeight = true;
                    layout.childForceExpandWidth = true;
                    layout.childForceExpandHeight = false;
                }

                Transform summaryArea = FindChildTransform(roundInfoPanel.transform, "RoundInfoSummaryArea");
                if (summaryArea == null)
                {
                    summaryArea = CreateRuntimeRect("RoundInfoSummaryArea", roundInfoPanel.transform);
                    summaryArea.SetSiblingIndex(0);
                    ConfigureRuntimeVerticalLayout(summaryArea as RectTransform, 0, 0, TextAnchor.UpperCenter);
                    LayoutElement summaryLayout = summaryArea.gameObject.AddComponent<LayoutElement>();
                    summaryLayout.minHeight = 112f;
                    summaryLayout.preferredHeight = 112f;
                    summaryLayout.flexibleHeight = 0f;
                }

                if (summaryArea.GetComponent<RectMask2D>() == null)
                {
                    summaryArea.gameObject.AddComponent<RectMask2D>();
                }

                Transform tabsArea = FindChildTransform(roundInfoPanel.transform, "RoundInfoTabsArea");
                if (tabsArea == null)
                {
                    tabsArea = CreateRuntimeRect("RoundInfoTabsArea", roundInfoPanel.transform);
                    tabsArea.SetSiblingIndex(Mathf.Min(1, roundInfoPanel.transform.childCount - 1));
                    ConfigureRuntimeVerticalLayout(tabsArea as RectTransform, 12, 0, TextAnchor.UpperLeft);
                    LayoutElement tabsLayout = tabsArea.gameObject.AddComponent<LayoutElement>();
                    tabsLayout.minHeight = 0f;
                    tabsLayout.preferredHeight = 0f;
                    tabsLayout.flexibleHeight = 1f;
                    tabsLayout.flexibleWidth = 1f;
                }

                if (rollText != null && rollText.transform.parent != summaryArea)
                {
                    rollText.transform.SetParent(summaryArea, false);
                }

                if (rollText != null)
                {
                    rollText.transform.SetSiblingIndex(0);
                    ConfigureRuntimeRollText(rollText);
                }

                if (roundInfoTitleText != null && roundInfoTitleText.transform.parent != tabsArea)
                {
                    roundInfoTitleText.transform.SetParent(tabsArea, false);
                }

                Transform tabRow = FindChildTransform(tabsArea, "RoundInfoTabRow");
                if (tabRow == null)
                {
                    tabRow = CreateRuntimeRect("RoundInfoTabRow", tabsArea);
                    tabRow.SetSiblingIndex(Mathf.Min(1, tabsArea.childCount - 1));
                    ConfigureRuntimeHorizontalLayout(tabRow as RectTransform, 8, 0);
                    LayoutElement rowLayout = tabRow.gameObject.AddComponent<LayoutElement>();
                    rowLayout.minHeight = 36f;
                    rowLayout.preferredHeight = 36f;
                    rowLayout.flexibleWidth = 1f;
                }

                if (roundInfoScoringButton != null && roundInfoScoringButton.transform.parent != tabRow)
                {
                    roundInfoScoringButton.transform.SetParent(tabRow, false);
                }
                if (roundInfoCoinsButton != null && roundInfoCoinsButton.transform.parent != tabRow)
                {
                    roundInfoCoinsButton.transform.SetParent(tabRow, false);
                }
                if (roundInfoActiveEffectsButton != null && roundInfoActiveEffectsButton.transform.parent != tabRow)
                {
                    roundInfoActiveEffectsButton.transform.SetParent(tabRow, false);
                }
                if (roundInfoRunInfoButton != null && roundInfoRunInfoButton.transform.parent != tabRow)
                {
                    roundInfoRunInfoButton.transform.SetParent(tabRow, false);
                }

                Transform divider = FindChildTransform(tabsArea, "Divider");
                if (divider == null)
                {
                    Transform orphanedDivider = FindChildTransform(roundInfoPanel.transform, "Divider");
                    if (orphanedDivider != null)
                    {
                        orphanedDivider.SetParent(tabsArea, false);
                        divider = orphanedDivider;
                    }
                }

                if (divider == null)
                {
                    divider = CreateRuntimeRect("Divider", tabsArea);
                    LayoutElement dividerLayout = divider.gameObject.AddComponent<LayoutElement>();
                    dividerLayout.minHeight = 2f;
                    dividerLayout.preferredHeight = 2f;
                    dividerLayout.flexibleWidth = 1f;
                }

                Transform contentCard = FindChildTransform(tabsArea, "RoundInfoContentCard");
                if (contentCard == null)
                {
                    Transform orphanedContentCard = FindChildTransform(roundInfoPanel.transform, "RoundInfoContentCard");
                    if (orphanedContentCard != null)
                    {
                        orphanedContentCard.SetParent(tabsArea, false);
                        contentCard = orphanedContentCard;
                    }
                }

                if (contentCard == null)
                {
                    contentCard = CreateRuntimeRect("RoundInfoContentCard", tabsArea);
                    LayoutElement contentLayout = contentCard.gameObject.AddComponent<LayoutElement>();
                    contentLayout.minHeight = 180f;
                    contentLayout.flexibleHeight = 1f;
                    contentLayout.flexibleWidth = 1f;
                }

                if (contentCard.GetComponent<RectMask2D>() == null)
                {
                    contentCard.gameObject.AddComponent<RectMask2D>();
                }

                if (roundInfoText != null && roundInfoText.transform.parent != contentCard)
                {
                    roundInfoText.transform.SetParent(contentCard, false);
                }
            }

            if (roundUtilityBar != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(roundUtilityBar.transform as RectTransform);
            }

            if (roundInfoPanel != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(roundInfoPanel.transform as RectTransform);
            }
        }

        private Transform CreateRunHudStatsGroup(Transform parent)
        {
            RectTransform group = CreateRuntimeRect("RoundHudStatsGroup", parent);
            ConfigureRuntimeHorizontalLayout(group, 12, 0);

            HorizontalLayoutGroup layout = group.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            LayoutElement layoutElement = group.gameObject.AddComponent<LayoutElement>();
            layoutElement.minWidth = 360f;
            layoutElement.preferredWidth = 360f;
            layoutElement.minHeight = 68f;
            layoutElement.preferredHeight = 68f;
            layoutElement.flexibleWidth = 0f;
            return group;
        }

        private Transform CreateRunHudSpacer(Transform parent)
        {
            RectTransform spacer = CreateRuntimeRect("RoundHudSpacer", parent);
            LayoutElement layoutElement = spacer.gameObject.AddComponent<LayoutElement>();
            layoutElement.minWidth = 0f;
            layoutElement.preferredWidth = 0f;
            layoutElement.flexibleWidth = 1f;
            return spacer;
        }

        private static void ConfigureRuntimeHudStatText(TMP_Text text, float width)
        {
            if (text == null)
            {
                return;
            }

            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.fontSize = 22f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 22f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.lineSpacing = 1f;

            LayoutElement layoutElement = text.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.minWidth = width;
                layoutElement.preferredWidth = width;
                layoutElement.minHeight = 40f;
                layoutElement.preferredHeight = 40f;
                layoutElement.flexibleWidth = 0f;
            }
        }

        private static void ConfigureRuntimeRollText(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            text.alignment = TextAlignmentOptions.Top;
            text.fontSize = 23f;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.lineSpacing = 1.02f;

            LayoutElement layoutElement = text.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = text.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minWidth = 0f;
            layoutElement.preferredWidth = 0f;
            layoutElement.flexibleWidth = 1f;
            if (layoutElement != null)
            {
                layoutElement.minHeight = 104f;
                layoutElement.preferredHeight = 104f;
                layoutElement.flexibleHeight = 0f;
            }
        }

        private static void ConfigureRuntimeButtonLabelFit(Button button, float minFontSize, float maxFontSize)
        {
            if (button == null)
            {
                return;
            }

            TMP_Text labelText = button.GetComponentInChildren<TMP_Text>(true);
            if (labelText == null)
            {
                return;
            }

            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = minFontSize;
            labelText.fontSizeMax = maxFontSize;
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Overflow;
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static Transform FindChildTransform(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static GameObject FindChildGameObject(Transform root, string name)
        {
            Transform child = FindChildTransform(root, name);
            return child != null ? child.gameObject : null;
        }

        private static T FindChildComponent<T>(Transform root, string name) where T : Component
        {
            Transform child = FindChildTransform(root, name);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static bool WasBackPressed()
        {
            return GameSettingsService.WasBackPressed();
        }
    }
}
