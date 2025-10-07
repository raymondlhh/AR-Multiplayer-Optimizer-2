using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private bool spawnOnEnable = true;
    [SerializeField] private bool spawnOnlyOnce = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private bool hasSpawned = false;
    private bool isLocalPlayer = false;
    
    void OnEnable()
    {
        if (showDebugLogs)
            Debug.Log("SpawnPoint " + gameObject.name + " enabled");
            
        if (spawnOnEnable)
        {
            TrySpawnPlayer();
        }
    }
    
    void OnDisable()
    {
        if (showDebugLogs)
            Debug.Log("SpawnPoint " + gameObject.name + " disabled");
    }
    
    void Start()
    {
        // Check if this is the local player's spawn point
        CheckIfLocalPlayerSpawnPoint();
        
        // Try to spawn if enabled and not already spawned
        if (spawnOnEnable && !hasSpawned)
        {
            TrySpawnPlayer();
        }
    }
    
    private void CheckIfLocalPlayerSpawnPoint()
    {
        // This is a simple way to determine if this is the local player's spawn point
        // You can modify this logic based on your needs
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            // For now, we'll assume the first active spawn point is for the local player
            // You can modify this logic to match your specific requirements
            isLocalPlayer = true;
            
            if (showDebugLogs)
                Debug.Log("SpawnPoint " + gameObject.name + " is for local player");
        }
    }
    
    public void TrySpawnPlayer()
    {
        if (showDebugLogs)
            Debug.Log("SpawnPoint " + gameObject.name + " attempting to spawn player");
            
        // Check if we should spawn
        if (!CanSpawn())
        {
            if (showDebugLogs)
                Debug.Log("SpawnPoint " + gameObject.name + " cannot spawn player - conditions not met");
            return;
        }
        
        // Spawn the player
        SpawnPlayer();
    }
    
    private bool CanSpawn()
    {
        // Check if we're connected to Photon
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            if (showDebugLogs)
                Debug.LogWarning("Not connected to Photon room");
            return false;
        }
        
        // Check if player prefab is assigned
        if (playerPrefab == null)
        {
            if (showDebugLogs)
                Debug.LogError("Player prefab is not assigned!");
            return false;
        }
        
        // Check if we should only spawn once and already spawned
        if (spawnOnlyOnce && hasSpawned)
        {
            if (showDebugLogs)
                Debug.Log("Player already spawned at this spawn point");
            return false;
        }
        
        // Check if this is the local player's spawn point
        if (!isLocalPlayer)
        {
            if (showDebugLogs)
                Debug.Log("This is not the local player's spawn point");
            return false;
        }
        
        return true;
    }
    
    private void SpawnPlayer()
    {
        if (showDebugLogs)
            Debug.Log("Spawning player at " + gameObject.name + " at position " + transform.position);
        
        try
        {
            // Instantiate the player prefab
            GameObject spawnedPlayer = PhotonNetwork.Instantiate(playerPrefab.name, transform.position, transform.rotation);
            
            if (spawnedPlayer != null)
            {
                hasSpawned = true;
                
                if (showDebugLogs)
                    Debug.Log("Player successfully spawned at " + gameObject.name);
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogError("Failed to spawn player - PhotonNetwork.Instantiate returned null");
            }
        }
        catch (System.Exception e)
        {
            if (showDebugLogs)
                Debug.LogError("Exception while spawning player: " + e.Message);
        }
    }
    
    // Public method to manually trigger spawning
    public void SpawnPlayerNow()
    {
        if (showDebugLogs)
            Debug.Log("Manual spawn triggered for " + gameObject.name);
            
        TrySpawnPlayer();
    }
    
    // Public method to reset spawn state
    public void ResetSpawnState()
    {
        hasSpawned = false;
        
        if (showDebugLogs)
            Debug.Log("Spawn state reset for " + gameObject.name);
    }
    
    // Public method to set player prefab
    public void SetPlayerPrefab(GameObject prefab)
    {
        playerPrefab = prefab;
        
        if (showDebugLogs)
            Debug.Log("Player prefab set to " + (prefab != null ? prefab.name : "null"));
    }
    
    // Public method to check if player has been spawned
    public bool HasPlayerSpawned()
    {
        return hasSpawned;
    }
    
    // Public method to set as local player spawn point
    public void SetAsLocalPlayerSpawnPoint(bool isLocal)
    {
        isLocalPlayer = isLocal;
        
        if (showDebugLogs)
            Debug.Log("SpawnPoint " + gameObject.name + " set as local player: " + isLocal);
    }
}
