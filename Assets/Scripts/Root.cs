using UnityEngine;
using UnityEngine.SceneManagement;

public class Root : MonoBehaviour
{
    [SerializeField]
    private string m_initialSceneToLoad;

    [SerializeField]
    private CoroutineHelper m_coroutineHelper;
    public CoroutineHelper CoroutineHelper { get { return m_coroutineHelper; } }

    // Singleton
    private static Root m_instance = null;
    public static Root Instance
    {
        get { return m_instance; }
    }

    private void Awake()
    {
        Application.targetFrameRate = 30;
        Screen.SetResolution((int) (Screen.width * 0.7f), (int) (Screen.height * 0.7f), false);

        Input.multiTouchEnabled = false;

        if (m_instance == null)
        {
            m_instance = this;
        }
        else if (m_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        Initialize();

        CoroutineHelper.CallDelayed(this, 5f, LogScreenResolution);
    }

    /// <summary>
    /// Initializes the root.
    /// </summary>
    private void Initialize()
    {
        SceneManager.LoadScene(m_initialSceneToLoad, LoadSceneMode.Additive);
    }

    /// <summary>
    /// Logs the screen resolution.
    /// </summary>
    private void LogScreenResolution()
    {
        Debug.Log("Resolution: " + Screen.currentResolution);
    }
}
