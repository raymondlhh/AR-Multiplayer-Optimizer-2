using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SpawnPointManager : MonoBehaviourPunCallbacks
{
    [Header("Spawn Point Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private SpawnPoint[] spawnPoints;
    [SerializeField] private bool autoAssignSpawnPoints = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private int currentPlayerCount = 0;
    private Dictionary<int, SpawnPoint> playerSpawnPoints = new Dictionary<int, SpawnPoint>();
    
    void Start()
    {
        // Find all spawn points if not assigned
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            FindAllSpawnPoints();
        }
        
        // Initialize spawn points
        InitializeSpawnPoints();
    }
    
    private void FindAllSpawnPoints()
    {
        spawnPoints = FindObjectsOfType<SpawnPoint>();
        
        if (showDebugLogs)
            Debug.Log("Found " + spawnPoints.Length + " spawn points");
    }
    
    private void InitializeSpawnPoints()
    {
        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    // Set player prefab
                    spawnPoints[i].SetPlayerPrefab(playerPrefab);
                    
                    // Initially disable all spawn points
                    spawnPoints[i].gameObject.SetActive(false);
                    
                    if (showDebugLogs)
                        Debug.Log("Initialized spawn point " + i + ": " + spawnPoints[i].name);
                }
            }
        }
    }
    
    public override void OnJoinedRoom()
    {
        if (showDebugLogs)
            Debug.Log("Player joined room. Current player count: " + PhotonNetwork.CurrentRoom.PlayerCount);
            
        // Assign spawn point to current player
        AssignSpawnPointToPlayer();
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (showDebugLogs)
            Debug.Log("Player " + newPlayer.NickName + " entered room. New player count: " + PhotonNetwork.CurrentRoom.PlayerCount);
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (showDebugLogs)
            Debug.Log("Player " + otherPlayer.NickName + " left room. New player count: " + PhotonNetwork.CurrentRoom.PlayerCount);
            
        // Clean up spawn point for the player who left
        CleanupSpawnPointForPlayer(otherPlayer.ActorNumber);
    }
    
    private void AssignSpawnPointToPlayer()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            if (showDebugLogs)
                Debug.LogWarning("Not connected to room, cannot assign spawn point");
            return;
        }
        
        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        int spawnIndex = playerCount - 1; // 0-based index
        
        if (spawnPoints != null && spawnIndex < spawnPoints.Length)
        {
            if (spawnPoints[spawnIndex] != null)
            {
                // Set as local player spawn point
                spawnPoints[spawnIndex].SetAsLocalPlayerSpawnPoint(true);
                
                // Activate the spawn point (this will trigger OnEnable and spawn the player)
                spawnPoints[spawnIndex].gameObject.SetActive(true);
                
                // Store the assignment
                playerSpawnPoints[PhotonNetwork.LocalPlayer.ActorNumber] = spawnPoints[spawnIndex];
                
                if (showDebugLogs)
                    Debug.Log("Assigned spawn point " + spawnIndex + " to player " + PhotonNetwork.LocalPlayer.ActorNumber);
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogError("Spawn point " + spawnIndex + " is null!");
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.LogError("No spawn points available for player " + playerCount);
        }
    }
    
    private void CleanupSpawnPointForPlayer(int playerActorNumber)
    {
        if (playerSpawnPoints.ContainsKey(playerActorNumber))
        {
            SpawnPoint spawnPoint = playerSpawnPoints[playerActorNumber];
            if (spawnPoint != null)
            {
                spawnPoint.gameObject.SetActive(false);
                spawnPoint.ResetSpawnState();
            }
            
            playerSpawnPoints.Remove(playerActorNumber);
            
            if (showDebugLogs)
                Debug.Log("Cleaned up spawn point for player " + playerActorNumber);
        }
    }
    
    // Public method to manually assign spawn point to current player
    public void AssignSpawnPointToCurrentPlayer()
    {
        AssignSpawnPointToPlayer();
    }
    
    // Public method to get spawn point for a specific player
    public SpawnPoint GetSpawnPointForPlayer(int playerActorNumber)
    {
        if (playerSpawnPoints.ContainsKey(playerActorNumber))
        {
            return playerSpawnPoints[playerActorNumber];
        }
        return null;
    }
    
    // Public method to get all spawn points
    public SpawnPoint[] GetAllSpawnPoints()
    {
        return spawnPoints;
    }
    
    // Public method to set player prefab
    public void SetPlayerPrefab(GameObject prefab)
    {
        playerPrefab = prefab;
        
        // Update all spawn points
        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    spawnPoints[i].SetPlayerPrefab(prefab);
                }
            }
        }
        
        if (showDebugLogs)
            Debug.Log("Player prefab set to " + (prefab != null ? prefab.name : "null"));
    }
}
