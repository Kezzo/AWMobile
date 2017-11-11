using UnityEngine;

namespace AWM.EditorAndDebugOnly
{
    public class PathfindingNodeDebugData
    {
        public int CostToMoveToNode { get; set; }
        public int NodePriority { get; set; }

        public Vector2 PreviousNode { get; set; }
    }
}
