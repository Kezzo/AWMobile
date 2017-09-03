using UnityEngine;

public class LevelSelector : MonoBehaviour
{
    private string m_levelName;

    /// <summary>
    /// Sets the name of the level this selector should start.
    /// </summary>
    /// <param name="levelName">Name of the level.</param>
    public void SetLevelName(string levelName)
    {
        m_levelName = levelName;
    }

    /// <summary>
    /// Called when this LevelSelector was selected.
    /// </summary>
    public void OnSelected()
    {
        Debug.Log(string.Format("Selected LevelSelector representing level: {0}", m_levelName));
    }
}
