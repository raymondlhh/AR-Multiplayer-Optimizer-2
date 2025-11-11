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
/// - Automatically creates and configures AMOConfig with sensible defaults.
/// </summary>
public class AMOSessionManager : MonoBehaviour, IPunObservable
{
	public static AMOSessionManager Instance { get; private set; }

	[SerializeField, HideInInspector]
	private AMOConfig config;

	[SerializeField]
	private Transform anchorRoot;

	[SerializeField]
	private AMOAnchorTracker anchorTracker;

	[SerializeField, HideInInspector]
	private GameObject anchorVisualization;

	public bool IsAligned { get; private set; }

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
        private readonly HashSet<int> alignedActors = new HashSet<int>();
        private int anchorAuthorityActorNumber = -1;
        private bool awaitingAuthoritativeAnchor;
#endif

        private System.Type cachedPlayerControllerType;

        private bool hasBroadcastLocalAlignment;

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
	
	private void Start()
	{
		// Check if we're already in a room and need to sync
		StartCoroutine(CheckForExistingPlayers());
	}
	
	private System.Collections.IEnumerator CheckForExistingPlayers()
	{
		// Wait a frame for Photon to initialize
		yield return null;
		
#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
		// If we're in a room and there are other players, request anchor position
		if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount > 1)
		{
			yield return new WaitForSeconds(1f); // Wait a bit for other players to be ready
			RequestAnchorPositionFromOthers();
			
			// Also request periodic sync to ensure we stay aligned
			yield return new WaitForSeconds(2f);
			RequestPeriodicSync();
		}
#endif
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
		
		// Create anchor visualization
		EnsureAnchorVisualization();
	}

	private void OnDestroy()
	{
		if (anchorTracker != null)
			anchorTracker.onAlignedOnce -= HandleLocalAligned;
			
		// Clean up visualization
		if (anchorVisualization != null)
		{
			DestroyImmediate(anchorVisualization);
		}
	}

	/// <summary>
	/// Creates and manages the anchor center visualization for runtime builds
	/// </summary>
	private void EnsureAnchorVisualization()
	{
		if (!config.showAnchorCenter)
		{
			if (anchorVisualization != null)
			{
				anchorVisualization.SetActive(false);
			}
			return;
		}

		if (anchorVisualization == null)
		{
			CreateAnchorVisualization();
		}
		else
		{
			anchorVisualization.SetActive(true);
			UpdateVisualizationProperties();
		}
	}

	/// <summary>
	/// Creates the anchor center visualization GameObject
	/// </summary>
	private void CreateAnchorVisualization()
	{
		if (anchorRoot == null) return;

		// Create visualization GameObject
		anchorVisualization = new GameObject("AnchorCenterVisualization");
		anchorVisualization.transform.SetParent(anchorRoot, false);
		anchorVisualization.transform.localPosition = Vector3.zero;
		anchorVisualization.transform.localRotation = Quaternion.identity;

		// Add visual components
		UpdateVisualizationProperties();
		
		Debug.Log("[AMOSession] [AUTOMATIC] Created anchor center visualization");
	}

	/// <summary>
	/// Updates the visualization properties based on config
	/// </summary>
	private void UpdateVisualizationProperties()
	{
		if (anchorVisualization == null) return;

		// Update scale
		float scale = config.anchorCenterSize;
		anchorVisualization.transform.localScale = Vector3.one * scale;

		// Create or update visual components
		CreateVisualizationComponents();
	}

	/// <summary>
	/// Creates the visual components for the anchor center
	/// </summary>
	private void CreateVisualizationComponents()
	{
		if (anchorVisualization == null) return;

		// Clear existing components
		var existingRenderers = anchorVisualization.GetComponentsInChildren<Renderer>();
		foreach (var renderer in existingRenderers)
		{
			if (Application.isPlaying)
				Destroy(renderer.gameObject);
			else
				DestroyImmediate(renderer.gameObject);
		}

		// Create center sphere
		var centerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		centerSphere.name = "CenterSphere";
		centerSphere.transform.SetParent(anchorVisualization.transform, false);
		centerSphere.transform.localPosition = Vector3.zero;
		centerSphere.transform.localScale = Vector3.one * 0.1f;

		// Set material color
		var centerRenderer = centerSphere.GetComponent<Renderer>();
		if (centerRenderer != null)
		{
			var material = new Material(Shader.Find("Standard"));
			material.color = config.anchorCenterColor;
			material.SetFloat("_Metallic", 0.5f);
			material.SetFloat("_Smoothness", 0.8f);
			centerRenderer.material = material;
		}

		// Remove collider
		var collider = centerSphere.GetComponent<Collider>();
		if (collider != null)
		{
			if (Application.isPlaying)
				Destroy(collider);
			else
				DestroyImmediate(collider);
		}

		// Create coordinate axes
		CreateAxis("XAxis", Vector3.right, Color.red);
		CreateAxis("YAxis", Vector3.up, Color.green);
		CreateAxis("ZAxis", Vector3.forward, Color.blue);

		// Create circle
		CreateCircle();
	}

	/// <summary>
	/// Creates a coordinate axis
	/// </summary>
	private void CreateAxis(string name, Vector3 direction, Color color)
	{
		var axis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		axis.name = name;
		axis.transform.SetParent(anchorVisualization.transform, false);
		
		// Position and rotate
		axis.transform.localPosition = direction * 0.15f;
		axis.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
		axis.transform.localScale = new Vector3(0.02f, 0.3f, 0.02f);

		// Set material
		var axisRenderer = axis.GetComponent<Renderer>();
		if (axisRenderer != null)
		{
			var material = new Material(Shader.Find("Standard"));
			material.color = color;
			axisRenderer.material = material;
		}

		// Remove collider
		var collider = axis.GetComponent<Collider>();
		if (collider != null)
		{
			if (Application.isPlaying)
				Destroy(collider);
			else
				DestroyImmediate(collider);
		}
	}

	/// <summary>
	/// Creates a circle around the center
	/// </summary>
	private void CreateCircle()
	{
		var circle = new GameObject("Circle");
		circle.transform.SetParent(anchorVisualization.transform, false);
		circle.transform.localPosition = Vector3.zero;
		circle.transform.localRotation = Quaternion.identity;

		// Create circle using LineRenderer
		var lineRenderer = circle.AddComponent<LineRenderer>();
		var lineMaterial = new Material(Shader.Find("Sprites/Default"));
		lineMaterial.color = config.anchorCenterColor;
		lineRenderer.material = lineMaterial;
		lineRenderer.startWidth = 0.01f;
		lineRenderer.endWidth = 0.01f;
		lineRenderer.positionCount = 33; // 32 segments + 1 to close the circle
		lineRenderer.useWorldSpace = false;

		// Generate circle points
		float radius = 0.2f;
		for (int i = 0; i <= 32; i++)
		{
			float angle = i * 360f / 32f * Mathf.Deg2Rad;
			float x = Mathf.Cos(angle) * radius;
			float z = Mathf.Sin(angle) * radius;
			lineRenderer.SetPosition(i, new Vector3(x, 0, z));
		}
	}

        private void HandleLocalAligned()
        {
                hasBroadcastLocalAlignment = false;
                IsAligned = true;

                // Automatically anchor all virtual objects to Image Target center
                AnchorAllVirtualObjects();

                // Start continuous synchronization
                StartContinuousSync();

                // Update visualization when aligned
                EnsureAnchorVisualization();

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
                awaitingAuthoritativeAnchor = false;

                if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null)
                {
                        if (TryClaimAnchorAuthority())
                        {
                                anchorAuthorityActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                                PhotonView photonView = GetOrCreatePhotonView();

                                // Send anchor root position and rotation to all other clients with higher priority
                                photonView.RPC(nameof(RPC_SyncAnchorRoot), RpcTarget.OthersBuffered,
                                        anchorRoot.position, anchorRoot.rotation);

                                // Also broadcast to all players to ensure everyone gets the update
                                photonView.RPC(nameof(RPC_SyncAnchorRoot), RpcTarget.All,
                                        anchorRoot.position, anchorRoot.rotation);

                                // Send multiple times to ensure reliability
                                StartCoroutine(SendMultipleSyncUpdates());

                                CompleteLocalAlignment(broadcastObjects: true);
                        }
                        else
                        {
                                awaitingAuthoritativeAnchor = true;
                                RequestAnchorPositionFromOthers();
                        }

                        return;
                }
#endif

                CompleteLocalAlignment(broadcastObjects: true);
        }

        private void CompleteLocalAlignment(bool broadcastObjects)
        {
                if (broadcastObjects)
                {
                        BroadcastAnchoredObjectStates();
                }

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
                if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null)
                {
                        alignedActors.Add(PhotonNetwork.LocalPlayer.ActorNumber);

                        if (!hasBroadcastLocalAlignment)
                        {
                                hasBroadcastLocalAlignment = true;
                                PhotonView photonView = GetOrCreatePhotonView();
                                photonView.RPC(nameof(RPC_RemoteAligned), RpcTarget.OthersBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
                        }

                        CheckAllReady();
                }
#endif
        }

        public void HandleJoinedRoom()
        {
                hasBroadcastLocalAlignment = false;
                IsAligned = false;

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
                awaitingAuthoritativeAnchor = false;
                anchorAuthorityActorNumber = -1;
                alignedActors.Clear();
#endif

                CancelInvoke(nameof(SendPeriodicAnchorSync));
                CancelInvoke(nameof(EnsureObjectsAnchored));
        }

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
        [PunRPC]
        private void RPC_SyncAnchorRoot(Vector3 position, Quaternion rotation, PhotonMessageInfo info)
        {
                if (anchorRoot == null)
                        return;

                int senderActorNumber = info.Sender != null ? info.Sender.ActorNumber : -1;

                if (anchorAuthorityActorNumber != -1 && senderActorNumber != -1 && senderActorNumber != anchorAuthorityActorNumber)
                {
                        Debug.Log($"[AMOSession] Ignoring anchor sync from non-authoritative actor {senderActorNumber}");
                        return;
                }

                if (senderActorNumber != -1)
                {
                        anchorAuthorityActorNumber = senderActorNumber;
                }

                bool localIsAuthority = IsLocalAnchorAuthority();
                bool senderIsAuthority = senderActorNumber != -1 && senderActorNumber == anchorAuthorityActorNumber;

                bool shouldCompleteAlignment = false;

                if (!IsAligned)
                {
                        Debug.Log($"[AMOSession] Syncing anchor root from remote client {senderActorNumber}: {position}");
                        anchorRoot.SetPositionAndRotation(position, rotation);

                        IsAligned = true;

                        AnchorAllVirtualObjects();
                        StartContinuousSync();
                        EnsureAnchorVisualization();

                        awaitingAuthoritativeAnchor = false;
                        shouldCompleteAlignment = true;
                }
                else
                {
                        float distance = Vector3.Distance(anchorRoot.position, position);
                        float angle = Quaternion.Angle(anchorRoot.rotation, rotation);

                        if ((!localIsAuthority && senderIsAuthority) || distance > 0.1f || angle > 2f)
                        {
                                Debug.Log($"[AMOSession] Updating anchor root to match remote client {senderActorNumber}: {position} (distance: {distance:F2}m, angle: {angle:F1}°)");
                                anchorRoot.SetPositionAndRotation(position, rotation);

                                AnchorAllVirtualObjects();

                                if (!localIsAuthority && senderIsAuthority)
                                {
                                        shouldCompleteAlignment = awaitingAuthoritativeAnchor || !hasBroadcastLocalAlignment;
                                        awaitingAuthoritativeAnchor = false;
                                }
                        }
                        else
                        {
                                return;
                        }
                }

                if (shouldCompleteAlignment)
                {
                        CompleteLocalAlignment(broadcastObjects: true);
                }
                else
                {
                        BroadcastAnchoredObjectStates();
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

        private bool TryClaimAnchorAuthority()
        {
                if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
                        return true;

                if (anchorAuthorityActorNumber != -1 && PhotonNetwork.CurrentRoom != null &&
                        !PhotonNetwork.CurrentRoom.Players.ContainsKey(anchorAuthorityActorNumber))
                {
                        anchorAuthorityActorNumber = -1;
                }

                if (anchorAuthorityActorNumber == -1)
                {
                        if (PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom?.PlayerCount <= 1)
                        {
                                anchorAuthorityActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                                return true;
                        }

                        return false;
                }

                return anchorAuthorityActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
        }

        private bool IsLocalAnchorAuthority()
        {
                if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
                        return true;

                if (anchorAuthorityActorNumber != -1 && PhotonNetwork.CurrentRoom != null &&
                        !PhotonNetwork.CurrentRoom.Players.ContainsKey(anchorAuthorityActorNumber))
                {
                        anchorAuthorityActorNumber = -1;
                }

                if (anchorAuthorityActorNumber == -1 && PhotonNetwork.IsMasterClient)
                {
                        TryClaimAnchorAuthority();
                }

                return anchorAuthorityActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
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
	
	/// <summary>
	/// [AUTOMATIC] Anchors all virtual objects to the Image Target center for proper synchronization
	/// </summary>
	private System.Type GetPlayerControllerType()
        {
#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
                if (cachedPlayerControllerType != null)
                        return cachedPlayerControllerType;

                cachedPlayerControllerType = System.Type.GetType("PlayerController");
                if (cachedPlayerControllerType != null)
                        return cachedPlayerControllerType;

                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                        cachedPlayerControllerType = assembly.GetType("PlayerController");
                        if (cachedPlayerControllerType != null)
                                break;
                }

                return cachedPlayerControllerType;
#else
                return null;
#endif
        }

	private void AnchorAllVirtualObjects()
        {
                Debug.Log("[AMOSession] [AUTOMATIC] Anchoring all virtual objects to Image Target center...");
		
		// Find and anchor all objects with common virtual object names
		string[] objectNames = { "Cube", "Player", "VirtualObject", "ARObject" };
		foreach (string name in objectNames)
		{
			foreach (GameObject obj in FindObjectsByName(name))
			{
				AnchorObjectToImageTarget(obj);
			}
		}
		
                // Find and anchor all objects with common virtual object tags
                string[] tags = { "Player", "VirtualObject", "ARObject" };
                foreach (string tag in tags)
                {
                        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
                        foreach (GameObject obj in objects)
                        {
                                AnchorObjectToImageTarget(obj);
                        }
                }

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
                // Also anchor any Photon networked objects to guarantee shared alignment
                foreach (GameObject photonObject in FindPhotonNetworkObjects())
                {
                        AnchorObjectToImageTarget(photonObject);
                }
#endif
		
		Debug.Log("[AMOSession] [AUTOMATIC] Virtual objects anchored to Image Target center");
	}
	
	/// <summary>
	/// Anchors a specific object to the Image Target center with improved synchronization
	/// </summary>
	private void AnchorObjectToImageTarget(GameObject obj)
	{
		if (obj == null || anchorRoot == null) return;
		
		// Store world position before re-parenting
		Vector3 worldPosition = obj.transform.position;
		Quaternion worldRotation = obj.transform.rotation;
		
		// Re-parent to anchor root (which is positioned at Image Target center)
		obj.transform.SetParent(anchorRoot, true);
		
		var photonView = obj.GetComponent<PhotonView>();
		bool isRemotePhotonObject = photonView != null && !photonView.IsMine;
		
		if (!isRemotePhotonObject)
		{
			// Convert world position to local position relative to Image Target center
			Vector3 localPos = anchorRoot.InverseTransformPoint(worldPosition);
			obj.transform.localPosition = localPos;
			
			// Convert world rotation to local rotation relative to Image Target center
			Quaternion localRot = Quaternion.Inverse(anchorRoot.rotation) * worldRotation;
			obj.transform.localRotation = localRot;
		}
		
                // Add PhotonView for individual object synchronization if not present
                EnsureObjectSynchronization(obj);
#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
                EnsurePlayerControllerCompatibility(obj);
#endif

                Debug.Log($"[AMOSession] [AUTOMATIC] Anchored {obj.name} to Image Target center");
        }
	
	/// <summary>
	/// Starts continuous synchronization to maintain consistent positions
	/// </summary>
        private void StartContinuousSync()
        {
                Debug.Log("[AMOSession] [AUTOMATIC] Starting continuous synchronization...");

                // Ensure all objects are properly anchored
                InvokeRepeating(nameof(EnsureObjectsAnchored), 1f, 2f);

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
                // Start periodic anchor synchronization to keep all players aligned
                InvokeRepeating(nameof(SendPeriodicAnchorSync), 3f, 5f);
#endif
        }
	
	/// <summary>
	/// Continuously ensures all objects remain properly anchored
	/// </summary>
	private void EnsureObjectsAnchored()
	{
		if (!IsAligned || anchorRoot == null) return;
		
		// Find all objects that should be anchored
		string[] objectNames = { "Cube", "Player", "VirtualObject", "ARObject" };
		foreach (string name in objectNames)
		{
			foreach (GameObject obj in FindObjectsByName(name))
			{
				if (obj != null && obj.transform.parent != anchorRoot)
				{
					Debug.Log($"[AMOSession] [AUTOMATIC] Re-anchoring {obj.name} to Image Target center");
					AnchorObjectToImageTarget(obj);
				}
			}
		}
		
                // Check objects by tags
                string[] tags = { "Player", "VirtualObject", "ARObject" };
                foreach (string tag in tags)
                {
                        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
                        foreach (GameObject obj in objects)
                        {
                                if (obj != null && obj.transform.parent != anchorRoot)
                                {
                                        Debug.Log($"[AMOSession] [AUTOMATIC] Re-anchoring {obj.name} to Image Target center");
                                        AnchorObjectToImageTarget(obj);
                                }
                        }
                }

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
                foreach (GameObject photonObject in FindPhotonNetworkObjects())
                {
                        if (photonObject != null && photonObject.transform.parent != anchorRoot)
                        {
                                Debug.Log($"[AMOSession] [AUTOMATIC] Re-anchoring networked object {photonObject.name} to Image Target center");
                                AnchorObjectToImageTarget(photonObject);
                        }
                }
#endif
	}

        private static IEnumerable<GameObject> FindObjectsByName(string targetName)
        {
                if (string.IsNullOrEmpty(targetName))
                        yield break;

		Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
		foreach (Transform transform in allTransforms)
		{
			if (transform == null)
				continue;

			GameObject gameObject = transform.gameObject;
			if (gameObject == null)
				continue;

			// Skip editor-only objects and assets
			if (!gameObject.scene.IsValid())
				continue;
			if (gameObject.hideFlags != HideFlags.None)
				continue;

			if (string.Equals(gameObject.name, targetName, StringComparison.Ordinal))
			{
				yield return gameObject;
			}
		}
        }

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
        private static IEnumerable<GameObject> FindPhotonNetworkObjects()
        {
                var photonViews = GameObject.FindObjectsOfType<PhotonView>();
                foreach (var view in photonViews)
                {
                        if (view == null)
                                continue;

                        var go = view.gameObject;
                        if (go == null)
                                continue;

                        // Ignore objects that are part of the AMO session system itself
                        if (go.GetComponent<AMOSessionManager>() != null)
                                continue;

                        // Skip the anchor root container
                        if (go.transform == AMOSessionManager.Instance?.anchorRoot)
                                continue;

                        if (!go.scene.IsValid())
                                continue;

                        yield return go;
                }
        }
#endif

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
	private void BroadcastAnchoredObjectStates()
	{
		if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
			return;

		var syncComponents = FindObjectsOfType<AMOObjectPositionSync>();
		foreach (var sync in syncComponents)
		{
			sync.BroadcastStateToOthers();
		}
	}

	private void SendAnchoredObjectStatesToPlayer(Player targetPlayer)
	{
		if (targetPlayer == null)
			return;

		var syncComponents = FindObjectsOfType<AMOObjectPositionSync>();
		foreach (var sync in syncComponents)
		{
			sync.SendStateToPlayer(targetPlayer);
		}
	}
#else
	private void BroadcastAnchoredObjectStates() { }
	private void SendAnchoredObjectStatesToPlayer(object _) { }
#endif
	
	/// <summary>
	/// Ensures individual objects have proper Photon synchronization
	/// </summary>
	private void EnsureObjectSynchronization(GameObject obj)
        {
#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
                var photonView = obj.GetComponent<PhotonView>();
                if (photonView == null)
		{
			photonView = obj.AddComponent<PhotonView>();
			photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
			Debug.Log($"[AMOSession] [AUTOMATIC] Added PhotonView to {obj.name} for synchronization");
		}
		
		// Ensure AMOObjectPositionSync exists and is registered with the PhotonView
		var positionSync = obj.GetComponent<AMOObjectPositionSync>();
		if (positionSync == null)
		{
			positionSync = obj.AddComponent<AMOObjectPositionSync>();
		}

		if (photonView.ObservedComponents == null)
		{
			photonView.ObservedComponents = new System.Collections.Generic.List<Component>();
		}
                if (!photonView.ObservedComponents.Contains(positionSync))
                {
                        photonView.ObservedComponents.Add(positionSync);
                }
                photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
#endif
        }

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
	private void EnsurePlayerControllerCompatibility(GameObject obj)
        {
                var photonView = obj.GetComponent<PhotonView>();
                if (photonView == null)
                        return;

                var playerControllerType = GetPlayerControllerType();
                if (playerControllerType == null)
                        return;

                var playerController = obj.GetComponent(playerControllerType);
                if (playerController == null)
                        return;

                if (photonView.ObservedComponents == null)
                {
                        photonView.ObservedComponents = new System.Collections.Generic.List<Component>();
                }

                if (photonView.ObservedComponents.Contains(playerController))
                {
                        photonView.ObservedComponents.Remove(playerController);
                        Debug.Log($"[AMOSession] [AUTOMATIC] Redirected PhotonView sync from PlayerController on {obj.name} to AMOObjectPositionSync");
                }

                var fixup = obj.GetComponent<AMOPlayerRuntimeFixup>();
                if (fixup == null)
                {
                        fixup = obj.AddComponent<AMOPlayerRuntimeFixup>();
                }

                fixup.Initialize(playerController);
        }
#endif

        /// <summary>
        /// Sends periodic anchor synchronization to keep all players aligned
        /// </summary>
        private void SendPeriodicAnchorSync()
        {
#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
                if (IsAligned && anchorRoot != null && PhotonNetwork.IsConnected && PhotonNetwork.InRoom && IsLocalAnchorAuthority())
                {
                        PhotonView photonView = GetOrCreatePhotonView();
                        photonView.RPC(nameof(RPC_SyncAnchorRoot), RpcTarget.All,
                                anchorRoot.position, anchorRoot.rotation);

			Debug.Log("[AMOSession] [AUTOMATIC] Sent periodic anchor sync to all players");
		}
#endif
	}

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
	/// <summary>
	/// Requests anchor position from other players when we join late
	/// </summary>
        private void RequestAnchorPositionFromOthers()
        {
                if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
                        return;

                PhotonView photonView = GetOrCreatePhotonView();
                Player master = PhotonNetwork.MasterClient;

                if (master != null && master.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                        photonView.RPC(nameof(RPC_RequestAnchorPosition), master,
                                PhotonNetwork.LocalPlayer.ActorNumber);
                        Debug.Log($"[AMOSession] [AUTOMATIC] Requesting anchor position from master client {master.ActorNumber}");
                }
                else
                {
                        photonView.RPC(nameof(RPC_RequestAnchorPosition), RpcTarget.OthersBuffered,
                                PhotonNetwork.LocalPlayer.ActorNumber);
                        Debug.Log("[AMOSession] [AUTOMATIC] Requesting anchor position from other players");
                }
        }

        [PunRPC]
        private void RPC_RequestAnchorPosition(int requesterActorNumber, PhotonMessageInfo _)
        {
                // Only respond if we're aligned and the requester is not us
                if (!IsLocalAnchorAuthority())
                        return;

                if (IsAligned && anchorRoot != null && requesterActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                        PhotonView photonView = GetOrCreatePhotonView();
                        photonView.RPC(nameof(RPC_SyncAnchorRoot), RpcTarget.All,
                                anchorRoot.position, anchorRoot.rotation);
			
			Debug.Log($"[AMOSession] [AUTOMATIC] Responded to anchor position request from player {requesterActorNumber}");
		}
	}

	/// <summary>
	/// Sends multiple sync updates to ensure all players receive the anchor position
	/// </summary>
        private System.Collections.IEnumerator SendMultipleSyncUpdates()
        {
                PhotonView photonView = GetOrCreatePhotonView();

                if (!IsLocalAnchorAuthority())
                        yield break;

                // Send multiple updates over time to ensure reliability
                for (int i = 0; i < 3; i++)
                {
                        yield return new WaitForSeconds(0.5f);

                        if (IsAligned && anchorRoot != null && IsLocalAnchorAuthority())
                        {
                                photonView.RPC(nameof(RPC_SyncAnchorRoot), RpcTarget.All,
                                        anchorRoot.position, anchorRoot.rotation);
                                Debug.Log($"[AMOSession] [AUTOMATIC] Sent sync update {i + 1}/3 to all players");
                        }
		}
	}

	/// <summary>
	/// Requests periodic synchronization to ensure all players stay aligned
	/// </summary>
        private void RequestPeriodicSync()
        {
                if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
                        return;

                if (IsLocalAnchorAuthority())
                {
                        if (IsAligned && anchorRoot != null)
                        {
                                PhotonView photonView = GetOrCreatePhotonView();
                                photonView.RPC(nameof(RPC_SyncAnchorRoot), RpcTarget.All,
                                        anchorRoot.position, anchorRoot.rotation);

                                Debug.Log("[AMOSession] [AUTOMATIC] Sent periodic sync to all players");
                        }
                }
                else
                {
                        RequestAnchorPositionFromOthers();
                }
        }

	/// <summary>
	/// Enhanced method to handle player joining with better synchronization
	/// </summary>
        public void HandlePlayerEnteredRoom(Player newPlayer)
        {
                Debug.Log($"[AMOSession] [AUTOMATIC] Player {newPlayer.ActorNumber} joined the room");

                if (IsAligned && anchorRoot != null && IsLocalAnchorAuthority())
                {
                        // Send anchor position multiple times to ensure the new player gets it
                        StartCoroutine(SendAnchorToNewPlayer(newPlayer));

                        Debug.Log($"[AMOSession] [AUTOMATIC] Sending anchor position to new player {newPlayer.ActorNumber}: {anchorRoot.position}");
                }
                else
                {
                        RequestAnchorPositionFromOthers();
                }
        }

	/// <summary>
	/// Sends anchor position to a new player multiple times for reliability
	/// </summary>
        private System.Collections.IEnumerator SendAnchorToNewPlayer(Player newPlayer)
        {
                PhotonView photonView = GetOrCreatePhotonView();

                // Send multiple times to ensure the new player receives it
                for (int i = 0; i < 5; i++)
                {
                        if (!IsLocalAnchorAuthority())
                                yield break;

                        if (IsAligned && anchorRoot != null)
                        {
                                photonView.RPC(nameof(RPC_SyncAnchorRoot), newPlayer,
                                        anchorRoot.position, anchorRoot.rotation);
                                SendAnchoredObjectStatesToPlayer(newPlayer);
                                Debug.Log($"[AMOSession] [AUTOMATIC] Sent anchor position to new player {newPlayer.ActorNumber} (attempt {i + 1}/5)");
                        }

                        yield return new WaitForSeconds(0.2f);
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

                                AnchorAllVirtualObjects();
                                StartContinuousSync();
                                EnsureAnchorVisualization();

                                awaitingAuthoritativeAnchor = false;
                                CompleteLocalAlignment(broadcastObjects: true);
                        }
                }
        }
#endif

	/// <summary>
	/// Toggle anchor center visualization on/off
	/// </summary>
	public void ToggleAnchorVisualization()
	{
		if (config != null)
		{
			config.showAnchorCenter = !config.showAnchorCenter;
			EnsureAnchorVisualization();
			Debug.Log($"[AMOSession] Anchor visualization {(config.showAnchorCenter ? "enabled" : "disabled")}");
		}
	}

	/// <summary>
	/// Set anchor center visualization visibility
	/// </summary>
	public void SetAnchorVisualization(bool visible)
	{
		if (config != null)
		{
			config.showAnchorCenter = visible;
			EnsureAnchorVisualization();
			Debug.Log($"[AMOSession] Anchor visualization {(visible ? "enabled" : "disabled")}");
		}
	}

	/// <summary>
	/// Draw anchor center visualization in Scene view
	/// </summary>
	private void OnDrawGizmos()
	{
		if (!config.showAnchorCenter || anchorRoot == null) return;

		// Set gizmo color
		Gizmos.color = config.anchorCenterColor;
		
		// Draw a sphere at the anchor center
		Gizmos.DrawSphere(anchorRoot.position, config.anchorCenterSize * 0.1f);
		
		// Draw coordinate axes
		float axisLength = config.anchorCenterSize * 0.3f;
		
		// X axis (red)
		Gizmos.color = Color.red;
		Gizmos.DrawLine(anchorRoot.position, anchorRoot.position + anchorRoot.right * axisLength);
		
		// Y axis (green)
		Gizmos.color = Color.green;
		Gizmos.DrawLine(anchorRoot.position, anchorRoot.position + anchorRoot.up * axisLength);
		
		// Z axis (blue)
		Gizmos.color = Color.blue;
		Gizmos.DrawLine(anchorRoot.position, anchorRoot.position + anchorRoot.forward * axisLength);
		
		// Draw a circle around the center
		Gizmos.color = config.anchorCenterColor;
		DrawCircle(anchorRoot.position, anchorRoot.up, config.anchorCenterSize * 0.2f);
	}

	/// <summary>
	/// Draw anchor center visualization when selected
	/// </summary>
	private void OnDrawGizmosSelected()
	{
		if (!config.showAnchorCenter || anchorRoot == null) return;

		// Draw additional visualization when selected
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(anchorRoot.position, config.anchorCenterSize * 0.15f);
	}

	/// <summary>
	/// Helper method to draw a circle using Gizmos
	/// </summary>
	private void DrawCircle(Vector3 center, Vector3 normal, float radius)
	{
		int segments = 32;
		float angleStep = 360f / segments;
		
		Vector3 previousPoint = center + Vector3.Cross(normal, Vector3.right).normalized * radius;
		
		for (int i = 1; i <= segments; i++)
		{
			float angle = i * angleStep * Mathf.Deg2Rad;
			Vector3 currentPoint = center + Vector3.Cross(normal, Vector3.right).normalized * radius * Mathf.Cos(angle) + 
								   Vector3.Cross(normal, Vector3.Cross(normal, Vector3.right)).normalized * radius * Mathf.Sin(angle);
			
			Gizmos.DrawLine(previousPoint, currentPoint);
			previousPoint = currentPoint;
		}
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


