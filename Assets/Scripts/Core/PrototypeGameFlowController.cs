using System.Collections.Generic;
using DiceMadness.Dice;
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
        private static readonly string[] DiceUnlocksLines =
        {
            "Loaded D6: Unlock an alternate weighted prototype die.",
            "Frost Die: Placeholder for future elemental dice variants.",
            "Twin Face Die: Placeholder for repeated-value dice experiments.",
        };

        private static readonly string[] EfficiencyLines =
        {
            "Auto Roll I: Placeholder for passive round automation.",
            "Tray Magnet: Placeholder for faster post-roll cleanup.",
            "Quick Evaluate: Placeholder for shorter reveal timings.",
        };

        private static readonly string[] ScoreLines =
        {
            "Combo Lens: Placeholder scoring item for streak bonuses.",
            "Lucky Ledger: Placeholder meta item for better payout conversion.",
            "Prime Chip: Placeholder multiplier upgrade for specific totals.",
        };

        private static readonly string[] ChallengeLines =
        {
            "Roll three even results in one round.",
            "Finish a round with a total of 15 or more.",
            "Win a future challenge run without buying an upgrade.",
        };

        private static readonly string[] SettingsLines =
        {
            "Presentation Speed: currently tuned in the DiceManager inspector.",
            "Audio Mix: placeholder for future music / SFX sliders.",
            "Accessibility: placeholder for larger text and reduced motion options.",
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

        [Header("Scene References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private DiceManager diceManager;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private TMP_Text rollText;

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject challengesPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject roundUtilityBar;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button mainMenuShopButton;
        [SerializeField] private Button mainMenuChallengesButton;
        [SerializeField] private Button mainMenuSettingsButton;

        [Header("Shop")]
        [SerializeField] private Button shopDiceUnlocksButton;
        [SerializeField] private Button shopEfficiencyButton;
        [SerializeField] private Button shopScoreMultipliersButton;
        [SerializeField] private Button shopReturnButton;
        [SerializeField] private TMP_Text shopContextText;
        [SerializeField] private TMP_Text shopSectionTitleText;
        [SerializeField] private TMP_Text shopContentText;

        [Header("Challenges")]
        [SerializeField] private Button challengesBackButton;
        [SerializeField] private TMP_Text challengesContentText;

        [Header("Settings")]
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private TMP_Text settingsContentText;

        [Header("In-Round Utility")]
        [SerializeField] private Button roundChallengesButton;
        [SerializeField] private Button roundSettingsButton;
        [SerializeField] private Button roundMainMenuButton;

        private readonly List<GameObject> roundRoots = new List<GameObject>();
        private ViewState currentState;
        private bool listenersBound;

        private void Reset()
        {
            AutoWireSceneReferences();
        }

        private void Awake()
        {
            AutoWireSceneReferences();
            EnsureEventSystem();
            DiscoverRoundRoots();
            BindButtonListeners();
            PopulateStaticPanelText();
            PushRollTextReference();
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
            if (WasBackPressed())
            {
                HandleBackAction();
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        [ContextMenu("Auto Wire Scene References")]
        private void AutoWireSceneReferences()
        {
            canvas ??= FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            diceManager ??= FindFirstObjectByType<DiceManager>(FindObjectsInactive.Include);
            eventSystem ??= FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);

            Transform uiRoot = canvas != null ? FindChildTransform(canvas.transform, "MenuUIRoot") : null;
            Transform searchRoot = uiRoot != null ? uiRoot : canvas != null ? canvas.transform : null;

            if (searchRoot == null)
            {
                return;
            }

            rollText ??= FindChildComponent<TMP_Text>(searchRoot, "RollText");
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
            shopContextText ??= FindChildComponent<TMP_Text>(searchRoot, "ShopContextText");
            shopSectionTitleText ??= FindChildComponent<TMP_Text>(searchRoot, "ShopSectionTitleText");
            shopContentText ??= FindChildComponent<TMP_Text>(searchRoot, "ShopContentText");

            challengesBackButton ??= FindChildComponent<Button>(searchRoot, "ChallengesBackButton");
            challengesContentText ??= FindChildComponent<TMP_Text>(searchRoot, "ChallengesContentText");

            settingsBackButton ??= FindChildComponent<Button>(searchRoot, "SettingsBackButton");
            settingsContentText ??= FindChildComponent<TMP_Text>(searchRoot, "SettingsContentText");

            roundChallengesButton ??= FindChildComponent<Button>(searchRoot, "RoundChallengesButton");
            roundSettingsButton ??= FindChildComponent<Button>(searchRoot, "RoundSettingsButton");
            roundMainMenuButton ??= FindChildComponent<Button>(searchRoot, "RoundMainMenuButton");
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
            BindButton(shopReturnButton, EnterMainMenu);

            BindButton(challengesBackButton, HandleBackAction);
            BindButton(settingsBackButton, HandleBackAction);

            BindButton(roundChallengesButton, EnterRoundChallenges);
            BindButton(roundSettingsButton, EnterRoundSettings);
            BindButton(roundMainMenuButton, EnterMainMenu);

            listenersBound = true;
        }

        private void PopulateStaticPanelText()
        {
            SetShopSection(ShopSection.DiceUnlocks);

            if (challengesContentText != null)
            {
                challengesContentText.text = string.Join("\n\n", ChallengeLines);
            }

            if (settingsContentText != null)
            {
                settingsContentText.text = string.Join("\n\n", SettingsLines);
            }
        }

        private void PushRollTextReference()
        {
            if (diceManager != null && rollText != null)
            {
                diceManager.SetResultText(rollText);
            }
        }

        private bool HasRequiredReferences()
        {
            return canvas != null &&
                   diceManager != null &&
                   mainMenuPanel != null &&
                   shopPanel != null &&
                   challengesPanel != null &&
                   settingsPanel != null &&
                   roundUtilityBar != null;
        }

        private void StartRound()
        {
            Time.timeScale = 1f;
            currentState = ViewState.Round;
            SetRoundRootsActive(true);
            diceManager.ResetRoundState();
            diceManager.SetRollInputEnabled(true);
            SetHudVisible(true);
            SetOnlyPanelActive(roundUtilityBar);
        }

        private void ResumeRound()
        {
            Time.timeScale = 1f;
            currentState = ViewState.Round;
            diceManager.SetRollInputEnabled(true);
            SetHudVisible(true);
            SetOnlyPanelActive(roundUtilityBar);
        }

        private void EnterMainMenu()
        {
            Time.timeScale = 1f;
            currentState = ViewState.MainMenu;
            diceManager.SetRollInputEnabled(false);
            diceManager.ResetRoundState();
            SetRoundRootsActive(false);
            SetHudVisible(false);
            SetOnlyPanelActive(mainMenuPanel);
        }

        private void EnterMetaShop()
        {
            Time.timeScale = 1f;
            currentState = ViewState.Shop;
            diceManager.SetRollInputEnabled(false);
            SetRoundRootsActive(false);
            SetHudVisible(false);

            if (shopContextText != null)
            {
                shopContextText.text = "Meta Progression";
            }

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
            SetOnlyPanelActive(settingsPanel);
        }

        private void EnterRoundChallenges()
        {
            Time.timeScale = 0f;
            currentState = ViewState.RoundChallenges;
            diceManager.SetRollInputEnabled(false);

            if (rollText != null)
            {
                rollText.gameObject.SetActive(false);
            }

            SetOnlyPanelActive(challengesPanel);
        }

        private void EnterRoundSettings()
        {
            Time.timeScale = 0f;
            currentState = ViewState.RoundSettings;
            diceManager.SetRollInputEnabled(false);

            if (rollText != null)
            {
                rollText.gameObject.SetActive(false);
            }

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
            if (shopSectionTitleText == null || shopContentText == null)
            {
                return;
            }

            switch (section)
            {
                case ShopSection.DiceUnlocks:
                    shopSectionTitleText.text = "Dice Unlocks";
                    shopContentText.text = string.Join("\n\n", DiceUnlocksLines);
                    break;
                case ShopSection.Efficiency:
                    shopSectionTitleText.text = "Efficiency / Automation";
                    shopContentText.text = string.Join("\n\n", EfficiencyLines);
                    break;
                case ShopSection.ScoreMultipliers:
                    shopSectionTitleText.text = "Score Multipliers / Scoring Items";
                    shopContentText.text = string.Join("\n\n", ScoreLines);
                    break;
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
            if (rollText != null)
            {
                rollText.gameObject.SetActive(visible);
            }

            if (roundUtilityBar != null)
            {
                roundUtilityBar.SetActive(visible && currentState == ViewState.Round);
            }
        }

        private void SetOnlyPanelActive(GameObject activePanel)
        {
            SetActive(mainMenuPanel, activePanel == mainMenuPanel);
            SetActive(shopPanel, activePanel == shopPanel);
            SetActive(challengesPanel, activePanel == challengesPanel);
            SetActive(settingsPanel, activePanel == settingsPanel);
            SetActive(roundUtilityBar, activePanel == roundUtilityBar);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
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
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                return true;
            }
#endif

            return false;
        }
    }
}
