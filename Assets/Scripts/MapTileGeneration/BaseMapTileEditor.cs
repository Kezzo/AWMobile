#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script to enable function of the BaseMapTile class in the editor.
/// </summary>
[CustomEditor(typeof(BaseMapTile))]
public class BaseMapTileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        BaseMapTile baseMapTile = (BaseMapTile)target;

        if (GUILayout.Button("Validate"))
        {
            baseMapTile.Validate();
        }
    }

}
#endif