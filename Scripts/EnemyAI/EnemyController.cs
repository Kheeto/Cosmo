using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Movement")]
    [Range(0, 100)]
    [SerializeField] private float throttle = 0f;
    [SerializeField] private float thrust = 500f;
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField] private float deviationSpeed = 2f;
    [SerializeField] private float deviationAmount = 2f;
    // The spaceship will try to keep this minimum speed and will stop turning if it's too slow
    [SerializeField] private float minimumSpeed = 30f;
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
    [SerializeField] private List<Missile> missiles = new List<Missile>();
    [SerializeField] private float missileMinDistance = 100f;
    [SerializeField] private List<Cannon> cannons = new List<Cannon>();
    [SerializeField] private float cannonMaxDistance = 500f;
    [SerializeField] private LayerMask cannonMask;
    private Rigidbody lockedOn;

    [Header("Countermeasures")]
    [SerializeField] private float minCountermeasureDistance;
    [SerializeField] private float maxCountermeasureDistance;
    [SerializeField] private float countermeasureCount = 75f;
    [SerializeField] private float dropAmount = 1f;
    [SerializeField] private Transform dropPosition;
    [SerializeField] private GameObject countermeasurePrefab;
    private List<Missile> incomingMissiles;

    private Vector3 movementDirection;
    private Vector3 currentSpeed;
    private Rigidbody rb;
    private GameObject player;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        player = FindObjectOfType<ShipController>().gameObject;
    }

    private void Update()
    {
        // Movement
        RotateShip();
        HandleThrust();
        HandleBooster();

        // Combat
        LookForPlayer();
        HandleWeapons();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    #region Movement

    private Vector3 predictionOffset;
    private Quaternion currentRotation;
    private void RotateShip()
    {
        Vector3 playerDirection = player.transform.position - transform.position;

        Vector3 deviation = new Vector3(Mathf.Cos(Time.time * deviationSpeed), 0f, 0f);
        predictionOffset = transform.TransformDirection(deviation) * deviationAmount;

        currentRotation = Quaternion.LookRotation(playerDirection + predictionOffset);

        // Only turns when going fast enough
        if (rb.velocity.magnitude > minimumSpeed)
        {
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, currentRotation, turnSpeed * Time.deltaTime));
            usingBooster = false;
        }
        else
        {
            // If too slow, use boosters
            usingBooster = true;
        }
    }

    private void HandleThrust()
    {
        float targetThrust = thrust * (throttle / 100);
        if (usingBooster) targetThrust += boosterThrust;
        currentThrust = targetThrust;
    }

    private void HandleMovement()
    {
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

    private bool playerVisible = false;
    private void LookForPlayer()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, cannonMask))
        {
            if (hit.collider.gameObject.CompareTag("Player")) playerVisible = true;
        }
    }

    private void HandleWeapons()
    {
        if (Vector3.Distance(player.transform.position, transform.position) < cannonMaxDistance)
            foreach (Cannon c in cannons)
            {
                c.SetShooting(playerVisible);
            }

        if (Vector3.Distance(player.transform.position, transform.position) > missileMinDistance)
            ShootMissile();
    }

    private void ShootMissile()
    {
        if (missiles.Count == 0) return;

        Missile nextMissile = missiles[0];
        if (nextMissile == null) return; // out of missiles

        nextMissile.SetTarget(player.GetComponent<Rigidbody>());
        nextMissile.Launch(rb.velocity, rb.angularVelocity);
        missiles.Remove(nextMissile);
    }

    private void DropCountermeasures()
    {
        if (incomingMissiles.Count == 0) return;

        List<Missile> closeMissiles = new List<Missile>();
        foreach (Missile m in incomingMissiles)
        {
            float d = Vector3.Distance(transform.position, m.transform.position);
            if (d < maxCountermeasureDistance && d > minCountermeasureDistance)
                closeMissiles.Add(m);
        }

        if (closeMissiles.Count == 0) return;

        if (countermeasureCount > 0)
        {
            countermeasureCount -= dropAmount;
            for (int i = 0; i < dropAmount; i++)
            {
                Instantiate(countermeasurePrefab, dropPosition.position, dropPosition.rotation, dropPosition);
            }
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

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, player.transform.position);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(player.transform.position, player.transform.position + predictionOffset);
    }

    public void AddMissile(Missile m)
    {
        incomingMissiles.Add(m);
    }
}
