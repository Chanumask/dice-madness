using DiceMadness.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DiceMadness.UI
{
    public sealed class UiHoverTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [SerializeField] private UiTooltipPresenter presenter;
        [SerializeField] private string tooltipTitle;
        [SerializeField, TextArea(2, 6)] private string tooltipBody;

        public void Configure(UiTooltipPresenter tooltipPresenter, string title, string body)
        {
            presenter = tooltipPresenter;
            tooltipTitle = title;
            tooltipBody = body;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            AudioManager.Instance?.PlayUIHover();
            presenter?.Show(tooltipTitle, tooltipBody, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            presenter?.Hide();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            presenter?.Move(eventData.position);
        }
    }
}
