using System.Reflection;
using UnityEngine;
#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

/// <summary>
/// [AUTOMATIC] Keeps demo PlayerController scripts compatible with AMO's anchor-relative sync.
/// The fixup simply mirrors Photon ownership into the controller's private isLocal flag while leaving
/// the behaviour enabled so its interpolation logic continues to run for remote players.
/// </summary>
#if PUN_2_OR_NEWER || PHOTON_UNITY_NETWORKING
public class AMOPlayerRuntimeFixup : MonoBehaviour
{
        private PhotonView photonView;
        private Component playerControllerComponent;
        private System.Type playerControllerType;
        private FieldInfo isLocalPlayerField;
        private bool cachedIsMine;
        private bool attemptedDiscovery;

        public void Initialize(Component controller)
        {
                playerControllerComponent = controller;
                playerControllerType = controller != null ? controller.GetType() : null;
                CacheReflectionFields();
                ApplyState();
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

                ApplyState();
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
        }

        private void Update()
        {
                if (photonView == null || playerControllerComponent == null)
                        return;

                if (cachedIsMine != photonView.IsMine)
                {
                        cachedIsMine = photonView.IsMine;
                }

                ApplyState();
        }

        private void ApplyState()
        {
                if (playerControllerComponent == null)
                        return;

                if (playerControllerComponent is MonoBehaviour behaviour && !behaviour.enabled)
                {
                        behaviour.enabled = true;
                }

                bool isMine = photonView != null && photonView.IsMine;
                isLocalPlayerField?.SetValue(playerControllerComponent, isMine);
        }
}
#else
public class AMOPlayerRuntimeFixup : MonoBehaviour { }
#endif
