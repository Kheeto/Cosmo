using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShipController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private MovementMode movementMode = MovementMode.VTOL;
    [SerializeField] private bool engineOn;

    [Header("Conventional")]
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

    [Header("VTOL")]
    [SerializeField] private float forwardSpeed = 200f;
    [SerializeField] private float backwardSpeed = 60f;
    [SerializeField] private float strafeSpeed = 110f;
    [SerializeField] private float hoverSpeed = 110f;
    [SerializeField] private float accelerationVTOL = 2.5f;
    [SerializeField] private float rollSpeedVTOL = 45f;

    [Header("Look")]
    [SerializeField] private float lookSpeedConventional = 360f;
    [SerializeField] private float lookSpeedVTOL = 700f;

    [Header("UI")]
    [SerializeField] private RawImage boosterBar;
    [SerializeField] private RawImage throttleBar;
    [SerializeField] private TMP_Text throttleText;
    [SerializeField] private TMP_Text infoText;
    private float throttleBarHeight;
    private float boosterBarHeight;

    [Header("Keybinding")]
    [SerializeField] private KeyCode movementModeKey = KeyCode.H;
    [SerializeField] private KeyCode engineKey = KeyCode.I;
    [SerializeField] private KeyCode boosterKey = KeyCode.LeftShift;

    // Input & movement
    private Vector3 currentSpeedConventional;
    private Vector3 currentSpeedVTOL;
    private Vector3 movementInput;
    private float hoverInput;
    private Vector2 lookInput, screenCenter, mouseDistance;

    private Rigidbody rb;
    private Vector3 lastVelocity;
    private enum MovementMode
    {
        Conventional,
        VTOL,
    }

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
        if (Input.GetKeyDown(movementModeKey) && movementMode == MovementMode.Conventional)
            movementMode = MovementMode.VTOL;
        else if (Input.GetKeyDown(movementModeKey) && movementMode == MovementMode.VTOL)
            movementMode = MovementMode.Conventional;

        // Movement input
        float Vertical = Input.GetAxis("Vertical");
        float Horizontal = Input.GetAxis("Horizontal");
        float Diagonal = Input.GetAxis("Diagonal");
        movementInput = new Vector3(Vertical, Horizontal, Diagonal);
        hoverInput = Mathf.Lerp(hoverInput, Input.GetAxis("Hover"), accelerationVTOL * Time.fixedDeltaTime);

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

        if (movementMode == MovementMode.Conventional) {
            // Conventional input only controls rotation, thrust is automatically added

            Vector3 lookRotation = new Vector3(-mouseDistance.y * lookSpeedConventional * Time.fixedDeltaTime,
                mouseDistance.x * lookSpeedConventional * Time.fixedDeltaTime, 0f);
            // Keyboard rotation
            currentSpeedConventional.x = Mathf.Lerp(currentSpeedConventional.x,
                movementInput.x * pitchSpeed,
                acceleration * Time.fixedDeltaTime);
            currentSpeedConventional.y = Mathf.Lerp(currentSpeedConventional.y,
                -movementInput.z * yawSpeed,
                acceleration * Time.fixedDeltaTime);
            currentSpeedConventional.z = Mathf.Lerp(currentSpeedConventional.z,
                -movementInput.y * rollSpeed,
                acceleration * Time.fixedDeltaTime);

            // applies thrust and the bigger rotation value between the look input and movement input
            if(lookRotation.magnitude > currentSpeedConventional.magnitude)
            {
                rb.AddRelativeTorque(lookRotation, ForceMode.Force);
            } else
                rb.AddRelativeTorque(currentSpeedConventional.x * Time.fixedDeltaTime,
                    currentSpeedConventional.y * Time.fixedDeltaTime, currentSpeedConventional.z * Time.fixedDeltaTime);

            rb.AddForce(transform.forward * currentThrust, ForceMode.Force);
        }
        else if (movementMode == MovementMode.VTOL)
        {
            // Look input controls rotation, keyboard inputs control movement
            rb.AddRelativeTorque(-mouseDistance.y * lookSpeedVTOL * Time.fixedDeltaTime,
                mouseDistance.x * lookSpeedVTOL * Time.fixedDeltaTime, movementInput.z * rollSpeedVTOL * Time.fixedDeltaTime);

            currentSpeedVTOL.x = Mathf.Lerp(currentSpeedVTOL.x, movementInput.x * forwardSpeed, accelerationVTOL * Time.fixedDeltaTime);
            currentSpeedVTOL.y = Mathf.Lerp(currentSpeedVTOL.y, hoverInput * hoverSpeed, accelerationVTOL * Time.fixedDeltaTime);
            currentSpeedVTOL.z = Mathf.Lerp(currentSpeedVTOL.z, movementInput.y * strafeSpeed, accelerationVTOL * Time.fixedDeltaTime);

            // applies movement and look rotation
            rb.AddForce(transform.forward * currentSpeedVTOL.x, ForceMode.Force);
            rb.AddForce(transform.up * currentSpeedVTOL.y + transform.right * currentSpeedVTOL.z, ForceMode.Force);
        }
    }

    private void HandleUI()
    {
        throttleBar.rectTransform.sizeDelta = new Vector2(throttleBar.rectTransform.rect.width,
            throttleBarHeight * throttle / 100);
        boosterBar.rectTransform.sizeDelta = new Vector2(boosterBar.rectTransform.rect.width,
            boosterBarHeight * boosterTimer / boosterDuration);

        throttleText.text = Mathf.Round(throttle).ToString() + "%";
        infoText.text = Mathf.Round(rb.velocity.magnitude * 3.6f).ToString() + "km/h\n"
            + Mathf.Round(rb.position.y).ToString() + "m";
    }
}
