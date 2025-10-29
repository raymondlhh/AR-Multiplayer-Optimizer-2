using UnityEditor;
using UnityEngine;

/// <summary>
/// Simple setup/validation window for one-click Fix Anchors.
/// </summary>
public class AMOSetupWindow : EditorWindow
{
	private AMOConfig config;

	[MenuItem("Tools/AR Multiplayer Optimizer/Setup & Validate")] 
	public static void ShowWindow()
	{
		var wnd = GetWindow<AMOSetupWindow>(true, "AR Multiplayer Optimizer Setup", true);
		wnd.minSize = new Vector2(420, 260);
		wnd.Show();
	}

	private void OnEnable()
	{
		config = AMOResources.LoadOrCreateConfig();
	}

	private void OnGUI()
	{
		EditorGUILayout.LabelField("Auto-Fix", EditorStyles.boldLabel);
		config.autoFixOnPlay = EditorGUILayout.Toggle(new GUIContent("Auto-Fix On Play"), config.autoFixOnPlay);
		config.imageTargetName = EditorGUILayout.TextField(new GUIContent("ImageTarget Name (optional)"), config.imageTargetName);
		config.anchorRootName = EditorGUILayout.TextField(new GUIContent("Anchor Root Name"), string.IsNullOrWhiteSpace(config.anchorRootName) ? "AnchorRoot" : config.anchorRootName);
		config.waitForAllClients = EditorGUILayout.Toggle(new GUIContent("PUN2 Everyone Ready Gate"), config.waitForAllClients);
		config.alignSmoothing = EditorGUILayout.Slider(new GUIContent("Align Smoothing"), config.alignSmoothing, 0f, 1f);

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Debug Visualization", EditorStyles.boldLabel);
		config.showAnchorCenter = EditorGUILayout.Toggle(new GUIContent("Show Anchor Center"), config.showAnchorCenter);
		
		if (config.showAnchorCenter)
		{
			config.anchorCenterSize = EditorGUILayout.Slider(new GUIContent("Anchor Center Size"), config.anchorCenterSize, 0.1f, 2.0f);
			config.anchorCenterColor = EditorGUILayout.ColorField(new GUIContent("Anchor Center Color"), config.anchorCenterColor);
		}

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Position Stabilization", EditorStyles.boldLabel);
		config.enablePositionStabilization = EditorGUILayout.Toggle(new GUIContent("Enable Position Stabilization"), config.enablePositionStabilization);
		
		if (config.enablePositionStabilization)
		{
			config.stabilizationUpdateRate = EditorGUILayout.Slider(new GUIContent("Update Rate (seconds)"), config.stabilizationUpdateRate, 0.01f, 1.0f);
			config.stabilizationSmoothing = EditorGUILayout.Slider(new GUIContent("Smoothing Factor"), config.stabilizationSmoothing, 0.1f, 10.0f);
			config.maxAnchorDrift = EditorGUILayout.Slider(new GUIContent("Max Drift Distance (meters)"), config.maxAnchorDrift, 0.1f, 2.0f);
		}

		EditorGUILayout.Space();
		if (GUILayout.Button("Validate & Fix Now"))
		{
			AMOAutoFixOnImport.RunOnceNow();
			AssetDatabase.SaveAssets();
			EditorUtility.DisplayDialog("AR Multiplayer Optimizer", "Validation complete. Config saved.", "OK");
		}

		if (GUI.changed)
		{
			EditorUtility.SetDirty(config);
		}
	}
}


