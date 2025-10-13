using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

/// <summary>
/// Ensures minimal setup when the package is imported or the editor opens.
/// - Creates a default AMOConfig in Resources if missing.
/// - Attempts to ensure Vuforia sample dependency flow is triggered (if their helper exists).
/// </summary>
public static class AMOAutoFixOnImport
{
	private const string ResourcesFolder = "Assets/Resources";
	private const string ConfigAssetPath = ResourcesFolder + "/AMOConfig.asset";

	[InitializeOnLoadMethod]
	private static void OnEditorLoad()
	{
		EditorApplication.delayCall += EnsureBaselineAssets;
	}

	[DidReloadScripts]
	private static void OnScriptsReloaded()
	{
		EnsureBaselineAssets();
	}

	public static void RunOnceNow()
	{
		EnsureBaselineAssets();
	}

	private static void EnsureBaselineAssets()
	{
		if (!AssetDatabase.IsValidFolder(ResourcesFolder))
		{
			Directory.CreateDirectory(ResourcesFolder);
			AssetDatabase.Refresh();
		}

		var config = AssetDatabase.LoadAssetAtPath<AMOConfig>(ConfigAssetPath);
		if (config == null)
		{
			config = ScriptableObject.CreateInstance<AMOConfig>();
			AssetDatabase.CreateAsset(config, ConfigAssetPath);
			AssetDatabase.SaveAssets();
		}

		// If Vuforia's migration helper exists, run its dependency resolver silently to avoid dialogs.
		var type = System.Type.GetType("AddVuforiaEnginePackage, Assembly-CSharp-Editor");
		if (type != null)
		{
			var method = type.GetMethod("ResolveDependenciesSilent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			method?.Invoke(null, null);
		}
	}
}


