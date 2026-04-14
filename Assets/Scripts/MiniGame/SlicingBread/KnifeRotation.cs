using UnityEngine;

public class KnifeRotation : MonoBehaviour
{
    [SerializeField] private float maxAngle = 70f;
    [SerializeField] private float rotationSpeed = 120f;

    private float currentAngle;
    private int direction = 1;

    private void OnEnable()
    {
        currentAngle = -maxAngle;
        ApplyRotation();
        direction = 1;
    }

    private void Update()
    {
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

    private void ApplyRotation()
    {
        transform.localEulerAngles = new Vector3(0f, 0f, currentAngle);
    }
}