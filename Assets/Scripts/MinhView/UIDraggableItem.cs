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
        private Vector2 inventoryAnchorMin;
        private Vector2 inventoryAnchorMax;
        private Vector2 inventorySizeDelta;
        private Vector2 renderedSize;
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
            inventoryAnchorMin = rectTransform.anchorMin;
            inventoryAnchorMax = rectTransform.anchorMax;
            inventorySizeDelta = rectTransform.sizeDelta;
            renderedSize = rectTransform.rect.size;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            controller?.NotifyTableDragStarted();
            acceptedThisDrag = false;
            canvasGroup.blocksRaycasts = false;
            var worldPosition = rectTransform.position;
            transform.SetParent(rootCanvas.transform, true);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = renderedSize;
            rectTransform.position = worldPosition;
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
            controller?.NotifyTableInvalidDrop();
            IsPlaced = false;
            transform.SetParent(inventoryParent, false);
            rectTransform.anchorMin = inventoryAnchorMin;
            rectTransform.anchorMax = inventoryAnchorMax;
            rectTransform.sizeDelta = inventorySizeDelta;
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
            rectTransform.sizeDelta = renderedSize;
            rectTransform.anchoredPosition = Vector2.zero;
            controller?.NotifyTableItemPlaced(itemIndex);
        }
    }
}
