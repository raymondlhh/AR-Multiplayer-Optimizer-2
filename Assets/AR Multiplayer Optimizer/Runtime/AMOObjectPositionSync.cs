using UnityEngine;

#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

/// <summary>
/// [AUTOMATIC] Synchronizes individual object positions relative to the Image Target center.
/// This ensures objects maintain consistent positions across all clients.
/// </summary>
public class AMOObjectPositionSync : MonoBehaviour, IPunObservable
{
    [Header("Sync Settings")]
    [Tooltip("Smoothing factor for position interpolation")]
    public float smoothingFactor = 10f;
    
    [Tooltip("Threshold for position updates (smaller = more frequent updates)")]
    public float positionThreshold = 0.01f;
    
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private bool isLocalObject = true;
    private Transform anchorRoot;
    
    private void Start()
    {
#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
        var photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            isLocalObject = photonView.IsMine;
        }
#endif
        
        // Find the anchor root
        FindAnchorRoot();
    }
    
    private void Update()
    {
        if (!isLocalObject)
        {
            // Smoothly interpolate to network position for remote objects
            transform.localPosition = Vector3.Lerp(transform.localPosition, networkPosition, Time.deltaTime * smoothingFactor);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, networkRotation, Time.deltaTime * smoothingFactor);
        }
        else
        {
            // For local objects, ensure they stay relative to anchor root
            if (anchorRoot != null && transform.parent != anchorRoot)
            {
                // Re-parent to anchor root if somehow detached
                transform.SetParent(anchorRoot, true);
            }
        }
    }
    
    private void FindAnchorRoot()
    {
        // Find anchor root by name
        var anchorRootGO = GameObject.Find("AnchorRoot");
        if (anchorRootGO != null)
        {
            anchorRoot = anchorRootGO.transform;
        }
        else
        {
            // Try to get from AMOSessionManager
            if (AMOSessionManager.Instance != null)
            {
                var anchorRootField = typeof(AMOSessionManager).GetField("anchorRoot", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (anchorRootField != null)
                {
                    anchorRoot = anchorRootField.GetValue(AMOSessionManager.Instance) as Transform;
                }
            }
        }
    }
    
#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send local position and rotation relative to anchor root
            stream.SendNext(transform.localPosition);
            stream.SendNext(transform.localRotation);
        }
        else
        {
            // Receive position and rotation
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
        }
    }
#endif
    
    /// <summary>
    /// Force update the object position (useful for manual synchronization)
    /// </summary>
    public void ForcePositionUpdate()
    {
        if (isLocalObject && anchorRoot != null)
        {
            // Ensure object is properly positioned relative to anchor root
            transform.SetParent(anchorRoot, true);
        }
    }
    
    /// <summary>
    /// Set the local position relative to the Image Target center
    /// </summary>
    public void SetLocalPosition(Vector3 localPos)
    {
        transform.localPosition = localPos;
        ForcePositionUpdate();
    }
    
    /// <summary>
    /// Set the local rotation relative to the Image Target center
    /// </summary>
    public void SetLocalRotation(Quaternion localRot)
    {
        transform.localRotation = localRot;
        ForcePositionUpdate();
    }
}
