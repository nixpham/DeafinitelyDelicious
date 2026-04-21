using UnityEngine;

public class PersistentSignEngineBootstrap : MonoBehaviour
{
    [SerializeField] private PersistentSignEngine enginePrefab;

    private void Awake()
    {
        if (PersistentSignEngine.Instance == null)
        {
            if (enginePrefab != null)
            {
                Instantiate(enginePrefab);
                Debug.Log("[PersistentSignEngineBootstrap] Spawned persistent sign engine.");
            }
            else
            {
                Debug.LogError("[PersistentSignEngineBootstrap] No engine prefab assigned.");
            }
        }
    }
}