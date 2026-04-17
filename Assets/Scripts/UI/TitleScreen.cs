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
        if (_startGame != null && _startGame.onClick.GetPersistentEventCount() == 0)
        {
            _startGame.onClick.AddListener(StartGame);
        }
    }

    public void StartGame()
    {
        AudioManager.Instance.PlaySfx(GameAudioPaths.UiRoomTransition, 0.75f);
        SceneManager.LoadScene(ScenesManager.Scene.PrologueScene.ToString());
    }

    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}
