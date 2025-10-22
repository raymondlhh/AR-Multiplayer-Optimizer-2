using UnityEngine;

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

/// <summary>
/// [AUTOMATIC] Ensures virtual objects are properly anchored to the Image Target center.
/// This component automatically re-parents objects to the AnchorRoot when alignment occurs.
/// </summary>
public class AMOObjectAnchor : MonoBehaviour
{
    [Header("Anchor Settings")]
    [Tooltip("Whether to automatically anchor this object to the Image Target center")]
    public bool autoAnchor = true;
    
    [Tooltip("Local position offset from the Image Target center")]
    public Vector3 localOffset = Vector3.zero;
    
    [Tooltip("Local rotation offset from the Image Target center")]
    public Vector3 localRotationOffset = Vector3.zero;
    
    [Tooltip("Whether to maintain world position when re-parenting")]
    public bool maintainWorldPosition = true;
    
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private bool hasBeenAnchored = false;

    private void Start()
    {
        // Store original parent and transform
        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        
        // Subscribe to alignment events
        if (AMOSessionManager.Instance != null)
        {
            // If already aligned, anchor immediately
            if (AMOSessionManager.Instance.IsAligned)
            {
                AnchorToImageTarget();
            }
        }
    }

    private void Update()
    {
        // Check for alignment and anchor if needed
        if (autoAnchor && !hasBeenAnchored && AMOSessionManager.Instance != null && AMOSessionManager.Instance.IsAligned)
        {
            AnchorToImageTarget();
        }
    }

    private void AnchorToImageTarget()
    {
        if (hasBeenAnchored) return;
        
        var sessionManager = AMOSessionManager.Instance;
        if (sessionManager == null) return;
        
        // Get the anchor root (this should be positioned at the Image Target)
        var anchorRoot = GetAnchorRoot();
        if (anchorRoot == null) return;
        
        Debug.Log($"[AMOObjectAnchor] [AUTOMATIC] Anchoring {gameObject.name} to Image Target center");
        
        // Store world position/rotation if maintaining world position
        Vector3 worldPosition = transform.position;
        Quaternion worldRotation = transform.rotation;
        
        // Re-parent to anchor root
        transform.SetParent(anchorRoot, maintainWorldPosition);
        
        // Apply local offset and rotation
        if (!maintainWorldPosition)
        {
            transform.localPosition = localOffset;
            transform.localRotation = Quaternion.Euler(localRotationOffset);
        }
        else
        {
            // Convert world position to local position relative to anchor root
            Vector3 localPos = anchorRoot.InverseTransformPoint(worldPosition);
            transform.localPosition = localPos + localOffset;
            
            // Convert world rotation to local rotation relative to anchor root
            Quaternion localRot = Quaternion.Inverse(anchorRoot.rotation) * worldRotation;
            transform.localRotation = localRot * Quaternion.Euler(localRotationOffset);
        }
        
        hasBeenAnchored = true;
        Debug.Log($"[AMOObjectAnchor] [AUTOMATIC] {gameObject.name} successfully anchored to Image Target");
    }
    
    private Transform GetAnchorRoot()
    {
        var sessionManager = AMOSessionManager.Instance;
        if (sessionManager == null) return null;
        
        // Use reflection to get the anchor root from AMOSessionManager
        var anchorRootField = typeof(AMOSessionManager).GetField("anchorRoot", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (anchorRootField != null)
        {
            return anchorRootField.GetValue(sessionManager) as Transform;
        }
        
        // Fallback: find by name
        return GameObject.Find("AnchorRoot")?.transform;
    }
    
    /// <summary>
    /// Manually anchor this object to the Image Target center
    /// </summary>
    public void ManualAnchor()
    {
        AnchorToImageTarget();
    }
    
    /// <summary>
    /// Reset to original parent and position
    /// </summary>
    public void ResetToOriginal()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent, maintainWorldPosition);
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
            hasBeenAnchored = false;
        }
    }
    
    /// <summary>
    /// Set the local offset from Image Target center
    /// </summary>
    public void SetLocalOffset(Vector3 offset)
    {
        localOffset = offset;
        if (hasBeenAnchored)
        {
            transform.localPosition = transform.localPosition - localOffset + offset;
        }
    }
    
    /// <summary>
    /// Set the local rotation offset from Image Target center
    /// </summary>
    public void SetLocalRotationOffset(Vector3 rotationOffset)
    {
        localRotationOffset = rotationOffset;
        if (hasBeenAnchored)
        {
            transform.localRotation = transform.localRotation * Quaternion.Inverse(Quaternion.Euler(localRotationOffset)) * Quaternion.Euler(rotationOffset);
        }
    }
}
