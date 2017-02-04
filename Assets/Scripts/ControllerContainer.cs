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

#pragma warning restore 649
}
