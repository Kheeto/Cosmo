using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Radar : MonoBehaviour {

    [Header("Settings")]
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

    [Header("UI")]
    [SerializeField] private bool enableRadarUI = false;
    [SerializeField] private GameObject pingPrefab;
    [SerializeField] private GameObject radarUI;
    [SerializeField] private GameObject radarSweep;
    [SerializeField] private Transform radarInfoHolder;
    [SerializeField] private RadarInfoUI radarInfoElement;

    [Space(15)]
    [SerializeField] private List<Collider> objectList;
    [SerializeField] private List<RadarPing> pingList;
    [SerializeField] private List<RadarInfoUI> infoList;

    [Header("Keybinding")]
    [SerializeField] private KeyCode radarLockKey = KeyCode.R;

    private float previousRotation;
    private float currentRotation;

    private void Awake()
    {
        objectList = new List<Collider>();
        pingList = new List<RadarPing>();
        infoList = new List<RadarInfoUI>();
    }

    private void Update()
    {
        RotateRadar();
        HandleRadar();
        HandleTargetLock();
    }

    /// <summary>
    /// Rotates the radar orientation
    /// </summary>
    private void RotateRadar()
    {
        previousRotation = radarOrientation.eulerAngles.y / 360f;

        radarOrientation.Rotate(0f, 6 * radarRotateSpeed * Time.deltaTime, 0f);
        if (enableRadarUI && radarSweep != null) radarSweep.transform.Rotate(Vector3.forward * 6 * radarRotateSpeed * Time.deltaTime);

        currentRotation = radarOrientation.eulerAngles.y / 360f;

        // completed a rotation, clears radar
        if (previousRotation <= 1 && previousRotation > currentRotation && currentRotation > 0)
        {
            objectList.Clear();
        }

        // rotates radar UI based on ship rotation
        if (enableRadarUI && radarUI != null) radarUI.transform.eulerAngles = new Vector3(0f, 0f, transform.eulerAngles.y);
    }

    /// <summary>
    /// Scans for objects by shooting a conecast towards the radar orientation
    /// </summary>
    private void HandleRadar()
    {
        RaycastHit[] hits = ConeCastAll(radarOrientation.position, castRadius,
            radarOrientation.forward, radarRange, radarAngle, targetMask, QueryTriggerInteraction.Collide);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.GetComponentInParent<Rigidbody>() == rb) continue;

            // When an object is hit for the first time, add it to the object list
            if (!objectList.Contains(hit.collider))
            {
                objectList.Add(hit.collider);

                if (!enableRadarUI) continue;

                // Calculates ping position in the UI
                Vector3 hitPointFromRadar = -(radarOrientation.position - hit.point);
                Vector3 positionOnRadar = new Vector3(hitPointFromRadar.x / radarRange * backgroundWidth,
                    hitPointFromRadar.z / radarRange * backgroundWidth, 0f);

                // Creates a ping in the appriopriate position
                GameObject ping = Instantiate(pingPrefab, positionOnRadar,
                    Quaternion.Euler(0f,0f,0f));
                ping.transform.SetParent(radarUI.transform, false);
                ping.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                RadarPing rp = ping.GetComponent<RadarPing>();
                pingList.Add(rp);

                float duration = 360f / (6 * radarRotateSpeed);

                rp.EnableTimer(true);
                rp.SetTimer(duration);
                StartCoroutine(RemovePing(rp, duration));

                rp.SetOwner(hit.collider);
                rp.SetRadar(this);

                UpdateRadarInfoUI();
            }
        }
    }

    /// <summary>
    /// Locks on the closest target when a key is pressed
    /// </summary>
    private void HandleTargetLock()
    {
        if (Input.GetKeyDown(radarLockKey))
        {
            Rigidbody closestTarget = null;
            float minDistance = Mathf.Infinity;

            foreach (RadarPing ping in pingList)
            {
                Rigidbody target = ping.GetOwner().GetComponentInParent<Rigidbody>();
                if (target == null) return;

                float distance = Vector3.Distance(transform.position, target.position);

                if (distance < minDistance)
                {
                    closestTarget = target;
                    minDistance = distance;
                }
            }

            if (closestTarget != null) lockedOn = closestTarget;
        }
    }

    /// <summary>
    /// Removes a ping from the list after waiting the provided delay
    /// </summary>
    private IEnumerator RemovePing(RadarPing ping, float duration)
    {
        yield return new WaitForSeconds(duration);
        pingList.Remove(ping);
    }

    /// <summary>
    /// Returns the list of pings this radar is tracking
    /// </summary>
    public List<RadarPing> GetRadarPings()
    {
        return pingList;
    }

    /// <summary>
    /// Returns the target the radar is currently locked on
    /// </summary>
    public Rigidbody GetTarget() { return lockedOn; }

    /// <summary>
    /// Creates and updates Radar UI objects while deleting the old ones.
    /// </summary>
    public void UpdateRadarInfoUI()
    {
        // If a radar ping doesn't have an UI object, create it
        foreach (RadarPing ping in pingList)
        {
            bool found = false;
            foreach (RadarInfoUI info in infoList)
                if (ping == info.GetPing()) found = true;

            if (!found) // If no UI object is linked to this radar ping
            {
                RadarInfoUI newInfoObject = Instantiate(radarInfoElement).gameObject.GetComponent<RadarInfoUI>();
                newInfoObject.transform.SetParent(radarInfoHolder);
                newInfoObject.SetPing(ping);

                infoList.Add(newInfoObject);
            }
        }

        List<RadarInfoUI> toRemove = new List<RadarInfoUI>();

        // Makes sure info ui objects of radar pings that don't exist anymore get destroyed
        // also updates them if they are locked on or not
        foreach (RadarInfoUI info in infoList)
        {
            if (!info)
            {
                toRemove.Add(info);
                continue;
            }

            bool found = false;

            foreach (RadarPing ping in pingList)
                if (ping == info.GetPing())
                {
                    found = true;
                    if (ping.GetOwner().GetComponentInParent<Rigidbody>() == lockedOn)
                        info.UpdateLockState(true);
                    else
                        info.UpdateLockState(false);
                }

            if (!found)
                toRemove.Add(info);
        }

        foreach (RadarInfoUI info in toRemove)
        {
            infoList.Remove(info);
            if (info.gameObject) Destroy(info.gameObject);
        }
    }

    /// <summary>
    /// Returns all the hits the cone intersects by using a SphereCast and cutting out the hits that exceed the maximum angle
    /// </summary>
    /// <param name="origin">The origin of the cone</param>
    /// <param name="maxRadius">The maximum distance between the origin and the hit</param>
    /// <param name="direction">The direction the cone will be casted</param>
    /// <param name="maxDistance">The maximum length of the sweep</param>
    /// <param name="coneAngle">The maximum angle between the origin direction and the hit direction</param>
    /// <param name="mask">The layer mask of the objects detectable by this radar</param>
    /// <param name="qti">The query trigger interaction</param>
    /// <returns></returns>
    public static RaycastHit[] ConeCastAll(
        Vector3 origin,
        float maxRadius,
        Vector3 direction,
        float maxDistance,
        float coneAngle,
        LayerMask mask,
        QueryTriggerInteraction qti)
    {
        // Find all hits with a Sphere Cast
        RaycastHit[] sphereCastHits = Physics.SphereCastAll(origin - new Vector3(0, 0, maxRadius), maxRadius, direction, maxDistance, mask, qti);
        List<RaycastHit> coneCastHitList = new List<RaycastHit>();

        if (sphereCastHits.Length > 0)
        {
            for (int i = 0; i < sphereCastHits.Length; i++)
            {
                // Calculate the angle
                Vector3 hitPoint = sphereCastHits[i].point;
                Vector3 directionToHit = hitPoint - origin;
                float angleToHit = Vector3.Angle(direction, directionToHit);

                // Only count the hits within the maximum angle
                if (angleToHit < coneAngle)
                {
                    coneCastHitList.Add(sphereCastHits[i]);
                }
            }
        }

        // Only return the hits that are within the cone
        RaycastHit[] coneCastHits = new RaycastHit[coneCastHitList.Count];
        coneCastHits = coneCastHitList.ToArray();

        return coneCastHits;
    }
}
