using UnityEngine;
using UnityEngine.UI;

public class Cannon : MonoBehaviour {

    [Header("Settings")]
    [SerializeField] private float damage;
    [SerializeField] private float range;
    [SerializeField] private float fireRate; // shots per second
    [SerializeField] private LayerMask objectMask;

    [Header("Ammo")]
    [SerializeField] private int maxAmmo;
    [SerializeField] private float reloadSpeed;
    [SerializeField] private float reloadDelay;
    [SerializeField] private float overheatTime;
    [SerializeField] private float overheatDelay;
    [SerializeField] private float coolingSpeed;

    [Header("References")]
    [SerializeField] private Transform gunBarrel;
    [SerializeField] private GameObject muzzleFlash;

    [Header("UI")]
    [SerializeField] private bool enableCannonUI;
    [SerializeField] private RawImage ammoBarUI;
    [SerializeField] private AmmoText ammoText;
    [SerializeField] private Color normalColor;
    [SerializeField] private Color overheatColor;

    [Header("Audio")]
    [SerializeField] private GameObject soundPrefab;

    private float currentReloadDelay;
    private float currentOverheatTime;
    private float currentOverheatDelay;
    private float currentAmmo;
    private bool canShoot;
    private bool isShooting;
    private bool shouldShoot;
    private bool overheated;
    private float ammoBarHeight;

    private void Awake()
    {
        currentAmmo = maxAmmo;
        currentReloadDelay = 0f;
        currentOverheatTime = 0f;
        currentOverheatDelay = 0f;
        canShoot = true;
        isShooting = false;
        shouldShoot = false;
        overheated = false;
        if (enableCannonUI) ammoBarHeight = ammoBarUI.rectTransform.rect.height;
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
        if (currentAmmo < 1) return;
        if (!canShoot) return;
        Debug.Log("shot");

        isShooting = true;
        currentReloadDelay = 0f; // reset the reload timer if shooting

        RaycastHit hit;
        if (Physics.Raycast(gunBarrel.position, gunBarrel.forward, out hit, range, objectMask))
        {
            Debug.Log(hit.collider.gameObject.name);
            ShipModule m = hit.collider.gameObject.GetComponent<ShipModule>();
            if (m != null) { // the hit object is a spaceship
                if (m.GetComponentInParent<ShipCombat>())
                    m.GetComponentInParent<ShipCombat>().Damage(m, damage);
                else if (m.GetComponentInParent<EnemyController>())
                    m.GetComponentInParent<EnemyController>().Damage(m, damage);
            }
        }

        currentAmmo--;
        canShoot = false;
        Invoke(nameof(ResetShooting), 1f / fireRate);

        Instantiate(muzzleFlash, transform.position, transform.rotation);
        Instantiate(soundPrefab, transform.position, transform.rotation);

        UpdateUI();
    }

    private void Overheat()
    {
        if (isShooting) currentOverheatTime += Time.deltaTime;
        else if (!isShooting && !overheated && currentOverheatTime > 0) currentOverheatTime -= Time.deltaTime * coolingSpeed;

        // cannon is overheated
        if (currentOverheatTime > overheatTime)
        {
            overheated = true;
            UpdateUI();
        }
        if (overheated) currentOverheatDelay += Time.deltaTime;

        // no longer overheated
        if (currentOverheatDelay >= overheatDelay)
        {
            overheated = false;
            currentOverheatDelay = 0f;
            currentOverheatTime = overheatTime;
            UpdateUI();
        }
    }

    private void Reload()
    {
        if (overheated) return; // can't reload while overheated

        if (!isShooting && currentReloadDelay < reloadDelay) currentReloadDelay += Time.deltaTime;
        else if (isShooting) return;

        // can reload
        if (currentReloadDelay >= reloadDelay)
        {
            currentAmmo = Mathf.Lerp(currentAmmo, maxAmmo, Time.deltaTime / reloadSpeed);
            UpdateUI();
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

    public float GetCurrentAmmo() { return currentAmmo; }

    private void UpdateUI()
    {
        if (!enableCannonUI) return;

        if (overheated)
        {
            ammoBarUI.rectTransform.sizeDelta = new Vector2(ammoBarUI.rectTransform.rect.width, ammoBarHeight);
            ammoBarUI.color = overheatColor;
        }
        else
        {
            ammoBarUI.rectTransform.sizeDelta = new Vector2(ammoBarUI.rectTransform.rect.width,
                ammoBarHeight / maxAmmo * currentAmmo);
            ammoBarUI.color = normalColor;
        }

        ammoText.UpdateAmmoText();
    }
}
