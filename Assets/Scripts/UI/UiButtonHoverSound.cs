using DiceMadness.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceMadness.UI
{
    public sealed class UiButtonHoverSound : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private Selectable selectable;

        private void Awake()
        {
            selectable ??= GetComponent<Selectable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            selectable ??= GetComponent<Selectable>();

            if (selectable != null && !selectable.IsInteractable())
            {
                return;
            }

            AudioManager.Instance?.PlayUIHover();
        }
    }
}
