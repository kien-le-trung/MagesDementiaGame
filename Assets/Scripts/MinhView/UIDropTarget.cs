using UnityEngine;
using UnityEngine.EventSystems;

namespace MagesDementiaGame
{
    public sealed class UIDropTarget : MonoBehaviour, IDropHandler
    {
        [SerializeField] private int acceptedItemIndex;

        public int AcceptedItemIndex => acceptedItemIndex;

        public void Configure(int index) => acceptedItemIndex = index;

        public void OnDrop(PointerEventData eventData)
        {
            var item = eventData.pointerDrag?.GetComponent<UIDraggableItem>();
            if (item == null || item.ItemIndex != acceptedItemIndex) return;
            item.AcceptDrop((RectTransform)transform);
        }
    }
}
