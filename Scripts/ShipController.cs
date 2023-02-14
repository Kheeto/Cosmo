using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShipController : MonoBehaviour
{
    [Header("Flight Settings")]
    [SerializeField] private bool engineOn;
    [Range(0, 100)]
    [SerializeField] private float throttle = 0f;
    [SerializeField] private float throttleSensivity = 10f;
    [SerializeField] private float thrust = 500f;
    [SerializeField] private float thrustAcceleration = 5f;
    [SerializeField] private float rollSpeed = 90f;
    [SerializeField] private float pitchSpeed = 90f;
    [SerializeField] private float yawSpeed = 60f;
    [SerializeField] private float acceleration = 4.5f;
    private float currentThrust = 0f;

    [Header("Booster")]
    [SerializeField] private float boosterThrust = 250f;
    [SerializeField] private float boosterDuration = 10f;
    [SerializeField] private float timeBeforeReload = 1.5f;
    [SerializeField] private float reloadRate = 1f;
    private float boosterTimer = 0f;
    private float timeBeforeReloadTimer = 0f;
    private bool usingBooster;
    private bool wasUsingBooster;

    [Header("Look")]
    [SerializeField] private float lookSpeed = 360f;

    [Header("UI")]
    [SerializeField] private RawImage boosterBar;
    [SerializeField] private RawImage throttleBar;
    [SerializeField] private TMP_Text throttleText;
    [SerializeField] private TMP_Text infoText;
    private float throttleBarHeight;
    private float boosterBarHeight;

    [Header("Keybinding")]
    [SerializeField] private KeyCode engineKey = KeyCode.I;
    [SerializeField] private KeyCode boosterKey = KeyCode.LeftShift;

    // Input & movement
    private Vector3 currentSpeed;
    private Vector3 movementInput;
    private float hoverInput;
    private Vector2 lookInput, screenCenter, mouseDistance;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        screenCenter.x = Screen.width / 2;
        screenCenter.y = Screen.height / 2;
        Cursor.lockState = CursorLockMode.Confined;

        throttleBarHeight = throttleBar.rectTransform.rect.height;
        boosterBarHeight = boosterBar.rectTransform.rect.height;
    }

    private void Update()
    {
        HandleInput();
        HandleThrust();
        HandleBooster();
        HandleUI();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleInput()
    {
        // Movement input
        float Pitch = Input.GetAxis("Vertical");
        float Roll = Input.GetAxis("Horizontal");
        float Yaw = Input.GetAxis("Diagonal");
        movementInput = new Vector3(Pitch, Roll, Yaw);

        // Look input
        lookInput.x = Input.mousePosition.x;
        lookInput.y = Input.mousePosition.y;

        mouseDistance.x = (lookInput.x - screenCenter.x) / screenCenter.y;
        mouseDistance.y = (lookInput.y - screenCenter.y) / screenCenter.y;
        mouseDistance = Vector2.ClampMagnitude(mouseDistance, 1f);

        // Throttle and engine control
        float ThrottleInput = Input.GetAxis("Mouse ScrollWheel") * throttleSensivity;
        throttle = Mathf.Clamp(throttle += ThrottleInput, 0, 100);

        if (Input.GetKeyDown(engineKey)) engineOn = !engineOn;

        if (boosterTimer > 0) usingBooster = Input.GetKey(boosterKey);
        else usingBooster = false;
    }

    private void HandleThrust()
    {
        float targetThrust = thrust * (throttle / 100);
        if (usingBooster) targetThrust += boosterThrust;
        currentThrust = Mathf.Lerp(currentThrust, targetThrust, thrustAcceleration * Time.deltaTime);
    }

    private void HandleBooster()
    {
        if (usingBooster)
        {
            boosterTimer -= Time.deltaTime;
            wasUsingBooster = true;
        }
        else // if not using booster, reload it
        {
            if (wasUsingBooster)
            {
                timeBeforeReloadTimer = timeBeforeReload;
                wasUsingBooster = false;
                return;
            }

            if (timeBeforeReloadTimer > 0) timeBeforeReloadTimer -= Time.deltaTime;
            else if (timeBeforeReloadTimer <= 0 && boosterTimer < boosterDuration)
                boosterTimer += reloadRate * Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        if (!engineOn) return;

        Vector3 lookRotation = new Vector3(-mouseDistance.y * lookSpeed * Time.fixedDeltaTime,
            mouseDistance.x * lookSpeed * Time.fixedDeltaTime, 0f);
        // Keyboard rotation
        currentSpeed.x = Mathf.Lerp(currentSpeed.x,
            movementInput.x * pitchSpeed,
            acceleration * Time.fixedDeltaTime);
        currentSpeed.y = Mathf.Lerp(currentSpeed.y,
            -movementInput.z * yawSpeed,
            acceleration * Time.fixedDeltaTime);
        currentSpeed.z = Mathf.Lerp(currentSpeed.z,
            -movementInput.y * rollSpeed,
            acceleration * Time.fixedDeltaTime);

        // applies thrust and the look rotation
        rb.AddRelativeTorque(lookRotation, ForceMode.Force);
        rb.AddRelativeTorque(currentSpeed.x * Time.fixedDeltaTime,
                currentSpeed.y * Time.fixedDeltaTime, currentSpeed.z * Time.fixedDeltaTime);
        rb.AddForce(transform.forward * currentThrust, ForceMode.Force);
    }

    private void HandleUI()
    {
        throttleBar.rectTransform.sizeDelta = new Vector2(throttleBar.rectTransform.rect.width,
            throttleBarHeight * throttle / 100);
        boosterBar.rectTransform.sizeDelta = new Vector2(boosterBar.rectTransform.rect.width,
            boosterBarHeight * boosterTimer / boosterDuration);

        throttleText.text = Mathf.Round(throttle).ToString() + "%";
        infoText.text = Mathf.Round(rb.velocity.magnitude * 3.6f).ToString() + "km/h\n";
            //+ Mathf.Round(rb.position.y).ToString() + "m";
    }
}
