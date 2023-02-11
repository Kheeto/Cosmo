using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RadarInfoUI : MonoBehaviour
{
    [Header("Radar Lock")]
    [SerializeField] private Color normalColor;
    [SerializeField] private Color lockColor;

    [Header("References")]
    [SerializeField] private RadarPing radarPing;
    [SerializeField] private TMP_Text radarText;
    [SerializeField] private RawImage radarIcon;

    private bool isLockedOn = false;

    private void Update()
    {
        if (!radarPing || !radarPing.GetOwner())
            Destroy(gameObject);

        UpdatePosition();
        UpdateUI();
        // TODO add aim prediction
    }

    private void UpdatePosition()
    {
        if (!radarPing || !radarPing.GetOwner())
            return;

        Vector3 targPos = radarPing.GetOwner().transform.position;
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camPos = Camera.main.transform.position + camForward;

        float frontDistance = Vector3.Dot(targPos - camPos, camForward);
        if (frontDistance < 0f)
        {
            targPos -= camForward * frontDistance;
        }

        transform.position = RectTransformUtility.WorldToScreenPoint(Camera.main, targPos);
    }

    private void UpdateUI()
    {
        if (!radarPing || !radarPing.GetOwner())
            return;

        float kmDistance = Vector3.Distance(Camera.main.transform.position,
            radarPing.GetOwner().transform.position) / 1000f;

        radarText.text = radarPing.GetOwner().transform.parent.name + "\n" + kmDistance.ToString("0.00") + "km";
    }

    public void UpdateLockState(bool state)
    {
        if (state)
        {
            isLockedOn = true;
            radarIcon.color = lockColor;
            radarText.color = lockColor;
        }
        else
        {
            isLockedOn = false;
            radarIcon.color = normalColor;
            radarText.color = normalColor;
        }
    }

    public RadarPing GetPing() { return radarPing; }

    public void SetPing(RadarPing rp) { radarPing = rp; }
}
