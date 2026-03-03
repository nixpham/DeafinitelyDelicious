using UnityEngine;
using UnityEngine.UI;

public class DialogueLockUI : MonoBehaviour
{
    [Header("Buttons to disable during ANY dialogue")]
    public Button[] buttonsToLock;

    [Header("Optional Highlights (same order as buttons)")]
    public GameObject[] highlightObjects;

    void OnEnable()
    {
        NPC.OnAnyDialogueActiveChanged += HandleDialogueActive;

        HandleDialogueActive(NPC.AnyDialogueActive);
    }

    void OnDisable()
    {
        NPC.OnAnyDialogueActiveChanged -= HandleDialogueActive;
    }

    private void HandleDialogueActive(bool isActive)
    {
        if (buttonsToLock == null) return;

        for (int i = 0; i < buttonsToLock.Length; i++)
        {
            var button = buttonsToLock[i];

            if (button != null)
                button.interactable = !isActive;

            if (highlightObjects != null && i < highlightObjects.Length)
            {
                if (highlightObjects[i] != null)
                    highlightObjects[i].SetActive(!isActive);
            }
        }
    }
}