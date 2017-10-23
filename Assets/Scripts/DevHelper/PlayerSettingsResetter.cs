using UnityEditor;
using UnityEngine;

public class PlayerSettingsResetter
{
    [MenuItem("Tools/ResetPlayerPrefs")]
    private static void ResetPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
    }
	
}
