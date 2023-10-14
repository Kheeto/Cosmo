using System.Collections.Generic;
using UnityEngine;

public class ShipCombat : MonoBehaviour {

    [Header("Health")]
    [SerializeField] private List<ShipModule> modules = new List<ShipModule>();

    [Header("Weapons")]
    [SerializeField] private Radar radar;
    [SerializeField] private List<Cannon> cannons = new List<Cannon>();
    [SerializeField] private List<Missile> missiles = new List<Missile>();

    [Header("Countermeasures")]
    [SerializeField] private float flareAmount = 75f;
    [SerializeField] private float flareDropAmount = 1f;
    [SerializeField] private Transform flareDropPosition;
    [SerializeField] private GameObject flarePrefab;

    [Header("Keybinding")]
    [SerializeField] private KeyCode fireCannonKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode fireMissileKey = KeyCode.Mouse5;
    [SerializeField] private KeyCode fireCountermeasureKey = KeyCode.Mouse4;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        HandleCannons();
        HandleMissiles();
        HandleCountermeasures();
    }

    #region Weapons and Countermeasures

    /// <summary>
    /// Sets every cannon's shooting status based on the player's input
    /// </summary>
    private void HandleCannons()
    {
        foreach(Cannon c in cannons)
        {
            c.SetShooting(Input.GetKey(fireCannonKey));
        }
    }

    /// <summary>
    /// Sets the target of the next missile and launches it
    /// </summary>
    private void HandleMissiles()
    {
        if (Input.GetKeyDown(fireMissileKey))
        {
            if (missiles.Count == 0) return;

            Missile nextMissile = missiles[0];
            if (nextMissile == null) return; // out of missiles

            if (radar.GetTarget() == null) return; // no target
            else nextMissile.SetTarget(radar.GetTarget());

            nextMissile.Launch(rb.velocity, rb.angularVelocity);
            missiles.Remove(nextMissile);
        }
    }

    /// <summary>
    /// Drops countermeasures when a key is pressed
    /// </summary>
    private void HandleCountermeasures()
    {
        if (Input.GetKeyDown(fireCountermeasureKey))
        {
            if (flareAmount > 0)
            {
                flareAmount -= flareDropAmount;
                Instantiate(flarePrefab, flareDropPosition.position, flareDropPosition.rotation, flareDropPosition);
            }
        }
    }

    #endregion

    #region Damage and health

    /// <summary>
    /// Damages the specified module of this spaceship
    /// </summary>
    public void Damage(ShipModule module, float damage)
    {
        module.Damage(damage);
        if (CalculateTotalHealth() <= 0) Die();
    }

    /// <summary>
    /// Returns the total health of this spaceship which is the sum of all the modules' health
    /// </summary>
    private float CalculateTotalHealth()
    {
        float health = 0f;
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

    #endregion
}
