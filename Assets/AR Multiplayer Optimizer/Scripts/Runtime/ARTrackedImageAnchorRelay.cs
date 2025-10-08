using UnityEngine;

namespace AR.Multiplayer.Optimizer
{
    /// <summary>
    /// Optional helper for AR Foundation projects. When AR Foundation is present this component listens
    /// to <see cref="UnityEngine.XR.ARFoundation.ARTrackedImageManager"/> events and forwards tracking
    /// updates to an <see cref="ARReferenceAnchor"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class ARTrackedImageAnchorRelay : MonoBehaviour
    {
#if UNITY_XR_ARFOUNDATION || AR_FOUNDATION_PRESENT
        [SerializeField] private UnityEngine.XR.ARFoundation.ARTrackedImageManager trackedImageManager;
        [SerializeField] private ARReferenceAnchor referenceAnchor;

        private void OnEnable()
        {
            if (trackedImageManager == null)
            {
                trackedImageManager = FindObjectOfType<UnityEngine.XR.ARFoundation.ARTrackedImageManager>();
            }

            if (referenceAnchor == null)
            {
                referenceAnchor = GetComponent<ARReferenceAnchor>();
            }

            if (trackedImageManager != null)
            {
                trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
            }
        }

        private void OnDisable()
        {
            if (trackedImageManager != null)
            {
                trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
            }
        }

        private void OnTrackedImagesChanged(UnityEngine.XR.ARFoundation.ARTrackedImagesChangedEventArgs args)
        {
            foreach (var trackedImage in args.added)
            {
                HandleTrackedImage(trackedImage);
            }

            foreach (var trackedImage in args.updated)
            {
                HandleTrackedImage(trackedImage);
            }
        }

        private void HandleTrackedImage(UnityEngine.XR.ARFoundation.ARTrackedImage trackedImage)
        {
            if (trackedImage == null)
            {
                return;
            }

            if (trackedImage.trackingState != UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                return;
            }

            if (referenceAnchor == null)
            {
                referenceAnchor = trackedImage.GetComponent<ARReferenceAnchor>();
                if (referenceAnchor == null)
                {
                    referenceAnchor = trackedImage.gameObject.AddComponent<ARReferenceAnchor>();
                }
            }

            referenceAnchor.NotifyAnchorTracked();
        }
#else
        [Tooltip("The relay requires AR Foundation to be installed. Define AR_FOUNDATION_PRESENT or enable the package to activate it.")]
        [SerializeField] private string information = "Install AR Foundation to enable tracking relay.";
#endif
    }
}
