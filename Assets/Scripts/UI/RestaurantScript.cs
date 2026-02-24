using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RestuarantScript : MonoBehaviour
{
    [SerializeField] Button _kitchen;
    [SerializeField] Button _map;

    void Start()
    {
        _kitchen.onClick.AddListener(LoadKitchen);
        _map.onClick.AddListener(LoadMap);
    }

    private void LoadKitchen()
    {
        ScenesManager.Instance.LoadScene(ScenesManager.Scene.KitchenScene);
    }

    private void LoadMap()
    {
        ScenesManager.Instance.LoadScene(ScenesManager.Scene.MapScene);
    }
}
