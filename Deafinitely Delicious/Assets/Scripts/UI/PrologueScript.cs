using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrologueScript : MonoBehaviour
{
    [SerializeField] Button _prologue;

    void Start()
    {
        _prologue.onClick.AddListener(ContinueGame);
    }

    private void ContinueGame()
    {
        ScenesManager.Instance.LoadNextScene();
    }
}
