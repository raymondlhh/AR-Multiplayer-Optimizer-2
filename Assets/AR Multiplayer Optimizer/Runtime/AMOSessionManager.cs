using System;
using System.Collections.Generic;
using UnityEngine;

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
#endif

/// <summary>
/// Orchestrates anchor alignment and (optionally) a multiplayer "everyone ready" barrier.
/// - Ensures an AnchorRoot exists.
/// - Tracks a chosen Vuforia ImageTarget by name and snaps AnchorRoot to it when first detected.
/// - Optionally gates gameplay until all PUN2 clients are aligned.
/// </summary>
public class AMOSessionManager : MonoBehaviour
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
	}

	private void EnsureTracker()
	{
		if (anchorTracker == null)
			anchorTracker = gameObject.AddComponent<AMOAnchorTracker>();

		anchorTracker.Initialize(config, anchorRoot);
		anchorTracker.onAlignedOnce += HandleLocalAligned;
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
			photonView.RPC(nameof(RPC_RemoteAligned), RpcTarget.OthersBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
			CheckAllReady();
		}
#endif
	}

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
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


