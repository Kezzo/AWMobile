using UnityEngine;

public class Root : MonoBehaviour
{
    [SerializeField]
    private string m_initialSceneToLoad;

    [SerializeField]
    private CoroutineHelper m_coroutineHelper;
    public CoroutineHelper CoroutineHelper { get { return m_coroutineHelper; } }

    [SerializeField]
    private SceneLoadingService m_sceneLoadingService;
    public SceneLoadingService SceneLoading { get { return m_sceneLoadingService; } }

#if UNITY_EDITOR
    [SerializeField]
    private DebugValues m_debugValues;
    public DebugValues DebugValues { get { return m_debugValues; } }
#endif

    // Singleton
    private static Root m_instance = null;
    public static Root Instance
    {
        get { return m_instance; }
    }

    private void Awake()
    {
        Application.targetFrameRate = 30;
        Screen.SetResolution((int) (Screen.width * 0.7f), (int) (Screen.height * 0.7f), true);

        Input.multiTouchEnabled = true;

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

        CoroutineHelper.CallDelayed(this, 1f, LogScreenResolution);
    }

    /// <summary>
    /// Initializes the root.
    /// </summary>
    private void Initialize()
    {
        ControllerContainer.UnitBalancingProvider.InitializeBalancingData();

        ControllerContainer.BattleController.IntializeBattle(new[] {
            new Team
            {
                //TODO: Move this setup into Scriptable Object
                m_TeamColor = TeamColor.Blue,
                m_IsPlayersTeam = true
            },
            new Team
            {
                m_TeamColor = TeamColor.Red,
                m_IsPlayersTeam = false
            },
            new Team
            {
                m_TeamColor = TeamColor.Yellow,
                m_IsPlayersTeam = false
            },
            new Team
            {
                m_TeamColor = TeamColor.Green,
                m_IsPlayersTeam = false
            }
        });

        SceneLoading.LoadSceneAsync("BattlegroundUI", null, () =>
        {
            SceneLoading.LoadSceneAsync(m_initialSceneToLoad, null, () =>
            {
                MapTileGeneratorEditor mapTileGeneratorEditor;

                if (ControllerContainer.MonoBehaviourRegistry.TryGet(out mapTileGeneratorEditor))
                {
                    mapTileGeneratorEditor.LoadExistingMap("Level1");
                }

                ControllerContainer.BattleController.StartBattle();
            });
        });
    }

    /// <summary>
    /// Logs the screen resolution.
    /// </summary>
    private void LogScreenResolution()
    {
        Debug.Log("Resolution: " + Screen.currentResolution);
    }
}
