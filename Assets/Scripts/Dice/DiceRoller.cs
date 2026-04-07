using System.Collections.Generic;
using UnityEngine;

namespace DiceMadness.Dice
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(DiceFaceReader))]
    public class DiceRoller : MonoBehaviour
    {
        public enum ImpactAudioType
        {
            Table = 0,
            Dice = 1,
        }

        // Animates a controlled roll that lands on an explicitly selected face.
        [SerializeField] private float rollDuration = 0.9f;
        [SerializeField] private float startPositionJitter = 0.25f;
        [SerializeField] private float landingSpread = 1.3f;
        [SerializeField] private float hopHeight = 1.4f;
        [SerializeField] private float sidewaysArc = 0.32f;
        [SerializeField] private Vector2 spinTurnsRange = new Vector2(2.75f, 4.25f);
        [SerializeField] private float collisionProbeInset = 0.01f;
        [SerializeField] private float collisionSweepPadding = 0.02f;

        private Rigidbody cachedRigidbody;
        private BoxCollider cachedCollider;
        private DiceFaceReader cachedFaceReader;

        private Vector3 rollStartPosition;
        private Vector3 rollTargetPosition;
        private Vector3 rollSideDirection;
        private Quaternion rollStartRotation;
        private Quaternion rollTargetRotation;
        private Vector3 rollSpinAxis;
        private float rollSpinDegrees;
        private float rollElapsed;
        private bool isRollAnimating;
        private readonly HashSet<Collider> activeOverlaps = new HashSet<Collider>();
        private readonly HashSet<Collider> overlapScratch = new HashSet<Collider>();
        private readonly HashSet<Collider> sweepScratch = new HashSet<Collider>();
        private readonly Collider[] overlapBuffer = new Collider[24];
        private readonly RaycastHit[] sweepHitBuffer = new RaycastHit[24];
        private Vector3 lastProbeCenter;
        private Quaternion lastProbeRotation;
        private bool hasLastProbePose;

        public event System.Action<ImpactAudioType, DiceRoller> ImpactDetected;

        public DiceFaceReader FaceReader => cachedFaceReader;
        public float RollDuration => rollDuration;
        public bool IsRollAnimationComplete => !isRollAnimating;

        private void Awake()
        {
            CacheReferences();
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.Sleep();
        }

        public void BeginOutcomeRoll(Vector3 anchorPosition, int targetFaceIndex)
        {
            CacheReferences();
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.Sleep();

            BuildRollPlan(anchorPosition, targetFaceIndex);
            isRollAnimating = true;
            rollElapsed = 0f;
            ApplyRollPose(0f);
            RefreshCollisionState(notifyNewContacts: false);
        }

        public void AdvanceOutcomeRoll(float deltaTime)
        {
            if (!isRollAnimating)
            {
                return;
            }

            rollElapsed = Mathf.Min(rollDuration, rollElapsed + deltaTime);
            float progress = rollDuration > 0f ? rollElapsed / rollDuration : 1f;
            ApplyRollPose(progress);
            RefreshCollisionState(notifyNewContacts: true);

            if (progress >= 1f)
            {
                isRollAnimating = false;
            }
        }

        public void CompleteOutcomeRoll()
        {
            ApplyRollPose(1f);
            RefreshCollisionState(notifyNewContacts: true);
            isRollAnimating = false;
        }

        // Stops simulation so the manager can move the die into an evaluation row without changing its landed rotation.
        public void BeginEvaluationPresentation()
        {
            CacheReferences();
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.Sleep();
            activeOverlaps.Clear();
            hasLastProbePose = false;
        }

        // Repositions the die while explicitly preserving the exact landed rotation.
        public void SetEvaluationPose(Vector3 position, Quaternion landedRotation)
        {
            CacheReferences();

            cachedRigidbody.position = position;
            cachedRigidbody.rotation = landedRotation;
            transform.SetPositionAndRotation(position, landedRotation);
            activeOverlaps.Clear();
            hasLastProbePose = false;
        }

        // Restores the die to a stable anchor pose so menu/game transitions can start from a predictable state.
        public void ResetToAnchor(Vector3 anchorPosition, Quaternion rotation)
        {
            CacheReferences();
            isRollAnimating = false;
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.position = anchorPosition;
            cachedRigidbody.rotation = rotation;
            transform.SetPositionAndRotation(anchorPosition, rotation);
            cachedRigidbody.Sleep();
            RefreshCollisionState(notifyNewContacts: false);
        }

        private void BuildRollPlan(Vector3 anchorPosition, int targetFaceIndex)
        {
            Vector2 startOffset = Random.insideUnitCircle * startPositionJitter;
            rollStartPosition = anchorPosition + new Vector3(startOffset.x, 0f, startOffset.y);
            rollTargetPosition = ComputeLandingPosition(anchorPosition);
            rollStartRotation = Random.rotationUniform;
            rollTargetRotation = cachedFaceReader.GetFaceUpRotation(targetFaceIndex, Random.Range(0f, 360f));

            rollSpinAxis = Random.onUnitSphere;
            if (rollSpinAxis.sqrMagnitude < 0.001f)
            {
                rollSpinAxis = Vector3.up;
            }
            rollSpinAxis.Normalize();

            rollSideDirection = Vector3.ProjectOnPlane(Random.onUnitSphere, Vector3.up);
            if (rollSideDirection.sqrMagnitude < 0.001f)
            {
                rollSideDirection = Vector3.right;
            }
            rollSideDirection.Normalize();

            rollSpinDegrees = Random.Range(spinTurnsRange.x, spinTurnsRange.y) * 360f;
        }

        private Vector3 ComputeLandingPosition(Vector3 anchorPosition)
        {
            Vector2 spread = Random.insideUnitCircle * landingSpread;
            Vector3 probeOrigin = anchorPosition + new Vector3(spread.x, 5f, spread.y);
            Vector3 fallbackPosition = anchorPosition + new Vector3(spread.x, 0f, spread.y);

            RaycastHit[] hits = Physics.RaycastAll(
                probeOrigin,
                Vector3.down,
                12f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hitInfo = hits[i];

                if (hitInfo.collider == cachedCollider)
                {
                    continue;
                }

                if (hitInfo.collider.GetComponentInParent<DiceRoller>() != null)
                {
                    continue;
                }

                float halfHeight = GetHalfHeight();
                return new Vector3(fallbackPosition.x, hitInfo.point.y + halfHeight + 0.002f, fallbackPosition.z);
            }

            return fallbackPosition;
        }

        private void ApplyRollPose(float progress)
        {
            float clampedProgress = Mathf.Clamp01(progress);
            float eased = Mathf.SmoothStep(0f, 1f, clampedProgress);
            float hop = Mathf.Sin(clampedProgress * Mathf.PI);
            float side = Mathf.Sin(clampedProgress * Mathf.PI) * (1f - Mathf.Abs(clampedProgress - 0.5f) * 2f);

            Vector3 position = Vector3.Lerp(rollStartPosition, rollTargetPosition, eased)
                               + Vector3.up * (hop * hopHeight)
                               + rollSideDirection * (side * sidewaysArc);

            Quaternion baseRotation = Quaternion.Slerp(rollStartRotation, rollTargetRotation, eased);
            Quaternion spinRotation = Quaternion.AngleAxis((1f - eased) * rollSpinDegrees, rollSpinAxis);
            Quaternion rotation = spinRotation * baseRotation;

            cachedRigidbody.position = position;
            cachedRigidbody.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
        }

        private float GetHalfHeight()
        {
            Vector3 scaledSize = Vector3.Scale(cachedCollider.size, transform.lossyScale);
            return scaledSize.y * 0.5f;
        }

        private void CacheReferences()
        {
            if (cachedRigidbody == null)
            {
                cachedRigidbody = GetComponent<Rigidbody>();
            }

            if (cachedCollider == null)
            {
                cachedCollider = GetComponent<BoxCollider>();
            }

            if (cachedFaceReader == null)
            {
                cachedFaceReader = GetComponent<DiceFaceReader>();
            }
        }

        private void RefreshCollisionState(bool notifyNewContacts)
        {
            CacheReferences();

            overlapScratch.Clear();
            sweepScratch.Clear();

            Vector3 halfExtents = GetProbeHalfExtents();
            if (halfExtents.x <= 0f || halfExtents.y <= 0f || halfExtents.z <= 0f)
            {
                activeOverlaps.Clear();
                hasLastProbePose = false;
                return;
            }

            Vector3 center = transform.TransformPoint(cachedCollider.center);
            Quaternion rotation = transform.rotation;

            if (notifyNewContacts && hasLastProbePose)
            {
                Vector3 sweepDelta = center - lastProbeCenter;
                float sweepDistance = sweepDelta.magnitude;

                if (sweepDistance > 0.0001f)
                {
                    int sweepHitCount = Physics.BoxCastNonAlloc(
                        lastProbeCenter,
                        halfExtents,
                        sweepDelta / sweepDistance,
                        sweepHitBuffer,
                        lastProbeRotation,
                        sweepDistance + Mathf.Max(0f, collisionSweepPadding),
                        Physics.DefaultRaycastLayers,
                        QueryTriggerInteraction.Ignore);

                    for (int i = 0; i < sweepHitCount; i++)
                    {
                        Collider other = sweepHitBuffer[i].collider;
                        sweepHitBuffer[i] = default;

                        if (other == null || other == cachedCollider || !sweepScratch.Add(other))
                        {
                            continue;
                        }

                        EmitImpactForCollider(other);
                    }
                }
            }

            int hitCount = Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                overlapBuffer,
                rotation,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider other = overlapBuffer[i];
                overlapBuffer[i] = null;

                if (other == null || other == cachedCollider)
                {
                    continue;
                }

                if (other.transform.IsChildOf(transform))
                {
                    continue;
                }

                overlapScratch.Add(other);

                if (!notifyNewContacts || activeOverlaps.Contains(other))
                {
                    continue;
                }
                EmitImpactForCollider(other);
            }

            activeOverlaps.Clear();
            foreach (Collider overlap in overlapScratch)
            {
                activeOverlaps.Add(overlap);
            }

            lastProbeCenter = center;
            lastProbeRotation = rotation;
            hasLastProbePose = true;
        }

        private Vector3 GetProbeHalfExtents()
        {
            Vector3 scaledSize = Vector3.Scale(cachedCollider.size, transform.lossyScale);
            Vector3 inset = Vector3.one * Mathf.Max(0f, collisionProbeInset);
            Vector3 halfExtents = (scaledSize * 0.5f) - inset;

            halfExtents.x = Mathf.Max(0.001f, halfExtents.x);
            halfExtents.y = Mathf.Max(0.001f, halfExtents.y);
            halfExtents.z = Mathf.Max(0.001f, halfExtents.z);
            return halfExtents;
        }

        private void EmitImpactForCollider(Collider other)
        {
            if (other == null)
            {
                return;
            }

            DiceRoller otherDie = other.GetComponentInParent<DiceRoller>();
            ImpactAudioType impactType = otherDie != null && otherDie != this
                ? ImpactAudioType.Dice
                : ImpactAudioType.Table;

            ImpactDetected?.Invoke(impactType, this);
        }
    }
}
