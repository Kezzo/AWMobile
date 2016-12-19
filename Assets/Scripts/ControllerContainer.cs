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

#pragma warning restore 649
}
