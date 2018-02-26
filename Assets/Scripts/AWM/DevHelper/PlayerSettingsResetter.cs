#if UNITY_EDITOR
using AWM.Models;
using AWM.System;
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
            Debug.Log("Resetted PlayerPrefs!");
        }

        [MenuItem("Tools/UnlockAllLevels")]
        private static void UnlockAllLevels()
        {
            int levelCount = 1;

            while (true)
            {
                string levelName = string.Format("Levels/Level{0}", levelCount);
                MapGenerationData mapGenerationData = CC.ADS.GetAssetDataAtPath<MapGenerationData>(levelName);

                if (mapGenerationData == null)
                {
                    break;
                }
                else
                {
                    CC.PPS.TrackLevelAsCompleted(mapGenerationData.m_LevelName);
                    CC.PPS.LastUnlockedLevel = mapGenerationData.m_LevelName;
                }

                levelCount++;
            }

            Debug.Log("Unlocked all Levels!");
        }

    }
}
#endif