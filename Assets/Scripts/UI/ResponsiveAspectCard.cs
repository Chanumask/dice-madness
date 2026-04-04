using UnityEngine;

namespace DiceMadness.UI
{
    // Keeps a centered UI card at a stable aspect ratio while clamping it to
    // sensible minimum and maximum sizes inside the current parent rect.
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ResponsiveAspectCard : MonoBehaviour
    {
        [SerializeField, Range(0.1f, 1f)]
        private float widthPercentOfParent = 0.5f;

        [SerializeField, Range(0.1f, 1f)]
        private float maxHeightPercentOfParent = 0.88f;

        [SerializeField, Min(0.1f)]
        private float aspectRatio = 1f;

        [SerializeField, Min(0f)]
        private float minWidth = 720f;

        [SerializeField, Min(0f)]
        private float maxWidth = 920f;

        [SerializeField, Min(0f)]
        private float minHeight = 0f;

        [SerializeField, Min(0f)]
        private float maxHeight = 0f;

        private RectTransform rectTransform;
        private RectTransform parentRect;
        private bool isApplyingLayout;

#if UNITY_EDITOR
        private bool refreshQueued;
#endif

        public void Configure(
            float widthPercent,
            float heightPercent,
            float targetAspectRatio,
            float minimumWidth,
            float maximumWidth)
        {
            widthPercentOfParent = Mathf.Clamp(widthPercent, 0.1f, 1f);
            maxHeightPercentOfParent = Mathf.Clamp(heightPercent, 0.1f, 1f);
            aspectRatio = Mathf.Max(0.1f, targetAspectRatio);
            minWidth = Mathf.Max(0f, minimumWidth);
            maxWidth = Mathf.Max(0f, maximumWidth);
            minHeight = minWidth > 0f ? minWidth / aspectRatio : 0f;
            maxHeight = maxWidth > 0f ? maxWidth / aspectRatio : 0f;
            RefreshLayout();
        }

        private void OnEnable()
        {
            RequestRefresh();
        }

        private void OnValidate()
        {
            minHeight = minWidth > 0f ? minWidth / Mathf.Max(0.1f, aspectRatio) : 0f;
            maxHeight = maxWidth > 0f ? maxWidth / Mathf.Max(0.1f, aspectRatio) : 0f;

#if UNITY_EDITOR
            QueueEditorRefresh();
#endif
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isApplyingLayout)
            {
                return;
            }

            RefreshLayout();
        }

        [ContextMenu("Refresh Layout")]
        public void RefreshLayout()
        {
            if (isApplyingLayout)
            {
                return;
            }

            if (!TryResolveRects())
            {
                return;
            }

            Rect parentBounds = parentRect.rect;
            if (parentBounds.width <= 0f || parentBounds.height <= 0f)
            {
                return;
            }

            float clampedAspect = Mathf.Max(0.1f, aspectRatio);
            float targetWidth = parentBounds.width * widthPercentOfParent;
            float maxWidthFromHeight = parentBounds.height * maxHeightPercentOfParent * clampedAspect;
            float width = Mathf.Min(targetWidth, maxWidthFromHeight);

            if (maxWidth > 0f)
            {
                width = Mathf.Min(width, maxWidth);
            }

            if (minWidth > 0f)
            {
                width = Mathf.Max(width, minWidth);
            }

            float height = width / clampedAspect;

            if (maxHeight > 0f && height > maxHeight)
            {
                height = maxHeight;
                width = height * clampedAspect;
            }

            if (minHeight > 0f && height < minHeight)
            {
                height = minHeight;
                width = height * clampedAspect;
            }

            isApplyingLayout = true;
            try
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
            finally
            {
                isApplyingLayout = false;
            }
        }

        private bool TryResolveRects()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (rectTransform == null)
            {
                return false;
            }

            if (parentRect == null)
            {
                parentRect = rectTransform.parent as RectTransform;
            }

            return parentRect != null;
        }

        private void RequestRefresh()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                QueueEditorRefresh();
                return;
            }
#endif

            RefreshLayout();
        }

#if UNITY_EDITOR
        private void QueueEditorRefresh()
        {
            if (refreshQueued)
            {
                return;
            }

            refreshQueued = true;
            UnityEditor.EditorApplication.delayCall += DelayedRefresh;
        }

        private void DelayedRefresh()
        {
            refreshQueued = false;

            if (this == null || !isActiveAndEnabled)
            {
                return;
            }

            RefreshLayout();
        }
#endif
    }
}
