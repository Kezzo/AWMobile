using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Part of the BattleUI.
/// Handles initialization of the battle sequences.
/// </summary>
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

    [SerializeField]
    [Range(0, 1)]
    private float m_healthBarAnimationSpeed;

    /// <summary>
    /// Initializes and starts the battle sequence.
    /// </summary>
    /// <param name="leftMapTileData">The left map tile data.</param>
    /// <param name="currentHealthOfLeftUnit">The health of left unit.</param>
    /// <param name="rightMapTileData">The right map tile data.</param>
    /// <param name="currentHealthOfRightUnit">The health of right unit.</param>
    /// <param name="damageDoneToRightUnit">The damage done to right unit.</param>
    /// <param name="onBattleSequenceFinished">The on battle sequence finished.</param>
    public void InitializeAndStartSequence(MapGenerationData.MapTile leftMapTileData, int currentHealthOfLeftUnit,
        MapGenerationData.MapTile rightMapTileData, int currentHealthOfRightUnit, int damageDoneToRightUnit, Action onBattleSequenceFinished)
    {
        ControllerContainer.InputBlocker.ChangeBattleControlInput(true);

        leftMapTileData.m_Unit.m_Orientation = CardinalDirection.East;
        m_leftBaseMapTile.InitializeVisually(leftMapTileData);

        rightMapTileData.m_Unit.m_Orientation = CardinalDirection.West;
        m_rightBaseMapTile.InitializeVisually(rightMapTileData);

        //TODO: Implement counter attack
        
        StartCoroutine(AnimateHealthBar(m_rightHealthLeftBar, m_rightDamageTakenBar, 
        currentHealthOfRightUnit, damageDoneToRightUnit, rightMapTileData.m_Unit.m_UnitType, 0.3f));

        StartCoroutine(AnimateHealthBar(m_leftHealthLeftBar, m_leftDamageTakenBar,
            currentHealthOfLeftUnit, 0, leftMapTileData.m_Unit.m_UnitType, 1.3f));

        m_battleSequenceCanvas.gameObject.SetActive(true);

        // Play pew pew animations here!

        CameraControls cameraController;
        if (ControllerContainer.MonoBehaviourRegistry.TryGet(out cameraController))
        {
            cameraController.CameraLookAtPosition(ControllerContainer.TileNavigationController.GetMapTile(
                leftMapTileData.m_PositionVector).UnitRoot.position, 0.1f);
        }

        Root.Instance.CoroutineHelper.CallDelayed(this, 2f, () =>
        {
            m_battleSequenceCanvas.gameObject.SetActive(false);

            if (onBattleSequenceFinished != null)
            {
                onBattleSequenceFinished();
            }

            ControllerContainer.InputBlocker.ChangeBattleControlInput(false);
        });
    }

    /// <summary>
    /// Initializes the health bars.
    /// </summary>
    /// <param name="healthLeftBar">The health left bar.</param>
    /// <param name="damageTakenBar">The damage taken bar.</param>
    /// <param name="health">The health.</param>
    /// <param name="damageTaken">The damage taken.</param>
    /// <param name="unitType">Type of the unit.</param>
    /// <param name="delayBy">The delay by.</param>
    /// <returns></returns>
    private IEnumerator AnimateHealthBar(Image healthLeftBar, Image damageTakenBar, int health, int damageTaken, UnitType unitType, float delayBy)
    {
        var unitBalancing = ControllerContainer.UnitBalancingProvider.GetUnitBalancing(unitType);

        if (unitBalancing == null)
        {
            Debug.LogError(string.Format("UnitBalancing of UnitType: '{0}' not found!", unitType));

            yield break;
        }

        float currentHealthAsFloat = health;

        UpdateHealthBar(healthLeftBar, damageTakenBar, unitBalancing.m_Health, currentHealthAsFloat);

        yield return new WaitForSeconds(delayBy);

        float finalHealth = Mathf.Clamp(health - damageTaken, 0, unitBalancing.m_Health);

        while (currentHealthAsFloat > finalHealth)
        {
            float healthToDeductThisFrame = damageTaken * m_healthBarAnimationSpeed * Time.deltaTime;

            currentHealthAsFloat -= healthToDeductThisFrame;
            UpdateHealthBar(healthLeftBar, damageTakenBar, unitBalancing.m_Health, currentHealthAsFloat);

            yield return null;
        }
    }

    /// <summary>
    /// Updates the health bar.
    /// </summary>
    /// <param name="healthLeftBar">The health left bar.</param>
    /// <param name="damageTakenBar">The damage taken bar.</param>
    /// <param name="maxHealth">The maximum health.</param>
    /// <param name="currentHealthAsFloat">The current health as float.</param>
    private void UpdateHealthBar(Image healthLeftBar, Image damageTakenBar, int maxHealth, float currentHealthAsFloat)
    {
        float normalizedHealthLeft = currentHealthAsFloat / maxHealth;

        healthLeftBar.fillAmount = normalizedHealthLeft;
        damageTakenBar.fillAmount = 1 - normalizedHealthLeft;
    }
}
