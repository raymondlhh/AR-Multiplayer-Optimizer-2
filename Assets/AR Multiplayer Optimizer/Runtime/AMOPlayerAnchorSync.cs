using System.Reflection;
using Photon.Pun;
using UnityEngine;

public partial class PlayerController
{
    private Vector3 amoNetworkLocalPosition;
    private Quaternion amoNetworkLocalRotation;
    private Vector3 amoPendingLocalPosition;
    private Quaternion amoPendingLocalRotation;
    private bool amoHasPendingLocalData;
    private Transform amoAnchorRoot;

    partial void AMO_OnAwake()
    {
        networkPosition = transform.position;
        networkRotation = transform.rotation;
        amoNetworkLocalPosition = transform.localPosition;
        amoNetworkLocalRotation = transform.localRotation;
    }

    partial void AMO_OnStart()
    {
        AMO_TryAssignAnchorRoot();
    }

    partial void AMO_OnUpdate()
    {
        AMO_TryAssignAnchorRoot();
        AMO_EnsureParentedToAnchor();
    }

    partial void AMO_HandleRemoteUpdate(ref bool handled)
    {
        if (amoAnchorRoot == null)
        {
            return;
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, amoNetworkLocalPosition, Time.deltaTime * 10f);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, amoNetworkLocalRotation, Time.deltaTime * 10f);
        handled = true;
    }

    partial void AMO_OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info, ref bool handled)
    {
        bool hasAnchor = amoAnchorRoot != null;

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
                if (amoAnchorRoot != null)
                {
                    amoNetworkLocalPosition = receivedPosition;
                    amoNetworkLocalRotation = receivedRotation;
                    networkPosition = amoAnchorRoot.TransformPoint(amoNetworkLocalPosition);
                    networkRotation = amoAnchorRoot.rotation * amoNetworkLocalRotation;

                    transform.localPosition = amoNetworkLocalPosition;
                    transform.localRotation = amoNetworkLocalRotation;
                }
                else
                {
                    amoPendingLocalPosition = receivedPosition;
                    amoPendingLocalRotation = receivedRotation;
                    amoHasPendingLocalData = true;
                }
            }
            else
            {
                networkPosition = receivedPosition;
                networkRotation = receivedRotation;

                if (amoAnchorRoot != null)
                {
                    amoNetworkLocalPosition = amoAnchorRoot.InverseTransformPoint(networkPosition);
                    amoNetworkLocalRotation = Quaternion.Inverse(amoAnchorRoot.rotation) * networkRotation;
                    transform.localPosition = amoNetworkLocalPosition;
                    transform.localRotation = amoNetworkLocalRotation;
                }
            }
        }

        handled = true;
    }

    private void AMO_TryAssignAnchorRoot()
    {
        if (amoAnchorRoot != null)
        {
            return;
        }

        if (AMOSessionManager.Instance != null)
        {
            var anchorField = typeof(AMOSessionManager).GetField("anchorRoot", BindingFlags.NonPublic | BindingFlags.Instance);
            if (anchorField != null)
            {
                if (anchorField.GetValue(AMOSessionManager.Instance) is Transform resolvedAnchor && resolvedAnchor != null)
                {
                    amoAnchorRoot = resolvedAnchor;
                    AMO_OnAnchorRootAssigned();
                    return;
                }
            }
        }

        var anchorRootObject = GameObject.Find("AnchorRoot");
        if (anchorRootObject != null)
        {
            amoAnchorRoot = anchorRootObject.transform;
            AMO_OnAnchorRootAssigned();
        }
    }

    private void AMO_EnsureParentedToAnchor()
    {
        if (amoAnchorRoot == null)
        {
            return;
        }

        if (transform.parent != amoAnchorRoot)
        {
            transform.SetParent(amoAnchorRoot, true);
        }
    }

    private void AMO_OnAnchorRootAssigned()
    {
        AMO_EnsureParentedToAnchor();

        if (isLocalPlayer)
        {
            return;
        }

        if (amoHasPendingLocalData)
        {
            amoNetworkLocalPosition = amoPendingLocalPosition;
            amoNetworkLocalRotation = amoPendingLocalRotation;
            amoHasPendingLocalData = false;
        }
        else
        {
            amoNetworkLocalPosition = transform.localPosition;
            amoNetworkLocalRotation = transform.localRotation;
        }

        networkPosition = amoAnchorRoot.TransformPoint(amoNetworkLocalPosition);
        networkRotation = amoAnchorRoot.rotation * amoNetworkLocalRotation;

        transform.localPosition = amoNetworkLocalPosition;
        transform.localRotation = amoNetworkLocalRotation;
    }
}
