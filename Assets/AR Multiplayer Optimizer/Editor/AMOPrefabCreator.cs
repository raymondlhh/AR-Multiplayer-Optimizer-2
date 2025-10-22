using UnityEngine;
using UnityEditor;

/// <summary>
/// [AUTOMATIC] Creates a ready-to-use AR Multiplayer Optimizer prefab
/// </summary>
public class AMOPrefabCreator
{
    [MenuItem("AR Multiplayer Optimizer/Create Auto-Setup Prefab")]
    public static void CreateAutoSetupPrefab()
    {
        // Create the main GameObject
        GameObject amoPrefab = new GameObject("AR_Multiplayer_Optimizer_AutoSetup");
        
        // Add AMOAutoBoot component
        amoPrefab.AddComponent<AMOAutoBoot>();
        
        // Create the prefab
        string prefabPath = "Assets/AR Multiplayer Optimizer/AR_Multiplayer_Optimizer_AutoSetup.prefab";
        
        // Ensure the directory exists
        string directory = System.IO.Path.GetDirectoryName(prefabPath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        
        // Create the prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(amoPrefab, prefabPath);
        
        // Clean up the temporary GameObject
        Object.DestroyImmediate(amoPrefab);
        
        // Select the created prefab
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        
        Debug.Log($"[AMOPrefabCreator] [AUTOMATIC] Created auto-setup prefab at: {prefabPath}");
        Debug.Log("[AMOPrefabCreator] [AUTOMATIC] Just drag this prefab into your scene - no other setup needed!");
    }
}
