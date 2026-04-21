using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class DraggableServeItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private ServingManager servingManager;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private Vector2 startAnchoredPosition;
    private bool dragEnabled;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rootCanvas = GetComponentInParent<Canvas>();

        if (rectTransform != null)
            startAnchoredPosition = rectTransform.anchoredPosition;
    }

    public void SetServingManager(ServingManager manager)
    {
        servingManager = manager;
    }

    public void SetDragEnabled(bool enabled)
    {
        dragEnabled = enabled;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = enabled;
            canvasGroup.interactable = enabled;
            canvasGroup.alpha = 1f;
        }
    }

    public void ResetToStart()
    {
        if (rectTransform != null)
            rectTransform.anchoredPosition = startAnchoredPosition;
    }

    public void CacheStartPosition()
    {
        if (rectTransform != null)
            startAnchoredPosition = rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!dragEnabled) return;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.85f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragEnabled) return;
        if (rootCanvas == null || rectTransform == null) return;

        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragEnabled) return;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        if (servingManager != null && servingManager.CanAcceptDrop(eventData.position, eventData.pressEventCamera))
            servingManager.HandleSuccessfulServe(this);
        else
            ResetToStart();
    }
}