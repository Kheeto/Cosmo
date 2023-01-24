using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
	[SerializeField] private List<Transform> Povs = new List<Transform>();
	private int index = 0;

	[Header("Settings")]
	[SerializeField] private float followSpeed = 10;
	[SerializeField] private float lookSpeed = 10;
	[SerializeField] private KeyCode cameraKey = KeyCode.V;

	[Header("References")]
	[SerializeField] private Transform objectToLookAt;

	private void Update()
	{
		if (Input.GetKeyDown(cameraKey))
		{
			if (index < Povs.Count - 1) index++;
			else if (index == Povs.Count - 1) index = 0;
		}
	}

	private void FixedUpdate()
	{
		LookAtTarget();
		MoveToTarget();
	}

	private void LookAtTarget()
	{
		Vector3 _lookDirection = objectToLookAt.position - transform.position;
		Quaternion _rot = Quaternion.LookRotation(_lookDirection, Vector3.up);
		transform.rotation = Quaternion.Lerp(transform.rotation, _rot, lookSpeed * Time.deltaTime);
	}

	private void MoveToTarget()
	{
		Vector3 _targetPos = Povs[index].position;
		transform.position = Vector3.Lerp(transform.position, _targetPos, followSpeed * Time.deltaTime);
	}
}
