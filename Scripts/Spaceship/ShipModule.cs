using UnityEngine;
using UnityEngine.UI;

public enum ModuleStatus
{
    Normal,
    Unstable,
    Critical,
    Failure
}

public class ShipModule : MonoBehaviour {

    [Header("Health")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float currentHealth;
    [SerializeField] private ModuleStatus status;
    [SerializeField] private short repairsLeft = -1;

    [Header("Damage levels")]
    [SerializeField] private float unstableHealth;
    [SerializeField] private float criticalHealth;
    [SerializeField] private float failureHealth;

    [Header("UI")]
    [SerializeField] private Image moduleUI;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color unstableColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField] private Color failureColor = Color.gray;

    /// <summary>
    /// Fully restores the module's health and status
    /// </summary>
    public void Repair()
    {
        if (repairsLeft == 0) return;

        currentHealth = maxHealth;
        status = ModuleStatus.Normal;
        UpdateUI();

        repairsLeft--;
    }

    /// <summary>
    /// Damages this module and updates its status
    /// </summary>
    public void Damage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        status = ModuleStatus.Normal;
        if (currentHealth <= unstableHealth) status = ModuleStatus.Unstable;
        if (currentHealth <= criticalHealth) status = ModuleStatus.Critical;
        if (currentHealth <= failureHealth) status = ModuleStatus.Failure;

        UpdateUI();
    }

    /// <summary>
    /// Heals this module and updates its status
    /// </summary>
    public void Heal(float healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        status = ModuleStatus.Normal;
        if (currentHealth <= unstableHealth) status = ModuleStatus.Unstable;
        if (currentHealth <= criticalHealth) status = ModuleStatus.Critical;
        if (currentHealth <= failureHealth) status = ModuleStatus.Failure;

        UpdateUI();
    }

    /// <summary>
    /// Updates the module's color in the UI based on its current status
    /// </summary>
    private void UpdateUI()
    {
        switch(status)
        {
            case ModuleStatus.Normal:
                moduleUI.color = normalColor;
                break;
            case ModuleStatus.Unstable:
                moduleUI.color = unstableColor;
                break;
            case ModuleStatus.Critical:
                moduleUI.color = criticalColor;
                break;
            case ModuleStatus.Failure:
                moduleUI.color = failureColor;
                break;
        }
    }

    public float GetMaxHealth() { return maxHealth; }

    public float GetHealth() { return currentHealth; }

    public ModuleStatus GetStatus() { return status; }
}
