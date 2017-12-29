using AWM.BattleMechanics;
using AWM.Controls;
using AWM.DevHelper;
using AWM.LevelSelection;
using AWM.MapTileGeneration;
using AWM.PlayerProgression;

namespace AWM.System
{
    /// <summary>
    /// Container class that holds static instances of all controller and provides easy to them.
    /// </summary>
    public static class CC
    {
#pragma warning disable 649

        private static MapTileGenerationService m_mapTileGenerationService;
        public static MapTileGenerationService MGS { get { return m_mapTileGenerationService ?? (m_mapTileGenerationService = new MapTileGenerationService()); } }

        private static MonoBehaviourRegistry m_monoBehaviourRegistry;
        public static MonoBehaviourRegistry MBR { get { return m_monoBehaviourRegistry ?? (m_monoBehaviourRegistry = new MonoBehaviourRegistry()); } }

        private static BattleStateController m_battleStateController;
        public static BattleStateController BSC { get { return m_battleStateController ?? (m_battleStateController = new BattleStateController()); } }

        private static TileNavigationController m_tileNavigationController;
        public static TileNavigationController TNC { get { return m_tileNavigationController ?? (m_tileNavigationController = new TileNavigationController()); } }

        private static UnitBalancingProvider m_unitBalancingProvider;
        public static UnitBalancingProvider UBP { get { return m_unitBalancingProvider ?? (m_unitBalancingProvider = new UnitBalancingProvider()); } }

        private static AssetDatabaseService m_assetDatabaseService;
        public static AssetDatabaseService ADS { get { return m_assetDatabaseService ?? (m_assetDatabaseService = new AssetDatabaseService()); } }

        private static LevelSelectionInitializationController m_levelSelectionInitializationController;
        public static LevelSelectionInitializationController LSIC { get { return m_levelSelectionInitializationController ?? (m_levelSelectionInitializationController = new LevelSelectionInitializationController()); } }

        private static InputBlocker m_inputBlocker;
        public static InputBlocker InputBlocker { get { return m_inputBlocker ?? (m_inputBlocker = new InputBlocker()); } }

        private static PlayerProgressionService m_playerProgressionService;
        public static PlayerProgressionService PPS
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
            m_battleStateController = new BattleStateController();
            m_tileNavigationController = new TileNavigationController();
        }

#pragma warning restore 649
    }
}
