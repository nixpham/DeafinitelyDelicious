using UnityEngine;
using UnityEngine.UI;

public class AttemptsUI : MonoBehaviour
{
    [Header("Circle Images (in order)")]
    [SerializeField] private Image[] attemptCircles;

    [Header("Colors")]
    [SerializeField] private Color defaultColor = Color.grey;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failColor = Color.red;

    private int currentIndex = 0;

    private void Awake()
    {
        if (attemptCircles == null || attemptCircles.Length == 0)
        {
            attemptCircles = GetComponentsInChildren<Image>(includeInactive: true);
        }

        ResetAttempts();
    }

    public void ResetAttempts()
    {
        currentIndex = 0;

        if (attemptCircles == null) return;

        foreach (var img in attemptCircles)
        {
            if (!img) continue;
            img.gameObject.SetActive(true);
            img.color = defaultColor;
        }
    }


    public void RegisterAttempt(bool success)
    {
        if (attemptCircles == null || attemptCircles.Length == 0) return;

        int index = Mathf.Clamp(currentIndex, 0, attemptCircles.Length - 1);

        var img = attemptCircles[index];
        if (img)
        {
            img.color = success ? successColor : failColor;
        }

        if (currentIndex < attemptCircles.Length - 1)
        {
            currentIndex++;
        }
    }

    public void SetAttemptAt(int attemptIndex, bool success)
    {
        if (attemptCircles == null) return;
        if (attemptIndex < 0 || attemptIndex >= attemptCircles.Length) return;

        var img = attemptCircles[attemptIndex];
        if (img)
        {
            img.color = success ? successColor : failColor;
        }
    }
}
