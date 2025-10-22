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
}


