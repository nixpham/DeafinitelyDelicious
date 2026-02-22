using UnityEngine;

public class KitchenPanController : MonoBehaviour
{
    [Header("Pan limits (world units)")]
    public float minX = -10f;
    public float maxX = 10f;

    [Header("Tuning")]
    public float dragSpeed = 0.01f;

    public bool panEnabled = true;

    private bool dragging;
    private Vector2 lastPos;

    void Update()
    {
        if (!panEnabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            dragging = true;
            lastPos = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0) && dragging)
        {
            Vector2 pos = Input.mousePosition;
            float dx = pos.x - lastPos.x;
            lastPos = pos;

            // drag to move camera
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x - dx * dragSpeed, minX, maxX);
            transform.position = p;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }
    }
}