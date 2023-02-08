using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utilities
{
    public static RaycastHit[] ConeCastAll(Vector3 origin, float maxRadius, Vector3 direction,
        float maxDistance, float coneAngle, LayerMask mask, QueryTriggerInteraction qti)
    {
        RaycastHit[] sphereCastHits = Physics.SphereCastAll(origin - new Vector3(0, 0, maxRadius),
            maxRadius, direction, maxDistance, mask, qti);
        List<RaycastHit> coneCastHitList = new List<RaycastHit>();

        if (sphereCastHits.Length > 0)
        {
            for (int i = 0; i < sphereCastHits.Length; i++)
            {
                Vector3 hitPoint = sphereCastHits[i].point;
                Vector3 directionToHit = hitPoint - origin;
                float angleToHit = Vector3.Angle(direction, directionToHit);

                if (angleToHit < coneAngle)
                {
                    coneCastHitList.Add(sphereCastHits[i]);
                }
            }
        }

        RaycastHit[] coneCastHits = new RaycastHit[coneCastHitList.Count];
        coneCastHits = coneCastHitList.ToArray();

        return coneCastHits;
    }

    public static float CalculateGForce(Rigidbody rb, Vector3 lastVelocity)
    {
        Vector3 currentVelocity = rb.velocity;
        Vector3 acceleration = (currentVelocity - lastVelocity) / Time.fixedDeltaTime;

        float Gforce = acceleration.normalized.magnitude / Physics.gravity.magnitude;
        return Gforce;
    }
}
