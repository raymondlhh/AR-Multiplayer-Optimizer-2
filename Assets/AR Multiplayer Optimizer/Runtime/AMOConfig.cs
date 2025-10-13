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
}
