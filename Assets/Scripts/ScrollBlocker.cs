using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollBlocker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ScrollRect parentScrollRect;

    void Start()
    {
        // Find the parent scroll rect (the one you want to block)
        parentScrollRect = GetComponentInParent<ScrollRect>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        parentScrollRect.enabled = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        parentScrollRect.enabled = true;
    }
}