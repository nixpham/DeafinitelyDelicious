using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cabinet : MonoBehaviour
{
    public static bool CabinetOpen = false;

    public GameObject cabinetUI;

    public void OpenCabinet()
    {
        cabinetUI.SetActive(true);
    }
}
