using System.Reflection;
using UnityEngine;
#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

/// <summary>
/// [AUTOMATIC] Ensures demo PlayerController scripts defer to AMOObjectPositionSync for network transforms.
/// The fixup disables remote PlayerController updates, keeps local control intact, and normalizes
/// the private synchronization fields so AMO's anchor-relative replication drives the visuals.
/// </summary>
#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
public class AMOPlayerRuntimeFixup : MonoBehaviour
{
        private PhotonView photonView;
        private Component playerControllerComponent;
        private System.Type playerControllerType;
        private FieldInfo isLocalPlayerField;
        private FieldInfo networkPositionField;
        private FieldInfo networkRotationField;
        private bool cachedIsMine;
        private bool attemptedDiscovery;

        public void Initialize(Component controller)
        {
                playerControllerComponent = controller;
                playerControllerType = controller != null ? controller.GetType() : null;
                CacheReflectionFields();
                ApplyState(true);
        }

        private void Awake()
        {
                photonView = GetComponent<PhotonView>();
                if (photonView != null)
                {
                        cachedIsMine = photonView.IsMine;
                }
        }

        private void Start()
        {
                if (playerControllerComponent == null)
                {
                        DiscoverController();
                        CacheReflectionFields();
                }

                ApplyState(true);
        }

        private void DiscoverController()
        {
                if (attemptedDiscovery)
                        return;

                attemptedDiscovery = true;
                var type = System.Type.GetType("PlayerController");
                if (type == null)
                {
                        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                                type = assembly.GetType("PlayerController");
                                if (type != null)
                                        break;
                        }
                }

                if (type == null)
                        return;

                playerControllerComponent = GetComponent(type);
                playerControllerType = type;
        }

        private void CacheReflectionFields()
        {
                if (playerControllerComponent == null)
                        return;

                playerControllerType ??= playerControllerComponent.GetType();
                if (playerControllerType == null)
                        return;

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                isLocalPlayerField = playerControllerType.GetField("isLocalPlayer", flags);
                networkPositionField = playerControllerType.GetField("networkPosition", flags);
                networkRotationField = playerControllerType.GetField("networkRotation", flags);
        }

        private void Update()
        {
                if (photonView == null || playerControllerComponent == null)
                        return;

                if (cachedIsMine != photonView.IsMine)
                {
                        cachedIsMine = photonView.IsMine;
                        ApplyState(true);
                }

                if (!photonView.IsMine)
                {
                        ClampRemoteSmoothingTargets();
                }
        }

        private void ApplyState(bool force)
        {
                if (playerControllerComponent == null)
                        return;

                bool isMine = photonView != null && photonView.IsMine;

                if (playerControllerComponent is MonoBehaviour behaviour && behaviour.enabled != isMine)
                {
                        behaviour.enabled = isMine;
                }

                isLocalPlayerField?.SetValue(playerControllerComponent, isMine);

                if (force || !isMine)
                {
                        ClampRemoteSmoothingTargets();
                }
        }

        private void ClampRemoteSmoothingTargets()
        {
                if (playerControllerComponent == null)
                        return;

                if (networkPositionField != null)
                {
                        networkPositionField.SetValue(playerControllerComponent, transform.position);
                }

                if (networkRotationField != null)
                {
                        networkRotationField.SetValue(playerControllerComponent, transform.rotation);
                }
        }
}
#else
public class AMOPlayerRuntimeFixup : MonoBehaviour { }
#endif
