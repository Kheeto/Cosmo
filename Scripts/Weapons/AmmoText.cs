using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AmmoText : MonoBehaviour
{
    [SerializeField] private List<Missile> missiles = new List<Missile>();
    [SerializeField] private List<Cannon> cannons = new List<Cannon>();

    [Header("References")]
    [SerializeField] private TMP_Text ammoText;

    private void Start()
    {
        UpdateAmmoText();
    }

    public void UpdateAmmoText()
    {
        int cannonAmmo = 0;
        foreach (Cannon c in cannons)
        {
            cannonAmmo += c.GetCurrentAmmo();
        }
        int missileCount = 0;
        foreach (Missile m in missiles)
        {
            if (!m.wasLaunched) missileCount++;
        }

        ammoText.text = "CNN " + cannonAmmo + "\nMSL " + missileCount;
    }
}
