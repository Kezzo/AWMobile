using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper component to handle stat management of a unit (health, damage calculation etc.)
/// </summary>
public class UnitStatManagement : MonoBehaviour
{
    [SerializeField]
    private Canvas m_healthBarCanvas;

    [SerializeField]
    private Image[] m_healthBars;

    [Serializable]
    public class HealthBarSpriteMapping
    {
        public int m_Size;
        public Sprite m_Sprite;
    }

    [SerializeField]
    private List<HealthBarSpriteMapping> m_healthBarSpriteMapping;

    [SerializeField]
    private List<HealthBarSpriteMapping> m_missingHealthBarSpriteMapping;

    private int m_currentHealth;
    public int CurrentHealth { get { return m_currentHealth; } }

    public bool IsDead { get { return CurrentHealth == 0; } }

    private int m_maxHealth;
    private BaseUnit m_baseUnit;

    private const int GeneralUnitMaxHealth = 8;
    private int m_factorOfGeneralMaxHealth;

    private Sprite m_healthSpriteToUse;
    private Sprite m_missingHealthSpriteToUse;

    private Coroutine m_runningHealthBarBlinking;

    /// <summary>
    /// Initializes stat management with the base stat values.
    /// </summary>
    /// <param name="baseUnit">The base unit.</param>
    /// <param name="maxHealth">The maximum health.</param>
    public void Initialize(BaseUnit baseUnit, int maxHealth)
    {
        m_baseUnit = baseUnit;

        m_maxHealth = maxHealth;
        m_currentHealth = maxHealth;

        m_healthBarCanvas.gameObject.SetActive(false);

        m_factorOfGeneralMaxHealth = GeneralUnitMaxHealth/maxHealth;

        m_healthSpriteToUse = m_healthBarSpriteMapping.Find(mapping => mapping.m_Size == m_factorOfGeneralMaxHealth).m_Sprite;
        m_missingHealthSpriteToUse = m_missingHealthBarSpriteMapping.Find(mapping => mapping.m_Size == m_factorOfGeneralMaxHealth).m_Sprite;

        for (int i = 0; i < m_healthBars.Length; i++)
        {
            bool activate = i%m_factorOfGeneralMaxHealth == 0;
            m_healthBars[i].gameObject.SetActive(activate);

            if (activate)
            {
                m_healthBars[i].sprite = m_healthSpriteToUse;
                m_healthBars[i].SetNativeSize();
            }
        }
    }

    /// <summary>
    /// Takes the damage.
    /// </summary>
    /// <param name="damage">The damage.</param>
    public void TakeDamage(int damage)
    {
        m_currentHealth = Mathf.Clamp(m_currentHealth - damage, 0, m_maxHealth);

        if (m_currentHealth < m_maxHealth)
        {
            m_healthBarCanvas.gameObject.SetActive(true);
            UpdateHealtBarVisuals(m_currentHealth);
        }

        if (m_currentHealth == 0)
        {
            m_healthBarCanvas.enabled = false;
            m_baseUnit.Die();
        }
    }

    /// <summary>
    /// Set the corrent missing/left health bar sprites depending on the previous and current health.
    /// </summary>
    private void UpdateHealtBarVisuals(int currentHealth)
    {
        for (int i = 0; i < m_healthBars.Length; i++)
        {
            if (!m_healthBars[i].gameObject.activeInHierarchy)
            {
                continue;
            }

            int indexedHealth = i / m_factorOfGeneralMaxHealth;

            m_healthBars[i].sprite = indexedHealth < currentHealth ? m_healthSpriteToUse : m_missingHealthSpriteToUse;
            m_healthBars[i].SetNativeSize();
        }
    }

    /// <summary>
    /// Gets the health based damage modifier.
    /// </summary>
    /// <returns></returns>
    public float GetHealthBasedDamageModifier()
    {
        return (float) m_currentHealth / m_maxHealth;
    }

    /// <summary>
    /// Displays the potential damage.
    /// </summary>
    public void DisplayPotentialDamage(BaseUnit attackingUnit)
    {
        m_healthBarCanvas.gameObject.SetActive(true);

        int potentialDamage = attackingUnit.GetDamageOnUnit(m_baseUnit);

        m_runningHealthBarBlinking = StartCoroutine(BlinkPotentialDamage(potentialDamage));
    }

    /// <summary>
    /// Blinks the potential damage in the health bar.
    /// </summary>
    private IEnumerator BlinkPotentialDamage(int potentialDamage)
    {
        while (true)
        {
            UpdateHealtBarVisuals(m_currentHealth - potentialDamage);

            yield return new WaitForSeconds(0.5f);

            UpdateHealtBarVisuals(m_currentHealth);

            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// Hides the potential damage.
    /// </summary>
    public void HidePotentialDamage()
    {
        if (m_runningHealthBarBlinking != null)
        {
            StopCoroutine(m_runningHealthBarBlinking);
        }

        if (CurrentHealth == m_maxHealth)
        {
           m_healthBarCanvas.gameObject.SetActive(false);
        }
        else
        {
            UpdateHealtBarVisuals(m_currentHealth);
        }
    }
}
