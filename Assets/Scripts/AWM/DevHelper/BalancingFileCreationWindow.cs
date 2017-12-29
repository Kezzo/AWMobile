#if UNITY_EDITOR
using System.Collections.Generic;
using AWM.Enums;
using AWM.Models;
using AWM.System;
using UnityEditor;
using UnityEngine;

namespace AWM.DevHelper
{
    public class BalancingFileCreationWindow : EditorWindow
    {
        private readonly List<string> m_propertyNamesToDisplay = new List<string>
        {
            "m_UnitMetaType",
            "m_MovementRangePerRound",
            "m_WalkableMapTileTypes",

            "m_PassableUnitMetaTypes",

            "m_Health",

            "m_AttackRange",
            "m_AttackableUnitMetaTypes",
            "m_DamageOnUnitsList",
        };

        private UnitType m_unitTypeToEdit;
        private UnitBalancingData m_unitBalancingData;

        private Vector2 m_scrollPosition;

        [MenuItem("Tools/Create Balancing File")]
        private static void CreateWindows()
        {
            BalancingFileCreationWindow balancingFileCreationWindow =
                (BalancingFileCreationWindow) GetWindow(typeof(BalancingFileCreationWindow));

            balancingFileCreationWindow.Show();
        }

        private void OnGUI()
        {
            UnitType unitTypeToEdit = (UnitType) EditorGUILayout.EnumPopup("Unit type: ", m_unitTypeToEdit);

            if (m_unitTypeToEdit != unitTypeToEdit)
            {
                m_unitTypeToEdit = unitTypeToEdit;
                m_unitBalancingData = GetUnitBalancingDataFromFile();
            }

            if (m_unitBalancingData != null)
            {
                SerializedObject serializedObject = new SerializedObject(m_unitBalancingData);

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.Height(600));

                for (int propertyIndex = 0; propertyIndex < m_propertyNamesToDisplay.Count; propertyIndex++)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(m_propertyNamesToDisplay[propertyIndex]), true);
                }

                EditorGUILayout.EndScrollView();

                serializedObject.ApplyModifiedProperties();

                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Create Balancing"))
            {
                m_unitBalancingData = CreateInstance<UnitBalancingData>();
                m_unitBalancingData.m_UnitType = m_unitTypeToEdit;

                string pathToAsset = string.Format("Assets/Resources/Balancing/{0}.asset", m_unitTypeToEdit);
                AssetDatabase.CreateAsset(m_unitBalancingData, pathToAsset);
                m_unitBalancingData = GetUnitBalancingDataFromFile();
            }

            if (GUILayout.Button("Update Balancing"))
            {
                CC.ADS.UpdateExistingAssetFile(m_unitBalancingData);
            }
        }

        /// <summary>
        /// Gets the unit balancing data from file.
        /// </summary>
        /// <returns></returns>
        private UnitBalancingData GetUnitBalancingDataFromFile()
        {
            return CC.ADS.GetAssetDataAtPath<UnitBalancingData>(string.Format("Balancing/{0}", m_unitTypeToEdit));
        }
    }
}
#endif