using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FridgeScript : MonoBehaviour
{
    [SerializeField] Button _kitchen;

    void Start()
    {
        _kitchen.onClick.AddListener(LoadKitchen);
    }

    private void LoadKitchen()
    {
        ScenesManager.Instance.LoadScene(ScenesManager.Scene.KitchenScene);
    }
}
