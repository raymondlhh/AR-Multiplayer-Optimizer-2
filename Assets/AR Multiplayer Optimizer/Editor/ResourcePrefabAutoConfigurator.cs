using UnityEditor;
using UnityEngine;

namespace AR.Multiplayer.Optimizer.Editor
{
    /// <summary>
    /// Automatically ensures that all prefabs under Assets/Resources include the
    /// <see cref="AR.Multiplayer.Optimizer.ARNetworkedObjectBinder"/> component so that
    /// networked instances are parented beneath the aligned origin.
    /// </summary>
    [InitializeOnLoad]
    internal static class ResourcePrefabAutoConfigurator
    {
        static ResourcePrefabAutoConfigurator()
        {
            EditorApplication.delayCall += EnsurePrefabsConfigured;
        }

        public static void EnsurePrefabsConfigured()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                return;
            }

            int configuredCount = ApplyBinderToResources();
            if (configuredCount > 0)
            {
                Debug.Log($"[AR Multiplayer Optimizer] Added ARNetworkedObjectBinder to {configuredCount} Resources prefab(s).");
            }
        }

        internal static int ApplyBinderToResources()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources" });
            int configured = 0;

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (EnsureBinderOnPrefab(assetPath))
                {
                    configured++;
                }
            }

            if (configured > 0)
            {
                AssetDatabase.SaveAssets();
            }

            return configured;
        }

        private static bool EnsureBinderOnPrefab(string assetPath)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
            if (prefabRoot == null)
            {
                return false;
            }

            bool modified = false;

            try
            {
                if (prefabRoot.GetComponent<ARNetworkedObjectBinder>() == null)
                {
                    prefabRoot.AddComponent<ARNetworkedObjectBinder>();
                    modified = true;
                }
            }
            finally
            {
                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                }

                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            return modified;
        }
    }
}
