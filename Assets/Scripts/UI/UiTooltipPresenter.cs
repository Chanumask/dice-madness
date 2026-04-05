using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceMadness.UI
{
    public sealed class UiTooltipPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform containerRect;
        [SerializeField] private RectTransform tooltipRect;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Vector2 cursorOffset = new Vector2(22f, -18f);
        [SerializeField] private Vector2 screenPadding = new Vector2(18f, 18f);

        public void Configure(RectTransform container, RectTransform tooltipRoot, TMP_Text tooltipTitle, TMP_Text tooltipBody)
        {
            containerRect = container;
            tooltipRect = tooltipRoot;
            titleText = tooltipTitle;
            bodyText = tooltipBody;
            Hide();
        }

        public void Show(string title, string body, Vector2 screenPosition)
        {
            if (tooltipRect == null || containerRect == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = title;
            }

            if (bodyText != null)
            {
                bodyText.text = body;
            }

            containerRect.gameObject.SetActive(true);
            tooltipRect.gameObject.SetActive(true);
            if (tooltipRect.parent != null)
            {
                tooltipRect.parent.SetAsLastSibling();
            }
            tooltipRect.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
            Move(screenPosition);
        }

        public void Move(Vector2 screenPosition)
        {
            if (tooltipRect == null || containerRect == null || !tooltipRect.gameObject.activeSelf)
            {
                return;
            }

            Canvas canvas = containerRect.GetComponentInParent<Canvas>();
            Camera camera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                camera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }

            Vector2 anchoredPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, screenPosition, camera, out anchoredPosition);

            Vector2 size = tooltipRect.rect.size;
            Vector2 pivot = new Vector2(0f, 1f);
            Vector2 target = anchoredPosition + new Vector2(cursorOffset.x, cursorOffset.y);

            float minX = containerRect.rect.xMin + screenPadding.x;
            float maxX = containerRect.rect.xMax - screenPadding.x - size.x;
            float minY = containerRect.rect.yMin + screenPadding.y + size.y;
            float maxY = containerRect.rect.yMax - screenPadding.y;

            if (target.x > maxX)
            {
                target.x = anchoredPosition.x - cursorOffset.x - size.x;
                pivot.x = 1f;
            }

            target.x = Mathf.Clamp(target.x, minX, maxX);

            if (target.y < minY)
            {
                target.y = anchoredPosition.y - cursorOffset.y;
                pivot.y = 0f;
            }

            target.y = Mathf.Clamp(target.y, minY, maxY);

            tooltipRect.pivot = pivot;
            tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRect.anchoredPosition = target;
        }

        public void Hide()
        {
            if (containerRect != null)
            {
                containerRect.gameObject.SetActive(false);
            }

            if (tooltipRect != null)
            {
                tooltipRect.gameObject.SetActive(false);
            }
        }
    }
}
