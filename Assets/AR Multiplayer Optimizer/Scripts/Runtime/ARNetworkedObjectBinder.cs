using UnityEngine;

namespace AR.Multiplayer.Optimizer
{
    /// <summary>
    /// Ensures that a network-instantiated object is parented under the shared alignment root so that
    /// all world space offsets are automatically corrected once the <see cref="ARSharedSpaceManager"/>
    /// applies the anchor transform.
    /// </summary>
    [DisallowMultipleComponent]
    public class ARNetworkedObjectBinder : MonoBehaviour
    {
        [Tooltip("If enabled, the component keeps listening for alignment updates even after it successfully binds once.")]
        [SerializeField] private bool reapplyOnAlignmentUpdates = true;

        [Tooltip("If true, the object keeps its current world transform when reparented under the alignment root.")]
        [SerializeField] private bool keepWorldPosition = true;

        private void OnEnable()
        {
            ARSharedSpaceManager.AlignmentRootReady += OnAlignmentRootReady;
            if (reapplyOnAlignmentUpdates)
            {
                ARSharedSpaceManager.AlignmentPoseChanged += OnAlignmentPoseChanged;
            }

            TryAttach();
        }

        private void OnDisable()
        {
            ARSharedSpaceManager.AlignmentRootReady -= OnAlignmentRootReady;
            if (reapplyOnAlignmentUpdates)
            {
                ARSharedSpaceManager.AlignmentPoseChanged -= OnAlignmentPoseChanged;
            }
        }

        private void OnAlignmentRootReady(Transform root)
        {
            TryAttach();
        }

        private void OnAlignmentPoseChanged(Transform root)
        {
            if (transform.parent != root)
            {
                TryAttach();
            }
        }

        private void TryAttach()
        {
            if (ARSharedSpaceManager.TryGetAlignmentRoot(out Transform root) && root != null)
            {
                if (transform.parent != root)
                {
                    transform.SetParent(root, keepWorldPosition);
                }
            }
        }
    }
}
