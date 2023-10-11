using System;
using System.Collections;
using UnityEngine;

public class MouseFlightHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MouseFlight mouseFlight;
    [SerializeField] private RectTransform boresight;
    [SerializeField] private RectTransform mousePos;
    private Camera camera;

    private void Awake()
    {
        camera = mouseFlight.GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (mouseFlight == null || camera == null)
            return;

        UpdateGraphics(mouseFlight);
    }

    private void UpdateGraphics(MouseFlight controller)
    {
        if (boresight != null)
        {
            boresight.position = camera.WorldToScreenPoint(controller.BoresightPos);
            boresight.gameObject.SetActive(boresight.position.z > 1f);
        }

        if (mousePos != null)
        {
            mousePos.position = camera.WorldToScreenPoint(controller.MouseAimPos);
            mousePos.gameObject.SetActive(mousePos.position.z > 1f);
        }
    }

    public void SetReferenceMouseFlight(MouseFlight controller)
    {
        mouseFlight = controller;
    }
}