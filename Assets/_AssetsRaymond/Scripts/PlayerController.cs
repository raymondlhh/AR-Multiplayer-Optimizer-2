using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 100f;
    
    [Header("Player Info")]
    [SerializeField] private string playerName = "Player";
    [SerializeField] private Color playerColor = Color.white;
    
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private bool isLocalPlayer;
    
    void Start()
    {
        isLocalPlayer = photonView.IsMine;
        
        if (isLocalPlayer)
        {
            // Set up local player
            playerName = "Player " + PhotonNetwork.LocalPlayer.ActorNumber;
            SetPlayerColor();
            
            // Add camera follow for local player
            SetupCamera();
        }
        else
        {
            // Set up remote player
            playerName = "Player " + photonView.owner.ActorNumber;
            SetPlayerColor();
        }
        
        // Set player name
        gameObject.name = playerName;
    }
    
    void Update()
    {
        if (isLocalPlayer)
        {
            HandleInput();
        }
        else
        {
            // Smooth interpolation for remote players
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10f);
        }
    }
    
    void HandleInput()
    {
        // Movement input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 movement = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);
        
        // Rotation input
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(0, -rotationSpeed * Time.deltaTime, 0);
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
        
        // Jump input
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
        
        int playerIndex = (photonView.owner.ActorNumber - 1) % colors.Length;
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
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this player: send the others our data
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // Network player, receive data
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
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
}
