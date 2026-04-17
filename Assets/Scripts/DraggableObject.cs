using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableObject : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector3 startPosition;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    public RectTransform countertopArea; // Assign the Countertop Panel in Inspector
    public RecipeManager recipeManager; // Reference to the RecipeManager

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        startPosition = transform.position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false; // Allows dragging through UI elements
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition; // Move object with the mouse
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true; // Enable raycasts again

        if (IsInsideCountertop())
        {
            if (recipeManager != null)
            {
                Debug.Log($"Placed {gameObject.name} on the countertop.");
                AudioManager.Instance.PlaySfx(GameAudioPaths.UiInventoryEquip, 0.85f);
                recipeManager.ObjectPlaced(gameObject);  // Notify RecipeManager
            }
            else
            {
                Debug.LogError("RecipeManager is not assigned!");
            }
        }
        else
        {
            Debug.Log($"{gameObject.name} was not placed on the countertop, resetting position.");
            transform.position = startPosition; // Reset position if outside countertop
        }
    }


    private bool IsInsideCountertop()
    {
        return RectTransformUtility.RectangleContainsScreenPoint(countertopArea, Input.mousePosition);
    }
}
