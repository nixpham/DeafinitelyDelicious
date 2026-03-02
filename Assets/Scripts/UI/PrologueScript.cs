using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrologueScript : MonoBehaviour
{
    [SerializeField] Button _prologue;
    [SerializeField] NPC dialogueData;
    void Start()
    {
        _prologue.onClick.AddListener(ContinueGame);
    }

    private void ContinueGame()
    {
        if (dialogueData.dialogueIndex == 9)
        {
            ScenesManager.Instance.LoadNextScene();

        }
    }
}
