using UnityEngine;

public class KnifeRotation : MonoBehaviour
{
    public float rotationSpeed = 50f;
    public float minAngle = -50f;
    public float maxAngle = -50f;
    private bool rotatingRight = true;
    private bool isPickedUp = false;

    public Transform tablePosition;
    public Transform pivotPosition;
    public BreadManager breadManager; // Reference to the bread manager

    void Start()
    {
        transform.position = tablePosition.position;
    }

    void Update()
    {
        if (isPickedUp)
        {
            RotateKnife();
        }
    }

    void RotateKnife()
    {
        float rotationStep = rotationSpeed * Time.deltaTime;
        float currentAngle = transform.eulerAngles.z;

        if (rotatingRight)
        {
            transform.Rotate(0, 0, -rotationStep);
            if (currentAngle < 360 + minAngle && currentAngle > 180)
            {
                rotatingRight = false;
            }
        }
        else
        {
            transform.Rotate(0, 0, rotationStep);
            if (currentAngle > maxAngle && currentAngle < 180)
            {
                rotatingRight = true;
            }
        }
    }

    public bool AttemptSlice()
    {
        float currentAngle = transform.eulerAngles.z;
        bool isSuccessful = Mathf.Abs(currentAngle) < 5f || Mathf.Abs(currentAngle - 360f) < 5f;

        if (isSuccessful)
        {
            Debug.Log("Perfect slice!");
        }
        else
        {
            Debug.Log("Angled slice! Adjusting...");
        }

        if (breadManager != null)
        {
            breadManager.UpdateBreadSprite(isSuccessful); // Update bread & track success/fail
        }

        return isSuccessful;
    }

    public void PickUpKnife()
    {
        if (!isPickedUp)
        {
            isPickedUp = true;
            transform.position = pivotPosition.position;
            Debug.Log("Knife picked up!");
        }
    }
}
