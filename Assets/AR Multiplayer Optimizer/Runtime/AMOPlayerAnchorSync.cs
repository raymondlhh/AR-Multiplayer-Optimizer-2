using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PhotonView))]
public class AMOPlayerAnchorSync : MonoBehaviour, IPunObservable
{
    private static readonly BindingFlags InstancePrivateFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    private PlayerController controller;
    private PhotonView photonView;

    private FieldInfo networkPositionField;
    private FieldInfo networkRotationField;
    private bool networkFieldsResolved;

    private Transform anchorRoot;
    private Vector3 networkLocalPosition;
    private Quaternion networkLocalRotation = Quaternion.identity;
    private Vector3 pendingLocalPosition;
    private Quaternion pendingLocalRotation = Quaternion.identity;
    private bool hasPendingLocalData;

    private void OnEnable()
    {
        if (controller != null || !Application.isPlaying)
        {
            EnsureObservedComponents();
        }
    }

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        photonView = GetComponent<PhotonView>();

        var controllerType = controller.GetType();
        networkPositionField = controllerType.GetField("networkPosition", InstancePrivateFlags);
        networkRotationField = controllerType.GetField("networkRotation", InstancePrivateFlags);
        networkFieldsResolved = networkPositionField != null && networkRotationField != null;

        if (!networkFieldsResolved)
        {
            Debug.LogWarning(
                "AMOPlayerAnchorSync could not access PlayerController network fields. Anchor synchronization will be skipped.",
                this);
            EnsureObservedComponents();
            enabled = false;
            return;
        }

        networkLocalPosition = transform.localPosition;
        networkLocalRotation = transform.localRotation;

        SetNetworkWorld(transform.position, transform.rotation);

        EnsureObservedComponents();
    }

    private void Update()
    {
        TryAssignAnchorRoot();
        EnsureParentedToAnchor();

        if (!photonView.IsMine && anchorRoot != null && networkFieldsResolved)
        {
            Vector3 targetWorldPosition = anchorRoot.TransformPoint(networkLocalPosition);
            Quaternion targetWorldRotation = anchorRoot.rotation * networkLocalRotation;
            SetNetworkWorld(targetWorldPosition, targetWorldRotation);
        }
    }

    private void EnsureObservedComponents()
    {
        if (photonView == null)
        {
            return;
        }

        if (photonView.ObservedComponents == null)
        {
            photonView.ObservedComponents = new List<Component>();
        }

        List<Component> observed = photonView.ObservedComponents;
        bool changed = false;

        if (!networkFieldsResolved)
        {
            if (!observed.Contains(controller))
            {
                observed.Add(controller);
                changed = true;
            }

            if (observed.Contains(this))
            {
                observed.Remove(this);
                changed = true;
            }

            if (changed && photonView.Synchronization == ViewSynchronization.Off)
            {
                photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
            }

            return;
        }

        if (!observed.Contains(this))
        {
            observed.Add(this);
            changed = true;
        }

        PlayerController playerControllerComponent = controller;
        if (playerControllerComponent != null && observed.Contains(playerControllerComponent))
        {
            observed.Remove(playerControllerComponent);
            changed = true;
        }

        if (changed && photonView.Synchronization == ViewSynchronization.Off)
        {
            photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
        }
    }

    private void TryAssignAnchorRoot()
    {
        if (anchorRoot != null)
        {
            return;
        }

        if (AMOSessionManager.Instance != null)
        {
            var anchorField = typeof(AMOSessionManager).GetField("anchorRoot", InstancePrivateFlags);
            if (anchorField != null)
            {
                if (anchorField.GetValue(AMOSessionManager.Instance) is Transform resolvedAnchor && resolvedAnchor != null)
                {
                    anchorRoot = resolvedAnchor;
                    OnAnchorRootAssigned();
                    return;
                }
            }
        }

        GameObject anchorRootObject = GameObject.Find("AnchorRoot");
        if (anchorRootObject != null)
        {
            anchorRoot = anchorRootObject.transform;
            OnAnchorRootAssigned();
        }
    }

    private void EnsureParentedToAnchor()
    {
        if (anchorRoot == null || transform.parent == anchorRoot)
        {
            return;
        }

        transform.SetParent(anchorRoot, true);
    }

    private void OnAnchorRootAssigned()
    {
        EnsureParentedToAnchor();

        if (photonView.IsMine)
        {
            return;
        }

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

        Vector3 worldPosition = anchorRoot.TransformPoint(networkLocalPosition);
        Quaternion worldRotation = anchorRoot.rotation * networkLocalRotation;

        SetNetworkWorld(worldPosition, worldRotation);
        transform.localPosition = networkLocalPosition;
        transform.localRotation = networkLocalRotation;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        bool hasAnchor = anchorRoot != null;

        if (!networkFieldsResolved)
        {
            return;
        }

        if (stream.IsWriting)
        {
            stream.SendNext(hasAnchor);
            if (hasAnchor)
            {
                stream.SendNext(transform.localPosition);
                stream.SendNext(transform.localRotation);
            }
            else
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
            }
        }
        else
        {
            bool senderHasAnchor = (bool)stream.ReceiveNext();
            Vector3 receivedPosition = (Vector3)stream.ReceiveNext();
            Quaternion receivedRotation = (Quaternion)stream.ReceiveNext();

            if (senderHasAnchor)
            {
                if (anchorRoot != null)
                {
                    networkLocalPosition = receivedPosition;
                    networkLocalRotation = receivedRotation;

                    transform.localPosition = networkLocalPosition;
                    transform.localRotation = networkLocalRotation;

                    Vector3 worldPosition = anchorRoot.TransformPoint(networkLocalPosition);
                    Quaternion worldRotation = anchorRoot.rotation * networkLocalRotation;
                    SetNetworkWorld(worldPosition, worldRotation);
                }
                else
                {
                    pendingLocalPosition = receivedPosition;
                    pendingLocalRotation = receivedRotation;
                    hasPendingLocalData = true;
                }
            }
            else
            {
                SetNetworkWorld(receivedPosition, receivedRotation);

                if (anchorRoot != null)
                {
                    networkLocalPosition = anchorRoot.InverseTransformPoint(receivedPosition);
                    networkLocalRotation = Quaternion.Inverse(anchorRoot.rotation) * receivedRotation;

                    transform.localPosition = networkLocalPosition;
                    transform.localRotation = networkLocalRotation;
                }
            }
        }
    }

    private void SetNetworkWorld(Vector3 position, Quaternion rotation)
    {
        if (!networkFieldsResolved)
        {
            return;
        }

        networkPositionField.SetValue(controller, position);
        networkRotationField.SetValue(controller, rotation);
    }
}
