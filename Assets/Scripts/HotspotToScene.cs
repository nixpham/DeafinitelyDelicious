using UnityEngine;
using UnityEngine.SceneManagement;

public class HotspotToScene : MonoBehaviour
{
    public string sceneToLoad;

    void OnMouseDown()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
