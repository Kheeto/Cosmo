using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileWarning : MonoBehaviour
{
    [Header("Flashing effect")]
    [SerializeField] private float showDuration = .5f;
    [SerializeField] private float hideDuration = .1f;
    private float showTimer = 0f;
    private float hideTimer = 0f;

    [Header("References")]
    [SerializeField] private GameObject missileWarningObject;
    [SerializeField] private Rigidbody owner;

    private List<Missile> incomingMissiles;
    private bool warningActive;

    private void Start()
    {
        incomingMissiles = new List<Missile>();
        warningActive = false;
        showTimer = 0f;
        hideTimer = 0f;
    }

    private void Update()
    {
        foreach (Missile m in incomingMissiles)
        {
            if (m.GetTarget() != owner) incomingMissiles.Remove(m);
            if (m == null) incomingMissiles.Remove(m);
        }

        if (incomingMissiles.Count > 0) warningActive = true;
        else warningActive = false;

        if (warningActive)
        {
            // Flashing effect
            if (!missileWarningObject.activeSelf)
            {
                hideTimer -= Time.deltaTime;

                if (hideTimer <= 0f)
                {
                    missileWarningObject.SetActive(true);
                    showTimer = showDuration;
                }
            }
            else if (missileWarningObject.activeSelf)
            {
                showTimer -= Time.deltaTime;

                if (showTimer <= 0f)
                {
                    missileWarningObject.SetActive(false);
                    hideTimer = hideDuration;
                }
            }
        }
        else missileWarningObject.SetActive(false);
    }

    public void AddMissile(Missile m)
    {
        incomingMissiles.Add(m);
    }
}
