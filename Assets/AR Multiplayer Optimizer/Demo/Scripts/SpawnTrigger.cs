using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnTrigger : MonoBehaviour
{
    [Header("Spawn Point Reference")]
    [SerializeField] private SpawnPoint spawnPoint;
    
    [Header("Settings")]
    [SerializeField] private bool autoFindSpawnPoint = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private bool hasTriggeredSpawn = false;
    
    void Start()
    {
        // Auto-find SpawnPoint if not assigned
        if (autoFindSpawnPoint && spawnPoint == null)
        {
            // Try to find SpawnPoint as a child (since SpawnPoint is a child of SpawnTrigger)
            spawnPoint = GetComponentInChildren<SpawnPoint>();
            
            if (spawnPoint == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning("SpawnTrigger: Could not find SpawnPoint child automatically!");
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log("SpawnTrigger: Found SpawnPoint child - " + spawnPoint.gameObject.name);
            }
        }
    }
    
    void OnEnable()
    {
        if (showDebugLogs)
            Debug.Log("SpawnTrigger activated!");
        
        // When this trigger becomes active, try to spawn player
        TriggerSpawn();
    }
    
    void OnDisable()
    {
        if (showDebugLogs)
            Debug.Log("SpawnTrigger deactivated!");
    }
    
    private void TriggerSpawn()
    {
        if (spawnPoint == null)
        {
            if (showDebugLogs)
                Debug.LogError("SpawnTrigger: SpawnPoint reference is not set!");
            return;
        }
        
        // Check if player already exists inside SpawnPoint
        if (spawnPoint.HasPlayerInside())
        {
            if (showDebugLogs)
                Debug.Log("SpawnTrigger: Player already exists in SpawnPoint, skipping spawn");
            return;
        }
        
        // Trigger spawn on the SpawnPoint
        if (showDebugLogs)
            Debug.Log("SpawnTrigger: Triggering player spawn...");
        
        spawnPoint.SpawnPlayerFromTrigger();
        hasTriggeredSpawn = true;
    }
    
    // Public method to manually trigger spawn
    public void ManualTriggerSpawn()
    {
        if (showDebugLogs)
            Debug.Log("SpawnTrigger: Manual spawn triggered");
        
        TriggerSpawn();
    }
    
    // Public method to reset trigger state
    public void ResetTrigger()
    {
        hasTriggeredSpawn = false;
        
        if (showDebugLogs)
            Debug.Log("SpawnTrigger: Reset trigger state");
    }
    
    // Public method to set SpawnPoint reference
    public void SetSpawnPoint(SpawnPoint point)
    {
        spawnPoint = point;
        
        if (showDebugLogs)
            Debug.Log("SpawnTrigger: SpawnPoint reference set to " + (point != null ? point.gameObject.name : "null"));
    }
    
    // Public method to find and assign SpawnPoint child
    public void FindAndAssignSpawnPointChild()
    {
        spawnPoint = GetComponentInChildren<SpawnPoint>();
        
        if (spawnPoint != null)
        {
            if (showDebugLogs)
                Debug.Log("SpawnTrigger: Successfully found and assigned SpawnPoint child - " + spawnPoint.gameObject.name);
        }
        else
        {
            if (showDebugLogs)
                Debug.LogError("SpawnTrigger: No SpawnPoint child found! Make sure SpawnPoint script is attached to the child SpawnPoint GameObject.");
        }
    }
}

