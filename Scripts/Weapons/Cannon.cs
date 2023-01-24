using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    [Header("Gun settings")]
    [SerializeField] private float damage;
    [SerializeField] private float penetration;
    [SerializeField] private float range;
    [SerializeField] private float fireRate; // shots per second
    [SerializeField] private LayerMask objectMask;

    [Header("Ammo")]
    [SerializeField] private AmmoType type;
    [SerializeField] private short maxAmmo;
    [SerializeField] private float reloadSpeed;
    [SerializeField] private float reloadDelay;
    [SerializeField] private float overheatTime;
    [SerializeField] private float overheatDelay;
    [SerializeField] private float coolingSpeed;

    [Header("References")]
    [SerializeField] private Transform gunBarrel;

    private float currentReloadDelay;
    private float currentOverheatTime;
    private float currentOverheatDelay;
    private short currentAmmo;
    private bool canShoot;
    private bool isShooting;
    private bool shouldShoot;
    private bool overheated;
    private enum AmmoType
    {
        Laser,
        Ballistic,
        HighExplosive,
        Distortion
    }

    private void Start()
    {
        currentAmmo = maxAmmo;
        currentReloadDelay = 0f;
        currentOverheatTime = 0f;
        currentOverheatDelay = 0f;
        canShoot = true;
        isShooting = false;
        shouldShoot = false;
        overheated = false;
    }

    private void Update()
    {
        Shoot();
        Overheat();
        Reload();
    }

    private void Shoot()
    {
        if (!shouldShoot) {
            isShooting = false;
            return;
        }
        if (overheated) return;

        isShooting = true;
        currentReloadDelay = 0f; // reset the reload timer if shooting

        RaycastHit hit;
        if (Physics.Raycast(gunBarrel.position, gunBarrel.forward, out hit, range, objectMask))
        {
            ShipModule m = hit.collider.gameObject.GetComponent<ShipModule>();
            if (m != null) // the hit object is a spaceship
                m.GetComponentInParent<ShipCombat>().Damage(m, damage);
        }

        currentAmmo--;
        canShoot = false;
        Invoke(nameof(ResetShooting), 1f / fireRate);
    }

    private void Overheat()
    {
        if (isShooting) currentOverheatTime += Time.deltaTime;
        else if (!isShooting && !overheated && currentOverheatTime > 0) currentOverheatTime -= Time.deltaTime * coolingSpeed;

        // cannon is overheated
        if (currentOverheatTime >= overheatTime) overheated = true;
        if (overheated) currentOverheatDelay += Time.deltaTime;

        // no longer overheated
        if (currentOverheatDelay == overheatDelay)
        {
            overheated = false;
            currentOverheatDelay = 0f;
        }
    }

    private void Reload()
    {
        // Ballistic and High Explosive guns don't reload
        if (type != AmmoType.Laser && type != AmmoType.Distortion) return;
        if (overheated) return; // can't reload while overheated

        if (!isShooting) currentReloadDelay += Time.deltaTime;

        // can reload
        if (currentReloadDelay >= reloadDelay)
        {
            short ammo = (short)Mathf.RoundToInt(Mathf.Lerp(currentAmmo, maxAmmo, reloadSpeed));
            currentAmmo = ammo;
        }
    }

    private void ResetShooting()
    {
        canShoot = true;
    }

    public void SetShooting(bool value)
    {
        shouldShoot = value;
    }
}
