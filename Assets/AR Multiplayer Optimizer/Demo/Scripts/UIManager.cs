using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class UIManager : MonoBehaviourPunCallbacks
{
	[SerializeField] private GameObject positionTextPrefab;
	[SerializeField] private Transform positionTextParent; // Optional. If not set, will try to find one.
	[SerializeField] private string playerObjectName = "Player"; // Optional hint-only
	[SerializeField] private float refreshDelayOnJoin = 0.5f; // Wait a moment for player objects to spawn

	private readonly Dictionary<int, Text> actorNumberToText = new Dictionary<int, Text>();
	private readonly Dictionary<int, Transform> actorNumberToTransform = new Dictionary<int, Transform>();

	void Awake()
	{
		// Try to auto-locate a default parent under Canvas
		if (positionTextParent == null)
		{
			var spawner = GameObject.Find("PositionTextSpawner");
			if (spawner != null)
			{
				positionTextParent = spawner.transform;
			}
			else
			{
				Canvas canvas = FindObjectOfType<Canvas>();
				if (canvas != null)
				{
					positionTextParent = canvas.transform;
				}
			}
		}
	}

	void Update()
	{
		// Update all known players' position text each frame
		if (actorNumberToText.Count == 0)
		{
			return;
		}

		foreach (var kvp in actorNumberToText)
		{
			int actorNumber = kvp.Key;
			Text text = kvp.Value;

			Transform targetTransform = null;
			actorNumberToTransform.TryGetValue(actorNumber, out targetTransform);

			if (targetTransform == null)
			{
				// Try to (re)locate the transform if missing
				var views = FindObjectsOfType<PhotonView>();
				for (int i = 0; i < views.Length; i++)
				{
					if (views[i].OwnerActorNr == actorNumber)
					{
						// Prefer an object with the hint name, otherwise take the first match
						if (views[i].gameObject.name == playerObjectName || targetTransform == null)
						{
							targetTransform = views[i].transform;
							actorNumberToTransform[actorNumber] = targetTransform;
							// Don't break if this wasn't the hinted name; keep looking for a better match
						}
					}
				}
			}

			if (text != null)
			{
				if (targetTransform != null)
				{
					Vector3 p = targetTransform.position;
					text.text = $"P{actorNumber} ({p.x:F1}, {p.y:F1}, {p.z:F1})";
				}
				else
				{
					text.text = $"P{actorNumber} (---)";
				}
			}
		}
	}

	public override void OnJoinedRoom()
	{
		StartCoroutine(RefreshPlayersAfterDelay());
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		AddOrUpdatePlayerUI(newPlayer.ActorNumber);
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		RemovePlayerUI(otherPlayer.ActorNumber);
	}

	private IEnumerator RefreshPlayersAfterDelay()
	{
		yield return new WaitForSeconds(refreshDelayOnJoin);
		SyncExistingPlayers();
	}

	private void SyncExistingPlayers()
	{
		// Ensure we have UI for everyone currently in the room
		if (!PhotonNetwork.InRoom)
		{
			return;
		}

		var players = PhotonNetwork.PlayerList;
		for (int i = 0; i < players.Length; i++)
		{
			AddOrUpdatePlayerUI(players[i].ActorNumber);
		}
	}

	private void AddOrUpdatePlayerUI(int actorNumber)
	{
		if (positionTextPrefab == null)
		{
			Debug.LogWarning("UIManager: PositionText prefab is not assigned.");
			return;
		}

		Text text;
		if (!actorNumberToText.TryGetValue(actorNumber, out text) || text == null)
		{
			var go = Instantiate(positionTextPrefab, positionTextParent);
			text = go.GetComponent<Text>();
			actorNumberToText[actorNumber] = text;
		}

		// Try to bind to an existing player transform
		var views = FindObjectsOfType<PhotonView>();
		Transform best = null;
		for (int i = 0; i < views.Length; i++)
		{
			if (views[i].OwnerActorNr == actorNumber)
			{
				if (views[i].gameObject.name == playerObjectName)
				{
					best = views[i].transform;
					break; // exact match
				}
				if (best == null)
				{
					best = views[i].transform; // fallback to first owned object
				}
			}
		}
		if (best != null)
		{
			actorNumberToTransform[actorNumber] = best;
		}
	}

	private void RemovePlayerUI(int actorNumber)
	{
		if (actorNumberToText.ContainsKey(actorNumber))
		{
			var text = actorNumberToText[actorNumber];
			if (text != null)
			{
				Destroy(text.gameObject);
			}
			actorNumberToText.Remove(actorNumber);
		}

		if (actorNumberToTransform.ContainsKey(actorNumber))
		{
			actorNumberToTransform.Remove(actorNumber);
		}
	}
}
