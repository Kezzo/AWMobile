#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor script to enable function of the BaseMapTileGroup class in the editor.
/// </summary>
[CustomEditor(typeof(BaseMapTileGroup))]
public class BaseMapTileGroupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        BaseMapTileGroup baseMapTileGroup = (BaseMapTileGroup)target;

        if (GUILayout.Button("Validate"))
        {
            baseMapTileGroup.Validate();
        }
    }
}
#endif