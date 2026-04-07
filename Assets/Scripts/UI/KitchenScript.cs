using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KitchenScript : MonoBehaviour
{
    [SerializeField] Button _restaurant;
    [SerializeField] Button _fridge;
    [SerializeField] Button _topCabinet;
    [SerializeField] Button _bottomCabinet;

    void Start()
    {
        _restaurant.onClick.AddListener(LoadRestaurant);
        _fridge.onClick.AddListener(LoadFridge);
        _topCabinet.onClick.AddListener(LoadTopCabinet);
        _bottomCabinet.onClick.AddListener(LoadBottomCabinet);
    }

    private void LoadRestaurant()
    {
        ScenesManager.Instance.LoadScene(ScenesManager.Scene.RestaurantScene);
    }

    private void LoadFridge()
    {
        ScenesManager.Instance.LoadScene(ScenesManager.Scene.FridgeScene);
    }
    private void LoadTopCabinet()
    {
        ScenesManager.Instance.LoadScene(ScenesManager.Scene.TopCabinetScene);
    }
    private void LoadBottomCabinet()
    {
        ScenesManager.Instance.LoadScene(ScenesManager.Scene.BottomCabinetScene);
    }
}
