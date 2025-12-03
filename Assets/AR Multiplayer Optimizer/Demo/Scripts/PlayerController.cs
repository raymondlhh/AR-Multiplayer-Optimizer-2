using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float remoteBaseLerpSpeed = 18f;
    [SerializeField] private float remoteCatchupDistanceMultiplier = 6f;
    [SerializeField] private float remoteRotationLerpSpeed = 12f;
    
    [Header("Joystick Controls")]
    [SerializeField] private Joystick movementJoystick;
    
    [Header("Player Info")]
    [SerializeField] private string playerName = "Player";
    [SerializeField] private Color playerColor = Color.white;
    
    private Vector3 networkPosition;
    private Vector3 networkVelocity;
    private Quaternion networkRotation;
    private bool isLocalPlayer;
    private Transform anchorRoot;
    private bool networkStateUsesAnchorSpace;
    private Vector3 lastSentPosition;
    private double lastSentTime;

    void Start()
    {
        isLocalPlayer = photonView.IsMine;

        FindAnchorRoot();
        InitializeNetworkStateDefaults();

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

        if (anchorRoot != null)
        {
            EnsureParentedToAnchor();
        }
    }

    void Update()
    {
        if (anchorRoot == null)
        {
            FindAnchorRoot();
            if (anchorRoot != null)
            {
                EnsureParentedToAnchor();

                if (!isLocalPlayer && networkStateUsesAnchorSpace)
                {
                    ApplyNetworkStateImmediate();
                }
            }
        }

        if (isLocalPlayer)
        {
            HandleInput();
        }
        else
        {
            // Smooth interpolation for remote players with lag compensation
            float lerpSpeed = 15f;
            if (anchorRoot != null && networkStateUsesAnchorSpace)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, networkPosition, Time.deltaTime * lerpSpeed);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, networkRotation, Time.deltaTime * lerpSpeed);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * lerpSpeed);
                transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * lerpSpeed);
            }
        }
    }

    void HandleInput()
    {
        EnsureParentedToAnchor();

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
            bool usingAnchorSpace = anchorRoot != null;
            Vector3 positionToSend = usingAnchorSpace ? transform.localPosition : transform.position;
            Quaternion rotationToSend = usingAnchorSpace ? transform.localRotation : transform.rotation;
            Vector3 velocityToSend = Vector3.zero;

            double now = PhotonNetwork.Time;
            if (lastSentTime > 0)
            {
                double delta = now - lastSentTime;
                if (delta > double.Epsilon)
                {
                    velocityToSend = (positionToSend - lastSentPosition) / (float)delta;
                }
            }

            stream.SendNext(usingAnchorSpace);
            stream.SendNext(positionToSend);
            stream.SendNext(rotationToSend);
            stream.SendNext(velocityToSend);

            lastSentPosition = positionToSend;
            lastSentTime = now;
        }
        else
        {
            // Network player, receive data
            networkStateUsesAnchorSpace = (bool)stream.ReceiveNext();
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkVelocity = (Vector3)stream.ReceiveNext();

            float lagSeconds = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            networkPosition += networkVelocity * lagSeconds;

            ApplyNetworkStateImmediate();
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

    private void InitializeNetworkStateDefaults()
    {
        if (anchorRoot != null)
        {
            networkPosition = transform.localPosition;
            networkRotation = transform.localRotation;
            networkStateUsesAnchorSpace = true;
        }
        else
        {
            networkPosition = transform.position;
            networkRotation = transform.rotation;
            networkStateUsesAnchorSpace = false;
        }
    }

    private void FindAnchorRoot()
    {
        if (anchorRoot != null)
            return;

        if (AMOSessionManager.Instance != null && AMOSessionManager.Instance.AnchorRoot != null)
        {
            anchorRoot = AMOSessionManager.Instance.AnchorRoot;
        }
        else
        {
            GameObject anchorObject = GameObject.Find("AnchorRoot");
            if (anchorObject != null)
            {
                anchorRoot = anchorObject.transform;
            }
        }

        if (anchorRoot != null && isLocalPlayer)
        {
            networkStateUsesAnchorSpace = true;
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

    private void ApplyNetworkStateImmediate()
    {
        if (isLocalPlayer)
            return;

        if (networkStateUsesAnchorSpace && anchorRoot != null)
        {
            EnsureParentedToAnchor();
            transform.localPosition = networkPosition;
            transform.localRotation = networkRotation;
        }
        else if (!networkStateUsesAnchorSpace)
        {
            transform.position = networkPosition;
            transform.rotation = networkRotation;
        }
    }
}
