using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KitchenScript : MonoBehaviour
{
    [SerializeField] Button _restaurant;
    [SerializeField] Button _fridge;

    void Start()
    {
        _restaurant.onClick.AddListener(LoadRestaurant);
        _fridge.onClick.AddListener(LoadFridge);
    }

    private void LoadRestaurant()
    {
        ScenesManager.Instance.LoadScene(ScenesManager.Scene.RestaurantScene);
    }

    private void LoadFridge()
    {
        ScenesManager.Instance.LoadScene(ScenesManager.Scene.FridgeScene);
    }
}
