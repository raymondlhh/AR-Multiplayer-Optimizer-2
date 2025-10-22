using UnityEngine;
using UnityEditor;

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

/// <summary>
/// Helper script to automatically configure AR Multiplayer Optimizer settings
/// </summary>
public class AMOSetupHelper : EditorWindow
{
    [MenuItem("AR Multiplayer Optimizer/Setup Helper")]
    public static void ShowWindow()
    {
        GetWindow<AMOSetupHelper>("AR Multiplayer Optimizer Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("AR Multiplayer Optimizer Setup Helper", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("🚀 AUTOMATIC SETUP (Recommended):", EditorStyles.boldLabel);
        GUILayout.Label("1. Add AMOAutoBoot script to any GameObject");
        GUILayout.Label("2. That's it! Everything else is automatic");
        GUILayout.Space(10);

        GUILayout.Label("🔧 MANUAL SETUP (If needed):", EditorStyles.boldLabel);
        
        GUILayout.Label("1. Create AMOConfig asset:");
        if (GUILayout.Button("Create AMOConfig"))
        {
            CreateAMOConfig();
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("2. Setup AMOSessionManager:");
        if (GUILayout.Button("Setup AMOSessionManager"))
        {
            SetupAMOSessionManager();
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("3. Configure Vuforia Image Target:");
        GUILayout.Label("   - Set the Image Target name in AMOConfig");
        GUILayout.Label("   - Ensure your Vuforia Image Target is named correctly");
        
        GUILayout.Space(10);
        
        GUILayout.Label("4. Test the setup:");
        if (GUILayout.Button("Test Configuration"))
        {
            TestConfiguration();
        }

        GUILayout.Space(20);
        GUILayout.Label("📋 What's Automatic:", EditorStyles.boldLabel);
        GUILayout.Label("✅ AnchorRoot creation");
        GUILayout.Label("✅ AMOAnchorTracker setup");
        GUILayout.Label("✅ Vuforia integration");
        GUILayout.Label("✅ Position synchronization");
        GUILayout.Label("✅ PhotonView configuration");
        GUILayout.Label("✅ Virtual object anchoring");
        GUILayout.Label("✅ Image Target center reference");
    }

    private void CreateAMOConfig()
    {
        // Create AMOConfig asset
        var config = ScriptableObject.CreateInstance<AMOConfig>();
        config.autoFixOnPlay = true;
        config.imageTargetName = "ARMascot"; // Default from your project
        config.anchorRootName = "AnchorRoot";
        config.waitForAllClients = true;
        config.alignSmoothing = 0.2f;

        string path = "Assets/Resources/AMOConfig.asset";
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("AMOConfig created at: " + path);
    }

    private void SetupAMOSessionManager()
    {
        // Find or create AMOSessionManager
        var sessionManager = FindObjectOfType<AMOSessionManager>();
        if (sessionManager == null)
        {
            var go = new GameObject("AMOSessionManager");
            sessionManager = go.AddComponent<AMOSessionManager>();
        }

        // Load the config
        var config = Resources.Load<AMOConfig>("AMOConfig");
        if (config == null)
        {
            Debug.LogWarning("AMOConfig not found. Please create it first.");
            return;
        }

        // Set the config using reflection
        var configField = typeof(AMOSessionManager).GetField("config", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (configField != null)
        {
            configField.SetValue(sessionManager, config);
        }

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
        // Add PhotonView if not present
        var photonView = sessionManager.GetComponent<PhotonView>();
        if (photonView == null)
        {
            photonView = sessionManager.gameObject.AddComponent<PhotonView>();
        }

        // Configure PhotonView
        photonView.ObservedComponents.Add(sessionManager);
        photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
#else
        Debug.LogWarning("Photon Unity Networking not found. PhotonView configuration skipped.");
#endif

        Debug.Log("AMOSessionManager configured successfully!");
    }

    private void TestConfiguration()
    {
        var sessionManager = FindObjectOfType<AMOSessionManager>();
        var config = Resources.Load<AMOConfig>("AMOConfig");
        var networkManager = FindObjectOfType<NetworkManager>();

        if (sessionManager == null)
        {
            Debug.LogError("❌ AMOSessionManager not found in scene!");
        }
        else
        {
            Debug.Log("✅ AMOSessionManager found");
        }

        if (config == null)
        {
            Debug.LogError("❌ AMOConfig not found in Resources folder!");
        }
        else
        {
            Debug.Log("✅ AMOConfig found");
            Debug.Log($"   - Image Target Name: {config.imageTargetName}");
            Debug.Log($"   - Anchor Root Name: {config.anchorRootName}");
            Debug.Log($"   - Wait for All Clients: {config.waitForAllClients}");
        }

        if (networkManager == null)
        {
            Debug.LogError("❌ NetworkManager not found in scene!");
        }
        else
        {
            Debug.Log("✅ NetworkManager found");
        }

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
        var photonView = sessionManager?.GetComponent<PhotonView>();
        if (photonView == null)
        {
            Debug.LogError("❌ PhotonView not found on AMOSessionManager!");
        }
        else
        {
            Debug.Log("✅ PhotonView configured");
        }
#else
        Debug.LogWarning("⚠️ Photon Unity Networking not available - PhotonView check skipped");
#endif
    }
}
