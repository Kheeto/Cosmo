using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Camera Look")]
    [SerializeField] private float LookSpeedX;
    [SerializeField] private float LookSpeedY;
    [SerializeField] private Transform Orientation;

    private void Update()
    {
        Look();
    }

    private float xRotation, yRotation;
    private void Look()
    {
        // Get mouse input
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * LookSpeedX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * LookSpeedY;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        Orientation.localRotation = Quaternion.Euler(0f, yRotation, 0f);
    }
}
