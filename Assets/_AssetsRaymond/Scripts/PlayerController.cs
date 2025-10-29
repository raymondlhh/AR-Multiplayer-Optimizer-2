using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 100f;
    
    [Header("Joystick Controls")]
    [SerializeField] private Joystick movementJoystick;
    
    [Header("Player Info")]
    [SerializeField] private string playerName = "Player";
    [SerializeField] private Color playerColor = Color.white;
    
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Vector3 networkLocalPosition;
    private Quaternion networkLocalRotation;
    private Vector3 pendingLocalPosition;
    private Quaternion pendingLocalRotation;
    private bool hasPendingLocalData;
    private bool isLocalPlayer;
    private Transform anchorRoot;

    private void Awake()
    {
        networkPosition = transform.position;
        networkRotation = transform.rotation;
        networkLocalPosition = transform.localPosition;
        networkLocalRotation = transform.localRotation;
    }
    
    void Start()
    {
        isLocalPlayer = photonView.IsMine;

        TryAssignAnchorRoot();
        
        if (isLocalPlayer)
        {
            // Set up local player
            playerName = "Player " + PhotonNetwork.LocalPlayer.ActorNumber;
            SetPlayerColor();
            
            // Add camera follow for local player
            //SetupCamera();
            
            // Auto-find joystick if not assigned
            AutoFindJoystick();
        }
        else
        {
            // Set up remote player
            playerName = "Player " + photonView.Owner.ActorNumber;
            SetPlayerColor();
        }
        
        // Set player name
        gameObject.name = playerName;
    }
    
    void Update()
    {
        TryAssignAnchorRoot();
        EnsureParentedToAnchor();

        if (isLocalPlayer)
        {
            HandleInput();
        }
        else
        {
            // Smooth interpolation for remote players
            if (anchorRoot != null)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, networkLocalPosition, Time.deltaTime * 10f);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, networkLocalRotation, Time.deltaTime * 10f);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10f);
            }
        }
    }
    
    void HandleInput()
    {
        // Movement input from joystick
        if (movementJoystick != null)
        {
            float horizontal = movementJoystick.Horizontal;
            float vertical = movementJoystick.Vertical;
            
            Vector3 movement = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
            transform.Translate(movement, Space.World);
        }
        else
        {
            // Fallback to keyboard input if joystick is not assigned
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            Vector3 movement = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
            transform.Translate(movement, Space.World);
        }
        
        // Rotation input (keyboard only)
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(0, -rotationSpeed * Time.deltaTime, 0);
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
        
        // Jump input (keyboard only)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }
    }
    
    void Jump()
    {
        // Simple jump implementation
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);
        }
    }
    
    void SetPlayerColor()
    {
        // Set different colors for different players
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Color[] colors = { Color.red, Color.blue, Color.green, Color.yellow };
        
        int playerIndex = (photonView.Owner.ActorNumber - 1) % colors.Length;
        playerColor = colors[playerIndex];
        
        foreach (Renderer renderer in renderers)
        {
            renderer.material.color = playerColor;
        }
    }
    
    void SetupCamera()
    {
        // Find main camera and make it follow this player
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Create a camera follow script or set parent
            mainCamera.transform.SetParent(transform);
            mainCamera.transform.localPosition = new Vector3(0, 5, -10);
            mainCamera.transform.LookAt(transform);
        }
    }
    
    void AutoFindJoystick()
    {
        // Auto-find joystick if not manually assigned
        if (movementJoystick == null)
        {
            // Try to find any Joystick component in the scene
            Joystick foundJoystick = FindObjectOfType<Joystick>();
            if (foundJoystick != null)
            {
                movementJoystick = foundJoystick;
                Debug.Log("Auto-found Joystick: " + foundJoystick.name + " (" + foundJoystick.GetType().Name + ")");
            }
            else
            {
                Debug.LogWarning("No joystick found in scene. Player will use keyboard controls only.");
            }
        }
    }
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this player: send the others our data
            Vector3 positionToSend = transform.position;
            Quaternion rotationToSend = transform.rotation;

            if (anchorRoot != null)
            {
                positionToSend = transform.localPosition;
                rotationToSend = transform.localRotation;
            }

            stream.SendNext(positionToSend);
            stream.SendNext(rotationToSend);
        }
        else
        {
            // Network player, receive data
            Vector3 receivedPosition = (Vector3)stream.ReceiveNext();
            Quaternion receivedRotation = (Quaternion)stream.ReceiveNext();

            if (anchorRoot != null)
            {
                networkLocalPosition = receivedPosition;
                networkLocalRotation = receivedRotation;
                networkPosition = anchorRoot.TransformPoint(networkLocalPosition);
                networkRotation = anchorRoot.rotation * networkLocalRotation;
            }
            else
            {
                pendingLocalPosition = receivedPosition;
                pendingLocalRotation = receivedRotation;
                hasPendingLocalData = true;

                networkPosition = receivedPosition;
                networkRotation = receivedRotation;
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (isLocalPlayer)
        {
            Debug.Log(playerName + " collided with " + other.name);
        }
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Player " + newPlayer.NickName + " entered the room");
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("Player " + otherPlayer.NickName + " left the room");
    }

    private void TryAssignAnchorRoot()
    {
        if (anchorRoot != null)
            return;

        if (AMOSessionManager.Instance != null)
        {
            var anchorField = typeof(AMOSessionManager).GetField("anchorRoot", BindingFlags.NonPublic | BindingFlags.Instance);
            if (anchorField != null)
            {
                var resolvedAnchor = anchorField.GetValue(AMOSessionManager.Instance) as Transform;
                if (resolvedAnchor != null)
                {
                    anchorRoot = resolvedAnchor;
                    OnAnchorRootAssigned();
                    return;
                }
            }
        }

        var anchorRootObject = GameObject.Find("AnchorRoot");
        if (anchorRootObject != null)
        {
            anchorRoot = anchorRootObject.transform;
            OnAnchorRootAssigned();
        }
    }

    private void EnsureParentedToAnchor()
    {
        if (anchorRoot == null)
            return;

        if (transform.parent != anchorRoot)
        {
            transform.SetParent(anchorRoot, true);
        }
    }

    private void OnAnchorRootAssigned()
    {
        EnsureParentedToAnchor();

        if (isLocalPlayer)
            return;

        if (hasPendingLocalData)
        {
            networkLocalPosition = pendingLocalPosition;
            networkLocalRotation = pendingLocalRotation;
            hasPendingLocalData = false;
        }
        else
        {
            networkLocalPosition = transform.localPosition;
            networkLocalRotation = transform.localRotation;
        }

        networkPosition = anchorRoot.TransformPoint(networkLocalPosition);
        networkRotation = anchorRoot.rotation * networkLocalRotation;

        transform.localPosition = networkLocalPosition;
        transform.localRotation = networkLocalRotation;
    }
}
