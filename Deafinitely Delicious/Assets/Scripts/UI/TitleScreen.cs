using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{  
    [SerializeField] Button _startGame;

    void Start()
    {
        _startGame.onClick.AddListener(StartGame);
    }

    private void StartGame()
    {
        ScenesManager.Instance.LoadStartGame();
    }

    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}
