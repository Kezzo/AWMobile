using UnityEditor;
using UnityEngine;

public class AssetDatabaseService
{

#if UNITY_EDITOR

    /// <summary>
    /// Updates the existing asset file.
    /// </summary>
    public void UpdateExistingAssetFile(Object assetToUpdate)
    {
        AssetDatabase.Refresh();
        EditorUtility.SetDirty(assetToUpdate);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Gets the map generation data at path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    public T GetAssetDataAtPath<T>(string path) where T : ScriptableObject
    {
        T mapGenerationDataToReturn;

        if (Application.isPlaying)
        {
            mapGenerationDataToReturn = (T)Resources.Load(path, typeof(T));
        }
        else
        {
            string assetPath = string.Format("Assets/Resources/{0}.asset", path);

            Debug.LogFormat("Loading asset from path: '{0}'", assetPath);

            mapGenerationDataToReturn = (T)AssetDatabase.LoadAssetAtPath(assetPath, typeof(T));
        }

        return mapGenerationDataToReturn;
    }

#else

    /// <summary>
    /// Gets the map generation data at path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    public T GetAssetDataAtPath<T>(string path) where T : ScriptableObject
    {
        return (T)Resources.Load(path, typeof(T));
    }

#endif

}
