using UnityEngine;

namespace AWM.DevHelper
{
    public class DebugValues : MonoBehaviour
    {
#if UNITY_EDITOR
        public bool m_ShowCoordinatesOnNodes;
        public bool m_ShowPathfindingDebugData;
#endif
    }
}
