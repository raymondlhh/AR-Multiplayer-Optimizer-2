using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AR.Multiplayer.Optimizer.Editor
{
    /// <summary>
    /// AR Optimizer - A Unity Editor tool for managing AR multiplayer objects
    /// </summary>
    public class ARMultiplayerOptimizerWindow : EditorWindow
    {
        private bool showObjectManager = false;
        private Vector2 scrollPosition;
        private List<GameObject> resourceObjects = new List<GameObject>();

        [MenuItem("AR Optimizer/Objects Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<ARMultiplayerOptimizerWindow>(false, "Objects Manager", true);
            window.minSize = new Vector2(400f, 300f);
            window.Focus();
        }

        private void OnEnable()
        {
            LoadResourceObjects();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Objects Com", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Object Manager Dropdown
            showObjectManager = EditorGUILayout.Foldout(showObjectManager, "Object Manager", true);
            
            if (showObjectManager)
            {
                DrawObjectManager();
            }
        }

        private void DrawObjectManager()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space();

            // Refresh button
            if (GUILayout.Button("Refresh Resource Objects", GUILayout.Height(25)))
            {
                LoadResourceObjects();
            }

            EditorGUILayout.Space();

            // Display resource objects
            if (resourceObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("No objects found in Resources folder.", MessageType.Info);
                }
                else
            {
                EditorGUILayout.LabelField($"Found {resourceObjects.Count} object(s) in Resources:", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

                foreach (var obj in resourceObjects)
                {
                    if (obj == null) continue;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    // Object name and ping button
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(obj.name, EditorStyles.boldLabel);
                    if (GUILayout.Button("Ping", GUILayout.Width(50)))
                    {
                        EditorGUIUtility.PingObject(obj);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    // Transform components
                    var transform = obj.transform;
                    
                    // Position
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Position:", GUILayout.Width(60));
                    EditorGUILayout.Vector3Field("", transform.position, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();

                    // Rotation
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Rotation:", GUILayout.Width(60));
                    EditorGUILayout.Vector3Field("", transform.eulerAngles, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();

                    // Scale
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Scale:", GUILayout.Width(60));
                    EditorGUILayout.Vector3Field("", transform.localScale, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void LoadResourceObjects()
        {
            resourceObjects.Clear();
            
            // Load all GameObjects from Resources folder
            var allObjects = Resources.LoadAll<GameObject>("");
            
            foreach (var obj in allObjects)
            {
                if (obj != null)
                {
                    resourceObjects.Add(obj);
                }
            }
        }
    }
}
