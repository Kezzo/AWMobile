using System.Linq.Expressions;

/// <summary>
/// Container class that holds static instances of all controller and provides easy to them.
/// </summary>
public static class ControllerContainer
{
#pragma warning disable 649

    private static MapTileGenerationService m_mapTileGenerationService;
    public static MapTileGenerationService MapTileGenerationService { get { return m_mapTileGenerationService ?? (m_mapTileGenerationService = new MapTileGenerationService()); } }

    private static MonoBehaviourRegistry m_monoBehaviourRegistry;
    public static MonoBehaviourRegistry MonoBehaviourRegistry { get { return m_monoBehaviourRegistry ?? (m_monoBehaviourRegistry = new MonoBehaviourRegistry()); } }

    private static BattleController m_battleController;
    public static BattleController BattleController { get { return m_battleController ?? (m_battleController = new BattleController()); } }

    private static TileNavigationController m_tileNavigationController;
    public static TileNavigationController TileNavigationController { get { return m_tileNavigationController ?? (m_tileNavigationController = new TileNavigationController()); } }

    private static UnitBalancingProvider m_unitBalancingProvider;
    public static UnitBalancingProvider UnitBalancingProvider { get { return m_unitBalancingProvider ?? (m_unitBalancingProvider = new UnitBalancingProvider()); } }

    private static AssetDatabaseService m_assetDatabaseService;
    public static AssetDatabaseService AssetDatabaseService { get { return m_assetDatabaseService ?? (m_assetDatabaseService = new AssetDatabaseService()); } }

    private static LevelSelectionInitializationController m_levelSelectionInitializationController;
    public static LevelSelectionInitializationController LevelSelectionInitializationController { get { return m_levelSelectionInitializationController ?? (m_levelSelectionInitializationController = new LevelSelectionInitializationController()); } }

    private static InputBlocker m_inputBlocker;
    public static InputBlocker InputBlocker { get { return m_inputBlocker ?? (m_inputBlocker = new InputBlocker()); } }

    private static PlayerProgressionService m_playerProgressionService;
    public static PlayerProgressionService PlayerProgressionService
    {
        get
        {
            return m_playerProgressionService ?? 
                (m_playerProgressionService = new PlayerProgressionService(new NewtonsoftJsonSerializer(), new PlayerPrefsStorageHelper()));
        }
    }

    /// <summary>
    /// Resets this instance.
    /// Will only reset stateful controller.
    /// </summary>
    public static void Reset()
    {
        m_monoBehaviourRegistry = new MonoBehaviourRegistry();
        m_battleController = new BattleController();
        m_tileNavigationController = new TileNavigationController();
    }

#pragma warning restore 649
}
