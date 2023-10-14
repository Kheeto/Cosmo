using UnityEngine;

public class RadarPing : MonoBehaviour {

    [Header("References")]
    [SerializeField] private Radar radar;
    [SerializeField] private Collider owner;
    [SerializeField] private bool enableTimer;
    [SerializeField] private float disappearTimer;
    private float disappearTime;

    private void Update()
    {
        if (!enableTimer) return;

        disappearTime += Time.deltaTime;
        if(disappearTime >= disappearTimer)
        {
            radar.UpdateRadarInfoUI();
            Destroy(gameObject);
        }
    }

    public void SetRadar(Radar r) { radar = r; }
    public void SetOwner(Collider owner) { this.owner = owner; }
    public Collider GetOwner() { return owner; }
    public void SetTimer(float timer) {
        disappearTimer = timer;
        ResetTimer();
    }
    private void ResetTimer() { disappearTime = 0; }
    public void EnableTimer(bool enable) { enableTimer = enable; }
}
