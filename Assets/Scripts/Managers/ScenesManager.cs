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
        SceneManager.LoadScene(Scene.PrologueScene.ToString());
    }

    public void LoadNextScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
