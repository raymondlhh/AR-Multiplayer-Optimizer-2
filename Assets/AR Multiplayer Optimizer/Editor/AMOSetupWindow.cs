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


