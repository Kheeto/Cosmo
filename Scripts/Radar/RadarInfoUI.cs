using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadarInfoUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject radarObject;

    private void Update()
    {
        Vector3 targPos = radarObject.transform.position;
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camPos = Camera.main.transform.position + camForward;

        float frontDistance = Vector3.Dot(targPos - camPos, camForward);
        if (frontDistance < 0f)
        {
            targPos -= camForward * frontDistance;
        }

        transform.position = RectTransformUtility.WorldToScreenPoint(Camera.main, targPos);
    }

    public GameObject GetObject() { return radarObject; }

    public void SetObject(GameObject obj) { radarObject = obj; }
}
