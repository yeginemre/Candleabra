using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Level Management")]
    [SerializeField] private Level[] levels;
    [SerializeField] private CandleCharacter player;
    [SerializeField] private float levelXOffset; // Add this line - 19.2 units = 1920 pixels at 100 PPU
    
    [Header("Camera Bounds")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraTransitionSpeed = 5f;
    private float leftBoundary;
    private float rightBoundary;

    private Level currentLevel;
    private int currentLevelIndex = 0;
    private bool isLevelComplete = false;
    private Vector3 targetCameraPosition;

    private bool isTransitioning = false;
    private Level previousLevel;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnYThreshold = -5f;
    private Vector3[] initialCollectablePositions;
    private GameObject[] collectables;

    private void Start()
    {
        // Position all levels at start
        for (int i = 0; i < levels.Length; i++)
        {
            levels[i].transform.position = new Vector3(i * levelXOffset, 0, 0);
        }
        
        // Load the first level
        LoadLevel(0);

        // Initialize camera boundaries
        UpdateCameraBoundaries();
        targetCameraPosition = mainCamera.transform.position;
    }

    private void UpdateCameraBoundaries()
    {
        if (mainCamera == null) return;
        
        float camHeight = mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;
        
        // Set boundaries based on current level position
        leftBoundary = currentLevel.transform.position.x - camWidth;
        rightBoundary = currentLevel.transform.position.x + camWidth;
    }

    private void LateUpdate()
    {
        if (player != null)
        {
            Vector3 playerPos = player.transform.position;
            
            // Check for fall death
            if (playerPos.y < respawnYThreshold)
            {
                Debug.Log("Player fell below threshold - respawning");
                player.Die();
                return;
            }

            if (!isLevelComplete)
            {
                // Only clamp position if level is not complete
                playerPos.x = Mathf.Clamp(playerPos.x, leftBoundary, rightBoundary);
                player.transform.position = playerPos;

                // Check if player is near right boundary
                if (playerPos.x >= rightBoundary - 1f)
                {
                    Debug.Log($"Near right boundary. Current level: {currentLevelIndex}");
                    CheckLevelComplete(); // Auto-complete for testing
                }
            }
            else if (!isTransitioning)
            {
                // Check if player has moved halfway to the next level
                float nextLevelTriggerPoint = currentLevel.transform.position.x + (levelXOffset / 2f);
                Debug.Log($"Player X: {playerPos.x}, Trigger point: {nextLevelTriggerPoint}");
                
                if (playerPos.x > nextLevelTriggerPoint)
                {
                    Debug.Log("Starting transition to next level");
                    isTransitioning = true;
                    previousLevel = currentLevel;
                    
                    // Set camera target to next level position
                    targetCameraPosition = new Vector3((currentLevelIndex + 1) * levelXOffset, mainCamera.transform.position.y, mainCamera.transform.position.z);

                    
                    LoadNextLevel();
                }
            }

            // Smooth camera movement
            if (mainCamera.transform.position != targetCameraPosition)
            {
                mainCamera.transform.position = Vector3.Lerp(
                    mainCamera.transform.position,
                    targetCameraPosition,
                    Time.deltaTime * cameraTransitionSpeed
                );

                // Check if camera has nearly reached its target
                if (Vector3.Distance(mainCamera.transform.position, targetCameraPosition) < 0.01f)
                {
                    if (isTransitioning)
                    {
                        Debug.Log("Camera transition complete, cleaning up previous level");
                        if (previousLevel != null)
                        {
                            previousLevel.gameObject.SetActive(false);
                            previousLevel = null;
                        }
                        isTransitioning = false;
                    }
                }
            }
        }
    }

    public void LoadLevel(int levelIndex)
    {
        Debug.Log($"Loading level {levelIndex}");
        
        // Validate level index
        if (levelIndex < 0 || levelIndex >= levels.Length)
        {
            Debug.LogError($"Invalid level index: {levelIndex}! Max level: {levels.Length - 1}");
            return;
        }

        if (!isTransitioning && currentLevel != null)
        {
            currentLevel.gameObject.SetActive(false);
        }

        // Enable new level
        currentLevel = levels[levelIndex];
        currentLevel.gameObject.SetActive(true);
        
        // Activate all children
        foreach (Transform child in currentLevel.transform)
        {
            child.gameObject.SetActive(true);
        }
        
        currentLevelIndex = levelIndex;

        Debug.Log($"New level position: {currentLevel.transform.position}");

        // Update boundaries for new level
        UpdateCameraBoundaries();
        Debug.Log($"New boundaries - Left: {leftBoundary}, Right: {rightBoundary}");

        isLevelComplete = false;
        targetCameraPosition = new Vector3(currentLevel.transform.position.x, mainCamera.transform.position.y, mainCamera.transform.position.z);

        // Store initial collectable positions
        StoreCollectablePositions();
    }

    public void LoadNextLevel()
    {
        LoadLevel(currentLevelIndex + 1);
    }

    // Call this when all stars in a level are collected
    public void CheckLevelComplete()
    {
        if (currentLevel != null && 
            GameObject.FindGameObjectsWithTag("Star").Length == 0)
        {
            Debug.Log("Level Complete!");
            isLevelComplete = true;  // Remove movement restrictions
        }
    }

    private void StoreCollectablePositions()
    {
        collectables = GameObject.FindGameObjectsWithTag("Star");
        initialCollectablePositions = new Vector3[collectables.Length];
        
        for (int i = 0; i < collectables.Length; i++)
        {
            initialCollectablePositions[i] = collectables[i].transform.position;
            Debug.Log($"Stored collectable {i} position: {initialCollectablePositions[i]}");
        }
    }

    public void RespawnPlayerAndCollectables()
    {
        // Respawn player
        if (player != null && currentLevel.playerSpawnPoint != null)
        {
            player.transform.position = currentLevel.playerSpawnPoint.position;
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            Debug.Log("Player respawned at spawn point");
        }

        // Respawn collectables
        for (int i = 0; i < collectables.Length; i++)
        {
            if (collectables[i] != null)
            {
                // If the collectable was deactivated (collected), reactivate it
                collectables[i].SetActive(true);
                collectables[i].transform.position = initialCollectablePositions[i];
                Debug.Log($"Respawned collectable {i} at {initialCollectablePositions[i]}");
            }
        }

        // Reset level completion status
        isLevelComplete = false;
    }

    // For debugging
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Draw level boundaries
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            new Vector3(leftBoundary, -10, 0),
            new Vector3(leftBoundary, 10, 0)
        );
        Gizmos.DrawLine(
            new Vector3(rightBoundary, -10, 0),
            new Vector3(rightBoundary, 10, 0)
        );
        
        // Draw next level trigger point
        if (isLevelComplete && currentLevel != null)
        {
            Gizmos.color = Color.yellow;
            float triggerX = currentLevel.transform.position.x + (levelXOffset / 2f);
            Gizmos.DrawLine(
                new Vector3(triggerX, -10, 0),
                new Vector3(triggerX, 10, 0)
            );
        }
    }

    public void RestartGame()
    {
        Debug.Log("Restarting game...");

        // Reset level index to 0
        currentLevelIndex = 0;
        
        // Reset transition state
        isTransitioning = false;
        previousLevel = null;
        
        // Reset level completion
        isLevelComplete = false;
        
        // Deactivate all levels except first one
        for (int i = 0; i < levels.Length; i++)
        {
            levels[i].gameObject.SetActive(i == 0);
        }
        
        // Load first level
        LoadLevel(0);
        
        // Reset camera position
        targetCameraPosition = new Vector3(0, mainCamera.transform.position.y, mainCamera.transform.position.z);
        mainCamera.transform.position = targetCameraPosition;
        player.Die();
        player.transform.position = currentLevel.playerSpawnPoint.position;
    }
} 