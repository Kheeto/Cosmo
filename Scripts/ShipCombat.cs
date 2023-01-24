using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipCombat : MonoBehaviour
{
    [SerializeField] private List<ShipModule> modules = new List<ShipModule>();

    [Header("Weapons")]
    [SerializeField] private Radar radar;
    [SerializeField] private List<Cannon> cannons = new List<Cannon>();
    [SerializeField] private List<Missile> missiles = new List<Missile>();

    [Header("IR Lock")]
    [SerializeField] private float checkDuration = 10f;
    [SerializeField] private float IRRange = 2500f;
    [SerializeField] private float IRRadius = 30f;
    [SerializeField] private LayerMask objectMask;
    [SerializeField] private Rigidbody lockedOn;
    private bool irTargetFound = false;
    private bool locking = false;
    private float currentCheckTime = 0f;

    [Header("Countermeasures")]
    [SerializeField] private Transform dropPosition;
    [SerializeField] private float flareCount = 150f;
    [SerializeField] private float flareDropPerClick = 2f;
    [SerializeField] private GameObject flarePrefab;
    [SerializeField] private float chaffCount = 75f;
    [SerializeField] private float chaffDropPerClick = 1f;
    [SerializeField] private GameObject chaffPrefab;

    [Header("Keybinding")]
    [SerializeField] private KeyCode fireCannonKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode fireMissileKey = KeyCode.Mouse5;
    [SerializeField] private KeyCode fireCountermeasureKey = KeyCode.Mouse4;
    [SerializeField] private KeyCode IRLockKey = KeyCode.T;

    private void Update()
    {
        HandleCannons();
        HandleMissiles();
        HandleCountermeasures();

        CheckForIRTarget();
        HandleTargetLock();
    }

    private void HandleCannons()
    {
        foreach(Cannon c in cannons)
        {
            c.SetShooting(Input.GetKey(fireCannonKey));
        }
    }

    private void HandleMissiles()
    {
        if (Input.GetKeyDown(fireMissileKey))
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
    }

    private void HandleCountermeasures()
    {
        if (Input.GetKeyDown(fireCountermeasureKey))
        {
            if (flareCount > 0)
            {
                flareCount -= flareDropPerClick;
                Instantiate(flarePrefab, dropPosition.position, dropPosition.rotation, dropPosition);
            }

            if (chaffCount > 0)
            {
                chaffCount -= chaffDropPerClick;
                Instantiate(chaffPrefab, dropPosition.position, dropPosition.rotation, dropPosition);
            }
        }
    }

    private void CheckForIRTarget()
    {
        irTargetFound = false;
        if (!locking) return;

        RaycastHit hit;
        if (Physics.SphereCast(transform.position, IRRadius, transform.forward, out hit, IRRange,
            objectMask, QueryTriggerInteraction.Collide))
        {
            // Checks if there's an object between the heat and the sensor
            Vector3 direction = hit.point - transform.position;
            RaycastHit hit2;
            if (Physics.Raycast(transform.position, direction, out hit2, IRRange,
                objectMask, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.gameObject != hit2.collider.gameObject)
                    return; // there was an object in between
            }

            // Locks on the rigidbody of the IR cross section
            Rigidbody rb = hit.collider.GetComponentInParent<Rigidbody>();
            if (rb != null)
            {
                lockedOn = rb;
                irTargetFound = true;
            }
            else lockedOn = null;
        }
    }

    private void HandleTargetLock()
    {
        if (Input.GetKeyDown(IRLockKey))
        {
            if (locking) locking = false;
            else locking = true;

            currentCheckTime = checkDuration;
        }

        if (locking && lockedOn == null && currentCheckTime > 0)
            currentCheckTime -= Time.deltaTime;

        if (currentCheckTime == 0)
        {
            locking = false;
            currentCheckTime = checkDuration;
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
        foreach(ShipModule m in modules)
        {
            health += m.GetHealth();
        }
        return health;
    }

    private void Die()
    {
        Debug.Log("Ship has been destroyed");
    }
}
