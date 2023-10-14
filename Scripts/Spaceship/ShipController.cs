using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShipController : MonoBehaviour {

    [Header("Handling")]
    [SerializeField] private bool engineOn;
    [Range(0, 100)]
    [SerializeField] private float throttle = 0f;
    [SerializeField] private float thrust = 500f;
    [SerializeField] private float rollSpeed = 90f;
    [SerializeField] private float pitchSpeed = 90f;
    [SerializeField] private float yawSpeed = 60f;

    [Header("Autopilot")]
    [SerializeField] private float sensitivity = 5f;
    [SerializeField] private float aggressiveTurnAngle = 10f;

    [Header("Booster")]
    [SerializeField] private float boosterThrust = 250f;
    [SerializeField] private float boosterDuration = 10f;
    [SerializeField] private float timeBeforeReload = 1.5f;
    [SerializeField] private float reloadRate = 1f;

    [Header("Input")]
    [SerializeField] private KeyCode engineKey = KeyCode.I;
    [SerializeField] private KeyCode boosterKey = KeyCode.LeftShift;
    [SerializeField] private float throttleSensivity = 10f;

    [Header("References")]
    [SerializeField] private MouseFlight controller;

    [Header("UI")]
    [SerializeField] private RawImage boosterBar;
    [SerializeField] private RawImage throttleBar;
    [SerializeField] private TMP_Text throttleText;
    [SerializeField] private TMP_Text infoText;

    [Header("Input")]
    [SerializeField] private float forceMultiplier;

    [Header("G Force")]
    [SerializeField] private float maxGForce = 8f;
    [SerializeField] private float minGForce = -3f;
    [SerializeField] private float overloadForceMultiplier = .2f;
    [Space(10)]
    [SerializeField] private TMP_Text gMeter;
    [SerializeField] private Color gMeterDefaultColor;
    [SerializeField] private Color gMeterWarningColor;

    // Physics
    private Rigidbody rb;
    private float currentGForce;
    private Vector3 lastVel;
    private float currentForceMultiplier;
    // UI
    private float throttleBarHeight;
    private float boosterBarHeight;
    // Booster timer
    private float boosterTimer = 0f;
    private float timeBeforeReloadTimer = 0f;
    private bool usingBooster;
    private bool wasUsingBooster;
    // Input
    private float pitch = 0f, yaw = 0f, roll = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        throttleBarHeight = throttleBar.rectTransform.rect.height;
        boosterBarHeight = boosterBar.rectTransform.rect.height;
    }

    private void Update()
    {
        HandleInput();
        HandleBooster();
        HandleUI();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        CalculateGForce();
    }

    #region Input and Autopilot

    /// <summary>
    /// Updates the pitch, roll, yaw, throttle, engine and boosters input
    /// </summary>
    private void HandleInput()
    {
        bool rollOverride = false;
        bool pitchOverride = false;

        float Pitch = Input.GetAxis("Vertical");
        float Roll = Input.GetAxis("Horizontal");

        // When the player commands their own input,
        // it should override the autopilot
        if (Mathf.Abs(Pitch) > .25f)
        {
            pitchOverride = true;
            rollOverride = true;
        }
        if (Mathf.Abs(Roll) > .25f)
            rollOverride = true;

        // Calculate the autopilot inputs.
        float autoPilotPitch;
        float autoPilotRoll;
        float autoPilotYaw;

        RunAutopilot(controller.MouseAimPos, out autoPilotYaw, out autoPilotPitch, out autoPilotRoll);

        pitch = (pitchOverride) ? Pitch : autoPilotPitch;
        roll = (rollOverride) ? Roll : autoPilotRoll;
        yaw = autoPilotYaw;

        // Update the throttle input, engine status and boosters
        float throttleInput = Input.GetAxis("Mouse ScrollWheel") * throttleSensivity;
        throttle = Mathf.Clamp(throttle += throttleInput, 0, 100);

        if (Input.GetKeyDown(engineKey)) engineOn = !engineOn;

        if (boosterTimer > 0) usingBooster = Input.GetKey(boosterKey);
        else usingBooster = false;
    }

    /// <summary>
    /// Calculates the yaw, pitch and roll inputs needed to rotate towards the flyTarget
    /// </summary>
    private void RunAutopilot(Vector3 flyTarget, out float yaw, out float pitch, out float roll)
    {
        // Converts the flyTarget position to local space
        Vector3 localFlyTarget = transform.InverseTransformPoint(flyTarget).normalized * sensitivity;

        yaw = Mathf.Clamp(localFlyTarget.x, -1f, 1f);
        pitch = -Mathf.Clamp(localFlyTarget.y, -1f, 1f);

        // There are two different roll commands depending on the situation.
        // If target is off axis, then roll into it, if it's directly in front, fly wings level
        float aggressiveRoll = Mathf.Clamp(localFlyTarget.x, -1f, 1f);
        float wingsLevelRoll = transform.right.y;

        // Calculate the angle to the target
        float angleOffTarget = Vector3.Angle(transform.forward, flyTarget - transform.position);

        // Blend between wingsLevel and aggressively rolling into the target.
        float wingsLevelInfluence = Mathf.InverseLerp(0f, aggressiveTurnAngle, angleOffTarget);
        roll = Mathf.Lerp(wingsLevelRoll, aggressiveRoll, wingsLevelInfluence);
    }

    #endregion
    #region Boosters

    /// <summary>
    /// Updates the booster timer
    /// </summary>
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

    #endregion
    #region Movement

    /// <summary>
    /// Propulses the spaceship forward and applies the pitch, yaw and roll forces
    /// </summary>
    private void HandleMovement()
    {
        if (!engineOn) return;

        rb.AddRelativeTorque(new Vector3(pitch * pitchSpeed,
            yaw * yawSpeed, -roll * rollSpeed) * currentForceMultiplier, ForceMode.Force);

        float currentThrust = thrust * (throttle / 100);
        if (usingBooster) currentThrust += boosterThrust;
        rb.AddForce(transform.forward * currentThrust, ForceMode.Force);
    }

    /// <summary>
    /// Calculates the current G Force based on the acceleration from the last velocity
    /// </summary>
    private void CalculateGForce()
    {
        Vector3 acceleration = (rb.velocity - lastVel);
        currentGForce = acceleration.magnitude / (Time.fixedDeltaTime * Physics.gravity.magnitude);
        lastVel = rb.velocity;
    }

    #endregion
    #region UI

    /// <summary>
    /// Updates the dimension of the throttle and booster bars, as well as the Speed and G Force indicators
    /// </summary>
    private void HandleUI()
    {
        throttleBar.rectTransform.sizeDelta = new Vector2(throttleBar.rectTransform.rect.width,
            throttleBarHeight * throttle / 100);
        boosterBar.rectTransform.sizeDelta = new Vector2(boosterBar.rectTransform.rect.width,
            boosterBarHeight * boosterTimer / boosterDuration);

        // Throttle and speed indicators
        throttleText.text = Mathf.Round(throttle).ToString() + "%";
        infoText.text = Mathf.Round(rb.velocity.magnitude * 3.6f).ToString() + "km/h\n";

        // G Force UI
        gMeter.text = currentGForce.ToString("0.00") + " G";
        if (currentGForce > maxGForce || currentGForce < minGForce)
        {
            gMeter.color = gMeterWarningColor;
            currentForceMultiplier = overloadForceMultiplier;
        }
        else
        {
            gMeter.color = gMeterDefaultColor;
            currentForceMultiplier = forceMultiplier;
        }
    }

    #endregion
}
