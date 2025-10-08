using Photon.Pun;
using UnityEngine;

namespace AR.Multiplayer.Optimizer
{
    /// <summary>
    /// Simple helper that reports the transform of a tracked AR reference to the
    /// <see cref="ARSharedSpaceManager"/>. Attach this to the GameObject that represents
    /// a shared physical marker (image target, plane, etc.).
    /// </summary>
    [DisallowMultipleComponent]
    public class ARReferenceAnchor : MonoBehaviour
    {
        [Tooltip("If true and this client is the Photon master client, the anchor pose will be published when the transform is registered.")]
        [SerializeField] private bool publishIfMasterClient = true;

        [Tooltip("Automatically register this transform as soon as it becomes enabled.")]
        [SerializeField] private bool autoRegisterOnEnable = true;

        private bool hasRegistered;

        /// <summary>
        /// Gets whether the reference has already been registered with the shared space manager.
        /// </summary>
        public bool HasRegistered => hasRegistered;

        private void OnEnable()
        {
            ARSharedSpaceManager.AlignmentRootReady += OnAlignmentRootReady;
            if (autoRegisterOnEnable)
            {
                TryRegisterReference();
            }
        }

        private void OnDisable()
        {
            ARSharedSpaceManager.AlignmentRootReady -= OnAlignmentRootReady;
        }

        private void OnAlignmentRootReady(Transform root)
        {
            if (autoRegisterOnEnable)
            {
                TryRegisterReference();
            }
        }

        /// <summary>
        /// Manually notifies the shared space manager that this reference transform has been localised.
        /// </summary>
        public void NotifyAnchorTracked()
        {
            TryRegisterReference();
        }

        /// <summary>
        /// Allows external systems to clear the registration flag so that a future tracking event
        /// can attempt to register again.
        /// </summary>
        public void ResetRegistration()
        {
            hasRegistered = false;
        }

        private void TryRegisterReference()
        {
            if (hasRegistered)
            {
                return;
            }

            if (ARSharedSpaceManager.Instance == null)
            {
                return;
            }

            ARSharedSpaceManager.Instance.RegisterLocalReference(transform);

            if (publishIfMasterClient && PhotonNetwork.IsMasterClient)
            {
                ARSharedSpaceManager.Instance.PublishAnchorFromReference(transform);
            }

            hasRegistered = true;
        }
    }
}
