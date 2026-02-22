using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapScene : MonoBehaviour
{
    [SerializeField] Button _restaurant;
    [SerializeField] Button _grocery;

    void Start()
    {
        _restaurant.onClick.AddListener(LoadRestaurant);
        _grocery.onClick.AddListener(LoadGroceryStore);
    }

    private void LoadRestaurant()
    {
        ScenesManager.Instance.LoadScene(ScenesManager.Scene.RestaurantScene);
    }

    private void LoadGroceryStore()
    {
        ScenesManager.Instance.LoadScene(ScenesManager.Scene.GroceryScene);
    }
}
