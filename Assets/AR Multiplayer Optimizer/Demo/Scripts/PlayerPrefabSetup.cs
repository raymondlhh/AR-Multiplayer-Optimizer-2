using UnityEngine;
using Photon.Pun;

/// <summary>
/// Helper script to set up a player prefab with all necessary components for PUN 2 networking
/// </summary>
public class PlayerPrefabSetup : MonoBehaviour
{
    [Header("Prefab Setup Instructions")]
    [TextArea(10, 15)]
    public string setupInstructions = 
        "SETUP INSTRUCTIONS:\n\n" +
        "1. Create a GameObject for your player prefab\n" +
        "2. Add a PhotonView component\n" +
        "3. Add a Rigidbody component\n" +
        "4. Add a Collider component (BoxCollider, CapsuleCollider, etc.)\n" +
        "5. Add the PlayerController script\n" +
        "6. Add a simple visual representation (Cube, Capsule, etc.)\n" +
        "7. Set the PhotonView's Observed Components to include PlayerController\n" +
        "8. Add the ARNetworkedObjectBinder component to ensure AR alignment\n" +
        "9. Save as a prefab in Resources folder\n" +
        "10. Assign the prefab to NetworkManager's Player Prefab field\n\n" +
        "The prefab will be automatically spawned for each player when they join the room.";
    
    void Start()
    {
        // This script is just for documentation purposes
        // The actual setup needs to be done manually in the Unity Editor
    }
    
    [ContextMenu("Create Basic Player Prefab")]
    void CreateBasicPlayerPrefab()
    {
        // This method can be called from the context menu to create a basic player prefab
        GameObject playerPrefab = new GameObject("PlayerPrefab");
        
        // Add PhotonView
        PhotonView photonView = playerPrefab.AddComponent<PhotonView>();
        
        // Add Rigidbody
        Rigidbody rb = playerPrefab.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;
        
        // Add Collider
        CapsuleCollider collider = playerPrefab.AddComponent<CapsuleCollider>();
        collider.height = 2f;
        collider.radius = 0.5f;
        
        // Add PlayerController
        playerPrefab.AddComponent<PlayerController>();
        
        // Add visual representation
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.transform.SetParent(playerPrefab.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one;
        
        // Remove the collider from the visual (we already have one on the parent)
        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            DestroyImmediate(visualCollider);
        }
        
        // Set up PhotonView
        photonView.ObservedComponents.Add(playerPrefab.GetComponent<PlayerController>());
        photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
        
        Debug.Log("Basic player prefab created! Remember to save it as a prefab in the Resources folder.");
    }
}
