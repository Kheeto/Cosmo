using UnityEngine;
using UnityEngine.UI;

public enum ModuleStatus
{
    Normal,
    Unstable,
    Critical,
    Failure
}

public class ShipModule : MonoBehaviour
{
    [Header("Module Settings")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float currentHealth;
    [SerializeField] private ModuleStatus status;
    [SerializeField] private short repairsLeft = -1;

    [Header("Module Damage")]
    [SerializeField] private float unstableHealth;
    [SerializeField] private float criticalHealth;
    [SerializeField] private float failureHealth;

    [Header("UI")]
    [SerializeField] private Image moduleUI;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color unstableColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField] private Color failureColor = Color.gray;

    private void Awake()
    {
        Repair();
    }

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

    public void Heal(short healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        status = ModuleStatus.Normal;
        if (currentHealth <= unstableHealth) status = ModuleStatus.Unstable;
        if (currentHealth <= criticalHealth) status = ModuleStatus.Critical;
        if (currentHealth <= failureHealth) status = ModuleStatus.Failure;

        UpdateUI();
    }

    public void Repair()
    {
        if (repairsLeft == 0) return;

        currentHealth = maxHealth;
        status = ModuleStatus.Normal;
        UpdateUI();

        repairsLeft--;
    }

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
