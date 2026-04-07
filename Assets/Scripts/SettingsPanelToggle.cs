using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelToggle : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton;

    [Header("Settings")]
    [SerializeField] private bool closeOnClickOutside = true;

    private bool isPanelOpen = false;

    void Start()
    {
        // Ensure panel starts hidden
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Hook up the button click listener
        if (settingsButton != null)
            settingsButton.onClick.AddListener(ToggleSettingsPanel);
    }

    void Update()
    {
        // Close panel when clicking outside of it (optional)
        if (closeOnClickOutside && isPanelOpen && Input.GetMouseButtonDown(0))
        {
            // Check if the click was outside the panel
            if (!IsPointerOverPanel())
                CloseSettingsPanel();
        }

        // Close with Escape key
        if (isPanelOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseSettingsPanel();
    }

    public void ToggleSettingsPanel()
    {
        if (isPanelOpen)
            CloseSettingsPanel();
        else
            OpenSettingsPanel();
    }

    public void OpenSettingsPanel()
    {
        settingsPanel.SetActive(true);
        isPanelOpen = true;
        Debug.Log("Settings panel opened.");
    }

    public void CloseSettingsPanel()
    {
        settingsPanel.SetActive(false);
        isPanelOpen = false;
        Debug.Log("Settings panel closed.");
    }

    private bool IsPointerOverPanel()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    void OnDestroy()
    {
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(ToggleSettingsPanel);
    }
}