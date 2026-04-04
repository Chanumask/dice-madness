using System.Text;
using UnityEngine;

namespace DiceMadness.Dice
{
    public class DiceFaceReader : MonoBehaviour
    {
        [System.Serializable]
        public struct FaceDefinition
        {
            public string faceId;
            public int value;
            public float weight;
            public Vector3 localDirection;
            public Vector3 presentationForwardHint;

            public FaceDefinition(
                string faceId,
                int value,
                float weight,
                Vector3 localDirection,
                Vector3 presentationForwardHint)
            {
                this.faceId = faceId;
                this.value = value;
                this.weight = weight;
                this.localDirection = localDirection;
                this.presentationForwardHint = presentationForwardHint;
            }
        }

        // Stores the physical face layout plus explicit per-face weights for weighted outcomes.
        [SerializeField] private FaceDefinition[] faces = CreateStandardD6Faces();

        private void Awake()
        {
            EnsureFaceData();
        }

        private void OnValidate()
        {
            EnsureFaceData();
        }

        public int FaceCount
        {
            get
            {
                EnsureFaceData();
                return faces.Length;
            }
        }

        public int GetTopFaceIndex()
        {
            EnsureFaceData();

            if (faces.Length == 0)
            {
                return -1;
            }

            int bestIndex = 0;
            float bestDot = float.NegativeInfinity;

            for (int i = 0; i < faces.Length; i++)
            {
                Vector3 worldDirection = transform.TransformDirection(faces[i].localDirection.normalized);
                float dot = Vector3.Dot(worldDirection, Vector3.up);

                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        public int GetTopFaceValue()
        {
            int topFaceIndex = GetTopFaceIndex();
            return topFaceIndex >= 0 ? faces[topFaceIndex].value : 0;
        }

        public int GetFaceValue(int faceIndex)
        {
            EnsureFaceData();
            return IsValidFaceIndex(faceIndex) ? faces[faceIndex].value : 0;
        }

        public string GetFaceId(int faceIndex)
        {
            EnsureFaceData();

            if (!IsValidFaceIndex(faceIndex))
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(faces[faceIndex].faceId)
                ? $"Face {faceIndex + 1}"
                : faces[faceIndex].faceId;
        }

        public float GetFaceWeight(int faceIndex)
        {
            EnsureFaceData();
            return IsValidFaceIndex(faceIndex) ? Mathf.Max(0f, faces[faceIndex].weight) : 0f;
        }

        public float[] GetNormalizedFaceProbabilities()
        {
            EnsureFaceData();

            float[] probabilities = new float[faces.Length];
            float totalWeight = 0f;

            for (int i = 0; i < faces.Length; i++)
            {
                totalWeight += Mathf.Max(0f, faces[i].weight);
            }

            if (faces.Length == 0)
            {
                return probabilities;
            }

            if (totalWeight <= 0f)
            {
                float uniformProbability = 1f / faces.Length;

                for (int i = 0; i < faces.Length; i++)
                {
                    probabilities[i] = uniformProbability;
                }

                return probabilities;
            }

            for (int i = 0; i < faces.Length; i++)
            {
                probabilities[i] = Mathf.Max(0f, faces[i].weight) / totalWeight;
            }

            return probabilities;
        }

        public int SampleWeightedFaceIndex()
        {
            EnsureFaceData();

            if (faces.Length == 0)
            {
                return -1;
            }

            float totalWeight = 0f;

            for (int i = 0; i < faces.Length; i++)
            {
                totalWeight += Mathf.Max(0f, faces[i].weight);
            }

            if (totalWeight <= 0f)
            {
                return Random.Range(0, faces.Length);
            }

            float pick = Random.value * totalWeight;

            for (int i = 0; i < faces.Length; i++)
            {
                pick -= Mathf.Max(0f, faces[i].weight);

                if (pick <= 0f)
                {
                    return i;
                }
            }

            return faces.Length - 1;
        }

        // Builds a world rotation that keeps the requested physical face on top with a tunable yaw.
        public Quaternion GetFaceUpRotation(int faceIndex, float yawDegrees)
        {
            EnsureFaceData();

            if (!IsValidFaceIndex(faceIndex))
            {
                return Random.rotationUniform;
            }

            FaceDefinition face = faces[faceIndex];
            Vector3 worldForward = Quaternion.AngleAxis(yawDegrees, Vector3.up) * Vector3.forward;
            Quaternion localOrientation = Quaternion.LookRotation(
                face.presentationForwardHint.normalized,
                face.localDirection.normalized);
            Quaternion worldOrientation = Quaternion.LookRotation(worldForward, Vector3.up);
            return worldOrientation * Quaternion.Inverse(localOrientation);
        }

        public void ResetToStandardD6()
        {
            faces = CreateStandardD6Faces();
        }

        [ContextMenu("Reset To Standard D6")]
        private void ResetToStandardD6FromContextMenu()
        {
            ResetToStandardD6();
        }

        [ContextMenu("Log Face Probability Preview")]
        private void LogFaceProbabilityPreview()
        {
            EnsureFaceData();
            float[] probabilities = GetNormalizedFaceProbabilities();
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"{name} face probabilities:");

            for (int i = 0; i < faces.Length; i++)
            {
                builder.AppendLine(
                    $"Face {i}: id={GetFaceId(i)}, value={faces[i].value}, weight={faces[i].weight:0.###}, probability={probabilities[i] * 100f:0.##}%");
            }

            Debug.Log(builder.ToString(), this);
        }

        private void EnsureFaceData()
        {
            if (faces == null || faces.Length == 0)
            {
                faces = CreateStandardD6Faces();
                return;
            }

            bool allWeightsZero = true;

            for (int i = 0; i < faces.Length; i++)
            {
                if (faces[i].weight > 0f)
                {
                    allWeightsZero = false;
                    break;
                }
            }

            for (int i = 0; i < faces.Length; i++)
            {
                FaceDefinition face = faces[i];

                if (allWeightsZero)
                {
                    face.weight = 1f;
                }

                if (string.IsNullOrWhiteSpace(face.faceId))
                {
                    face.faceId = $"Face {i + 1}";
                }

                if (face.localDirection == Vector3.zero)
                {
                    face.localDirection = Vector3.up;
                }

                if (face.presentationForwardHint == Vector3.zero)
                {
                    face.presentationForwardHint = Vector3.forward;
                }

                faces[i] = face;
            }
        }

        private bool IsValidFaceIndex(int faceIndex)
        {
            return faceIndex >= 0 && faceIndex < faces.Length;
        }

        private static FaceDefinition[] CreateStandardD6Faces()
        {
            return new[]
            {
                new FaceDefinition("Face 1", 1, 1f, Vector3.up, Vector3.forward),
                new FaceDefinition("Face 6", 6, 1f, Vector3.down, Vector3.forward),
                new FaceDefinition("Face 2", 2, 1f, Vector3.forward, Vector3.down),
                new FaceDefinition("Face 5", 5, 1f, Vector3.back, Vector3.up),
                new FaceDefinition("Face 3", 3, 1f, Vector3.right, Vector3.forward),
                new FaceDefinition("Face 4", 4, 1f, Vector3.left, Vector3.forward),
            };
        }
    }
}
