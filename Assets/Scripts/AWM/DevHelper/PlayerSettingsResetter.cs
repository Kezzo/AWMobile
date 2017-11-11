#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AWM.DevHelper
{
    public class PlayerSettingsResetter
    {
        [MenuItem("Tools/ResetPlayerPrefs")]
        private static void ResetPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
        }
	
    }
}
#endif