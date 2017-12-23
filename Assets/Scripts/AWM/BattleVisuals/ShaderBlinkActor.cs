using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AWM.BattleVisuals
{
    /// <summary>
    /// Controls the blinking of a group of <see cref="Renderer"/> instances of the same shader type.
    /// </summary>
    public class ShaderBlinkActor
    {
        private readonly ShaderBlinkOrchestrator.ShaderCategoryBlinkSetting m_shaderCategoryBlinkSetting;
        private readonly MonoBehaviour m_orchestratorInstance;
        private readonly MaterialPropertyBlock m_materialPropertyBlock;

        private readonly Dictionary<Vector2, Renderer> m_rendererBlinkCache =
            new Dictionary<Vector2, Renderer>();

        private bool m_isBlinking;

        private Coroutine m_runningBlinkingCoroutine;
        private Coroutine m_runningBlinkOnceCoroutine;

        public ShaderBlinkActor(ShaderBlinkOrchestrator.ShaderCategoryBlinkSetting shaderCategoryBlinkSetting,
            MonoBehaviour orchestratorInstance)
        {
            m_shaderCategoryBlinkSetting = shaderCategoryBlinkSetting;
            m_orchestratorInstance = orchestratorInstance;
            m_materialPropertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Adds a renderer to blink. When it's the first renderer, the blinking will be started.
        /// </summary>
        /// <param name="uniquePosition">The unique position or the instance.</param>
        /// <param name="rendererToBlink">The renderer to blink.</param>
        public void AddRenderer(Vector2 uniquePosition, Renderer rendererToBlink)
        {
            m_rendererBlinkCache[uniquePosition] = rendererToBlink;

            rendererToBlink.GetPropertyBlock(m_materialPropertyBlock);
            m_materialPropertyBlock.SetFloat(m_shaderCategoryBlinkSetting.m_ShaderPropertyName,
                m_shaderCategoryBlinkSetting.m_DecreaseValue ?
                m_shaderCategoryBlinkSetting.m_MaxValue : m_shaderCategoryBlinkSetting.m_MinValue);

            if (!m_isBlinking)
            {
                m_isBlinking = true;
                m_runningBlinkingCoroutine = m_orchestratorInstance.StartCoroutine(BlinkWithCooldown());
            }

            rendererToBlink.SetPropertyBlock(m_materialPropertyBlock);
        }

        /// <summary>
        /// Removed a previously added renderer. When it's the last renderer, the blinking will be stopped.
        /// </summary>
        /// <param name="uniquePosition">The unique position or the instance.</param>
        public void RemoveRenderer(Vector2 uniquePosition)
        {
            if (!m_rendererBlinkCache.ContainsKey(uniquePosition))
            {
                return;
            }

            m_rendererBlinkCache[uniquePosition].GetPropertyBlock(m_materialPropertyBlock);
            m_materialPropertyBlock.SetFloat(m_shaderCategoryBlinkSetting.m_ShaderPropertyName,
                m_shaderCategoryBlinkSetting.m_DecreaseValue ?
                m_shaderCategoryBlinkSetting.m_MaxValue : m_shaderCategoryBlinkSetting.m_MinValue);

            m_rendererBlinkCache[uniquePosition].SetPropertyBlock(m_materialPropertyBlock);

            m_rendererBlinkCache.Remove(uniquePosition);

            if (m_rendererBlinkCache.Count == 0 && m_isBlinking)
            {
                if(m_runningBlinkOnceCoroutine != null)
                {
                    m_orchestratorInstance.StopCoroutine(m_runningBlinkOnceCoroutine);
                }

                m_orchestratorInstance.StopCoroutine(m_runningBlinkingCoroutine);
                m_isBlinking = false;
            }
        }

        /// <summary>
        /// Triggers the BlinkOnce coroutine and waits for the defined blink cooldown as long as the gameobject is enabled.
        /// </summary>
        private IEnumerator BlinkWithCooldown()
        {
            while (true)
            {
                yield return new WaitForSeconds(m_shaderCategoryBlinkSetting.m_BlinkCooldown);

                m_runningBlinkOnceCoroutine = m_orchestratorInstance.StartCoroutine(BlinkOnce());
                yield return m_runningBlinkOnceCoroutine;
            }
        }

        /// <summary>
        /// Fades shader, this component is attached to, out once and fades it in again.
        /// </summary>
        private IEnumerator BlinkOnce()
        {
            float alphaClampValue = m_shaderCategoryBlinkSetting.m_DecreaseValue ? 
                m_shaderCategoryBlinkSetting.m_MaxValue : m_shaderCategoryBlinkSetting.m_MinValue;
            bool decreasingValue = m_shaderCategoryBlinkSetting.m_DecreaseValue;

            while (true)
            {
                if (decreasingValue)
                {
                    alphaClampValue -= m_shaderCategoryBlinkSetting.m_BlinkTime * Time.deltaTime;

                    if (alphaClampValue <= m_shaderCategoryBlinkSetting.m_MinValue)
                    {
                        if (m_shaderCategoryBlinkSetting.m_DecreaseValue)
                        {
                            decreasingValue = false;
                        }
                        else
                        {
                            yield break;
                        }
                        
                    }
                }
                else
                {
                    alphaClampValue += m_shaderCategoryBlinkSetting.m_BlinkTime * Time.deltaTime;

                    if (alphaClampValue >= m_shaderCategoryBlinkSetting.m_MaxValue)
                    {
                        if (!m_shaderCategoryBlinkSetting.m_DecreaseValue)
                        {
                            decreasingValue = true;
                        }
                        else
                        {
                            yield break;
                        }
                    }
                }

                m_materialPropertyBlock.SetFloat(m_shaderCategoryBlinkSetting.m_ShaderPropertyName,
                    Mathf.Clamp(alphaClampValue, m_shaderCategoryBlinkSetting.m_MinValue,
                    m_shaderCategoryBlinkSetting.m_MaxValue));

                foreach (var rendererToBlink in m_rendererBlinkCache.Values)
                {
                    rendererToBlink.SetPropertyBlock(m_materialPropertyBlock);
                }

                yield return null;
            }
        }
    }
}
