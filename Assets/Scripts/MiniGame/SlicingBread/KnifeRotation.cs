using UnityEngine;

public class KnifeRotation : MonoBehaviour
{
    [SerializeField] private float maxAngle = 70f;
    [SerializeField] private float rotationSpeed = 120f;

    public bool pauseRotation { get; set; }

    private float currentAngle;
    private int direction = 1;
    private bool hasInitialized;

    private void OnEnable()
    {
        if (!hasInitialized)
        {
            currentAngle = -maxAngle;
            direction = 1;
            ApplyRotation();
            hasInitialized = true;
        }
        else
        {
            ApplyRotation();
        }
    }

    private void Update()
    {
        if (pauseRotation)
            return;

        currentAngle += direction * rotationSpeed * Time.deltaTime;

        if (currentAngle >= maxAngle)
        {
            currentAngle = maxAngle;
            direction = -1;
        }
        else if (currentAngle <= -maxAngle)
        {
            currentAngle = -maxAngle;
            direction = 1;
        }

        ApplyRotation();
    }

    public bool IsStraight(float tolerance = 10f)
    {
        return Mathf.Abs(currentAngle) <= tolerance;
    }

    public void ResetRotation()
    {
        currentAngle = -maxAngle;
        direction = 1;
        ApplyRotation();
    }

    public void SetRotationState(float angle, int newDirection)
    {
        currentAngle = Mathf.Clamp(angle, -maxAngle, maxAngle);
        direction = newDirection >= 0 ? 1 : -1;
        ApplyRotation();
    }

    public float GetCurrentAngle()
    {
        return currentAngle;
    }

    public int GetDirection()
    {
        return direction;
    }

    private void ApplyRotation()
    {
        transform.localEulerAngles = new Vector3(0f, 0f, currentAngle);
    }
}