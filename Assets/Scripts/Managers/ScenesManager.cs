using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenesManager : MonoBehaviour
{
    public static ScenesManager Instance;

    private void Awake ()
    {
        Instance = this;
    }

    public enum Scene
    {
        TitleScreen,
        PrologueScene,
        RestaurantScene,
        StoreScene,
        MapScene,
        KitchenScene,
        FridgeScene,
        GroceryScene
    }

    public void LoadScene(Scene scene)
    {
        AudioManager.Instance.PlaySfx(GameAudioPaths.UiRoomTransition, 0.75f);
        SceneManager.LoadScene(scene.ToString());
    }

    public void LoadStartGame()
    {
        AudioManager.Instance.PlaySfx(GameAudioPaths.UiRoomTransition, 0.75f);
        SceneManager.LoadScene(Scene.PrologueScene.ToString());
    }

    public void LoadNextScene()
    {
        AudioManager.Instance.PlaySfx(GameAudioPaths.UiRoomTransition, 0.75f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
