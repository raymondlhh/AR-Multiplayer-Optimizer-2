using System;
using System.Collections.Generic;
using UnityEngine;

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
#endif

/// <summary>
/// [AUTOMATIC] Orchestrates anchor alignment and (optionally) a multiplayer "everyone ready" barrier.
/// - Automatically ensures an AnchorRoot exists.
/// - Automatically tracks a chosen Vuforia ImageTarget by name and snaps AnchorRoot to it when first detected.
/// - Automatically synchronizes anchor position across all multiplayer clients.
/// - Optionally gates gameplay until all PUN2 clients are aligned.
/// 
/// Manual Setup Required:
/// - Assign AMOConfig asset in Inspector (or use Setup Helper)
/// </summary>
public class AMOSessionManager : MonoBehaviour, IPunObservable
{
	public static AMOSessionManager Instance { get; private set; }

	[SerializeField]
	private AMOConfig config;

	[SerializeField]
	private Transform anchorRoot;

	[SerializeField]
	private AMOAnchorTracker anchorTracker;

	public bool IsAligned { get; private set; }

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
	private readonly HashSet<int> alignedActors = new HashSet<int>();
#endif

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);

		if (config == null)
		{
			config = AMOResources.LoadOrCreateConfig();
		}

		EnsureAnchorRoot();
		EnsureTracker();
	}

	private void EnsureAnchorRoot()
	{
		if (anchorRoot != null && anchorRoot.gameObject != null)
			return;

		var existing = GameObject.Find(config.anchorRootName);
		if (existing == null)
			existing = new GameObject(string.IsNullOrWhiteSpace(config.anchorRootName) ? "AnchorRoot" : config.anchorRootName);

		anchorRoot = existing.transform;
		Debug.Log($"[AMOSession] [AUTOMATIC] Created/Found AnchorRoot: {anchorRoot.name}");
	}

	private void EnsureTracker()
	{
		if (anchorTracker == null)
		{
			anchorTracker = gameObject.AddComponent<AMOAnchorTracker>();
			Debug.Log("[AMOSession] [AUTOMATIC] Created AMOAnchorTracker component");
		}

		anchorTracker.Initialize(config, anchorRoot);
		anchorTracker.onAlignedOnce += HandleLocalAligned;
		Debug.Log("[AMOSession] [AUTOMATIC] Initialized AMOAnchorTracker");
	}

	private void OnDestroy()
	{
		if (anchorTracker != null)
			anchorTracker.onAlignedOnce -= HandleLocalAligned;
	}

	private void HandleLocalAligned()
	{
		IsAligned = true;

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
		// Mark self ready and check if all are ready.
		if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
		{
			alignedActors.Add(PhotonNetwork.LocalPlayer.ActorNumber);
			PhotonView photonView = GetOrCreatePhotonView();
			
			// Send anchor root position and rotation to all other clients
			photonView.RPC(nameof(RPC_SyncAnchorRoot), RpcTarget.OthersBuffered, 
				anchorRoot.position, anchorRoot.rotation);
			
			photonView.RPC(nameof(RPC_RemoteAligned), RpcTarget.OthersBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
			CheckAllReady();
		}
#endif
	}

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
	[PunRPC]
	private void RPC_SyncAnchorRoot(Vector3 position, Quaternion rotation, PhotonMessageInfo _)
	{
		// Only apply anchor root sync if we haven't aligned locally yet
		if (!IsAligned && anchorRoot != null)
		{
			Debug.Log($"[AMOSession] Syncing anchor root from remote client: {position}");
			anchorRoot.SetPositionAndRotation(position, rotation);
			
			// Mark as aligned since we received the anchor position
			IsAligned = true;
			alignedActors.Add(PhotonNetwork.LocalPlayer.ActorNumber);
			CheckAllReady();
		}
	}

	[PunRPC]
	private void RPC_RemoteAligned(int actorNumber, PhotonMessageInfo _)
	{
		alignedActors.Add(actorNumber);
		CheckAllReady();
	}

	private void CheckAllReady()
	{
		if (!config.waitForAllClients)
			return;

		if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
			return;

		var players = PhotonNetwork.PlayerList;
		if (players == null || players.Length == 0)
			return;

		foreach (var p in players)
		{
			if (!alignedActors.Contains(p.ActorNumber))
				return; // someone not ready yet
		}

		// Everyone ready
		OnEveryoneReady();
	}

	private PhotonView GetOrCreatePhotonView()
	{
		var view = GetComponent<PhotonView>();
		if (view == null)
			view = gameObject.AddComponent<PhotonView>();
		return view;
	}
#endif

	private void OnEveryoneReady()
	{
		// Hook point: gameplay can safely proceed. For now, we simply log.
		Debug.Log("[AMOSession] All clients aligned. Gameplay may proceed.");
	}

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
	// Handle late-joining clients by sending them the current anchor position
	// This method should be called from NetworkManager's OnPlayerEnteredRoom callback
	public void HandlePlayerEnteredRoom(Player newPlayer)
	{
		if (IsAligned && anchorRoot != null)
		{
			PhotonView photonView = GetOrCreatePhotonView();
			photonView.RPC(nameof(RPC_SyncAnchorRoot), newPlayer, 
				anchorRoot.position, anchorRoot.rotation);
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			// Send anchor root position and rotation
			stream.SendNext(anchorRoot != null ? anchorRoot.position : Vector3.zero);
			stream.SendNext(anchorRoot != null ? anchorRoot.rotation : Quaternion.identity);
			stream.SendNext(IsAligned);
		}
		else
		{
			// Receive anchor root position and rotation
			Vector3 receivedPosition = (Vector3)stream.ReceiveNext();
			Quaternion receivedRotation = (Quaternion)stream.ReceiveNext();
			bool receivedAligned = (bool)stream.ReceiveNext();
			
			// Only apply if we haven't aligned locally and received valid data
			if (!IsAligned && anchorRoot != null && receivedAligned)
			{
				Debug.Log($"[AMOSession] Syncing anchor root from stream: {receivedPosition}");
				anchorRoot.SetPositionAndRotation(receivedPosition, receivedRotation);
				IsAligned = true;
			}
		}
	}
#endif
}

public static class AMOResources
{
	private const string DefaultResourcePath = "AMOConfig";

	public static AMOConfig LoadOrCreateConfig()
	{
		var cfg = Resources.Load<AMOConfig>(DefaultResourcePath);
		if (cfg != null)
			return cfg;

		// Create a transient ScriptableObject to avoid null refs at runtime.
		cfg = ScriptableObject.CreateInstance<AMOConfig>();
		return cfg;
	}
}


