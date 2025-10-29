using System;
using System.Collections;
using UnityEngine;

// [AUTOMATIC] We intentionally avoid hard dependencies on Vuforia's namespaces at compile-time
// so the package can import even before Vuforia is present. Where needed, we use
// reflection to find the ImageTarget by name and subscribe to status changes.
// This component automatically handles Vuforia ImageTarget detection and alignment.

public class AMOAnchorTracker : MonoBehaviour
{
	public event Action onAlignedOnce;

	private AMOConfig config;
	private Transform anchorRoot;
	private bool aligned;
	private UnityEngine.Object currentObserver;
	private Coroutine continuousTrackingCoroutine;
	private Vector3 lastAnchorPosition;
	private Quaternion lastAnchorRotation;

	public void Initialize(AMOConfig amoConfig, Transform anchorRootTransform)
	{
		config = amoConfig;
		anchorRoot = anchorRootTransform;
	}

	private void Start()
	{
		if (config == null || anchorRoot == null)
			return;

		if (config.autoFixOnPlay)
		{
			StartCoroutine(AutoAlignWhenTargetTracked());
		}
	}

	private IEnumerator AutoAlignWhenTargetTracked()
	{
		// Wait for Vuforia (if present) and an Observer with the chosen name
		var timeout = Time.realtimeSinceStartup + 10f;
		UnityEngine.Object observer = null;
		while (observer == null && Time.realtimeSinceStartup < timeout)
		{
			observer = FindVuforiaObserverByName(config.imageTargetName);
			yield return null;
		}

		if (observer == null)
			yield break;

		// Poll for tracking status stable; then align once
		while (!IsObserverTracked(observer))
		{
			yield return null;
		}

		SnapAnchorRootToObserver(observer);
		aligned = true;
		currentObserver = observer;
		lastAnchorPosition = anchorRoot.position;
		lastAnchorRotation = anchorRoot.rotation;
		
		// Start continuous tracking if enabled
		if (config.enablePositionStabilization)
		{
			StartContinuousTracking();
		}
		
		onAlignedOnce?.Invoke();
	}

	private UnityEngine.Object FindVuforiaObserverByName(string targetName)
	{
		if (string.IsNullOrWhiteSpace(targetName))
		{
			// If not specified, return any ObserverBehaviour in scene
			return FindObjectOfTypeByTypeName("Vuforia.ObserverBehaviour");
		}

		var all = FindObjectsOfTypeByTypeNameAll("Vuforia.ObserverBehaviour");
		foreach (var obj in all)
		{
			var n = GetFieldOrProperty<string>(obj, "TargetName");
			if (!string.IsNullOrEmpty(n) && string.Equals(n, targetName, StringComparison.Ordinal))
				return obj;
		}

		return null;
	}

	private bool IsObserverTracked(UnityEngine.Object observer)
	{
		// ObserverBehaviour.TargetStatus.Status is an enum; we check for TRACKED/EXTENDED_TRACKED
		var statusProp = observer.GetType().GetProperty("TargetStatus");
		if (statusProp == null)
			return false;

		var statusObj = statusProp.GetValue(observer, null);
		if (statusObj == null)
			return false;

		var statusEnumProp = statusObj.GetType().GetProperty("Status");
		if (statusEnumProp == null)
			return false;

		var statusEnumVal = statusEnumProp.GetValue(statusObj, null);
		var statusName = statusEnumVal != null ? statusEnumVal.ToString() : string.Empty;
		return statusName == "TRACKED" || statusName == "EXTENDED_TRACKED";
	}

	private void SnapAnchorRootToObserver(UnityEngine.Object observer)
	{
		var tr = GetFieldOrProperty<Transform>(observer, "transform");
		if (tr == null || anchorRoot == null)
			return;

		anchorRoot.SetPositionAndRotation(tr.position, tr.rotation);
	}

	private static UnityEngine.Object FindObjectOfTypeByTypeName(string typeName)
	{
		var type = Type.GetType(typeName + ", Vuforia.Unity.Engine" ) ?? Type.GetType(typeName);
		if (type == null)
			return null;
		var obj = FindObjectOfType(type);
		return obj;
	}

	private static UnityEngine.Object[] FindObjectsOfTypeByTypeNameAll(string typeName)
	{
		var type = Type.GetType(typeName + ", Vuforia.Unity.Engine") ?? Type.GetType(typeName);
		if (type == null)
			return Array.Empty<UnityEngine.Object>();
		var objs = FindObjectsOfType(type);
		return objs as UnityEngine.Object[] ?? Array.Empty<UnityEngine.Object>();
	}

	private static T GetFieldOrProperty<T>(object obj, string name) where T : class
	{
		if (obj == null) return null;
		var t = obj.GetType();
		var p = t.GetProperty(name);
		if (p != null)
		{
			var v = p.GetValue(obj, null);
			return v as T;
		}
		var f = t.GetField(name);
		if (f != null)
		{
			var v = f.GetValue(obj);
			return v as T;
		}
		return null;
	}

	/// <summary>
	/// Starts continuous tracking to prevent object drift when phone moves
	/// </summary>
	private void StartContinuousTracking()
	{
		if (continuousTrackingCoroutine != null)
		{
			StopCoroutine(continuousTrackingCoroutine);
		}
		
		continuousTrackingCoroutine = StartCoroutine(ContinuousTrackingLoop());
		Debug.Log("[AMOAnchorTracker] [AUTOMATIC] Started continuous position stabilization");
	}

	/// <summary>
	/// Stops continuous tracking
	/// </summary>
	private void StopContinuousTracking()
	{
		if (continuousTrackingCoroutine != null)
		{
			StopCoroutine(continuousTrackingCoroutine);
			continuousTrackingCoroutine = null;
			Debug.Log("[AMOAnchorTracker] [AUTOMATIC] Stopped continuous position stabilization");
		}
	}

	/// <summary>
	/// Continuous tracking loop that updates anchor position based on Image Target movement
	/// </summary>
	private IEnumerator ContinuousTrackingLoop()
	{
		while (aligned && currentObserver != null && config.enablePositionStabilization)
		{
			yield return new WaitForSeconds(config.stabilizationUpdateRate);
			
			if (IsObserverTracked(currentObserver))
			{
				UpdateAnchorPositionSmoothly();
			}
		}
	}

	/// <summary>
	/// Updates anchor position smoothly to prevent drift
	/// </summary>
	private void UpdateAnchorPositionSmoothly()
	{
		if (currentObserver == null || anchorRoot == null) return;

		var observerTransform = GetFieldOrProperty<Transform>(currentObserver, "transform");
		if (observerTransform == null) return;

		Vector3 targetPosition = observerTransform.position;
		Quaternion targetRotation = observerTransform.rotation;

		// Check if the movement is too large (prevent large jumps)
		float distance = Vector3.Distance(lastAnchorPosition, targetPosition);
		if (distance > config.maxAnchorDrift)
		{
			// Snap to new position if movement is too large
			Debug.Log($"[AMOAnchorTracker] [AUTOMATIC] Large movement detected ({distance:F2}m), snapping anchor");
			anchorRoot.SetPositionAndRotation(targetPosition, targetRotation);
			lastAnchorPosition = targetPosition;
			lastAnchorRotation = targetRotation;
		}
		else
		{
			// Smooth interpolation for small movements
			float smoothingFactor = config.stabilizationSmoothing * Time.deltaTime;
			Vector3 smoothedPosition = Vector3.Lerp(anchorRoot.position, targetPosition, smoothingFactor);
			Quaternion smoothedRotation = Quaternion.Lerp(anchorRoot.rotation, targetRotation, smoothingFactor);
			
			anchorRoot.SetPositionAndRotation(smoothedPosition, smoothedRotation);
			lastAnchorPosition = smoothedPosition;
			lastAnchorRotation = smoothedRotation;
		}
	}

	/// <summary>
	/// Public method to toggle stabilization on/off
	/// </summary>
	public void ToggleStabilization()
	{
		if (config != null)
		{
			config.enablePositionStabilization = !config.enablePositionStabilization;
			
			if (config.enablePositionStabilization && aligned)
			{
				StartContinuousTracking();
			}
			else
			{
				StopContinuousTracking();
			}
			
			Debug.Log($"[AMOAnchorTracker] Position stabilization {(config.enablePositionStabilization ? "enabled" : "disabled")}");
		}
	}

	/// <summary>
	/// Public method to set stabilization enabled/disabled
	/// </summary>
	public void SetStabilization(bool enabled)
	{
		if (config != null)
		{
			config.enablePositionStabilization = enabled;
			
			if (enabled && aligned)
			{
				StartContinuousTracking();
			}
			else
			{
				StopContinuousTracking();
			}
			
			Debug.Log($"[AMOAnchorTracker] Position stabilization {(enabled ? "enabled" : "disabled")}");
		}
	}

	private void OnDestroy()
	{
		StopContinuousTracking();
	}
}


