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
		IsAligned = true;
		
		// Automatically anchor all virtual objects to Image Target center
		AnchorAllVirtualObjects();
		
		// Start continuous synchronization
		StartContinuousSync();
		
		// Update visualization when aligned
		EnsureAnchorVisualization();

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
		// Mark self ready and check if all are ready.
		if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
		{
			alignedActors.Add(PhotonNetwork.LocalPlayer.ActorNumber);
			PhotonView photonView = GetOrCreatePhotonView();
			
			// Send anchor root position and rotation to all other clients
			photonView.RPC(nameof(RPC_SyncAnchorRoot), RpcTarget.OthersBuffered, 
				anchorRoot.position, anchorRoot.rotation);
			
			// Also broadcast to all players to ensure everyone gets the update
			photonView.RPC(nameof(RPC_SyncAnchorRoot), RpcTarget.All, 
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
		// Apply anchor root sync if we haven't aligned locally yet OR if this is a better position
		if (anchorRoot != null)
		{
			// If we're not aligned yet, use this position
			if (!IsAligned)
			{
				Debug.Log($"[AMOSession] Syncing anchor root from remote client: {position}");
				anchorRoot.SetPositionAndRotation(position, rotation);
				
				// Mark as aligned since we received the anchor position
				IsAligned = true;
				
				// Automatically anchor all virtual objects to Image Target center
				AnchorAllVirtualObjects();
				
				// Start continuous synchronization
				StartContinuousSync();
				
				alignedActors.Add(PhotonNetwork.LocalPlayer.ActorNumber);
				CheckAllReady();
			}
			else
			{
				// If we're already aligned, update our position to match the remote client
				Debug.Log($"[AMOSession] Updating anchor root to match remote client: {position}");
				anchorRoot.SetPositionAndRotation(position, rotation);
				
				// Re-anchor all objects to the new position
				AnchorAllVirtualObjects();
			}
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
	
	/// <summary>
	/// [AUTOMATIC] Anchors all virtual objects to the Image Target center for proper synchronization
	/// </summary>
	private void AnchorAllVirtualObjects()
	{
		Debug.Log("[AMOSession] [AUTOMATIC] Anchoring all virtual objects to Image Target center...");
		
		// Find and anchor all objects with common virtual object names
		string[] objectNames = { "Cube", "Player", "VirtualObject", "ARObject" };
		foreach (string name in objectNames)
		{
			GameObject obj = GameObject.Find(name);
			if (obj != null)
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
		
		// Convert world position to local position relative to Image Target center
		Vector3 localPos = anchorRoot.InverseTransformPoint(worldPosition);
		obj.transform.localPosition = localPos;
		
		// Convert world rotation to local rotation relative to Image Target center
		Quaternion localRot = Quaternion.Inverse(anchorRoot.rotation) * worldRotation;
		obj.transform.localRotation = localRot;
		
		// Add PhotonView for individual object synchronization if not present
		EnsureObjectSynchronization(obj);
		
		Debug.Log($"[AMOSession] [AUTOMATIC] Anchored {obj.name} to Image Target center at local position {localPos}");
	}
	
	/// <summary>
	/// Starts continuous synchronization to maintain consistent positions
	/// </summary>
	private void StartContinuousSync()
	{
		Debug.Log("[AMOSession] [AUTOMATIC] Starting continuous synchronization...");
		
		// Ensure all objects are properly anchored
		InvokeRepeating(nameof(EnsureObjectsAnchored), 1f, 2f);
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
			GameObject obj = GameObject.Find(name);
			if (obj != null && obj.transform.parent != anchorRoot)
			{
				Debug.Log($"[AMOSession] [AUTOMATIC] Re-anchoring {obj.name} to Image Target center");
				AnchorObjectToImageTarget(obj);
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
	}
	
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
		
		// Add a simple position synchronizer if not present
		var componentType = System.Type.GetType("AMOObjectPositionSync");
		if (componentType != null)
		{
			var positionSync = obj.GetComponent(componentType);
			if (positionSync == null)
			{
				obj.AddComponent(componentType);
			}
		}
#endif
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
			
			Debug.Log($"[AMOSession] [AUTOMATIC] Sent anchor position to new player {newPlayer.ActorNumber}: {anchorRoot.position}");
		}
		else if (!IsAligned)
		{
			// If we're not aligned yet, request anchor position from other players
			RequestAnchorPositionFromOthers();
		}
	}
	
	/// <summary>
	/// Requests anchor position from other players when we join late
	/// </summary>
	private void RequestAnchorPositionFromOthers()
	{
		PhotonView photonView = GetOrCreatePhotonView();
		photonView.RPC(nameof(RPC_RequestAnchorPosition), RpcTarget.OthersBuffered, 
			PhotonNetwork.LocalPlayer.ActorNumber);
		
		Debug.Log("[AMOSession] [AUTOMATIC] Requesting anchor position from other players");
	}
	
	[PunRPC]
	private void RPC_RequestAnchorPosition(int requesterActorNumber, PhotonMessageInfo _)
	{
		// Only respond if we're aligned and the requester is not us
		if (IsAligned && anchorRoot != null && requesterActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
		{
			PhotonView photonView = GetOrCreatePhotonView();
			photonView.RPC(nameof(RPC_SyncAnchorRoot), RpcTarget.All, 
				anchorRoot.position, anchorRoot.rotation);
			
			Debug.Log($"[AMOSession] [AUTOMATIC] Responded to anchor position request from player {requesterActorNumber}");
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


