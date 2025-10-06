using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class UIManager : MonoBehaviourPunCallbacks
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject connectionPanel;
    
    [Header("UI Text Elements")]
    [SerializeField] private Text connectionStatusText;
    [SerializeField] private Text roomInfoText;
    [SerializeField] private Text playerCountText;
    [SerializeField] private Text feedbackText;
    
    [Header("UI Buttons")]
    [SerializeField] private Button connectButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button leaveRoomButton;
    
    private NetworkManager networkManager;
    
    void Start()
    {
        networkManager = FindObjectOfType<NetworkManager>();
        
        // Set up button listeners
        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectButtonClicked);
        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(OnDisconnectButtonClicked);
        if (leaveRoomButton != null)
            leaveRoomButton.onClick.AddListener(OnLeaveRoomButtonClicked);
        
        // Initialize UI state
        UpdateUI();
    }
    
    void Update()
    {
        UpdateUI();
    }
    
    void UpdateUI()
    {
        // Update connection status
        if (connectionStatusText != null)
        {
            if (PhotonNetwork.IsConnected)
            {
                if (PhotonNetwork.InRoom)
                {
                    connectionStatusText.text = "Connected - In Room";
                    connectionStatusText.color = Color.green;
                }
                else
                {
                    connectionStatusText.text = "Connected - In Lobby";
                    connectionStatusText.color = Color.yellow;
                }
            }
            else
            {
                connectionStatusText.text = "Disconnected";
                connectionStatusText.color = Color.red;
            }
        }
        
        // Update room information
        if (roomInfoText != null && PhotonNetwork.InRoom)
        {
            roomInfoText.text = "Room: " + PhotonNetwork.CurrentRoom.Name;
        }
        else if (roomInfoText != null)
        {
            roomInfoText.text = "Not in a room";
        }
        
        // Update player count
        if (playerCountText != null && PhotonNetwork.InRoom)
        {
            playerCountText.text = "Players: " + PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;
        }
        else if (playerCountText != null)
        {
            playerCountText.text = "Players: 0/0";
        }
        
        // Show/hide appropriate panels
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(!PhotonNetwork.IsConnected);
        
        if (gamePanel != null)
            gamePanel.SetActive(PhotonNetwork.IsConnected && PhotonNetwork.InRoom);
        
        if (connectionPanel != null)
            connectionPanel.SetActive(PhotonNetwork.IsConnected && !PhotonNetwork.InRoom);
    }
    
    public void OnConnectButtonClicked()
    {
        if (networkManager != null)
        {
            networkManager.Connect();
        }
    }
    
    public void OnDisconnectButtonClicked()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
    }
    
    public void OnLeaveRoomButtonClicked()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }
    
    public void LogFeedback(string message)
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
        LogFeedback("Connected to Master Server");
    }
    
    public override void OnJoinedRoom()
    {
        LogFeedback("Joined room successfully");
    }
    
    public override void OnLeftRoom()
    {
        LogFeedback("Left room");
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        LogFeedback("Disconnected: " + cause);
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        LogFeedback("Player " + newPlayer.NickName + " joined");
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        LogFeedback("Player " + otherPlayer.NickName + " left");
    }
    
    #endregion
}
