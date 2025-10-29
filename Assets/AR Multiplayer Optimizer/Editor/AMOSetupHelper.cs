using UnityEngine;
using UnityEditor;

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

        GUILayout.Label("🚀 AUTOMATIC SETUP:", EditorStyles.boldLabel);
        GUILayout.Label("1. Add AMOAutoBoot script to any GameObject");
        GUILayout.Label("2. That's it! Everything else is automatic");
        GUILayout.Space(20);
        
        GUILayout.Label("📋 What's Automatic:", EditorStyles.boldLabel);
        GUILayout.Label("✅ AnchorRoot creation");
        GUILayout.Label("✅ AMOAnchorTracker setup");
        GUILayout.Label("✅ Vuforia integration");
        GUILayout.Label("✅ Position synchronization");
        GUILayout.Label("✅ PhotonView configuration");
        GUILayout.Label("✅ Virtual object anchoring");
        GUILayout.Label("✅ Image Target center reference");
        
        GUILayout.Space(20);
        
        GUILayout.Label("🎯 DEBUG VISUALIZATION:", EditorStyles.boldLabel);
        
        // Anchor visualization toggle
        var sessionManager = FindObjectOfType<AMOSessionManager>();
        if (sessionManager != null)
        {
            var config = AMOResources.LoadOrCreateConfig();
            bool showAnchor = config.showAnchorCenter;
            
            bool newShowAnchor = GUILayout.Toggle(showAnchor, "Show Anchor Center Point");
            if (newShowAnchor != showAnchor)
            {
                config.showAnchorCenter = newShowAnchor;
                sessionManager.SetAnchorVisualization(newShowAnchor);
                EditorUtility.SetDirty(config);
            }
            
            if (showAnchor)
            {
                GUILayout.Label($"   Size: {config.anchorCenterSize:F1}");
                GUILayout.Label($"   Color: {config.anchorCenterColor}");
                GUILayout.Label("   • Shows red sphere with coordinate axes");
                GUILayout.Label("   • Visible in both Editor and Build");
                GUILayout.Label("   • Helps debug synchronization issues");
            }
        }
        else
        {
            GUILayout.Label("⚠️ AMOSessionManager not found in scene");
            GUILayout.Label("   Add AMOAutoBoot script to enable visualization");
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("🔒 POSITION STABILIZATION:", EditorStyles.boldLabel);
        
        if (sessionManager != null)
        {
            var config = AMOResources.LoadOrCreateConfig();
            bool stabilizationEnabled = config.enablePositionStabilization;
            
            bool newStabilization = GUILayout.Toggle(stabilizationEnabled, "Enable Position Stabilization");
            if (newStabilization != stabilizationEnabled)
            {
                config.enablePositionStabilization = newStabilization;
                
                // Get the anchor tracker and update stabilization
                var anchorTracker = sessionManager.GetComponent<AMOAnchorTracker>();
                if (anchorTracker != null)
                {
                    anchorTracker.SetStabilization(newStabilization);
                }
                
                EditorUtility.SetDirty(config);
            }
            
            if (stabilizationEnabled)
            {
                GUILayout.Label($"   Update Rate: {config.stabilizationUpdateRate:F2}s");
                GUILayout.Label($"   Smoothing: {config.stabilizationSmoothing:F1}");
                GUILayout.Label($"   Max Drift: {config.maxAnchorDrift:F1}m");
                GUILayout.Label("   • Prevents objects from drifting when phone moves");
                GUILayout.Label("   • Continuously tracks Image Target position");
                GUILayout.Label("   • Smooth interpolation prevents jitter");
            }
        }
        else
        {
            GUILayout.Label("⚠️ AMOSessionManager not found in scene");
            GUILayout.Label("   Add AMOAutoBoot script to enable stabilization");
        }
    }

}
