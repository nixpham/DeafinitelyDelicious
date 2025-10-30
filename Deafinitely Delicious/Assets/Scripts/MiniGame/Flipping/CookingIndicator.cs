using UnityEngine;
using UnityEngine.UI;

public class CookingIndicator : MonoBehaviour
{
    [Header("UI References")]
    public Image fillBar;           // The main progress bar fill
    public Image perfectZone;       // Visual indicator of the perfect timing zone
    public Image dangerZone;        // Visual indicator of the danger/burn zone
    
    [Header("Cooking Thresholds")]
    public float undercookedThreshold = 3f;
    public float perfectMin = 5f;
    public float perfectMax = 7f;
    public float burntThreshold = 9f;
    public float maxTime = 10f;     // Maximum time on the bar
    
    [Header("Colors")]
    public Color rawColor = Color.white;
    public Color cookingColor = new Color(1f, 0.8f, 0.4f);  // Light orange
    public Color perfectColor = Color.green;
    public Color dangerColor = new Color(1f, 0.5f, 0f);     // Orange
    public Color burntColor = Color.red;
    
    [Header("Perfect Zone Indicator")]
    public GameObject perfectZoneMarker;  // Visual marker showing where to flip
    
    private float currentTime = 0f;
    
    void Start()
    {
        SetupPerfectZone();
        UpdateIndicator(0f);
    }
    
    /// <summary>
    /// Sets up the visual perfect zone on the bar
    /// </summary>
    private void SetupPerfectZone()
    {
        if (perfectZone != null)
        {
            // Position the perfect zone on the bar
            RectTransform perfectRect = perfectZone.GetComponent<RectTransform>();
            
            // Calculate position and width based on thresholds
            float startPercent = perfectMin / maxTime;
            float endPercent = perfectMax / maxTime;
            float width = endPercent - startPercent;
            
            // Set anchors to position the zone correctly
            perfectRect.anchorMin = new Vector2(startPercent, 0f);
            perfectRect.anchorMax = new Vector2(endPercent, 1f);
            perfectRect.offsetMin = Vector2.zero;
            perfectRect.offsetMax = Vector2.zero;
            
            // Set color
            perfectZone.color = new Color(0f, 1f, 0f, 0.3f); // Semi-transparent green
        }
        
        if (dangerZone != null)
        {
            // Position the danger zone
            RectTransform dangerRect = dangerZone.GetComponent<RectTransform>();
            
            float startPercent = perfectMax / maxTime;
            float endPercent = burntThreshold / maxTime;
            
            dangerRect.anchorMin = new Vector2(startPercent, 0f);
            dangerRect.anchorMax = new Vector2(endPercent, 1f);
            dangerRect.offsetMin = Vector2.zero;
            dangerRect.offsetMax = Vector2.zero;
            
            dangerZone.color = new Color(1f, 0.5f, 0f, 0.3f); // Semi-transparent orange
        }
    }
    
    /// <summary>
    /// Updates the indicator based on current cooking time
    /// </summary>
    public void UpdateIndicator(float cookingTime)
    {
        currentTime = cookingTime;
        
        // Update fill amount
        if (fillBar != null)
        {
            fillBar.fillAmount = Mathf.Clamp01(cookingTime / maxTime);
            
            // Update color based on cooking state
            fillBar.color = GetColorForTime(cookingTime);
        }
        
        // Pulse the perfect zone marker when in perfect range
        if (perfectZoneMarker != null)
        {
            if (cookingTime >= perfectMin && cookingTime <= perfectMax)
            {
                PulsePerfectMarker();
            }
            else
            {
                perfectZoneMarker.transform.localScale = Vector3.one;
            }
        }
    }
    
    /// <summary>
    /// Returns the appropriate color for the current cooking time
    /// </summary>
    private Color GetColorForTime(float time)
    {
        if (time < undercookedThreshold)
        {
            // Transition from raw to cooking
            float t = time / undercookedThreshold;
            return Color.Lerp(rawColor, cookingColor, t);
        }
        else if (time >= undercookedThreshold && time < perfectMin)
        {
            // Transition from cooking to perfect
            float t = (time - undercookedThreshold) / (perfectMin - undercookedThreshold);
            return Color.Lerp(cookingColor, perfectColor, t);
        }
        else if (time >= perfectMin && time <= perfectMax)
        {
            // Perfect zone - solid green
            return perfectColor;
        }
        else if (time > perfectMax && time <= burntThreshold)
        {
            // Transition from perfect to danger
            float t = (time - perfectMax) / (burntThreshold - perfectMax);
            return Color.Lerp(perfectColor, dangerColor, t);
        }
        else // time > burntThreshold
        {
            // Burnt - solid red
            return burntColor;
        }
    }
    
    /// <summary>
    /// Checks if the current time is in the perfect range
    /// </summary>
    public bool IsInPerfectRange()
    {
        return currentTime >= perfectMin && currentTime <= perfectMax;
    }
    
    /// <summary>
    /// Checks if the current time is undercooked
    /// </summary>
    public bool IsUndercooked()
    {
        return currentTime < undercookedThreshold;
    }
    
    /// <summary>
    /// Checks if the current time is burnt
    /// </summary>
    public bool IsBurnt()
    {
        return currentTime > burntThreshold;
    }
    
    /// <summary>
    /// Pulses the perfect zone marker to draw attention
    /// </summary>
    private void PulsePerfectMarker()
    {
        float scale = 1f + Mathf.Sin(Time.time * 5f) * 0.2f; // Pulse between 0.8 and 1.2
        perfectZoneMarker.transform.localScale = Vector3.one * scale;
    }
    
    /// <summary>
    /// Resets the indicator to initial state
    /// </summary>
    public void ResetIndicator()
    {
        currentTime = 0f;
        UpdateIndicator(0f);
        
        if (perfectZoneMarker != null)
        {
            perfectZoneMarker.transform.localScale = Vector3.one;
        }
    }
}
