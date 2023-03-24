using System.Collections.Generic;
using UnityEngine;

public class Missile : MonoBehaviour
{
    [Header("Missile Settings")]
    [SerializeField] private bool engineOn = false;
    [SerializeField] private float thrust = 350f;
    [SerializeField] private float turnRate = 250f;
    [SerializeField] private float maxGForce = 30f;
    [SerializeField] private AnimationCurve damageCurve;
    [SerializeField] private float range;

    [Header("Explosion")]
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float explosionRadius = 5f;

    [Header("Missile Launch")]
    [SerializeField] private Vector3 separationForce;
    [SerializeField] private float engineStartDelay = 1f;
    [SerializeField] private float turnDelay = 1f;
    private float engineStartTimer = 0f;
    private float turnTimer = 0f;

    [Header("Radar Guidance")]
    [SerializeField] private Radar radar;
    private bool radarTargetFound;

    [Header("Prediction")]
    [SerializeField] private bool enablePrediction = true;
    [SerializeField] private float minPredictionDistance;
    [SerializeField] private float maxPredictionTime;

    [Header("References")]
    [SerializeField] private Rigidbody target;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject particleEffects;
    [SerializeField] private AmmoText ammoText;
    [SerializeField] private MissileWarning missileWarning;

    private float targetLostTime;
    private Vector3 prediction;
    private float leadTimePercentage;
    private Vector3 lastVelocity;
    public float currentGForce;

    public bool wasLaunched = false;

    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        float leadTimePercentage = 0f;
        if (target != null)
        {
            // If target is too far away, the missile will stop tracking
            if (Vector3.Distance(rb.position, target.position) > range) SetTarget(null);

            // Percentage of the distance where 0 is the minPredictionDistance and 1 is the maxMissileDistance
            // If Distance < minPredictionDistance, the missile will directly follow the target, and not the prediction
            leadTimePercentage = Mathf.InverseLerp(minPredictionDistance, range,
                Vector3.Distance(transform.position, target.position));
        }

        PredictMovement(leadTimePercentage);
        RotateMissile();

        CheckForRadarTarget();

        if (wasLaunched && engineStartTimer < engineStartDelay)
        {
            engineStartTimer += Time.deltaTime;
            engineOn = false;
        }
        else if (wasLaunched && engineStartTimer >= engineStartDelay)
            engineOn = true;

        if (wasLaunched && turnTimer < turnDelay)
            turnTimer += Time.deltaTime;

        if (engineOn && particleEffects)
            particleEffects.SetActive(true);
    }

    private void FixedUpdate()
    {
        if(engineOn) rb.AddForce(transform.forward * thrust);

        currentGForce = Utilities.CalculateGForce(rb, lastVelocity);

        lastVelocity = rb.velocity;
    }

    private void PredictMovement(float leadTimePercentage)
    {
        if (target == null) return;
        if (!enablePrediction)
        {
            prediction = target.position;
            return;
        }

        // If target isn't visible, can't predict movement
        if (!radarTargetFound) return;

        float predictionTime = Mathf.Lerp(0, maxPredictionTime, leadTimePercentage);
        prediction = target.position + target.velocity * predictionTime;
    }

    private void RotateMissile()
    {
        if (!engineOn) return;
        if (turnTimer < turnDelay) return;

        Vector3 heading = prediction - transform.position;
        Quaternion rotation = Quaternion.LookRotation(heading);

        if (currentGForce < maxGForce)
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, rotation, turnRate * Time.deltaTime));
    }

    private void CheckForRadarTarget()
    {
        List<RadarPing> radarTargets = radar.GetRadarPings();

        radarTargetFound = false;
        foreach (RadarPing rp in radarTargets)
        {
            if (rp.GetOwner() == null) continue;
            if (rp.GetOwner().gameObject.GetComponentInParent<Rigidbody>() == target) radarTargetFound = true;
        }
    }

    bool alreadyExploded;
    private void OnCollisionEnter(Collision collision)
    {
        alreadyExploded = false;
        if (alreadyExploded) return;
        alreadyExploded = true;

        // Damages every object in explosion radius
        Collider[] colliders = Physics.OverlapSphere(rb.position, explosionRadius);
        foreach(Collider c in colliders)
        {
            ShipModule m = c.gameObject.GetComponent<ShipModule>();
            if (m != null)
            {
                float distance = Vector3.Distance(rb.position, c.gameObject.transform.position);

                if (m.GetComponentInParent<ShipCombat>())
                    m.GetComponentInParent<ShipCombat>().Damage(m, damageCurve.Evaluate(distance));
                else if (m.GetComponentInParent<EnemyController>())
                    m.GetComponentInParent<EnemyController>().Damage(m, damageCurve.Evaluate(distance));
            }

            Rigidbody r = c.gameObject.GetComponent<Rigidbody>();
            if (r != null)
            {
                r.AddExplosionForce(explosionForce, transform.position, explosionRadius, 0f, ForceMode.Impulse);
            }
        }
        
        if (explosionPrefab)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (target == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(rb.position, prediction);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(target.position, prediction);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(rb.position, target.position);
    }

    public void SetTarget(Rigidbody rb) { target = rb; }

    public Rigidbody GetTarget() { return target; }

    public void Launch(Vector3 initialVelocity, Vector3 angularVelocity) {
        rb.isKinematic = false;
        rb.velocity = initialVelocity;
        rb.angularVelocity = angularVelocity;
        rb.AddRelativeForce(separationForce, ForceMode.Impulse);

        wasLaunched = true;

        if (ammoText != null) ammoText.UpdateAmmoText();
        if (missileWarning != null) missileWarning.AddMissile(this);

        EnemyController enemy = target.gameObject.GetComponent<EnemyController>();
        if (enemy != null)
            enemy.AddMissile(this);
    }
}
