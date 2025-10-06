using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject controlPanel;
    [SerializeField] private Text feedbackText;
    [SerializeField] private Button connectButton;
    
    [Header("Room Settings")]
    [SerializeField] private byte maxPlayersPerRoom = 4;
    [SerializeField] private string roomName = "AR_Multiplayer_Room";
    
    [Header("Player Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    
    private bool isConnecting = false;
    private string gameVersion = "1.0";
    
    void Awake()
    {
        // Enable automatic scene synchronization
        PhotonNetwork.AutomaticallySyncScene = true;
        
        // Set up UI
        if (controlPanel != null)
            controlPanel.SetActive(true);
        if (feedbackText != null)
            feedbackText.text = "Ready to connect";
        if (connectButton != null)
            connectButton.onClick.AddListener(Connect);
    }
    
    void Start()
    {
        // Auto-connect when the scene starts
        Connect();
    }
    
    public void Connect()
    {
        if (feedbackText != null)
            feedbackText.text = "";
            
        isConnecting = true;
        
        if (controlPanel != null)
            controlPanel.SetActive(false);
            
        if (PhotonNetwork.IsConnected)
        {
            LogFeedback("Joining Room...");
            PhotonNetwork.JoinRoom(roomName);
        }
        else
        {
            LogFeedback("Connecting...");
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = gameVersion;
        }
    }
    
    void LogFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text += System.Environment.NewLine + message;
        }
        Debug.Log(message);
    }
    
    #region Photon Callbacks
    
    public override void OnConnectedToMaster()
    {
        if (isConnecting)
        {
            LogFeedback("Connected to Master Server. Joining room...");
            PhotonNetwork.JoinRoom(roomName);
        }
    }
    
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        LogFeedback("Room not found. Creating new room...");
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayersPerRoom;
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    
    public override void OnCreatedRoom()
    {
        LogFeedback("Room created successfully!");
    }
    
    public override void OnJoinedRoom()
    {
        LogFeedback("Joined room with " + PhotonNetwork.CurrentRoom.PlayerCount + " player(s)");
        
        // Spawn player
        SpawnPlayer();
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        LogFeedback("Disconnected: " + cause);
        isConnecting = false;
        
        if (controlPanel != null)
            controlPanel.SetActive(true);
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        LogFeedback("Player " + newPlayer.NickName + " joined the room");
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        LogFeedback("Player " + otherPlayer.NickName + " left the room");
    }
    
    #endregion
    
    private void SpawnPlayer()
    {
        if (playerPrefab != null)
        {
            Vector3 spawnPosition = GetSpawnPosition();
            PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogError("Player prefab is not assigned!");
        }
    }
    
    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIndex = PhotonNetwork.CurrentRoom.PlayerCount - 1;
            if (spawnIndex < spawnPoints.Length)
            {
                return spawnPoints[spawnIndex].position;
            }
        }
        
        // Default spawn position
        return new Vector3(0, 0, 0);
    }
    
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }
}
