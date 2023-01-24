using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravitationalObject : MonoBehaviour
{
	const float G = 667.4f;
	
	public static List<GravitationalObject> Objects;

	[Header("Gravitational Object")]
	public Rigidbody rb;
	public float mass
    {
		get { return rb.mass; }
		private set { rb.mass = value; }
    }

	[Header("Orbital Motion")]
	[SerializeField] private List<GravitationalObject> centralBodies = new List<GravitationalObject>();
	[SerializeField] private Vector3 initialVelocity;

	[Header("Rotation")]
	[SerializeField] private Vector3 rotationAxis = Vector3.up;
	[SerializeField] private float rotationSpeed;

	Vector3 originalPosition;
	Vector3 newPosition;
	Vector3 velocity;
	Vector3 acceleration;

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
				rb.velocity += transform.right * CalculateOrbitVelocityCircular(centralBody.rb);
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

	public Vector3 CalculateForce(Rigidbody target)
    {
		Vector3 distance = rb.position - target.position;
		// doesn't attract an object in the same position
		if (distance.magnitude == 0f)
			return Vector3.zero;

		float distanceSqr = Mathf.Pow(distance.magnitude, 2f);
		float magnitude = G * (rb.mass * target.mass) / distanceSqr;
		Vector3 force = distance.normalized * magnitude;

		return force;
	}

	/// <summary>
	/// Returns the velocity needed for this rigidbody to orbit around another attractor in a circular orbit.
	/// </summary>
	/// <param name="orbitAround">The central body this body is orbiting around</param>
	/// <returns>The orbit velocity.</returns>
	private float CalculateOrbitVelocityCircular(Rigidbody orbitAround)
    {
		// Circular orbit instant velocity
		// v = sqrt (( G * m2 ) / R )
		// Where G = gravitational constant, m2 = mass of central body, R = distance
		float R = Vector3.Distance(rb.position, orbitAround.position);
		if (R == 0f) return 0; // cannot orbit around an object in the same position
		
		float vSqr = (G * orbitAround.mass) / R;
		float velocity = Mathf.Sqrt(vSqr);

		return velocity;
	}

	private void RotateBody()
    {
		rb.AddRelativeTorque(rotationAxis * rotationSpeed, ForceMode.Force);
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
