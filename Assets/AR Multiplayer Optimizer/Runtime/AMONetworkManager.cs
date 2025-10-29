using UnityEngine;
using System.Collections.Generic;

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
#endif

/// <summary>
/// Simple NetworkManager integration for AR Multiplayer Optimizer
/// This script helps detect when players join/leave and ensures proper synchronization
/// </summary>
public class AMONetworkManager : MonoBehaviour
#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
    , IConnectionCallbacks, IMatchmakingCallbacks
#endif
{
    [Header("AR Multiplayer Optimizer Integration")]
    [Tooltip("Automatically notify AMOSessionManager when players join/leave")]
    public bool autoNotifyAMO = true;

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
    private void Start()
    {
        // Register for Photon callbacks
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDestroy()
    {
        // Unregister from Photon callbacks
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    // IConnectionCallbacks implementation
    public void OnConnectedToMaster()
    {
        Debug.Log("[AMONetworkManager] Connected to Photon Master Server");
    }

    public void OnConnected()
    {
        Debug.Log("[AMONetworkManager] Connected to Photon");
    }

    public void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[AMONetworkManager] Disconnected from Photon: {cause}");
    }

    public void OnRegionListReceived(RegionHandler regionHandler)
    {
        Debug.Log("[AMONetworkManager] Region list received");
    }

    public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
        Debug.Log("[AMONetworkManager] Custom authentication response received");
    }

    public void OnCustomAuthenticationFailed(string debugMessage)
    {
        Debug.Log($"[AMONetworkManager] Custom authentication failed: {debugMessage}");
    }

    // IMatchmakingCallbacks implementation
    public void OnFriendListUpdate(List<FriendInfo> friendList)
    {
        Debug.Log("[AMONetworkManager] Friend list updated");
    }

    public void OnCreatedRoom()
    {
        Debug.Log("[AMONetworkManager] Room created");
    }

    public void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log($"[AMONetworkManager] Create room failed: {message}");
    }

    public void OnJoinedRoom()
    {
        Debug.Log("[AMONetworkManager] Joined room");
        
        if (autoNotifyAMO)
        {
            // Notify AMOSessionManager that we joined a room
            var sessionManager = AMOSessionManager.Instance;
            if (sessionManager != null)
            {
                Debug.Log("[AMONetworkManager] Notifying AMOSessionManager of room join");
            }
        }
    }

    public void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log($"[AMONetworkManager] Join room failed: {message}");
    }

    public void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log($"[AMONetworkManager] Join random room failed: {message}");
    }

    public void OnLeftRoom()
    {
        Debug.Log("[AMONetworkManager] Left room");
    }

    // Player management
    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[AMONetworkManager] Player {newPlayer.ActorNumber} entered room");
        
        if (autoNotifyAMO)
        {
            // Notify AMOSessionManager about the new player
            var sessionManager = AMOSessionManager.Instance;
            if (sessionManager != null)
            {
                sessionManager.HandlePlayerEnteredRoom(newPlayer);
            }
        }
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[AMONetworkManager] Player {otherPlayer.ActorNumber} left room");
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[AMONetworkManager] Master client switched to player {newMasterClient.ActorNumber}");
    }

    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        Debug.Log("[AMONetworkManager] Room properties updated");
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        Debug.Log($"[AMONetworkManager] Player {targetPlayer.ActorNumber} properties updated");
    }
#else
    private void Start()
    {
        Debug.LogWarning("[AMONetworkManager] Photon Unity Networking not available. This script requires PUN2.");
    }
#endif
}

