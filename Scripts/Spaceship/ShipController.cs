using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

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

    [Header("Autopilot")]
    [SerializeField] private float sensitivity = 5f;
    [SerializeField] private float aggressiveTurnAngle = 10f;

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

    [Header("References")]
    [SerializeField] private MouseFlight controller;

    [Header("Input")]
    [SerializeField] private float forceMult;
    private float currentForceMult;
    [SerializeField] [Range(-1f, 1f)] private float pitch = 0f;
    [SerializeField] [Range(-1f, 1f)] private float yaw = 0f;
    [SerializeField] [Range(-1f, 1f)] private float roll = 0f;
    public float Pitch { set { pitch = Mathf.Clamp(value, -1f, 1f); } get { return pitch; } }
    public float Yaw { set { yaw = Mathf.Clamp(value, -1f, 1f); } get { return yaw; } }
    public float Roll { set { roll = Mathf.Clamp(value, -1f, 1f); } get { return roll; } }

    public Vector3 movementInput;

    public bool pitchOverride = false;
    public bool rollOverride = false;

    [Header("G Force")]
    [SerializeField] private float maxPositiveGForce = 15f;
    [SerializeField] private float minNegativeGForce = -4f;
    [SerializeField] private float gForceMovementMultiplier = .2f;
    [Space(10)]
    [SerializeField] private TMP_Text gMeter;
    [SerializeField] private Color gMeterDefaultColor;
    [SerializeField] private Color gMeterWarningColor;
    private float currentGForce;
    private Vector3 lastVel;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;

        throttleBarHeight = throttleBar.rectTransform.rect.height;
        boosterBarHeight = boosterBar.rectTransform.rect.height;
    }

    private void Update()
    {
        HandleInput();
        HandleThrust();
        HandleBooster();
        HandleUI();
        HandleGForce();
    }

    private void FixedUpdate()
    {
        HandleMovement();

        Vector3 acceleration = (rb.velocity - lastVel);
        currentGForce = acceleration.magnitude / (Time.fixedDeltaTime * Physics.gravity.magnitude);

        lastVel = rb.velocity;
    }

    private void HandleInput()
    {
        rollOverride = false;
        pitchOverride = false;

        // Movement input
        float Pitch = Input.GetAxis("Vertical");
        float Roll = Input.GetAxis("Horizontal");

        // When the player commands their own stick input,
        // it should override the autopilot
        if (Mathf.Abs(Pitch) > .25f)
        {
            pitchOverride = true;
            rollOverride = true;
        }
        if (Mathf.Abs(Roll) > .25f)
            rollOverride = true;

        // Throttle and engine control
        float ThrottleInput = Input.GetAxis("Mouse ScrollWheel") * throttleSensivity;
        throttle = Mathf.Clamp(throttle += ThrottleInput, 0, 100);

        if (Input.GetKeyDown(engineKey)) engineOn = !engineOn;

        if (boosterTimer > 0) usingBooster = Input.GetKey(boosterKey);
        else usingBooster = false;

        // Calculate the autopilot inputs.
        float autoPitch = 0f;
        float autoRoll = 0f;
        float autoYaw = 0f;

        if (controller != null)
            RunAutopilot(controller.MouseAimPos, out autoYaw, out autoPitch, out autoRoll);

        pitch = (pitchOverride) ? Pitch : autoPitch;
        roll = (rollOverride) ? Roll : autoRoll;
        yaw = autoYaw; // (yawOverride) ? Yaw
    }

    // These inputs are created proportionally, so this can be prone to overshooting.
    // The physics in this example are tweaked so that it's not a big issue,
    // but using a PID controller for each axis is highly recommended.
    private void RunAutopilot(Vector3 flyTarget, out float yaw, out float pitch, out float roll)
    {
        // Converts the fly to position to local space
        var localFlyTarget = transform.InverseTransformPoint(flyTarget).normalized * sensitivity;
        var angleOffTarget = Vector3.Angle(transform.forward, flyTarget - transform.position);

        // Pitch and Yaw
        yaw = Mathf.Clamp(localFlyTarget.x, -1f, 1f);
        pitch = -Mathf.Clamp(localFlyTarget.y, -1f, 1f);

        // There are two different roll commands depending on the situation.
        // If target is off axis, then roll into it, if it's directly in front, fly wings level

        // Rolls into the target so that pitching up will put the nose onto the target
        var agressiveRoll = Mathf.Clamp(localFlyTarget.x, -1f, 1f);

        // Commands the aircraft to fly wings level.
        var wingsLevelRoll = transform.right.y;

        // Blend between auto level and banking into the target.
        var wingsLevelInfluence = Mathf.InverseLerp(0f, aggressiveTurnAngle, angleOffTarget);
        roll = Mathf.Lerp(wingsLevelRoll, agressiveRoll, wingsLevelInfluence);
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

        rb.AddRelativeTorque(new Vector3(pitch * pitchSpeed,
            yaw * yawSpeed, -roll * rollSpeed) * currentForceMult, ForceMode.Force);

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

    private void HandleGForce()
    {
        gMeter.text = currentGForce.ToString("0.00") + " G";

        if (currentGForce > maxPositiveGForce || currentGForce < minNegativeGForce)
        {
            gMeter.color = gMeterWarningColor;
            currentForceMult = gForceMovementMultiplier;
        }
        else
        {
            gMeter.color = gMeterDefaultColor;
            currentForceMult = forceMult;
        }
    }
}
