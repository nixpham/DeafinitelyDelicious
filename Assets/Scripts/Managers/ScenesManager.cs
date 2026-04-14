using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenesManager : MonoBehaviour
{
    private const string TUTORIAL_COMPLETED_KEY = "DEMO_TUTORIAL_COMPLETED";

    public static ScenesManager Instance;

    private void Awake()
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
        GroceryScene,
        TopCabinetScene,
        BottomCabinetScene,
    }

    public void LoadScene(Scene scene)
    {
        SceneManager.LoadScene(scene.ToString());
    }

    public void LoadStartGame()
    {
        if (PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1)
            SceneManager.LoadScene(Scene.RestaurantScene.ToString());
        else
            SceneManager.LoadScene(Scene.PrologueScene.ToString());
    }

    public void LoadNextScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}