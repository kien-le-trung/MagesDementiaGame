using UnityEngine;
using UnityEngine.EventSystems;

namespace MagesDementiaGame
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIDraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private int itemIndex;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;
        private Transform inventoryParent;
        private Vector2 inventoryPosition;
        private bool acceptedThisDrag;
        private MinhViewSceneController controller;

        public int ItemIndex => itemIndex;
        public bool IsPlaced { get; private set; }

        public void Initialize(MinhViewSceneController owner, int index, Canvas canvas)
        {
            controller = owner;
            itemIndex = index;
            rootCanvas = canvas;
            rectTransform = (RectTransform)transform;
            canvasGroup = GetComponent<CanvasGroup>();
            inventoryParent = transform.parent;
            inventoryPosition = rectTransform.anchoredPosition;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            acceptedThisDrag = false;
            canvasGroup.blocksRaycasts = false;
            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;
            if (acceptedThisDrag) return;
            IsPlaced = false;
            transform.SetParent(inventoryParent, false);
            rectTransform.anchoredPosition = inventoryPosition;
            controller?.NotifyTableItemRemoved(itemIndex);
        }

        public void AcceptDrop(RectTransform target)
        {
            acceptedThisDrag = true;
            IsPlaced = true;
            transform.SetParent(target, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            controller?.NotifyTableItemPlaced(itemIndex);
        }
    }
}
