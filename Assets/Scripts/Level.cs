using UnityEngine;

public class Level : MonoBehaviour
{
    [Header("Level Elements")]
    public Transform playerSpawnPoint;
    public GameObject[] groundObjects;
    public GameObject[] collectables;
    
    private void Awake()
    {
        // Ensure this level is initially disabled
        gameObject.SetActive(false);
    }
}