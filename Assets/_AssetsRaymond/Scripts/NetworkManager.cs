using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private Text userText;
    [SerializeField] private Text roomText;
    
    [Header("Room Settings")]
    [SerializeField] private byte maxPlayersPerRoom = 4;
    [SerializeField] private string roomName = "AR_Multiplayer_Room";
    
    
    private bool isConnecting = false;
    private string gameVersion = "1.0";
    private string randomRoomId;
    
    void Awake()
    {
        // Enable automatic scene synchronization
        PhotonNetwork.AutomaticallySyncScene = true;
        
        // Generate random room ID
        GenerateRandomRoomId();
    }
    
    void Start()
    {
        // Update UI immediately with random room ID
        UpdateUIWithRandomId();
        
        // Auto-connect when the scene starts
        Connect();
    }
    
    public void Connect()
    {
        isConnecting = true;
            
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinRoom(roomName);
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = gameVersion;
        }
    }
    
    void LogFeedback(string message)
    {
        Debug.Log(message);
    }
    
    #region Photon Callbacks
    
    public override void OnConnectedToMaster()
    {
        if (isConnecting)
        {
            PhotonNetwork.JoinRoom(roomName);
        }
    }
    
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayersPerRoom;
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    
    public override void OnCreatedRoom()
    {
        // Room created successfully
    }
    
    public override void OnJoinedRoom()
    {
        // Update UI for current player
        UpdateUIForCurrentPlayer();
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        isConnecting = false;
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // Player joined the room
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // Player left the room
    }
    
    #endregion
    
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }
    
    private void GenerateRandomRoomId()
    {
        // Generate a random 6-character alphanumeric ID
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        randomRoomId = "";
        
        for (int i = 0; i < 6; i++)
        {
            randomRoomId += chars[random.Next(chars.Length)];
        }
        
        Debug.Log("Generated random room ID: " + randomRoomId);
    }
    
    // Public method to get the current random room ID
    public string GetRandomRoomId()
    {
        return randomRoomId;
    }
    
    // Public method to generate a new random room ID
    public void GenerateNewRandomRoomId()
    {
        GenerateRandomRoomId();
        Debug.Log("New random room ID generated: " + randomRoomId);
    }
    
    // Public method to manually update UI (for testing)
    public void UpdateUINow()
    {
        Debug.Log("=== MANUAL UI UPDATE ===");
        Debug.Log("Random Room ID: " + randomRoomId);
        Debug.Log("UserText assigned: " + (userText != null ? "YES" : "NO"));
        Debug.Log("RoomText assigned: " + (roomText != null ? "YES" : "NO"));
        
        UpdateUIWithRandomId();
        
        Debug.Log("=== END UI UPDATE ===");
    }
    
    private void UpdateUIWithRandomId()
    {
        Debug.Log("Updating UI with random room ID: " + randomRoomId);
        
        // Update RoomText with random ID immediately
        if (roomText != null)
        {
            roomText.text = "Room " + randomRoomId;
            roomText.gameObject.SetActive(true);
            Debug.Log("RoomText updated to: " + roomText.text);
        }
        else
        {
            Debug.LogWarning("RoomText is null! Please assign it in the Inspector.");
        }
        
        // Update UserText (will be updated when player joins room)
        if (userText != null)
        {
            userText.text = "User 0";
            userText.gameObject.SetActive(true);
            Debug.Log("UserText updated to: " + userText.text);
        }
        else
        {
            Debug.LogWarning("UserText is null! Please assign it in the Inspector.");
        }
    }
    
    private void UpdateUIForCurrentPlayer()
    {
        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        
        // Update UserText
        if (userText != null)
        {
            userText.text = "User " + playerCount;
            userText.gameObject.SetActive(true);
            Debug.Log("UserText updated to: " + userText.text);
        }
        
        // Update RoomText with random ID
        if (roomText != null)
        {
            roomText.text = "Room " + randomRoomId;
            roomText.gameObject.SetActive(true);
            Debug.Log("RoomText updated to: " + roomText.text);
        }
    }
}
