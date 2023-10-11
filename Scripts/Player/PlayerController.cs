using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float MovementSpeed = 5f;
    [SerializeField] private Transform Orientation;

    [Header("Jump")]
    [SerializeField] private float JumpForce = 2f;
    [SerializeField] private float PlayerHeight = 2f;
    [SerializeField] private LayerMask GroundMask;
    public bool grounded;

    private Rigidbody rb;
    private Vector3 moveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        // Ground check
        grounded = Physics.Raycast(transform.position, -transform.up, PlayerHeight * 0.5f + 0.2f, GroundMask);

        Movement();
        Jump();
    }

    private void Movement()
    {
        // Get the movement input
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Move the player
        moveDirection = Orientation.forward * verticalInput + Orientation.right * horizontalInput;
        rb.AddForce(moveDirection.normalized * MovementSpeed, ForceMode.Force);
    }

    private void Jump()
    {
        if (Input.GetKey(KeyCode.Space) && grounded)
        {
            rb.AddRelativeForce(Vector3.up * JumpForce);
        }
    }
}
