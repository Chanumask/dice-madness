using System;
using System.Collections;
using UnityEngine;
using DiceMadness.Dice;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DiceMadness.Core
{
    public class DiceManager : MonoBehaviour
    {
        public readonly struct RollOutcome
        {
            public readonly int[] Values;
            public readonly int Total;

            public RollOutcome(int[] values, int total)
            {
                Values = values;
                Total = total;
            }
        }

        // Coordinates weighted outcome selection, roll animation, evaluation motion, and the on-screen result text.
        [SerializeField] private DiceRoller[] dice;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private float presentationDuration = 0.45f;
        [SerializeField] private float evaluationDistanceFromCamera = 6f;
        [SerializeField] private float evaluationHeightOffset = -0.4f;
        [SerializeField] private float evaluationSpacing = 2.15f;
        [SerializeField] private float presentationLift = 0.18f;
        [SerializeField] private float minTableImpactSfxInterval = 0.07f;
        [SerializeField] private float minDiceImpactSfxInterval = 0.05f;

        private Vector3[] startPositions;
        private bool isRolling;
        private bool allowRollInput = true;
        private float lastTableImpactSfxTime = float.NegativeInfinity;
        private float lastDiceImpactSfxTime = float.NegativeInfinity;

        public event Action<RollOutcome> RollResolved;

        public bool IsRolling => isRolling;

        private void Awake()
        {
            if (dice == null || dice.Length == 0)
            {
                dice = FindObjectsByType<DiceRoller>(FindObjectsInactive.Exclude);
            }

            CacheStartTransforms();
            RegisterDiceImpactCallbacks();
            ShowIdleText();
        }

        private void OnDestroy()
        {
            UnregisterDiceImpactCallbacks();
        }

        private void Update()
        {
            if (allowRollInput && !isRolling && WasRollPressed())
            {
                StartCoroutine(RollRoutine());
            }
        }

        // Lets the setup script assign scene references without manual inspector work.
        public void SetReferences(DiceRoller[] diceToManage, TMP_Text outputText)
        {
            UnregisterDiceImpactCallbacks();
            dice = diceToManage;
            resultText = outputText;
            CacheStartTransforms();
            RegisterDiceImpactCallbacks();
            ShowIdleText();
        }

        public TMP_Text ResultText => resultText;

        public void SetResultText(TMP_Text outputText)
        {
            resultText = outputText;
            ShowIdleText();
        }

        public void SetRollInputEnabled(bool enabled)
        {
            allowRollInput = enabled;
        }

        public void SetResultSummaryText(string summary)
        {
            if (resultText != null)
            {
                resultText.text = summary;
            }
        }

        // Restores the tray setup so a new round starts from a clean baseline.
        public void ResetRoundState()
        {
            StopAllCoroutines();
            isRolling = false;
            CacheStartTransforms();

            for (int i = 0; i < dice.Length; i++)
            {
                if (dice[i] == null)
                {
                    continue;
                }

                dice[i].ResetToAnchor(startPositions[i], UnityEngine.Random.rotationUniform);
            }

            ShowIdleText();
        }

        private IEnumerator RollRoutine()
        {
            if (dice == null || dice.Length == 0)
            {
                yield break;
            }

            isRolling = true;
            lastTableImpactSfxTime = float.NegativeInfinity;
            lastDiceImpactSfxTime = float.NegativeInfinity;
            ShowRollingText();

            for (int i = 0; i < dice.Length; i++)
            {
                if (dice[i] == null)
                {
                    continue;
                }

                int targetFaceIndex = dice[i].FaceReader.SampleWeightedFaceIndex();
                dice[i].BeginOutcomeRoll(startPositions[i], targetFaceIndex);
            }

            while (!AllRollAnimationsComplete())
            {
                for (int i = 0; i < dice.Length; i++)
                {
                    if (dice[i] != null)
                    {
                        dice[i].AdvanceOutcomeRoll(Time.deltaTime);
                    }
                }

                yield return null;
            }

            for (int i = 0; i < dice.Length; i++)
            {
                if (dice[i] != null)
                {
                    dice[i].CompleteOutcomeRoll();
                }
            }

            int[] physicalValues = ReadValues();
            int total = SumValues(physicalValues);
            yield return PresentDiceForEvaluation();
            ShowResults(physicalValues, total);
            RollResolved?.Invoke(new RollOutcome(physicalValues, total));
            isRolling = false;
        }

        private bool AllRollAnimationsComplete()
        {
            for (int i = 0; i < dice.Length; i++)
            {
                if (dice[i] == null)
                {
                    continue;
                }

                if (!dice[i].IsRollAnimationComplete)
                {
                    return false;
                }
            }

            return true;
        }

        private void CacheStartTransforms()
        {
            if (dice == null)
            {
                startPositions = new Vector3[0];
                return;
            }

            startPositions = new Vector3[dice.Length];

            for (int i = 0; i < dice.Length; i++)
            {
                if (dice[i] == null)
                {
                    continue;
                }

                startPositions[i] = dice[i].transform.position;
            }
        }

        private void RegisterDiceImpactCallbacks()
        {
            if (dice == null)
            {
                return;
            }

            for (int i = 0; i < dice.Length; i++)
            {
                if (dice[i] == null)
                {
                    continue;
                }

                dice[i].ImpactDetected -= HandleDiceImpactDetected;
                dice[i].ImpactDetected += HandleDiceImpactDetected;
            }
        }

        private void UnregisterDiceImpactCallbacks()
        {
            if (dice == null)
            {
                return;
            }

            for (int i = 0; i < dice.Length; i++)
            {
                if (dice[i] == null)
                {
                    continue;
                }

                dice[i].ImpactDetected -= HandleDiceImpactDetected;
            }
        }

        private void HandleDiceImpactDetected(DiceRoller.ImpactAudioType impactType, DiceRoller sourceDie)
        {
            if (!isRolling)
            {
                return;
            }

            float now = Time.unscaledTime;

            switch (impactType)
            {
                case DiceRoller.ImpactAudioType.Dice:
                    if (now - lastDiceImpactSfxTime < minDiceImpactSfxInterval)
                    {
                        return;
                    }

                    lastDiceImpactSfxTime = now;
                    AudioManager.Instance?.PlayDiceHitDice();
                    break;

                case DiceRoller.ImpactAudioType.Table:
                default:
                    if (now - lastTableImpactSfxTime < minTableImpactSfxInterval)
                    {
                        return;
                    }

                    lastTableImpactSfxTime = now;
                    AudioManager.Instance?.PlayDiceHitTable();
                    break;
            }
        }

        private int[] ReadValues()
        {
            int[] values = new int[dice.Length];

            for (int i = 0; i < dice.Length; i++)
            {
                if (dice[i] == null)
                {
                    continue;
                }

                values[i] = dice[i].FaceReader.GetTopFaceValue();
            }

            return values;
        }

        private IEnumerator PresentDiceForEvaluation()
        {
            if (dice == null || dice.Length == 0)
            {
                yield break;
            }

            Camera evaluationCamera = Camera.main;

            if (evaluationCamera == null)
            {
                yield break;
            }

            Vector3[] startPositionsForPresentation = new Vector3[dice.Length];
            Quaternion[] landedRotations = new Quaternion[dice.Length];
            Vector3[] targetPositions = new Vector3[dice.Length];
            Vector3 evaluationCenter = GetEvaluationCenter(evaluationCamera.transform);
            Vector3 rowRight = evaluationCamera.transform.right;
            float rowOffset = (dice.Length - 1) * 0.5f;

            for (int i = 0; i < dice.Length; i++)
            {
                if (dice[i] == null)
                {
                    continue;
                }

                startPositionsForPresentation[i] = dice[i].transform.position;
                landedRotations[i] = dice[i].transform.rotation;
                targetPositions[i] = evaluationCenter + rowRight * ((i - rowOffset) * evaluationSpacing);
                dice[i].BeginEvaluationPresentation();
            }

            float elapsed = 0f;

            while (elapsed < presentationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / presentationDuration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                float lift = Mathf.Sin(eased * Mathf.PI) * presentationLift;

                for (int i = 0; i < dice.Length; i++)
                {
                    if (dice[i] == null)
                    {
                        continue;
                    }

                    Vector3 position = Vector3.Lerp(startPositionsForPresentation[i], targetPositions[i], eased) + Vector3.up * lift;
                    dice[i].SetEvaluationPose(position, landedRotations[i]);
                }

                yield return null;
            }

            for (int i = 0; i < dice.Length; i++)
            {
                if (dice[i] == null)
                {
                    continue;
                }

                dice[i].SetEvaluationPose(targetPositions[i], landedRotations[i]);
            }
        }

        private void ShowResults(int[] values, int total)
        {
            if (resultText == null)
            {
                return;
            }

            resultText.text = $"Roll: {string.Join(", ", values)}\nTotal: {total}";
        }

        private void ShowIdleText()
        {
            if (resultText != null)
            {
                resultText.text = "Roll: -, -, -\nTotal: -";
            }
        }

        private void ShowRollingText()
        {
            if (resultText != null)
            {
                resultText.text = "Rolling...\nTotal: -";
            }
        }

        private Vector3 GetEvaluationCenter(Transform cameraTransform)
        {
            return cameraTransform.position +
                   cameraTransform.forward * evaluationDistanceFromCamera +
                   cameraTransform.up * evaluationHeightOffset;
        }

        private static bool WasRollPressed()
        {
            return GameSettingsService.WasRollPressed();
        }

        private static int SumValues(int[] values)
        {
            int total = 0;

            for (int i = 0; i < values.Length; i++)
            {
                total += values[i];
            }

            return total;
        }
    }
}
