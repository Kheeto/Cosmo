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
    [SerializeField] private int maxAmmo;
    [SerializeField] private float reloadSpeed;
    [SerializeField] private float reloadDelay;
    [SerializeField] private float overheatTime;
    [SerializeField] private float overheatDelay;
    [SerializeField] private float coolingSpeed;

    [Header("References")]
    [SerializeField] private Transform gunBarrel;

    [SerializeField] private float currentReloadDelay;
    [SerializeField] private float currentOverheatTime;
    [SerializeField] private float currentOverheatDelay;
    [SerializeField] private int currentAmmo;
    [SerializeField] private bool canShoot;
    [SerializeField] private bool isShooting;
    [SerializeField] private bool shouldShoot;
    [SerializeField] private bool overheated;
    private enum AmmoType
    {
        Laser,
        Ballistic,
        HighExplosive
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
        if (currentAmmo <= 0) return; // need ammo to reload
        if (!canShoot) return;
        Debug.Log("shot");

        isShooting = true;
        currentReloadDelay = 0f; // reset the reload timer if shooting

        RaycastHit hit;
        if (Physics.Raycast(gunBarrel.position, gunBarrel.forward, out hit, range, objectMask))
        {
            Debug.Log(hit.collider.gameObject.name);
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
        if (currentOverheatTime > overheatTime) overheated = true;
        if (overheated) currentOverheatDelay += Time.deltaTime;

        // no longer overheated
        if (currentOverheatDelay >= overheatDelay)
        {
            overheated = false;
            currentOverheatDelay = 0f;
            currentOverheatTime = overheatTime;
        }
    }

    private void Reload()
    {
        // Ballistic and High Explosive guns don't reload
        if (type != AmmoType.Laser) return;
        if (overheated) return; // can't reload while overheated

        if (!isShooting && currentReloadDelay < reloadDelay) currentReloadDelay += Time.deltaTime;
        else if (isShooting) return;

        // can reload
        if (currentReloadDelay >= reloadDelay)
        {
            int ammo = Mathf.RoundToInt(Mathf.Lerp(currentAmmo, maxAmmo, reloadSpeed * Time.deltaTime));
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