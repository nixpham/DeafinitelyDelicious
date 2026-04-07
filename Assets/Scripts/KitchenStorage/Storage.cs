using UnityEngine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Engine;
using System;
using Common;
[CreateAssetMenu(fileName = "NewStorage", menuName = "Kitchen Storage")]

public class Storage : ScriptableObject
{
    [Header("Areas")]
    public Shelf[] fridge;
    public Shelf[] drawer;
    public Shelf[] lowerCabinet;
    public Shelf[] shelf;
}
[System.Serializable]
public class Shelf
{
    
    public Food[] shelf;
}
[System.Serializable]
public class Food
{
    //public GameObject food;
    public Sprite sprite;
    public string sign;
    
}
