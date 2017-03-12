#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

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
            m_targetInstance.PlayPfx(UnitParticleFx.Attack);
        }

        if (GUILayout.Button("Play Got Hit"))
        {
            m_targetInstance.PlayPfx(UnitParticleFx.GotHit);
        }

        if (GUILayout.Button("Play Death"))
        {
            m_targetInstance.PlayPfx(UnitParticleFx.Death);
        }
    }
	
}
#endif