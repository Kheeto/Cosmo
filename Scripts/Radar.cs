using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Radar : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool enableRadarUI = false;
    [SerializeField] private RadarMode mode = RadarMode.Continuous;
    [SerializeField] private LayerMask objectMask;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private float radarRotateSpeed = 10f; // rpm
    [SerializeField] private float radarRange = 2500f;
    [Range(0f, 90f)]
    [SerializeField] private float radarAngle = 15f; // vertical angle
    [SerializeField] private float castRadius = 10f;
    [SerializeField] private float backgroundWidth;

    [Header("Radar Lock")]
    [SerializeField] private float lockOnRadius;
    [SerializeField] private Rigidbody lockedOn;

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform radarOrientation;
    [SerializeField] private GameObject radarUI;
    [SerializeField] private GameObject radarSweep;
    [SerializeField] private GameObject pingPrefab;
    [SerializeField] private List<Collider> objectList;
    [SerializeField] private List<RadarPing> pingList;

    [Header("Keybinding")]
    [SerializeField] private KeyCode radarLockKey = KeyCode.R;

    private float previousRotation;
    private float currentRotation;
    private enum RadarMode
    {
        Continuous,
        Advanced
    }

    private void Awake()
    {
        objectList = new List<Collider>();
        pingList = new List<RadarPing>();
    }

    private void Update()
    {
        RotateRadar();
        HandleRadar();
        HandleTargetLock(); // TODO finish this
    }

    private void RotateRadar()
    {
        previousRotation = radarOrientation.eulerAngles.y / 360f;

        radarOrientation.Rotate(0f, 6 * radarRotateSpeed * Time.deltaTime, 0f);
        if (enableRadarUI) radarSweep.transform.Rotate(Vector3.forward * 6 * radarRotateSpeed * Time.deltaTime);

        currentRotation = radarOrientation.eulerAngles.y / 360f;

        // completed a rotation, clears radar
        if (mode == RadarMode.Continuous && previousRotation <= 1
            && previousRotation > currentRotation && currentRotation > 0)
        {
            objectList.Clear();
        }

        // rotates radar UI based on ship rotation
        if (enableRadarUI) radarUI.transform.eulerAngles = new Vector3(0f, 0f, transform.eulerAngles.y);
    }

    private void HandleRadar()
    {
        RaycastHit[] hits = Utilities.ConeCastAll(radarOrientation.position, castRadius,
            radarOrientation.forward, radarRange, radarAngle, targetMask, QueryTriggerInteraction.Collide);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.GetComponentInParent<Rigidbody>() == rb) continue;

            if (!objectList.Contains(hit.collider))
            {
                // hit object for the first time
                objectList.Add(hit.collider);

                if (!enableRadarUI) continue;

                // calculates ping position
                Vector3 hitPointFromRadar = -(radarOrientation.position - hit.point);
                Vector3 positionOnRadar = new Vector3(hitPointFromRadar.x / radarRange * backgroundWidth,
                    hitPointFromRadar.z / radarRange * backgroundWidth, 0f);

                GameObject ping = Instantiate(pingPrefab, positionOnRadar,
                    Quaternion.Euler(0f,0f,0f));
                ping.transform.SetParent(radarUI.transform, false);
                ping.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                RadarPing rp = ping.GetComponent<RadarPing>();
                pingList.Add(rp);

                if (mode == RadarMode.Continuous)
                {
                    float duration = 360f / (6 * radarRotateSpeed);

                    rp.EnableTimer(true);
                    rp.SetTimer(duration);
                    StartCoroutine(RemovePing(rp, duration));
                }

                rp.SetOwner(hit.collider);
            }
            else
            {
                if (!enableRadarUI) continue;

                // only works on advanced mode, continuous mode doesn't need to update pings since it creates new ones each time
                if (mode != RadarMode.Advanced) continue;

                // object already seen before, updates its position
                // checks each ping and finds the one owned by the collider that the raycast has hit
                foreach (RadarPing p in pingList)
                {
                    if (p.GetOwner() == hit.collider)
                    {
                        Vector3 hitPointFromRadar = -(radarOrientation.position - hit.point);
                        Vector3 positionOnRadar = new Vector3(hitPointFromRadar.x / radarRange * backgroundWidth,
                            hitPointFromRadar.z / radarRange * backgroundWidth, 0f);

                        p.transform.position = radarUI.transform.position + positionOnRadar;
                    }
                }
            }
        }

        // only works on advanced mode, continuous mode doesn't need to update pings since it creates new ones each time
        if (mode != RadarMode.Advanced) return;

        // Checks if an object on the radar is too far away or doesn't exist anymore, if so deletes the ping
        for (int i = 0; i < pingList.Count; i++)
        {
            RadarPing p = pingList[i];
            if (p == null) continue;

            Collider c = p.GetOwner();
            if (Vector3.Distance(radarOrientation.position, c.transform.position) > radarRange
                || c == null)
            {
                pingList.Remove(p);
                Destroy(p.gameObject);
                if (c != null) objectList.Remove(c);
            }
        }
    }

    private void HandleTargetLock()
    {
        if (Input.GetKeyDown(radarLockKey))
        {
            RaycastHit hit;
            if (Physics.SphereCast(transform.position, lockOnRadius, transform.forward, out hit, radarRange, targetMask))
            {
                Rigidbody hrb = hit.collider.GetComponent<Rigidbody>();
                if (hrb != null) lockedOn = hrb;
            }
        }
    }

    private IEnumerator RemovePing(RadarPing ping, float duration)
    {
        yield return new WaitForSeconds(duration);
        pingList.Remove(ping);
    }

    public List<RadarPing> GetRadarPings()
    {
        return pingList;
    }

    public Rigidbody GetTarget() { return lockedOn; }
}
