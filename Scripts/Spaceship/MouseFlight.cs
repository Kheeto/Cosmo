using System;
using System.Collections.Generic;
using UnityEngine;

public class MouseFlight : MonoBehaviour
{
    [Header("Mouse Look Settings")]
    [SerializeField] private float sensitivity = 3f;
    [SerializeField] private float cameraSpeed = 5f;
    [SerializeField] private float aimDistance = 500f;
    [SerializeField] private bool showDebugInfo;

    [Header("Keybinding")]
    [SerializeField] private KeyCode freeLookKey = KeyCode.C;

    [Header("References")]
    [SerializeField] private Transform spaceship;
    [SerializeField] private Transform mouseAim;
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Transform camera;

    private Vector3 frozenDirection = Vector3.forward;
    private bool isMouseAimFrozen = false;

    /// <summary>
    /// Get a point along the aircraft's boresight projected out to aimDistance meters.
    /// Useful for drawing a crosshair to aim fixed forward guns with, or to indicate what
    /// direction the aircraft is pointed.
    /// </summary>
    public Vector3 BoresightPos
    {
        get
        {
            return spaceship == null
                    ? transform.forward * aimDistance
                    : (spaceship.transform.forward * aimDistance) + spaceship.transform.position;
        }
    }

    /// <summary>
    /// Get the position that the mouse is indicating the aircraft should fly, projected
    /// out to aimDistance meters. Also meant to be used to draw a mouse cursor.
    /// </summary>
    public Vector3 MouseAimPos
    {
        get
        {
            if (mouseAim != null)
            {
                return isMouseAimFrozen
                    ? GetFrozenMouseAimPos()
                    : mouseAim.position + (mouseAim.forward * aimDistance);
            }
            else
            {
                return transform.forward * aimDistance;
            }
        }
    }

    private void Awake()
    {
        // To work correctly, the entire rig must not be parented to anything.
        transform.parent = null;
    }

    private void Update()
    {
        RotateRig();
    }

    private void FixedUpdate()
    {
        UpdateCameraPos();
    }

    private void RotateRig()
    {
        if (mouseAim == null || camera == null || cameraHolder == null)
            return;

        // Freeze the mouse aim direction when the free look key is pressed.
        if (Input.GetKeyDown(freeLookKey))
        {
            isMouseAimFrozen = true;
            frozenDirection = mouseAim.forward;
        }
        else if (Input.GetKeyUp(freeLookKey))
        {
            isMouseAimFrozen = false;
            mouseAim.forward = frozenDirection;
        }

        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = -Input.GetAxis("Mouse Y") * sensitivity;

        // Rotate the aim target to where the spaceship is meant to fly towards
        mouseAim.Rotate(camera.right, mouseY, Space.World);
        mouseAim.Rotate(camera.up, mouseX, Space.World);

        // The up vector of the camera normally is aligned to the horizon. However, when
        // looking straight up/down this can feel a bit weird. At those extremes, the camera
        // stops aligning to the horizon and instead aligns to itself.
        Vector3 upVec = (Mathf.Abs(mouseAim.forward.y) > 0.9f) ? cameraHolder.up : Vector3.up;

        // Smoothly rotate the camera towards the mouse aim position
        cameraHolder.rotation = Damp(cameraHolder.rotation,
                                    Quaternion.LookRotation(mouseAim.forward, upVec),
                                    cameraSpeed,
                                    Time.deltaTime);
    }

    private Vector3 GetFrozenMouseAimPos()
    {
        if (mouseAim != null)
            return mouseAim.position + (frozenDirection * aimDistance);
        else
            return transform.forward * aimDistance;
    }

    private void UpdateCameraPos()
    {
        if (!spaceship) return;

        transform.position = spaceship.position;
    }

    /// <summary>
    /// Creates dampened motion between a and b that is framerate independent.
    /// </summary>
    /// <param name="a">Initial parameter</param>
    /// <param name="b">Target parameter</param>
    /// <param name="lambda">Smoothing factor</param>
    /// <param name="dt">Time since last damp call</param>
    private Quaternion Damp(Quaternion a, Quaternion b, float lambda, float dt)
    {
        return Quaternion.Slerp(a, b, 1 - Mathf.Exp(-lambda * dt));
    }

    private void OnDrawGizmos()
    {
        if (showDebugInfo == true)
        {
            Color oldColor = Gizmos.color;

            // Draw the boresight position.
            if (spaceship != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(BoresightPos, 10f);
            }

            if (mouseAim != null)
            {
                // Draw the position of the mouse aim position.
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(MouseAimPos, 10f);

                // Draw axes for the mouse aim transform.
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(mouseAim.position, mouseAim.forward * 50f);
                Gizmos.color = Color.green;
                Gizmos.DrawRay(mouseAim.position, mouseAim.up * 50f);
                Gizmos.color = Color.red;
                Gizmos.DrawRay(mouseAim.position, mouseAim.right * 50f);
            }

            Gizmos.color = oldColor;
        }
    }
}