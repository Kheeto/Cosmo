using System.Collections.Generic;
using UnityEngine;

public enum Guidance
{
    IR, // Infrared homing (heat seeking)
    Radar, // Active or Semi-Active Radar Homing
}

public class Missile : MonoBehaviour
{
    [Header("Missile Settings")]
    [SerializeField] private Guidance mode;
    [SerializeField] private bool engineOn = false;
    [SerializeField] private float thrust = 350f;
    [SerializeField] private float turnRate = 250f;
    [SerializeField] private float maxGForce = 30f;
    [SerializeField] private AnimationCurve damageCurve;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float range;

    [Header("IR Guidance")]
    [SerializeField] private LayerMask IRObjectMask;
    [SerializeField] private LayerMask IRTargets;
    [SerializeField] private float IRRange;
    [SerializeField] private float IRRadius;
    private bool irTargetFound;

    [Header("Radar Guidance")]
    [SerializeField] private Radar radar;
    private bool radarTargetFound;

    [Header("Prediction")]
    [SerializeField] private bool enablePrediction = true;
    [SerializeField] private float minPredictionDistance;
    [SerializeField] private float maxPredictionTime;

    [Header("References")]
    [SerializeField] private Rigidbody target;
    private float targetLostTime;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject particleEffects;

    private Vector3 prediction;
    private float leadTimePercentage;
    private Vector3 lastVelocity;
    private float currentGForce;

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

        CheckForIRTarget();
        CheckForRadarTarget();
    }

    private void FixedUpdate()
    {
        if(engineOn) rb.AddForce(transform.forward * thrust);

        currentGForce = Utilities.CalculateGForce(rb, lastVelocity);
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
        if (mode == Guidance.Radar && !radarTargetFound) return;
        if (mode == Guidance.IR && !irTargetFound) return;

        float predictionTime = Mathf.Lerp(0, maxPredictionTime, leadTimePercentage);
        prediction = target.position + target.velocity * predictionTime;
    }

    private void RotateMissile()
    {
        Vector3 heading = prediction - transform.position;
        Quaternion rotation = Quaternion.LookRotation(heading);

        if (currentGForce < maxGForce)
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, rotation, turnRate * Time.deltaTime));
    }

    private void CheckForIRTarget()
    {
        if (mode != Guidance.IR) return;

        irTargetFound = false;

        RaycastHit hit;
        if (Physics.SphereCast(transform.position, IRRadius, transform.forward, out hit, IRRange,
            IRObjectMask, QueryTriggerInteraction.Collide))
        {
            Rigidbody rb = hit.collider.GetComponentInParent<Rigidbody>();
            if (rb != null)
            {
                target = rb;
                irTargetFound = true;
            }
            else target = null;
        }
    }

    private void CheckForRadarTarget()
    {
        if (mode != Guidance.Radar) return;

        List<RadarPing> radarTargets = radar.GetRadarPings();

        radarTargetFound = false;
        foreach (RadarPing rp in radarTargets)
        {
            if (rp.GetOwner().gameObject.GetComponentInParent<Rigidbody>() == target) radarTargetFound = true;
        }
    }

    bool alreadyHit;
    private void OnCollisionEnter(Collision collision)
    {
        alreadyHit = false;
        if (alreadyHit) return;
        alreadyHit = true;

        // Damages every object in explosion radius
        Collider[] colliders = Physics.OverlapSphere(rb.position, explosionRadius);
        foreach(Collider c in colliders)
        {
            ShipModule m = c.gameObject.GetComponent<ShipModule>();
            if (m != null)
            {
                float distance = Vector3.Distance(rb.position, c.gameObject.transform.position);
                m.GetComponentInParent<ShipCombat>().Damage(m, damageCurve.Evaluate(distance));
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

    public void Launch() {
        engineOn = true;
        if (particleEffects)
            particleEffects.SetActive(true);
    }

    public Guidance GetGuidance() { return mode; }
}
