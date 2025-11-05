using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures every PlayerController in the scene automatically receives an
/// AMOPlayerAnchorSync component at runtime so no manual setup is required.
/// </summary>
[AddComponentMenu("")]
[DefaultExecutionOrder(-5500)]
public class AMOPlayerAnchorSyncInstaller : MonoBehaviour
{
    private static AMOPlayerAnchorSyncInstaller instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        var go = new GameObject("AMOPlayerAnchorSyncInstaller");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AMOPlayerAnchorSyncInstaller>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        InstallAll();
        InvokeRepeating(nameof(InstallAll), 0.5f, 0.5f);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        CancelInvoke(nameof(InstallAll));
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallAll();
    }

    private void InstallAll()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        var controllers = Object.FindObjectsOfType<PlayerController>(true);
        foreach (var controller in controllers)
        {
            if (controller == null)
            {
                continue;
            }

            if (controller.GetComponent<AMOPlayerAnchorSync>() == null)
            {
                controller.gameObject.AddComponent<AMOPlayerAnchorSync>();
            }
        }
    }
}
