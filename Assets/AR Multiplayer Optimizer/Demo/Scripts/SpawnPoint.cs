using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class SpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private bool spawnOnEnable = false; // Changed to false - use SpawnTrigger instead
    [SerializeField] private bool spawnOnlyOnce = true;
    [SerializeField] private bool useExistingPlayerChild = true;
    
    [Header("UI Settings")]
    [SerializeField] private Text playerSpawnedText;
    [SerializeField] private float textDisplayDuration = 3f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private bool hasSpawned = false;
    private bool isLocalPlayer = false;
    
    void OnEnable()
    {
        if (showDebugLogs)
            Debug.Log("SpawnPoint " + gameObject.name + " enabled");
            
        // Check if this is the local player's spawn point first
        CheckIfLocalPlayerSpawnPoint();
            
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
            // Check if player already exists as child
            Transform existingPlayer = transform.Find("Player");
            if (existingPlayer != null && useExistingPlayerChild)
            {
                if (showDebugLogs)
                    Debug.Log("Player already exists as child of " + gameObject.name + ", activating it");
                
                // Activate the existing player
                existingPlayer.gameObject.SetActive(true);
                hasSpawned = true;
                
                // Show "Player Spawned" text
                ShowPlayerSpawnedText();
                
                if (showDebugLogs)
                    Debug.Log("Player successfully activated at " + gameObject.name);
            }
            else
            {
                // Check if we need to instantiate a new player
                if (playerPrefab != null)
                {
                    // Instantiate the player prefab as a child of this SpawnPoint
                    GameObject spawnedPlayer = PhotonNetwork.Instantiate(playerPrefab.name, transform.position, transform.rotation);
                    
                    if (spawnedPlayer != null)
                    {
                        // Make the spawned player a child of this SpawnPoint
                        spawnedPlayer.transform.SetParent(transform);
                        spawnedPlayer.name = "Player"; // Ensure it's named "Player"
                        
                        hasSpawned = true;
                        
                        // Show "Player Spawned" text
                        ShowPlayerSpawnedText();
                        
                        if (showDebugLogs)
                            Debug.Log("Player successfully spawned and parented to " + gameObject.name);
                    }
                    else
                    {
                        if (showDebugLogs)
                            Debug.LogError("Failed to spawn player - PhotonNetwork.Instantiate returned null");
                    }
                }
                else
                {
                    if (showDebugLogs)
                        Debug.LogError("No player prefab assigned and no existing player child found!");
                }
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
    
    // Public method to activate existing player child
    public void ActivateExistingPlayer()
    {
        Transform existingPlayer = transform.Find("Player");
        if (existingPlayer != null)
        {
            existingPlayer.gameObject.SetActive(true);
            hasSpawned = true;
            
            if (showDebugLogs)
                Debug.Log("Existing player activated at " + gameObject.name);
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning("No existing player child found at " + gameObject.name);
        }
    }
    
    // Public method to deactivate existing player child
    public void DeactivateExistingPlayer()
    {
        Transform existingPlayer = transform.Find("Player");
        if (existingPlayer != null)
        {
            existingPlayer.gameObject.SetActive(false);
            hasSpawned = false;
            
            if (showDebugLogs)
                Debug.Log("Existing player deactivated at " + gameObject.name);
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning("No existing player child found at " + gameObject.name);
        }
    }
    
    // Method to show "Player Spawned" text
    private void ShowPlayerSpawnedText()
    {
        if (playerSpawnedText != null)
        {
            playerSpawnedText.text = "Player Spawned";
            playerSpawnedText.gameObject.SetActive(true);
            
            if (showDebugLogs)
                Debug.Log("Player Spawned text shown");
            
            // Start coroutine to hide text after duration
            StartCoroutine(HidePlayerSpawnedTextAfterDelay());
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning("PlayerSpawnedText is not assigned!");
        }
    }
    
    // Coroutine to hide the text after a delay
    private IEnumerator HidePlayerSpawnedTextAfterDelay()
    {
        yield return new WaitForSeconds(textDisplayDuration);
        
        if (playerSpawnedText != null)
        {
            playerSpawnedText.gameObject.SetActive(false);
            
            if (showDebugLogs)
                Debug.Log("Player Spawned text hidden");
        }
    }
    
    // Public method to manually show the text
    public void ShowPlayerSpawnedTextManually()
    {
        ShowPlayerSpawnedText();
    }
    
    // Public method to hide the text immediately
    public void HidePlayerSpawnedText()
    {
        if (playerSpawnedText != null)
        {
            playerSpawnedText.gameObject.SetActive(false);
            
            if (showDebugLogs)
                Debug.Log("Player Spawned text hidden manually");
        }
    }
    
    // Public method to check if player exists inside (as child)
    public bool HasPlayerInside()
    {
        // Check if player exists as child
        Transform existingPlayer = transform.Find("Player");
        
        if (existingPlayer != null)
        {
            if (showDebugLogs)
                Debug.Log("Player exists inside " + gameObject.name);
            return true;
        }
        
        // Also check for any child with PhotonView component (networked player)
        PhotonView[] photonViews = GetComponentsInChildren<PhotonView>();
        if (photonViews.Length > 0)
        {
            if (showDebugLogs)
                Debug.Log("Networked player found inside " + gameObject.name);
            return true;
        }
        
        return false;
    }
    
    // Public method to spawn player when triggered by SpawnTrigger
    public void SpawnPlayerFromTrigger()
    {
        if (showDebugLogs)
            Debug.Log("SpawnPlayerFromTrigger called on " + gameObject.name);
        
        // Check if player already exists inside
        if (HasPlayerInside())
        {
            if (showDebugLogs)
                Debug.Log("Player already exists inside " + gameObject.name + ", skipping spawn");
            return;
        }
        
        // Check if this is the local player's spawn point
        CheckIfLocalPlayerSpawnPoint();
        
        // Attempt to spawn
        TrySpawnPlayer();
    }
}
