using UnityEngine;

public class KitchenPanController : MonoBehaviour
{
    [Header("Pan limits (world units)")]
    public float minX = -5.27f;
    public float maxX = 5.75f;

    [Header("Tuning")]
    [Tooltip("Multiplier for how much finger/mouse drag moves the camera in world units. 1 = 1:1 drag.")]
    public float dragSpeed = .8f;

    public bool panEnabled = true;

    private bool dragging;
    private Vector3 lastWorld;

    void Update()
    {
        if (!panEnabled) return;

        // Prefer touch if present (mobile)
        if (Input.touchCount > 0)
        {
            HandleTouch(Input.GetTouch(0));
        }
        else
        {
            HandleMouse();
        }
    }

    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragging = true;
            lastWorld = ScreenToWorld(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && dragging)
        {
            Vector3 nowWorld = ScreenToWorld(Input.mousePosition);
            float dxWorld = (nowWorld.x - lastWorld.x) * dragSpeed;
            lastWorld = nowWorld;

            MoveCameraBy(-dxWorld);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }
    }

    private void HandleTouch(Touch t)
    {
        if (t.phase == TouchPhase.Began)
        {
            dragging = true;
            lastWorld = ScreenToWorld(t.position);
        }
        else if ((t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) && dragging)
        {
            Vector3 nowWorld = ScreenToWorld(t.position);
            float dxWorld = (nowWorld.x - lastWorld.x) * dragSpeed;
            lastWorld = nowWorld;

            MoveCameraBy(-dxWorld);
        }
        else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
        {
            dragging = false;
        }
    }

    private void MoveCameraBy(float dx)
    {
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x + dx, minX, maxX);
        transform.position = p;
    }

    private Vector3 ScreenToWorld(Vector3 screenPos)
    {
        UnityEngine.Camera cam = UnityEngine.Camera.main;
        if (cam == null) return Vector3.zero;

        float zDist = 0f - cam.transform.position.z;
        screenPos.z = zDist;

        return cam.ScreenToWorldPoint(screenPos);
    }
}