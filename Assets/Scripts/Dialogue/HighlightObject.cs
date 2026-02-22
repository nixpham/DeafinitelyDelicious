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
            npcScript.runNextLine = false;
            Debug.Log("Highlight Door active");
        }
        else if (index == 2)
        {
            door.SetActive(false);
            Debug.Log("Highlight Door inactive");
            npcScript.runNextLine = false;
            Debug.Log("nextline is" + npcScript.runNextLine);
            mom.SetActive(true);
            Debug.Log("Highlight Mom active");

        }
        else if (index == 3)
        {
            mom.SetActive(false);
            npcScript.runNextLine = false;
            Debug.Log("2 nextline is" + npcScript.runNextLine);

            Debug.Log("Highlight Mom inactive");

        }
    }

    public void OnDoorClicked()
    {
        npcScript.runNextLine = true;
        door.SetActive(false);
        npcScript.ResumeAfterClick(2);
    }

    public void OnMomClicked()
    {
        npcScript.runNextLine = true;
        mom.SetActive(false);
        Debug.Log("3 nextline is" + npcScript.runNextLine);

        Debug.Log("Resuming code after mom");
        npcScript.ResumeAfterClick(3);
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}