using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GroceryScript : MonoBehaviour
{
    [SerializeField] Button _back;

    void Start()
    {
        _back.onClick.AddListener(LoadMap);
    }

    private void LoadMap()
    {
        ScenesManager.Instance.LoadScene(ScenesManager.Scene.MapScene);
    }
}
