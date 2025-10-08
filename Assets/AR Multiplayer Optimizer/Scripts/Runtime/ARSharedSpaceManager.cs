using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

namespace AR.Multiplayer.Optimizer
{
    /// <summary>
    /// Central coordinator that keeps every Photon client aligned to the same AR world origin.
    /// A shared anchor pose is written to the Photon room properties by the master client and
    /// each joined client re-aligns their <see cref="alignmentRoot"/> so that their local AR
    /// reference matches the shared pose.
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    public class ARSharedSpaceManager : MonoBehaviourPunCallbacks
    {
        private const string SharedAnchorKey = "AMO_SHARED_ANCHOR";

        private static ARSharedSpaceManager _instance;

        /// <summary>
        /// Raised when the alignment root has been created and is ready for parenting objects.
        /// </summary>
        public static event Action<Transform> AlignmentRootReady;

        /// <summary>
        /// Raised whenever a new alignment pose is applied to the root.
        /// </summary>
        public static event Action<Transform> AlignmentPoseChanged;

        [Header("Scene Graph")]
        [Tooltip("Transform that is rotated/translated to keep the shared space aligned across devices.")]
        [SerializeField] private Transform alignmentRoot;

        [Tooltip("Optional content root that will be parented under the alignment root if supplied.")]
        [SerializeField] private Transform contentRoot;

        [Header("Behaviour")]
        [SerializeField] private bool logDebugMessages = true;

        private Transform localReference;
        private Pose? sharedAnchorPose;

        /// <summary>
        /// Gets the singleton instance if one exists.
        /// </summary>
        public static ARSharedSpaceManager Instance => _instance;

        /// <summary>
        /// Gets the transform that is used as the aligned origin for all AR content.
        /// </summary>
        public Transform AlignmentRoot => alignmentRoot;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (logDebugMessages)
                {
                    Debug.LogWarning("Multiple ARSharedSpaceManager instances detected. Destroying the newest one.");
                }
                Destroy(gameObject);
                return;
            }

            _instance = this;

            EnsureAlignmentRoot();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Registers the local AR reference transform (for example a tracked marker or plane).
        /// The transform should already be in the correct real-world pose when registering.
        /// </summary>
        public void RegisterLocalReference(Transform referenceTransform)
        {
            if (referenceTransform == null)
            {
                return;
            }

            localReference = referenceTransform;

            if (logDebugMessages)
            {
                Debug.Log($"[ARSharedSpaceManager] Local reference registered: {referenceTransform.name}");
            }

            TryResolveAlignment();
        }

        /// <summary>
        /// Publishes the shared anchor pose to Photon so every joined client can align their world.
        /// This should normally be triggered by the master client once the physical reference has
        /// been localised on their device.
        /// </summary>
        public void PublishAnchorFromReference(Transform referenceTransform)
        {
            if (referenceTransform == null)
            {
                Debug.LogWarning("[ARSharedSpaceManager] Cannot publish anchor from a null reference transform.");
                return;
            }

            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[ARSharedSpaceManager] Cannot publish anchor because the client is not inside a Photon room.");
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[ARSharedSpaceManager] Only the master client should publish the shared anchor pose.");
                return;
            }

            RegisterLocalReference(referenceTransform);

            Pose pose = new Pose(referenceTransform.position, referenceTransform.rotation);
            sharedAnchorPose = pose;

            AnchorData anchorData = new AnchorData(pose);
            Hashtable properties = new Hashtable
            {
                { SharedAnchorKey, anchorData.ToHashtable() }
            };

            PhotonNetwork.CurrentRoom.SetCustomProperties(properties);

            if (logDebugMessages)
            {
                Debug.Log($"[ARSharedSpaceManager] Published shared anchor: position={pose.position}, rotation={pose.rotation.eulerAngles}");
            }

            ApplySharedAnchor(pose);
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();

            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(SharedAnchorKey, out object rawData))
            {
                AnchorData? anchorData = AnchorData.FromObject(rawData);
                if (anchorData.HasValue)
                {
                    sharedAnchorPose = anchorData.Value.ToPose();
                    if (logDebugMessages)
                    {
                        Debug.Log("[ARSharedSpaceManager] Loaded shared anchor when joining the room.");
                    }
                    TryResolveAlignment();
                }
            }
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            base.OnRoomPropertiesUpdate(propertiesThatChanged);

            if (propertiesThatChanged == null)
            {
                return;
            }

            if (propertiesThatChanged.TryGetValue(SharedAnchorKey, out object rawData))
            {
                AnchorData? anchorData = AnchorData.FromObject(rawData);
                if (anchorData.HasValue)
                {
                    sharedAnchorPose = anchorData.Value.ToPose();
                    if (logDebugMessages)
                    {
                        Debug.Log("[ARSharedSpaceManager] Shared anchor updated from room properties.");
                    }
                    TryResolveAlignment();
                }
            }
        }

        private void EnsureAlignmentRoot()
        {
            if (alignmentRoot == null)
            {
                GameObject rootObject = new GameObject("AR Shared Alignment Root");
                alignmentRoot = rootObject.transform;
                alignmentRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                if (logDebugMessages)
                {
                    Debug.Log("[ARSharedSpaceManager] Created alignment root dynamically.");
                }
            }

            if (contentRoot != null && contentRoot.parent != alignmentRoot)
            {
                alignmentRoot.SetParent(contentRoot.parent, false);
                contentRoot.SetParent(alignmentRoot, true);
            }

            AlignmentRootReady?.Invoke(alignmentRoot);
        }

        private void TryResolveAlignment()
        {
            if (!sharedAnchorPose.HasValue)
            {
                return;
            }

            if (localReference == null)
            {
                return;
            }

            ApplyAlignment(localReference, sharedAnchorPose.Value);
        }

        private void ApplySharedAnchor(Pose anchorPose)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                // The master client already represents the authoritative pose.
                return;
            }

            if (localReference != null)
            {
                ApplyAlignment(localReference, anchorPose);
            }
        }

        private void ApplyAlignment(Transform reference, Pose sharedPose)
        {
            if (alignmentRoot == null)
            {
                Debug.LogWarning("[ARSharedSpaceManager] Alignment root is missing. Cannot apply shared pose.");
                return;
            }

            Pose localPose = new Pose(reference.position, reference.rotation);

            Quaternion deltaRotation = sharedPose.rotation * Quaternion.Inverse(localPose.rotation);
            Vector3 deltaPosition = sharedPose.position - (deltaRotation * localPose.position);

            alignmentRoot.SetPositionAndRotation(deltaPosition, deltaRotation);

            if (logDebugMessages)
            {
                Debug.Log($"[ARSharedSpaceManager] Applied alignment. DeltaPosition={deltaPosition}, DeltaRotation={deltaRotation.eulerAngles}");
            }

            AlignmentPoseChanged?.Invoke(alignmentRoot);
        }

        /// <summary>
        /// Tries to get the current alignment root transform.
        /// </summary>
        public static bool TryGetAlignmentRoot(out Transform root)
        {
            if (_instance != null && _instance.alignmentRoot != null)
            {
                root = _instance.alignmentRoot;
                return true;
            }

            root = null;
            return false;
        }

        [Serializable]
        private struct AnchorData
        {
            public Vector3 position;
            public Quaternion rotation;

            public AnchorData(Pose pose)
            {
                position = pose.position;
                rotation = pose.rotation;
            }

            public Pose ToPose() => new Pose(position, rotation);

            public Hashtable ToHashtable()
            {
                return new Hashtable
                {
                    { "px", position.x },
                    { "py", position.y },
                    { "pz", position.z },
                    { "rx", rotation.x },
                    { "ry", rotation.y },
                    { "rz", rotation.z },
                    { "rw", rotation.w },
                };
            }

            public static AnchorData? FromObject(object value)
            {
                if (value is Hashtable table)
                {
                    AnchorData data = new AnchorData
                    {
                        position = new Vector3(
                            GetFloat(table, "px"),
                            GetFloat(table, "py"),
                            GetFloat(table, "pz")),
                        rotation = new Quaternion(
                            GetFloat(table, "rx"),
                            GetFloat(table, "ry"),
                            GetFloat(table, "rz"),
                            GetFloat(table, "rw"))
                    };

                    return data;
                }

                return null;
            }

            private static float GetFloat(Hashtable table, string key)
            {
                if (!table.TryGetValue(key, out object rawValue))
                {
                    return 0f;
                }

                switch (rawValue)
                {
                    case float f:
                        return f;
                    case double d:
                        return (float)d;
                    case int i:
                        return i;
                    case long l:
                        return l;
                    default:
                        return 0f;
                }
            }
        }
    }
}
