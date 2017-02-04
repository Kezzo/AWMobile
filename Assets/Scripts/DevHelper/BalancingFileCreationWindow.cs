using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BalancingFileCreationWindow : EditorWindow
{
    private readonly List<string> m_propertyNamesToDisplay = new List<string>
    {
        "m_UnitMetaType",
        "m_MovementRangePerRound",
        "m_WalkableMapTileTypes",
        "m_AttackRange",
        "m_AttackableUnitMetaTypes",
        "m_Damage",
        "m_Health"
    };

    private UnitType m_unitTypeToEdit;
    private UnitBalancingData m_unitBalancingData;

    [MenuItem("Tools/Create Balancing File")]
    private static void CreateWindows()
    {
        BalancingFileCreationWindow balancingFileCreationWindow =
            (BalancingFileCreationWindow) GetWindow(typeof(BalancingFileCreationWindow));

        balancingFileCreationWindow.Show();
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Load Balancing"))
        {
            m_unitBalancingData = GetUnitBalancingDataFromFile();
        }

        m_unitTypeToEdit = (UnitType) EditorGUILayout.EnumPopup("Unit type: ", m_unitTypeToEdit);

        if (m_unitBalancingData != null)
        {
            SerializedObject serializedObject = new SerializedObject(m_unitBalancingData);

            EditorGUILayout.Space();

            for (int propertyIndex = 0; propertyIndex < m_propertyNamesToDisplay.Count; propertyIndex++)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(m_propertyNamesToDisplay[propertyIndex]), true);
            }

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
            ControllerContainer.AssetDatabaseService.UpdateExistingAssetFile(m_unitBalancingData);
        }
    }

    /// <summary>
    /// Gets the unit balancing data from file.
    /// </summary>
    /// <returns></returns>
    private UnitBalancingData GetUnitBalancingDataFromFile()
    {
        return ControllerContainer.AssetDatabaseService.GetAssetDataAtPath<UnitBalancingData>(string.Format("Balancing/{0}", m_unitTypeToEdit));
    }
}
