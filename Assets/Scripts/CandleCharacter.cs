using UnityEngine;

public class CandleCharacter : MonoBehaviour
{
    [Header("Components")]
    private Rigidbody2D rb;
    [SerializeField] private Animator animator; // Reference to child's animator

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private Transform groundCheck;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpTimeMax = 0.3f; // Maximum time player can hold jump
    [SerializeField] private float jumpMultiplier = 0.5f; // Multiplier when releasing jump early
    
    [Header("Level Management")]
    [SerializeField] private LevelManager levelManager;
    
    private float moveInput;
    private bool facingRight = true;
    private bool isGrounded;
    private float jumpTimeCounter; // Tracks how long jump is held
    private bool isJumping; // Tracks if we're in a jump

    private void Awake()
    {
        // Get the Rigidbody2D component
        rb = GetComponent<Rigidbody2D>();
        
        // Verify we have the component
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component missing!");
        }
        
        // Get animator from child if not assigned
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // Create ground check if not assigned
        if (groundCheck == null)
        {
            // Create a child object for ground detection
            GameObject checkObject = new GameObject("GroundCheck");
            groundCheck = checkObject.transform;
            groundCheck.parent = transform;
            groundCheck.localPosition = new Vector3(0, -1f, 0); // Adjust position as needed
        }
    }

    private void Update()
    {
        // Check if grounded
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
        // Update animator with grounded state
        animator.SetBool("isJumping", !isGrounded);
        
        // Get input
        moveInput = Input.GetAxisRaw("Horizontal"); // Will be -1, 0, or 1
        

        // Set animator parameter
        animator.SetBool("isWalking", moveInput != 0);
        
        // Flip character based on movement direction
        if (moveInput > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && facingRight)
        {
            Flip();
        }

        // Jump Start
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            isJumping = true;
            jumpTimeCounter = jumpTimeMax;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            animator.SetBool("isJumping", true);
            Debug.Log("Jump!");
        }

        // Jump Hold
        if (Input.GetKey(KeyCode.Space) && isJumping)
        {
            if (jumpTimeCounter > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }

        // Jump Release
        if (Input.GetKeyUp(KeyCode.Space))
        {
            isJumping = false;
            if (rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpMultiplier);
            }
        }
    }

    private void FixedUpdate()
    {
        // Update to use linearVelocity instead of velocity
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // Helper to visualize ground check in editor
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Star"))
        {
            Destroy(other.gameObject);
            Debug.Log("Star collected!");
            
            // Notify level manager to check if level is complete
            if (levelManager != null)
            {
                levelManager.CheckLevelComplete();
            }
        }
    }
} 