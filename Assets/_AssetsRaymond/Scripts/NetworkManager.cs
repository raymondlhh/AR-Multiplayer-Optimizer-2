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
        
        // Don't generate random ID here - wait until we join a room
        // This ensures all users see the same ID
    }
    
    void Start()
    {
        // Set initial UI state
        UpdateUIWithRandomId();
        
        // Auto-connect when the scene starts
        Connect();
    }
    
    public void Connect()
    {
        isConnecting = true;
            
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Attempting to join room: " + roomName);
            PhotonNetwork.JoinRoom(roomName);
        }
        else
        {
            Debug.Log("Connecting to Photon with room: " + roomName);
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
            Debug.Log("Connected to master, joining room: " + roomName);
            PhotonNetwork.JoinRoom(roomName);
        }
    }
    
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("Room join failed, creating new room: " + roomName);
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayersPerRoom;
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    
    public override void OnCreatedRoom()
    {
        Debug.Log("Room created successfully: " + roomName);
    }
    
    public override void OnJoinedRoom()
    {
        Debug.Log("Successfully joined room: " + PhotonNetwork.CurrentRoom.Name);
        
        // Check if room already has a random ID set
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("RoomID"))
        {
            // Use the existing room ID
            randomRoomId = (string)PhotonNetwork.CurrentRoom.CustomProperties["RoomID"];
            Debug.Log("Using existing room ID: " + randomRoomId);
        }
        else
        {
            // Set the room ID for this room (first player)
            SetRoomID();
        }
        
        // Update UI for current player
        UpdateUIForCurrentPlayer();
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        isConnecting = false;
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Player entered room: " + newPlayer.NickName + " (Total players: " + PhotonNetwork.CurrentRoom.PlayerCount + ")");
        // Update UI when new player joins
        UpdateUIForCurrentPlayer();
    }
    
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // When room properties change, update the room ID if it was set
        if (propertiesThatChanged.ContainsKey("RoomID"))
        {
            randomRoomId = (string)PhotonNetwork.CurrentRoom.CustomProperties["RoomID"];
            Debug.Log("Room ID updated: " + randomRoomId);
            UpdateUIForCurrentPlayer();
        }
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("Player left room: " + otherPlayer.NickName + " (Total players: " + PhotonNetwork.CurrentRoom.PlayerCount + ")");
        // Update UI when player leaves
        UpdateUIForCurrentPlayer();
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
    
    // Public method to get the room name
    public string GetRoomName()
    {
        return roomName;
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
    
    private void SetRoomID()
    {
        // Generate a random room ID
        GenerateRandomRoomId();
        
        // Set it as a room property so all players see the same ID
        ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps["RoomID"] = randomRoomId;
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        
        Debug.Log("Set room ID: " + randomRoomId);
    }
    
    private void UpdateUIWithRandomId()
    {
        Debug.Log("Updating UI with random room ID: " + randomRoomId);
        
        // Update RoomText with random ID
        if (roomText != null)
        {
            if (string.IsNullOrEmpty(randomRoomId))
            {
                roomText.text = "Room Connecting...";
            }
            else
            {
                roomText.text = "Room " + randomRoomId;
            }
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
            userText.text = "Users: 0";
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
            userText.text = "Users: " + playerCount;
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
