using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    // Singleton so any script can access it easily
    public static ScoreManager Instance { get; private set; }

    [Header("Scoring Settings")]
    [SerializeField] private int pointsForSuccess = 10;
    [SerializeField] private int penaltyForFail = 5;

    private int currentScore = 0;

    // A public getter so other scripts can read the score if needed
    public int CurrentScore => currentScore;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Call this when a minigame is successfully completed
    public void AddWinPoints()
    {
        currentScore += pointsForSuccess;
        Debug.Log("[ScoreManager] Win! Points Added. Current Score: " + currentScore);
    }

    // Call this when a minigame fails and forces a restart
    public void AddFailPenalty()
    {
        currentScore -= penaltyForFail;
        
        // Prevent the score from going below zero
        if (currentScore < 0) 
            currentScore = 0;
            
        Debug.Log("[ScoreManager] Fail! Points Lost. Current Score: " + currentScore);
    }
}