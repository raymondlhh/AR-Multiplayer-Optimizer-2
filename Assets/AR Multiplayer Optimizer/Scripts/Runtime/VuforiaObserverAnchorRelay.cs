using UnityEngine;

namespace AR.Multiplayer.Optimizer
{
    /// <summary>
    /// Bridges Vuforia observer tracking events into the shared space workflow by forwarding
    /// tracking notifications to an <see cref="ARReferenceAnchor"/>.
    /// Attach this to the same GameObject as a Vuforia <see cref="Vuforia.ObserverBehaviour"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class VuforiaObserverAnchorRelay : MonoBehaviour
    {
#if VUFORIA || VUFORIA_ENABLED || VUFORIA_PRESENT
        [SerializeField] private Vuforia.ObserverBehaviour observer;
        [SerializeField] private ARReferenceAnchor referenceAnchor;

        [Header("Tracking")]
        [Tooltip("If true, EXTENDED_TRACKED targets are also considered valid for alignment registration.")]
        [SerializeField] private bool acceptExtendedTracking = true;

        [Tooltip("If true, the relay logs tracking transitions for easier debugging.")]
        [SerializeField] private bool logDebugMessages = true;

        private bool hasDispatched;

        private void Reset()
        {
            observer = GetComponent<Vuforia.ObserverBehaviour>();
            referenceAnchor = GetComponent<ARReferenceAnchor>();
        }

        private void Awake()
        {
            if (observer == null)
            {
                observer = GetComponent<Vuforia.ObserverBehaviour>();
            }

            if (referenceAnchor == null)
            {
                referenceAnchor = GetComponent<ARReferenceAnchor>();
            }
        }

        private void OnEnable()
        {
            if (observer == null)
            {
                observer = GetComponent<Vuforia.ObserverBehaviour>();
            }

            if (observer != null)
            {
                observer.OnTargetStatusChanged += OnTargetStatusChanged;
            }
            else if (logDebugMessages)
            {
                Debug.LogWarning("[VuforiaObserverAnchorRelay] Missing ObserverBehaviour reference.");
            }
        }

        private void OnDisable()
        {
            if (observer != null)
            {
                observer.OnTargetStatusChanged -= OnTargetStatusChanged;
            }

            hasDispatched = false;
        }

        private void OnTargetStatusChanged(Vuforia.ObserverBehaviour behaviour, Vuforia.TargetStatus targetStatus)
        {
            var status = targetStatus.Status;
            if (status == Vuforia.Status.TRACKED || (acceptExtendedTracking && status == Vuforia.Status.EXTENDED_TRACKED))
            {
                if (hasDispatched)
                {
                    return;
                }

                EnsureReferenceAnchor(behaviour);
                if (referenceAnchor == null)
                {
                    if (logDebugMessages)
                    {
                        Debug.LogWarning("[VuforiaObserverAnchorRelay] Unable to locate ARReferenceAnchor for tracked observer.");
                    }
                    return;
                }

                referenceAnchor.NotifyAnchorTracked();

                if (!referenceAnchor.HasRegistered)
                {
                    hasDispatched = false;
                    return;
                }

                if (logDebugMessages)
                {
                    Debug.Log($"[VuforiaObserverAnchorRelay] Target '{behaviour.TargetName}' tracked with status {status}.");
                }

                hasDispatched = true;
            }
            else if (status == Vuforia.Status.NO_POSE || status == Vuforia.Status.LIMITED)
            {
                hasDispatched = false;
            }
        }

        private void EnsureReferenceAnchor(Vuforia.ObserverBehaviour behaviour)
        {
            if (referenceAnchor != null)
            {
                return;
            }

            referenceAnchor = behaviour.GetComponent<ARReferenceAnchor>();
            if (referenceAnchor == null)
            {
                referenceAnchor = behaviour.gameObject.AddComponent<ARReferenceAnchor>();
            }
        }
#else
        [Tooltip("Install Vuforia Engine or define VUFORIA_PRESENT to enable this relay.")]
        [SerializeField] private string information = "Vuforia not detected.";
#endif
    }
}
