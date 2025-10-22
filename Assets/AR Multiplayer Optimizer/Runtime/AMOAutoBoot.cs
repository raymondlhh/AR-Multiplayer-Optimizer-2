using UnityEngine;

/// <summary>
/// [AUTOMATIC] Auto-creates a persistent AMOSessionManager at runtime if one is not present.
/// - Automatically creates AMOSessionManager if none exists
/// - Automatically loads or creates AMOConfig if missing
/// - No manual setup required - just attach to any GameObject
/// </summary>
public class AMOAutoBoot : MonoBehaviour
{
	[SerializeField]
	private AMOConfig config;

	private void Awake()
	{
		if (AMOSessionManager.Instance != null)
		{
			Debug.Log("[AMOAutoBoot] [AUTOMATIC] AMOSessionManager already exists, skipping creation");
			return;
		}

		Debug.Log("[AMOAutoBoot] [AUTOMATIC] Creating AMOSessionManager...");
		var go = new GameObject("AMOSessionManager");
		var session = go.AddComponent<AMOSessionManager>();
		
		if (config == null)
		{
			Debug.Log("[AMOAutoBoot] [AUTOMATIC] Loading or creating AMOConfig...");
			config = AMOResources.LoadOrCreateConfig();
		}

		// Wire config by serializing through inspector or default loader
		var field = typeof(AMOSessionManager).GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (field != null)
		{
			field.SetValue(session, config);
			Debug.Log("[AMOAutoBoot] [AUTOMATIC] AMOSessionManager configured successfully with license key preserved");
		}
		else
		{
			Debug.LogWarning("[AMOAutoBoot] Could not set config field - using default config");
		}
	}
}


