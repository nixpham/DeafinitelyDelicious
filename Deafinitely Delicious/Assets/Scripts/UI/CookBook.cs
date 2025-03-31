using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CookBook : MonoBehaviour
{
    public static bool CookBookOpen = false;

    public GameObject cookBookUI;
    [SerializeField] Button _cookbook;
    [SerializeField] Button _xbutton;

    void Start()
    {
        _cookbook.onClick.AddListener(OpenCookBook);
        _xbutton.onClick.AddListener(CloseCookBook);
    }

    private void OpenCookBook()
    {
        cookBookUI.SetActive(true);
        CookBookOpen = true;
    }

    private void CloseCookBook()
    {
        cookBookUI.SetActive(false);
        CookBookOpen = false; 
    }
}
