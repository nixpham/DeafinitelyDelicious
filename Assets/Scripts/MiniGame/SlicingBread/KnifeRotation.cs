using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnifeRotation : MonoBehaviour
{
    public float rotationSpeed = 55f; // Speed of rotation
    public float minAngle = 70f; // Left rotation limit
    public float maxAngle = 70f; // Right rotation limit
    private bool rotatingRight = true;

    void Update()
    {
        RotateKnife();
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
}

