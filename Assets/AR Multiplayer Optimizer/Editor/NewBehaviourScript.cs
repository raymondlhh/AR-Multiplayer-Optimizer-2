using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AR.Multiplayer.Optimizer.Editor
{
    /// <summary>
    /// Provides a central location for synchronisation tuning of multiplayer AR experiences.
    /// The tool scans the active scene for networking related behaviours and applies
    /// common optimisation settings such as send rates, batching and prediction toggles.
    /// </summary>
    public class ArMultiplayerOptimizerWindow : EditorWindow
    {
        private const string EditorPrefsPrefix = "AR.Multiplayer.Optimizer.";

        private readonly List<Component> collectedComponents = new List<Component>();
        private readonly List<string> logEntries = new List<string>();

        private Vector2 componentScrollPosition;
        private Vector2 logScrollPosition;

        private float networkUpdateRate = 20f;
        private bool autoCollectOnSceneChange = true;
        private bool includeInactiveObjects = false;
        private bool enableObjectBatching = true;
        private int batchSize = 8;
        private bool enablePrediction = true;
        private bool enableDiagnostics = true;
        private bool showDiagnostics = false;
        private bool autoApplyOnCollection = false;

        private static readonly string[] UpdateRateMemberNames =
        {
            "NetworkUpdateRate", "NetworkSendRate", "SendRate", "UpdateRate",
            "SynchronizationRate", "UpdatesPerSecond", "TickRate"
        };

        private static readonly string[] PredictionMemberNames =
        {
            "EnablePrediction", "UsePrediction", "PredictionEnabled", "IsPredictionEnabled"
        };

        private static readonly string[] BatchingMemberNames =
        {
            "EnableBatching", "UseBatching", "BatchingEnabled", "SupportsBatching"
        };

        private static readonly string[] BatchSizeMemberNames =
        {
            "BatchSize", "ObjectsPerBatch", "MaxBatchSize"
        };

        [MenuItem("AR Tools/AR Multiplayer Optimizer")] // Adds entry to the Unity Editor menu.
        public static void ShowWindow()
        {
            var window = GetWindow<ArMultiplayerOptimizerWindow>(false, "AR Multiplayer Optimizer", true);
            window.minSize = new Vector2(420f, 480f);
            window.Focus();
        }

        private void OnEnable()
        {
            LoadEditorPreferences();
            EditorSceneManager.sceneOpened += HandleSceneOpened;
            if (autoCollectOnSceneChange)
            {
                CollectRelevantObjects();
            }
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= HandleSceneOpened;
            SaveEditorPreferences();
        }

        private void HandleSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            if (!autoCollectOnSceneChange)
            {
                return;
            }

            LogInfo($"Scene '{scene.name}' opened. Refreshing AR multiplayer objects.");
            CollectRelevantObjects();
            if (autoApplyOnCollection)
            {
                ApplyOptimisationSettings();
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawSettings();
            DrawComponentList();
            DrawFooterControls();
            DrawDiagnostics();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AR Multiplayer Synchronisation Optimizer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use this tool to standardise synchronisation behaviour across your AR multiplayer objects. " +
                "Collect objects from the active scene, adjust their network settings, and push the optimisations " +
                "with a single click. Tooltips on each option explain how they influence the final gameplay.",
                MessageType.Info);
        }

        private void DrawSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Optimisation Settings", EditorStyles.boldLabel);

            networkUpdateRate = EditorGUILayout.Slider(
                new GUIContent("Network Update Rate (Hz)", "Target frequency for state replication across the network."),
                networkUpdateRate, 1f, 120f);

            enableObjectBatching = EditorGUILayout.Toggle(
                new GUIContent("Enable Object Batching", "Combine multiple object synchronisation payloads together to minimise bandwidth."),
                enableObjectBatching);

            using (new EditorGUI.DisabledScope(!enableObjectBatching))
            {
                batchSize = EditorGUILayout.IntSlider(
                    new GUIContent("Batch Size", "Maximum number of object states to bundle per network message."),
                    batchSize, 1, 32);
            }

            enablePrediction = EditorGUILayout.Toggle(
                new GUIContent("Enable Client Prediction", "Toggle prediction/interpolation systems to mask latency."),
                enablePrediction);

            enableDiagnostics = EditorGUILayout.Toggle(
                new GUIContent("Enable Diagnostics", "Automatically enable verbose diagnostics on components when supported."),
                enableDiagnostics);

            includeInactiveObjects = EditorGUILayout.Toggle(
                new GUIContent("Include Inactive Objects", "Include disabled objects when collecting targets."),
                includeInactiveObjects);

            autoCollectOnSceneChange = EditorGUILayout.Toggle(
                new GUIContent("Auto Collect On Scene Change", "Refresh the object list whenever a scene is opened."),
                autoCollectOnSceneChange);

            autoApplyOnCollection = EditorGUILayout.Toggle(
                new GUIContent("Auto Apply After Collection", "Immediately apply the optimisation settings after collecting objects."),
                autoApplyOnCollection);
        }

        private void DrawComponentList()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Discovered Multiplayer Objects", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The optimiser looks for components that appear to drive networked behaviour (PhotonView, Netcode NetworkObject, custom 'Network' scripts, etc.). " +
                "Review the list before applying changes to ensure all relevant objects are included.", MessageType.None);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                componentScrollPosition = EditorGUILayout.BeginScrollView(componentScrollPosition, GUILayout.Height(160));

                if (collectedComponents.Count == 0)
                {
                    EditorGUILayout.LabelField("No AR multiplayer objects found. Collect objects to populate this list.");
                }
                else
                {
                    foreach (var component in collectedComponents)
                    {
                        if (component == null)
                        {
                            continue;
                        }

                        var componentName = component.GetType().Name;
                        var go = component.gameObject;
                        var description = $"{componentName} on '{go.name}'";
                        EditorGUILayout.LabelField(new GUIContent(description, "Click to ping the object in the hierarchy."));

                        var lastRect = GUILayoutUtility.GetLastRect();
                        if (Event.current.type == EventType.MouseUp && lastRect.Contains(Event.current.mousePosition))
                        {
                            EditorGUIUtility.PingObject(go);
                            Event.current.Use();
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawFooterControls()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Collect Objects", "Scan the current scene for AR multiplayer components.")))
                {
                    CollectRelevantObjects();
                    if (autoApplyOnCollection)
                    {
                        ApplyOptimisationSettings();
                    }
                }

                EditorGUI.BeginDisabledGroup(collectedComponents.Count == 0);
                if (GUILayout.Button(new GUIContent("Apply Optimisation", "Push the selected optimisation options to collected components.")))
                {
                    ApplyOptimisationSettings();
                }

                if (GUILayout.Button(new GUIContent("Clear", "Clear the collected objects and diagnostic log.")))
                {
                    collectedComponents.Clear();
                    logEntries.Clear();
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawDiagnostics()
        {
            EditorGUILayout.Space();
            showDiagnostics = EditorGUILayout.Foldout(showDiagnostics, new GUIContent("Diagnostics & Log", "Review the actions performed by the optimiser."));
            if (!showDiagnostics)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.Height(120));
                if (logEntries.Count == 0)
                {
                    EditorGUILayout.LabelField("No diagnostic messages yet.");
                }
                else
                {
                    foreach (var entry in logEntries)
                    {
                        EditorGUILayout.LabelField(entry, EditorStyles.wordWrappedMiniLabel);
                    }
                }

                EditorGUILayout.EndScrollView();

                if (GUILayout.Button(new GUIContent("Copy Log To Clipboard", "Copies the diagnostic log for pasting into bug reports or documentation.")))
                {
                    EditorGUIUtility.systemCopyBuffer = string.Join("\n", logEntries);
                    LogInfo("Diagnostic log copied to clipboard.");
                }
            }
        }

        private void CollectRelevantObjects()
        {
            collectedComponents.Clear();

            var behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                if (!includeInactiveObjects && !behaviour.isActiveAndEnabled)
                {
                    continue;
                }

                // Ignore assets and prefabs that are not part of the active scene.
                if (EditorUtility.IsPersistent(behaviour))
                {
                    continue;
                }

                if (!IsLikelyMultiplayerComponent(behaviour))
                {
                    continue;
                }

                if (!collectedComponents.Contains(behaviour))
                {
                    collectedComponents.Add(behaviour);
                }
            }

            LogInfo($"Collected {collectedComponents.Count} multiplayer component(s).");
        }

        private static bool IsLikelyMultiplayerComponent(Component component)
        {
            var type = component.GetType();
            var typeName = type.Name;

            if (typeName.Contains("Network", StringComparison.OrdinalIgnoreCase) ||
                typeName.Contains("Netcode", StringComparison.OrdinalIgnoreCase) ||
                typeName.Contains("Photon", StringComparison.OrdinalIgnoreCase) ||
                typeName.Contains("Multiplayer", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Look for well-known multiplayer component types if referenced in the project.
            var fullName = type.FullName ?? string.Empty;
            if (fullName.Contains("Unity.Netcode") ||
                fullName.Contains("Photon.Pun") ||
                fullName.Contains("Mirror"))
            {
                return true;
            }

            return false;
        }

        private void ApplyOptimisationSettings()
        {
            if (collectedComponents.Count == 0)
            {
                LogWarning("No components collected. Run 'Collect Objects' first.");
                return;
            }

            int updateRateAssignments = 0;
            int predictionAssignments = 0;
            int batchingAssignments = 0;
            int diagnosticsAssignments = 0;

            foreach (var component in collectedComponents.ToList())
            {
                if (component == null)
                {
                    continue;
                }

                Undo.RecordObject(component, "AR Multiplayer Optimisation");

                if (TryAssignFloat(component, UpdateRateMemberNames, networkUpdateRate))
                {
                    updateRateAssignments++;
                }

                if (enablePrediction && TryAssignBool(component, PredictionMemberNames, true))
                {
                    predictionAssignments++;
                }
                else if (!enablePrediction && TryAssignBool(component, PredictionMemberNames, false))
                {
                    predictionAssignments++;
                }

                if (enableObjectBatching && TryAssignBool(component, BatchingMemberNames, true))
                {
                    batchingAssignments++;
                }
                else if (!enableObjectBatching && TryAssignBool(component, BatchingMemberNames, false))
                {
                    batchingAssignments++;
                }

                if (enableObjectBatching)
                {
                    if (TryAssignInt(component, BatchSizeMemberNames, batchSize))
                    {
                        batchingAssignments++;
                    }
                }

                if (enableDiagnostics && TryAssignBool(component, new[] { "EnableDiagnostics", "DiagnosticsEnabled", "VerboseLogging" }, true))
                {
                    diagnosticsAssignments++;
                }

                EditorUtility.SetDirty(component);
            }

            LogInfo("Optimisation applied.");
            LogInfo($" - Update rate assignments: {updateRateAssignments}");
            LogInfo($" - Prediction assignments: {predictionAssignments}");
            LogInfo($" - Batching assignments: {batchingAssignments}");
            LogInfo($" - Diagnostics assignments: {diagnosticsAssignments}");
        }

        private static bool TryAssignFloat(Component component, IEnumerable<string> memberNames, float value)
        {
            foreach (var name in memberNames)
            {
                if (TrySetMember(component, name, value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryAssignInt(Component component, IEnumerable<string> memberNames, int value)
        {
            foreach (var name in memberNames)
            {
                if (TrySetMember(component, name, value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryAssignBool(Component component, IEnumerable<string> memberNames, bool value)
        {
            foreach (var name in memberNames)
            {
                if (TrySetMember(component, name, value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TrySetMember(Component component, string memberName, object value)
        {
            var type = component.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var property = type.GetProperty(memberName, flags);
            if (property != null && property.CanWrite && property.PropertyType.IsAssignableFrom(value.GetType()))
            {
                property.SetValue(component, value);
                return true;
            }

            var field = type.GetField(memberName, flags);
            if (field != null && field.FieldType.IsAssignableFrom(value.GetType()))
            {
                field.SetValue(component, value);
                return true;
            }

            return false;
        }

        private void LoadEditorPreferences()
        {
            networkUpdateRate = EditorPrefs.GetFloat(EditorPrefsPrefix + nameof(networkUpdateRate), networkUpdateRate);
            enableObjectBatching = EditorPrefs.GetBool(EditorPrefsPrefix + nameof(enableObjectBatching), enableObjectBatching);
            batchSize = EditorPrefs.GetInt(EditorPrefsPrefix + nameof(batchSize), batchSize);
            enablePrediction = EditorPrefs.GetBool(EditorPrefsPrefix + nameof(enablePrediction), enablePrediction);
            enableDiagnostics = EditorPrefs.GetBool(EditorPrefsPrefix + nameof(enableDiagnostics), enableDiagnostics);
            includeInactiveObjects = EditorPrefs.GetBool(EditorPrefsPrefix + nameof(includeInactiveObjects), includeInactiveObjects);
            autoCollectOnSceneChange = EditorPrefs.GetBool(EditorPrefsPrefix + nameof(autoCollectOnSceneChange), autoCollectOnSceneChange);
            autoApplyOnCollection = EditorPrefs.GetBool(EditorPrefsPrefix + nameof(autoApplyOnCollection), autoApplyOnCollection);
        }

        private void SaveEditorPreferences()
        {
            EditorPrefs.SetFloat(EditorPrefsPrefix + nameof(networkUpdateRate), networkUpdateRate);
            EditorPrefs.SetBool(EditorPrefsPrefix + nameof(enableObjectBatching), enableObjectBatching);
            EditorPrefs.SetInt(EditorPrefsPrefix + nameof(batchSize), batchSize);
            EditorPrefs.SetBool(EditorPrefsPrefix + nameof(enablePrediction), enablePrediction);
            EditorPrefs.SetBool(EditorPrefsPrefix + nameof(enableDiagnostics), enableDiagnostics);
            EditorPrefs.SetBool(EditorPrefsPrefix + nameof(includeInactiveObjects), includeInactiveObjects);
            EditorPrefs.SetBool(EditorPrefsPrefix + nameof(autoCollectOnSceneChange), autoCollectOnSceneChange);
            EditorPrefs.SetBool(EditorPrefsPrefix + nameof(autoApplyOnCollection), autoApplyOnCollection);
        }

        private void LogInfo(string message)
        {
            var entry = $"[Info {DateTime.Now:HH:mm:ss}] {message}";
            logEntries.Add(entry);
            Debug.Log(entry);
        }

        private void LogWarning(string message)
        {
            var entry = $"[Warning {DateTime.Now:HH:mm:ss}] {message}";
            logEntries.Add(entry);
            Debug.LogWarning(entry);
        }
    }
}
