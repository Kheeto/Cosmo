using System.Collections.Generic;
using UnityEngine;

public class Missile : MonoBehaviour {

    [Header("Handling")]
    [SerializeField] private bool engineOn = false;
    [SerializeField] private float thrust = 350f;
    [SerializeField] private float turnRate = 250f;
    [SerializeField] private float maxGForce = 30f;

    [Header("Guidance")]
    [SerializeField] private float range = 2000f;
    [SerializeField] private Radar radar;

    [Header("Prediction")]
    [SerializeField] private bool enablePrediction = true;
    [SerializeField] private float minPredictionDistance = 0f;
    [SerializeField] private float maxPredictionTime = 1f;

    [Header("Explosion")]
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private AnimationCurve damageCurve;

    [Header("Launch")]
    [SerializeField] private Vector3 separationForce = Vector3.zero;
    [SerializeField] private float engineStartDelay = 1f;
    [SerializeField] private float turnDelay = 1f;
    private float engineStartTimer = 0f;
    private float turnTimer = 0f;

    [Header("References")]
    [SerializeField] private Rigidbody target;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject particleEffects;
    [SerializeField] private AmmoText ammoText;
    [SerializeField] private MissileWarning missileWarning;

    // Calculating G Force
    private Vector3 lastVelocity;
    private float currentGForce;

    public bool wasLaunched { get; private set; }  = false;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        HandleMissile();

        // Handle timer delays
        if (wasLaunched && engineStartTimer < engineStartDelay)
            engineStartTimer += Time.deltaTime;
        else if (wasLaunched && engineStartTimer >= engineStartDelay)
            engineOn = true;
        if (wasLaunched && turnTimer < turnDelay)
            turnTimer += Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if(engineOn) rb.AddForce(transform.forward * thrust);

        currentGForce = CalculateGForce(rb, lastVelocity);
        lastVelocity = rb.velocity;
    }

    /// <summary>
    /// Rotates the missile towards the next target's predicted position
    /// </summary>
    private void HandleMissile()
    {
        if (!engineOn) return;
        if (turnTimer < turnDelay) return;

        Vector3 prediction = PredictMovement(GetDistancePercentage());
        Vector3 heading = prediction - transform.position;
        Quaternion rotation = Quaternion.LookRotation(heading);

        if (currentGForce < maxGForce)
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, rotation, turnRate * Time.deltaTime));
    }

    /// <summary>
    /// Predicts the movement of the target based on its current position and velocity
    /// </summary>
    /// <param name="predictionTimePercentage">How far to predict into the future between 0 and 1</param>
    private Vector3 PredictMovement(float predictionTimePercentage)
    {
        if (target == null) return transform.position;
        if (!IsTargetVisibleOnRadar()) return transform.position;
        if (!enablePrediction) return target.position;

        // Calculate the next target's position based on its velocity and the predictionTimePercentage
        float predictionTime = Mathf.Lerp(0, maxPredictionTime, predictionTimePercentage);
        return target.position + target.velocity * predictionTime;
    }

    /// <summary>
    /// Returns a value between 0 and 1 where 0 is the minPredictionDistance and 1 is the range of this missile.
    /// </summary>
    private float GetDistancePercentage()
    {
        if (target == null) return 0f;

        float distance = Vector3.Distance(transform.position, target.position);

        // If target is too far away, the missile will stop tracking
        if (distance > range) SetTarget(null);

        return Mathf.InverseLerp(minPredictionDistance, range, distance);
    }

    /// <summary>
    /// Loops through the radar pings and checks if the current target is one of them
    /// </summary>
    private bool IsTargetVisibleOnRadar()
    {
        List<RadarPing> radarTargets = radar.GetRadarPings();

        bool radarTargetFound = false;
        foreach (RadarPing rp in radarTargets)
        {
            if (rp.GetOwner() == null) continue;
            if (rp.GetOwner().gameObject.GetComponentInParent<Rigidbody>() == target) radarTargetFound = true;
        }
        return radarTargetFound;
    }

    /// <summary>
    /// Calculates the G Force acting on the missile
    /// </summary>
    public static float CalculateGForce(Rigidbody rb, Vector3 lastVelocity)
    {
        Vector3 currentVelocity = rb.velocity;
        Vector3 acceleration = (currentVelocity - lastVelocity) / Time.fixedDeltaTime;

        float Gforce = acceleration.normalized.magnitude / Physics.gravity.magnitude;
        return Gforce;
    }

    /// <summary>
    /// Launches the missile with an inital velocity and angularVelocity
    /// </summary>
    public void Launch(Vector3 initialVelocity, Vector3 angularVelocity)
    {
        rb.isKinematic = false;
        rb.velocity = initialVelocity;
        rb.angularVelocity = angularVelocity;
        rb.AddRelativeForce(separationForce, ForceMode.Impulse);

        wasLaunched = true;

        if (ammoText != null) ammoText.UpdateAmmoText();
        if (missileWarning != null) missileWarning.AddMissile(this);
        particleEffects.SetActive(true);

        EnemyController enemy = target.gameObject.GetComponent<EnemyController>();
        if (enemy != null)
            enemy.AddMissile(this);
    }

    bool alreadyExploded;
    private void OnCollisionEnter(Collision collision)
    {
        if (!engineOn) return;

        // Prevents clipping of multiple OnCollisionEnter calls
        alreadyExploded = false;
        if (alreadyExploded) return;
        alreadyExploded = true;

        // Damages every object in explosion radius
        foreach(Collider c in Physics.OverlapSphere(rb.position, explosionRadius))
        {
            ShipModule m = c.GetComponent<ShipModule>();
            if (m != null)
            {
                float distance = Vector3.Distance(rb.position, c.transform.position);

                if (m.GetComponentInParent<ShipCombat>())
                    m.GetComponentInParent<ShipCombat>().Damage(m, damageCurve.Evaluate(distance));
                else if (m.GetComponentInParent<EnemyController>())
                    m.GetComponentInParent<EnemyController>().Damage(m, damageCurve.Evaluate(distance));
            }
            Rigidbody r = c.GetComponent<Rigidbody>();
            if (r != null)
                r.AddExplosionForce(explosionForce, transform.position, explosionRadius, 0f, ForceMode.Impulse);
        }

        if (explosionPrefab)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    public void SetTarget(Rigidbody rb) { target = rb; }

    public Rigidbody GetTarget() { return target; }
}
