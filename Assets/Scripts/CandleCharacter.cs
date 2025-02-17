using UnityEngine;

public class CandleCharacter : MonoBehaviour
{
    [Header("Components")]
    private Rigidbody2D rb;
    [SerializeField] private Animator animator; // Reference to child's animator

    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform sideCheck;
    [SerializeField] private float sideCheckRadius = 0.1f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpTimeMax; // Maximum time player can hold jump
    [SerializeField] private float jumpMultiplier; // Multiplier when releasing jump early
    
    [Header("Level Management")]
    [SerializeField] private LevelManager levelManager;
    
    [Header("Scale Settings")]
    [SerializeField] private float scaleSpeed = 0.1f;  // How fast to scale
    [SerializeField] private float minYScale = 0.5f;   // Minimum scale (percentage of original)
    private bool isScaling = false;
    private float originalYScale;
    private Vector3 originalBodyPosition;  // Store full position vector
    private Transform bodyTransform;

    [Header("Face Settings")]
    [SerializeField] private GameObject[] mouths;  // Array of 5 mouth objects
    private float scaleOffset;
    private float rangeSize;

    [Header("Visual Effects")]
    [SerializeField] private GameObject flame;  // Reference to flame object

    private float moveInput;
    private bool facingRight = true;
    private bool isGrounded;
    private float jumpTimeCounter; // Tracks how long jump is held
    private bool isJumping; // Tracks if we're in a jump
    private bool isSideCollision;

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

        // Get body transform and store original values
        bodyTransform = transform.GetChild(0);
        originalYScale = bodyTransform.localScale.y;
        originalBodyPosition = bodyTransform.localPosition;

        // Calculate scale ranges
        scaleOffset = 1.0f - minYScale;  // e.g., 1.0 - 0.5 = 0.5
        rangeSize = scaleOffset / 5f;    // e.g., 0.5 / 5 = 0.1

        // Start with first mouth
        SetMouthState(0);

        // Disable flame at start
        if (flame != null)
        {
            flame.SetActive(false);
        }
    }

    private void Update()
    {
        // Check if grounded and side collisions
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        isSideCollision = Physics2D.OverlapCircle(sideCheck.position, sideCheckRadius, groundLayer);
        
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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded || isSideCollision)
            {
                isJumping = true;
                jumpTimeCounter = jumpTimeMax;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                animator.SetBool("isJumping", true);
            }
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

        // Prevent movement when colliding with walls
        if (isSideCollision)
        {
            Debug.Log("Colliding with wall");
            rb.linearVelocity = new Vector2(0, 0);
        }

        // Toggle scaling with F key
        if (Input.GetKeyDown(KeyCode.F))
        {
            isScaling = !isScaling;
            
            // Toggle flame with scaling
            if (flame != null)
            {
                flame.SetActive(isScaling);
            }
        }

        // Handle scaling
        if (isScaling)
        {
            Vector3 newScale = bodyTransform.localScale;
            newScale.y -= scaleSpeed * Time.deltaTime;
            
            if (newScale.y <= originalYScale * minYScale)
            {
                Die();
                return;
            }
            
            bodyTransform.localScale = newScale;
            bodyTransform.localPosition = new Vector3(
                bodyTransform.localPosition.x, 
                bodyTransform.localPosition.y - scaleSpeed * Time.deltaTime * 2.5f
            );

            // Update mouth based on current scale
            float currentRatio = newScale.y / originalYScale;
            int mouthIndex;
            
            if (currentRatio > 1.0f - rangeSize) mouthIndex = 0;
            else if (currentRatio > 1.0f - rangeSize * 2) mouthIndex = 1;
            else if (currentRatio > 1.0f - rangeSize * 3) mouthIndex = 2;
            else if (currentRatio > 1.0f - rangeSize * 4) mouthIndex = 3;
            else mouthIndex = 4;

            SetMouthState(mouthIndex);
        }

        // Restart game with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (levelManager != null)
            {
                levelManager.RestartGame();
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
        
        if (sideCheck != null)
        {
            Gizmos.color = isSideCollision ? Color.green : Color.red;
            Gizmos.DrawWireSphere(sideCheck.position, sideCheckRadius);
        }
    
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Star"))
        {
            other.gameObject.SetActive(false);
            Debug.Log("Star collected!");
            
            // Notify level manager to check if level is complete
            if (levelManager != null)
            {
                levelManager.CheckLevelComplete();
            }
        }
    }

    private void SetMouthState(int index)
    {
        Debug.Log("Setting mouth state to " + index);
        // Deactivate all mouths first
        for (int i = 0; i < mouths.Length; i++)
        {
            mouths[i].SetActive(i == index);
        }
    }

    public void Die()
    {
        // Reset scaling state
        isScaling = false;
        
        // Disable flame
        if (flame != null)
        {
            flame.SetActive(false);
        }
        
        // Reset body scale and position
        Vector3 resetScale = bodyTransform.localScale;
        resetScale.y = originalYScale;
        bodyTransform.localScale = resetScale;
        bodyTransform.localPosition = originalBodyPosition;
        
        // Call level manager to respawn player and collectables
        if (levelManager != null)
        {
            levelManager.RespawnPlayerAndCollectables();
        }
        
        Debug.Log("Player died from melting!");
        SetMouthState(0);  // Reset to happy face
    }
} 