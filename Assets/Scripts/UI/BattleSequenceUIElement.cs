using System;
using UnityEngine;
using UnityEngine.UI;

public class BattleSequenceUIElement : MonoBehaviour
{
    [SerializeField]
    private BaseMapTile m_leftBaseMapTile;

    [SerializeField]
    private Image m_leftHealthLeftBar;

    [SerializeField]
    private Image m_leftDamageTakenBar;

    [SerializeField]
    private BaseMapTile m_rightBaseMapTile;

    [SerializeField]
    private Image m_rightHealthLeftBar;

    [SerializeField]
    private Image m_rightDamageTakenBar;

    [SerializeField]
    private Canvas m_battleSequenceCanvas;

    /// <summary>
    /// Initializes and starts the battle sequence.
    /// </summary>
    /// <param name="leftMapTileData">The left map tile data.</param>
    /// <param name="healthOfLeftUnit">The health of left unit.</param>
    /// <param name="rightMapTileData">The right map tile data.</param>
    /// <param name="healthOfRightUnit">The health of right unit.</param>
    /// <param name="onBattleSequenceFinished">The on battle sequence finished.</param>
    public void InitializeAndStartSequence(MapGenerationData.MapTile leftMapTileData, int healthOfLeftUnit,
        MapGenerationData.MapTile rightMapTileData, int healthOfRightUnit, Action onBattleSequenceFinished)
    {
        InputBlocker inputBlocker = new InputBlocker();
        inputBlocker.ChangeBattleControlInput(true);

        leftMapTileData.m_Unit.m_Orientation = CardinalDirection.East;
        m_leftBaseMapTile.InitializeVisually(leftMapTileData);

        rightMapTileData.m_Unit.m_Orientation = CardinalDirection.West;
        m_rightBaseMapTile.InitializeVisually(rightMapTileData);

        UpdateHealthBar(m_leftHealthLeftBar, m_leftDamageTakenBar, healthOfLeftUnit, 
            leftMapTileData.m_Unit.m_UnitType);

        UpdateHealthBar(m_rightHealthLeftBar, m_rightDamageTakenBar, healthOfRightUnit, 
            rightMapTileData.m_Unit.m_UnitType);

        m_battleSequenceCanvas.gameObject.SetActive(true);

        // Play pew pew animations here! 

        Root.Instance.CoroutineHelper.CallDelayed(this, 2f, () =>
        {
            m_battleSequenceCanvas.gameObject.SetActive(false);

            if (onBattleSequenceFinished != null)
            {
                onBattleSequenceFinished();
            }

            inputBlocker.ChangeBattleControlInput(false);
        });
    }

    /// <summary>
    /// Initializes the health bars.
    /// </summary>
    /// <param name="damageTakenBar">The damage taken bar.</param>
    /// <param name="healthLeftBar">The health left bar.</param>
    /// <param name="health">The health.</param>
    /// <param name="unitType">Type of the unit.</param>
    private void UpdateHealthBar(Image healthLeftBar, Image damageTakenBar, int health, UnitType unitType)
    {
        var unitBalancing = ControllerContainer.UnitBalancingProvider.GetUnitBalancing(unitType);

        if (unitBalancing == null)
        {
            Debug.LogError(string.Format("UnitBalancing of UnitType: '{0}' not found!", unitType));
            return;
        }

        float normalizedHealthLeft = (float)health / unitBalancing.m_Health;

        healthLeftBar.fillAmount = normalizedHealthLeft;
        damageTakenBar.fillAmount = 1 - normalizedHealthLeft;
    }
}
