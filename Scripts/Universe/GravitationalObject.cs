using System.Collections.Generic;
using UnityEngine;

public class GravitationalObject : MonoBehaviour {

	public static float G = 667.4f;
	public static List<GravitationalObject> Objects;

	[Header("Gravitational Object")]
	[SerializeField] private Rigidbody rb;

	[Header("Orbital Motion")]
	[SerializeField] private List<GravitationalObject> centralBodies = new List<GravitationalObject>();
	[SerializeField] private Vector3 initialVelocity;

	[Header("Rotation")]
	[SerializeField] private Vector3 rotationAxis = Vector3.up;
	[SerializeField] private float rotationSpeed;

	public float mass
	{
		get { return rb.mass; }
		private set { rb.mass = value; }
	}

	private void OnEnable()
	{
		if (Objects == null)
			Objects = new List<GravitationalObject>();

		Objects.Add(this);
	}

	private void OnDisable()
	{
		Objects.Remove(this);
	}

    private void Start()
    {
		rb.ResetCenterOfMass();
		rb.AddForce(initialVelocity, ForceMode.VelocityChange);

		if (centralBodies.Count > 0)
			foreach (GravitationalObject centralBody in centralBodies)
			{
				transform.LookAt(centralBody.transform);
				rb.velocity += transform.right * CalculateCircularOrbitVelocity(centralBody.rb);
			}
    }

    private void FixedUpdate()
	{
		foreach (GravitationalObject g in Objects)
		{
			if (g != this)
				g.rb.AddForce(CalculateForce(g.rb));
		}
		RotateBody();
	}

	/// <summary>
	/// Returns the velocity needed for this body to orbit around a central body in a circular motion.
	/// </summary>
	/// <param name="orbitAround">The central body this body is orbiting around</param>
	/// <returns>The orbit velocity.</returns>
	private float CalculateCircularOrbitVelocity(Rigidbody centralBody)
	{
		// Circular orbit instant velocity
		// v = sqrt (( G * m2 ) / R )
		// Where G = gravitational constant, m2 = mass of central body, R = distance
		float R = Vector3.Distance(rb.position, centralBody.position);
		if (R == 0f) return 0; // cannot orbit around an object in the same position

		float vSqr = (G * centralBody.mass) / R;
		float velocity = Mathf.Sqrt(vSqr);

		return velocity;
	}

	/// <summary>
	/// Returns the gravitational pull that this body will exert on the target
	/// </summary>
	public Vector3 CalculateForce(Rigidbody target)
    {
		Vector3 distance = rb.position - target.position;
		// Doesn't attract an object in the same position
		if (distance.magnitude == 0f)
			return Vector3.zero;

		float distanceSqr = Mathf.Pow(distance.magnitude, 2f);
		float magnitude = G * (rb.mass * target.mass) / distanceSqr;
		Vector3 force = distance.normalized * magnitude;

		return force;
	}

	private void RotateBody()
    {
		rb.AddRelativeTorque(rotationAxis * rotationSpeed, ForceMode.VelocityChange);
    }

	private void OnDrawGizmosSelected()
	{
		if (!Application.isPlaying) return;

		foreach (GravitationalObject body in Objects)
		{
			Gizmos.color = Color.white;
			Gizmos.DrawLine(transform.position, body.transform.position);
		}
	}
}