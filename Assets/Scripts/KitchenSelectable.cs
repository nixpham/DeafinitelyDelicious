using UnityEngine;
using UnityEngine.UI;

public class KitchenSelectable : MonoBehaviour
{
    [Header("References")]
    public RecipeManager recipeManager;

    [Header("Highlight")]
    public Color selectedColor = new Color(1f, 0.92f, 0.35f, 1f);

    private bool isSelected = false;
    private bool isInteractable = true;

    private Graphic uiGraphic;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    public bool IsSelected => isSelected;
    public bool IsInteractable => isInteractable;

    private void Awake()
    {
        uiGraphic = GetComponent<Graphic>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (uiGraphic != null)
        {
            originalColor = uiGraphic.color;
        }
        else if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void HandleClick()
    {
        if (!isInteractable || recipeManager == null)
            return;

        recipeManager.HandleKitchenObjectClicked(this);
    }

    private void OnMouseDown()
    {
        // Useful if this is a world object with a collider instead of a UI button
        HandleClick();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplyVisual();
    }

    public void ClearSelection()
    {
        isSelected = false;
        ApplyVisual();
    }

    public void SetInteractable(bool value)
    {
        isInteractable = value;
    }

    private void ApplyVisual()
    {
        Color targetColor = isSelected ? selectedColor : originalColor;

        if (uiGraphic != null)
        {
            uiGraphic.color = targetColor;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = targetColor;
        }
    }
}