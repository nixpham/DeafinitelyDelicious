using UnityEngine;
using UnityEngine.SceneManagement;

public class HotspotToScene : MonoBehaviour
{
    public string sceneToLoad;

    void OnMouseDown()
    {
        AudioManager.Instance.PlaySfx(GameAudioPaths.UiRoomTransition, 0.75f);
        SceneManager.LoadScene(sceneToLoad);
    }
}
