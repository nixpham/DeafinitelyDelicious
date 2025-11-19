using UnityEngine;
using UnityEngine.SceneManagement;

public class HighLightObject : MonoBehaviour
{
    public NPC npcScript;
    public GameObject door;
    public GameObject mom;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        npcScript.OnDialogueIndexChanged += HandleDialogueIndexChanged;
    }

    private void OnDestroy()
    {
        npcScript.OnDialogueIndexChanged -= HandleDialogueIndexChanged;
    }
    public void HandleDialogueIndexChanged(int index)
    {
        if (SceneManager.GetActiveScene().name != "RestaurantScene")
        {
            return;
        }
        if (index == 1)
        {
            door.SetActive(true);
            npcScript.BlockUntilObjectClicked();
            Debug.Log("Highlight Door active");
        }
        else if (index == 2)
        {
            door.SetActive(false);
            Debug.Log("Highlight Door inactive");

            mom.SetActive(true);
            npcScript.BlockUntilObjectClicked();
            Debug.Log("Highlight Mom active");

        }
        else if (index == 3)
        {
            mom.SetActive(false);
            Debug.Log("Highlight Mom inactive");

        }
    }

    public void OnDoorClicked()
    {
        npcScript.ResumeAfterClick(2);

        door.SetActive(false);
    }

    public void OnMomClicked()
    {
        npcScript.ResumeAfterClick(3);

        mom.SetActive(false);
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}