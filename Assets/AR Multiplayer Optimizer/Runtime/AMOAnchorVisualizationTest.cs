using UnityEngine;

/// <summary>
/// Simple test script to demonstrate anchor center visualization
/// Attach this to any GameObject to test the anchor visualization feature
/// </summary>
public class AMOAnchorVisualizationTest : MonoBehaviour
{
    [Header("Test Controls")]
    [Tooltip("Press this key to toggle anchor visualization")]
    public KeyCode toggleKey = KeyCode.V;
    
    [Tooltip("Press this key to cycle through different colors")]
    public KeyCode colorCycleKey = KeyCode.C;
    
    [Tooltip("Press this key to toggle position stabilization")]
    public KeyCode stabilizationToggleKey = KeyCode.S;
    
    private AMOSessionManager sessionManager;
    private AMOConfig config;
    private Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };
    private int currentColorIndex = 0;

    private void Start()
    {
        // Find the session manager
        sessionManager = FindObjectOfType<AMOSessionManager>();
        if (sessionManager == null)
        {
            Debug.LogWarning("[AMOAnchorVisualizationTest] AMOSessionManager not found. Make sure AMOAutoBoot is attached to a GameObject.");
            enabled = false;
            return;
        }

        // Get the config
        config = AMOResources.LoadOrCreateConfig();
        Debug.Log("[AMOAnchorVisualizationTest] Test script ready. Press V to toggle visualization, C to cycle colors, S to toggle stabilization.");
    }

    private void Update()
    {
        if (sessionManager == null || config == null) return;

        // Toggle visualization
        if (Input.GetKeyDown(toggleKey))
        {
            sessionManager.ToggleAnchorVisualization();
            Debug.Log($"[AMOAnchorVisualizationTest] Anchor visualization toggled. Now: {config.showAnchorCenter}");
        }

        // Cycle colors
        if (Input.GetKeyDown(colorCycleKey))
        {
            currentColorIndex = (currentColorIndex + 1) % colors.Length;
            config.anchorCenterColor = colors[currentColorIndex];
            sessionManager.SetAnchorVisualization(config.showAnchorCenter); // Refresh visualization
            Debug.Log($"[AMOAnchorVisualizationTest] Color changed to: {colors[currentColorIndex]}");
        }

        // Toggle stabilization
        if (Input.GetKeyDown(stabilizationToggleKey))
        {
            config.enablePositionStabilization = !config.enablePositionStabilization;
            
            // Get the anchor tracker and update stabilization
            var anchorTracker = sessionManager.GetComponent<AMOAnchorTracker>();
            if (anchorTracker != null)
            {
                anchorTracker.SetStabilization(config.enablePositionStabilization);
            }
            
            Debug.Log($"[AMOAnchorVisualizationTest] Position stabilization {(config.enablePositionStabilization ? "enabled" : "disabled")}");
        }
    }

    private void OnGUI()
    {
        if (sessionManager == null || config == null) return;

        // Create a simple GUI overlay
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("🎯 Anchor Visualization Test", GUI.skin.label);
        GUILayout.Label($"Press {toggleKey} to toggle visualization");
        GUILayout.Label($"Press {colorCycleKey} to cycle colors");
        GUILayout.Label($"Press {stabilizationToggleKey} to toggle stabilization");
        GUILayout.Space(10);
        
        GUILayout.Label($"Visualization: {(config.showAnchorCenter ? "ON" : "OFF")}");
        GUILayout.Label($"Color: {config.anchorCenterColor}");
        GUILayout.Label($"Size: {config.anchorCenterSize:F1}");
        GUILayout.Label($"Stabilization: {(config.enablePositionStabilization ? "ON" : "OFF")}");
        GUILayout.Label($"Update Rate: {config.stabilizationUpdateRate:F2}s");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}

