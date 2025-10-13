using UnityEngine;

/// <summary>
/// Auto-creates a persistent AMOSessionManager at runtime if one is not present.
/// Attach to any scene object or include via a bootstrap prefab.
/// </summary>
public class AMOAutoBoot : MonoBehaviour
{
	[SerializeField]
	private AMOConfig config;

	private void Awake()
	{
		if (AMOSessionManager.Instance != null)
			return;

		var go = new GameObject("AMOSessionManager");
		var session = go.AddComponent<AMOSessionManager>();
		if (config == null)
			config = AMOResources.LoadOrCreateConfig();

		// Wire config by serializing through inspector or default loader
		var field = typeof(AMOSessionManager).GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (field != null)
			field.SetValue(session, config);
	}
}


