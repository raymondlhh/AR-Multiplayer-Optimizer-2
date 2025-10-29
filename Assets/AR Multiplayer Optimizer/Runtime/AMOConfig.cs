using UnityEngine;

[CreateAssetMenu(fileName = "AMOConfig", menuName = "AR Multiplayer Optimizer/Config", order = 0)]
public class AMOConfig : ScriptableObject
{
	[Header("General")]
	public bool autoFixOnPlay = true;
	[Tooltip("Optional: Name of the Vuforia ImageTarget (Observer) to use as world center.")]
	public string imageTargetName = "";
	[Tooltip("Anchor root object that becomes the aligned root for all networked content.")]
	public string anchorRootName = "AnchorRoot";

	[Header("Multiplayer")]
	[Tooltip("Wait for all PUN2 clients to align before enabling gameplay.")]
	public bool waitForAllClients = true;

	[Header("Smoothing")]
	[Range(0.0f, 1.0f)]
	public float alignSmoothing = 0.2f;

	[Header("Debug Visualization")]
	[Tooltip("Show anchor center point visualization in both editor and build")]
	public bool showAnchorCenter = true;
	[Tooltip("Size of the anchor center visualization")]
	[Range(0.1f, 2.0f)]
	public float anchorCenterSize = 0.5f;
	[Tooltip("Color of the anchor center visualization")]
	public Color anchorCenterColor = Color.red;

	[Header("Position Stabilization")]
	[Tooltip("Enable continuous tracking to prevent object drift when phone moves")]
	public bool enablePositionStabilization = true;
	[Tooltip("How often to update anchor position (in seconds)")]
	[Range(0.01f, 1.0f)]
	public float stabilizationUpdateRate = 0.1f;
	[Tooltip("Smoothing factor for position updates (higher = smoother but slower response)")]
	[Range(0.1f, 10.0f)]
	public float stabilizationSmoothing = 2.0f;
	[Tooltip("Maximum distance the anchor can move before forcing a snap (prevents large jumps)")]
	[Range(0.1f, 2.0f)]
	public float maxAnchorDrift = 0.5f;
}
