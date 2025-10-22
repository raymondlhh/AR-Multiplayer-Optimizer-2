using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [AUTOMATIC] Automatically finds and anchors all virtual objects to the Image Target center.
/// This ensures all objects are positioned relative to the Image Target for proper synchronization.
/// </summary>
public class AMOObjectAnchorer : MonoBehaviour
{
    [Header("Auto-Anchor Settings")]
    [Tooltip("Automatically find and anchor objects with these tags")]
    public string[] objectTags = { "Player", "VirtualObject", "ARObject" };
    
    [Tooltip("Automatically find and anchor objects with these names")]
    public string[] objectNames = { "Cube", "Player", "VirtualObject" };
    
    [Tooltip("Whether to anchor objects that are children of Vuforia ImageTarget")]
    public bool anchorVuforiaChildren = true;
    
    [Tooltip("Default local offset for anchored objects")]
    public Vector3 defaultLocalOffset = Vector3.zero;
    
    [Tooltip("Default local rotation offset for anchored objects")]
    public Vector3 defaultLocalRotationOffset = Vector3.zero;
    
    private List<GameObject> anchoredObjects = new List<GameObject>();
    private bool hasAnchored = false;

    private void Start()
    {
        // Wait a frame for other components to initialize
        Invoke(nameof(FindAndAnchorObjects), 0.1f);
    }

    private void Update()
    {
        // Check if we should anchor objects
        if (!hasAnchored && AMOSessionManager.Instance != null && AMOSessionManager.Instance.IsAligned)
        {
            FindAndAnchorObjects();
        }
    }

    private void FindAndAnchorObjects()
    {
        if (hasAnchored) return;
        
        Debug.Log("[AMOObjectAnchorer] [AUTOMATIC] Finding and anchoring virtual objects...");
        
        // Find objects by tags
        foreach (string tag in objectTags)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objects)
            {
                AnchorObject(obj);
            }
        }
        
        // Find objects by names
        foreach (string name in objectNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                AnchorObject(obj);
            }
        }
        
        // Find children of Vuforia ImageTarget
        if (anchorVuforiaChildren)
        {
            FindAndAnchorVuforiaChildren();
        }
        
        hasAnchored = true;
        Debug.Log($"[AMOObjectAnchorer] [AUTOMATIC] Anchored {anchoredObjects.Count} objects to Image Target center");
    }
    
    private void FindAndAnchorVuforiaChildren()
    {
        // Find Vuforia ImageTarget objects
        var vuforiaTargets = FindVuforiaImageTargets();
        
        foreach (var target in vuforiaTargets)
        {
            // Get all children of the Vuforia target
            Transform[] children = target.GetComponentsInChildren<Transform>();
            
            foreach (Transform child in children)
            {
                // Skip the target itself
                if (child == target) continue;
                
                // Check if this child is a virtual object (has a renderer or collider)
                if (child.GetComponent<Renderer>() != null || child.GetComponent<Collider>() != null)
                {
                    AnchorObject(child.gameObject);
                }
            }
        }
    }
    
    private Transform[] FindVuforiaImageTargets()
    {
        List<Transform> targets = new List<Transform>();
        
        // Find by name (common Vuforia target names)
        string[] targetNames = { "ImageTarget", "ARMascot", "ImageTarget-ARMascot" };
        foreach (string name in targetNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                targets.Add(obj.transform);
            }
        }
        
        // Find by component type using reflection
        var observerType = System.Type.GetType("Vuforia.ObserverBehaviour, Vuforia.Unity.Engine");
        if (observerType != null)
        {
            var observers = FindObjectsOfType(observerType);
            foreach (var observer in observers)
            {
                if (observer is Component component)
                {
                    targets.Add(component.transform);
                }
            }
        }
        
        return targets.ToArray();
    }
    
    private void AnchorObject(GameObject obj)
    {
        if (obj == null || anchoredObjects.Contains(obj)) return;
        
        // Add AMOObjectAnchor component if not present
        var anchorComponent = obj.GetComponent<AMOObjectAnchor>();
        if (anchorComponent == null)
        {
            anchorComponent = obj.AddComponent<AMOObjectAnchor>();
            anchorComponent.localOffset = defaultLocalOffset;
            anchorComponent.localRotationOffset = defaultLocalRotationOffset;
        }
        
        // Manually anchor the object
        anchorComponent.ManualAnchor();
        
        anchoredObjects.Add(obj);
        Debug.Log($"[AMOObjectAnchorer] [AUTOMATIC] Anchored {obj.name} to Image Target center");
    }
    
    /// <summary>
    /// Manually anchor a specific object
    /// </summary>
    public void AnchorObjectManually(GameObject obj)
    {
        AnchorObject(obj);
    }
    
    /// <summary>
    /// Reset all anchored objects to their original positions
    /// </summary>
    public void ResetAllAnchoredObjects()
    {
        foreach (GameObject obj in anchoredObjects)
        {
            var anchorComponent = obj.GetComponent<AMOObjectAnchor>();
            if (anchorComponent != null)
            {
                anchorComponent.ResetToOriginal();
            }
        }
        anchoredObjects.Clear();
        hasAnchored = false;
    }
    
    /// <summary>
    /// Get list of all anchored objects
    /// </summary>
    public List<GameObject> GetAnchoredObjects()
    {
        return new List<GameObject>(anchoredObjects);
    }
}
