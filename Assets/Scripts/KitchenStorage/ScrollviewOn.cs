using UnityEngine;
using UnityEngine.UI;

public class ScrollviewOn : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject scrollView;
    public GameObject scrollView1;
    public GameObject scrollView2;
    public GameObject scrollView3;

    void Start()
    {
        Debug.Log("Buttons init");
    }
    public void Popup()
    {
        Debug.Log("Button clicked");
        scrollView1.SetActive(false);
        scrollView2.SetActive(false);
        scrollView3.SetActive(false);
        scrollView.SetActive(true);

    }

    public void Close()
    {
        scrollView.SetActive(false);
    }

}   
