using UnityEngine;

public class PlayerController : MonoBehaviour {

    [Header("Movement")]
    [SerializeField] private float movementSpeed = 5f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 2f;
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private LayerMask whatIsGround;

    [Header("References")]
    [SerializeField] private Transform orientation;

    private Rigidbody rb;
    private bool grounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        // Check if the player is grounded by shooting a raycast down
        grounded = Physics.Raycast(transform.position, -transform.up, playerHeight * 0.5f + 0.2f, whatIsGround);

        HandleMovement();
        HandleJump();
        RotateToGravity();
    }

    private void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        rb.AddForce(moveDirection.normalized * movementSpeed, ForceMode.Force);
    }

    private void HandleJump()
    {
        if (Input.GetKey(KeyCode.Space) && grounded)
        {
            rb.AddRelativeForce(Vector3.up * jumpForce);
        }
    }

    /// <summary>
    /// Rotates the Player relatively to the strongest gravitational pull, so he always appears standing perpendicularly to the planet's surface.
    /// </summary>
    private void RotateToGravity()
    {
        Vector3 strongestGravitationalPull = Vector3.zero;

        // Loop through all gravitational objects and find the strongest pull
        foreach (GravitationalObject obj in GravitationalObject.Objects)
        {
            float distanceSqr = (obj.transform.position - rb.position).sqrMagnitude;
            Vector3 forceDir = (obj.transform.position - rb.position).normalized;
            Vector3 acceleration = forceDir * GravitationalObject.G * obj.mass / distanceSqr;
            
            if (acceleration.sqrMagnitude > strongestGravitationalPull.sqrMagnitude) {
                strongestGravitationalPull = acceleration;
            }
        }

        Vector3 gravityUp = -strongestGravitationalPull.normalized;
        rb.rotation = Quaternion.FromToRotation(transform.up, gravityUp) * rb.rotation;
    }
}
