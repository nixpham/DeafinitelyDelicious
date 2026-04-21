using UnityEngine;
using UnityEngine.UI;
using Engine;

public class PersistentSignEngine : MonoBehaviour
{
    public static PersistentSignEngine Instance { get; private set; }

    [Header("Core")]
    [SerializeField] private GameObject engineRoot;
    [SerializeField] private SimpleExecutionEngine executionEngine;

    [Header("Preview UI")]
    [SerializeField] private CanvasGroup previewCanvasGroup;
    [SerializeField] private RawImage previewImage;

    public SimpleExecutionEngine Engine => executionEngine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (engineRoot != null)
            engineRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        SetPreviewVisible(false);

        Debug.Log("[PersistentSignEngine] Initialized. Engine is persistent and active.");
    }

    public void SetPreviewVisible(bool visible)
    {
        if (previewCanvasGroup != null)
        {
            previewCanvasGroup.alpha = visible ? 1f : 0f;
            previewCanvasGroup.interactable = visible;
            previewCanvasGroup.blocksRaycasts = visible;
        }
        else if (previewImage != null)
        {
            Color c = previewImage.color;
            c.a = visible ? 1f : 0f;
            previewImage.color = c;
            previewImage.raycastTarget = visible;
        }

        Debug.Log("[PersistentSignEngine] Preview " + (visible ? "VISIBLE" : "HIDDEN"));
    }
}