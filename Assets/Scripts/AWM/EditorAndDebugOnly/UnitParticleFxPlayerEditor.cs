#if UNITY_EDITOR
using AWM.BattleVisuals;
using AWM.Enums;
using UnityEditor;
using UnityEngine;

namespace AWM.EditorAndDebugOnly
{
    [CustomEditor(typeof(UnitParticleFxPlayer))]
    public class UnitParticleFxPlayerEditor : Editor
    {
        private UnitParticleFxPlayer m_targetInstance;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (m_targetInstance == null)
            {
                m_targetInstance = (UnitParticleFxPlayer) target;
            }

            if (GUILayout.Button("Play Attack"))
            {
                m_targetInstance.PlayPfxAt(UnitParticleFx.Attack, Vector3.zero);
            }

            if (GUILayout.Button("Play Got Hit"))
            {
                m_targetInstance.PlayPfxAt(UnitParticleFx.GotHit, Vector3.zero);
            }

            if (GUILayout.Button("Play Death"))
            {
                m_targetInstance.PlayPfxAt(UnitParticleFx.Death, Vector3.zero);
            }
        }
	
    }
}
#endif