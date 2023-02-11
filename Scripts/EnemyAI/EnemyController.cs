using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Movement")]
    [Range(0, 100)]
    [SerializeField] private float throttle = 0f;
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

    [Header("Combat")]
    [SerializeField] private Radar radar;
    [SerializeField] private List<ShipModule> modules = new List<ShipModule>();
    [SerializeField] private List<Cannon> cannons = new List<Cannon>();
    [SerializeField] private List<Missile> missiles = new List<Missile>();
    private Rigidbody lockedOn;

    [Header("Countermeasures")]
    [SerializeField] private float minCountermeasureDistance;
    [SerializeField] private float maxCountermeasureDistance;
    [SerializeField] private Transform dropPosition;
    [SerializeField] private float chaffCount = 75f;
    [SerializeField] private float chaffDropAmount = 1f;
    [SerializeField] private GameObject chaffPrefab;

    private Vector3 movementDirection;
    private Vector3 currentSpeed;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        HandleThrust();
        HandleBooster();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    #region Movement

    private void HandleThrust()
    {
        float targetThrust = thrust * (throttle / 100);
        if (usingBooster) targetThrust += boosterThrust;
        currentThrust = Mathf.Lerp(currentThrust, targetThrust, thrustAcceleration * Time.deltaTime);
    }

    private void HandleMovement()
    {
        currentSpeed.x = Mathf.Lerp(currentSpeed.x,
            movementDirection.x * pitchSpeed,
            acceleration * Time.fixedDeltaTime);
        currentSpeed.y = Mathf.Lerp(currentSpeed.y,
            -movementDirection.z * yawSpeed,
            acceleration * Time.fixedDeltaTime);
        currentSpeed.z = Mathf.Lerp(currentSpeed.z,
            -movementDirection.y * rollSpeed,
            acceleration * Time.fixedDeltaTime);

        // applies thrust rotation
        rb.AddRelativeTorque(currentSpeed.x * Time.fixedDeltaTime,
                currentSpeed.y * Time.fixedDeltaTime, currentSpeed.z * Time.fixedDeltaTime);
        rb.AddForce(transform.forward * currentThrust, ForceMode.Force);
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

    #endregion

    #region Combat

    private void ShootCannons(bool state)
    {
        foreach (Cannon c in cannons)
        {
            c.SetShooting(state);
        }
    }

    private void ShootMissile()
    {
        Missile nextMissile = missiles[0];
        if (nextMissile == null) return; // out of missiles

        if (nextMissile.GetGuidance() == Guidance.IR)
        {
            if (lockedOn == null) return; // no target
            else nextMissile.SetTarget(lockedOn);
        }
        else if (nextMissile.GetGuidance() == Guidance.Radar)
        {
            if (radar.GetTarget() == null) return; // no target
            else nextMissile.SetTarget(radar.GetTarget());
        }

        nextMissile.Launch();
        missiles.Remove(nextMissile);
    }

    private void DropCountermeasures()
    {
        if (chaffCount > 0)
        {
            chaffCount -= chaffDropAmount;
            Instantiate(chaffPrefab, dropPosition.position, dropPosition.rotation, dropPosition);
        }
    }

    public void Damage(ShipModule module, float damage)
    {
        module.Damage(damage);
        if (CalculateTotalHealth() <= 0) Die();
    }

    private float CalculateTotalHealth()
    {
        float health = 0;
        foreach (ShipModule m in modules)
        {
            health += m.GetHealth();
        }
        return health;
    }

    private void Die()
    {
        Debug.Log("Enemy Ship has been destroyed");
    }

    #endregion
}
