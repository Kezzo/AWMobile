using System.Collections;
using System.Collections.Generic;
using AWM.System;
using UnityEngine;

namespace AWM.BattleVisuals
{
    /// <summary>
    /// Class to synchronize blinking shaders.
    /// </summary>
    public class ShaderBlinkOrchestrator : MonoBehaviour
    {
        [SerializeField]
        private float m_alphaMax;

        [SerializeField]
        private float m_alphaMin;

        [Range(0f, 5f)]
        [SerializeField]
        private float m_blinkTime;

        [Range(0f, 5f)]
        [SerializeField]
        private float m_blinkCooldown;

        private readonly Dictionary<Vector2, Renderer> m_rendererToBlink = new Dictionary<Vector2, Renderer>();

        private bool m_blinkingInProgress;
        private Coroutine m_runningBlinkingCoroutine;
        private Coroutine m_runningBlinkOnceCoroutine;

        private MaterialPropertyBlock m_materialPropertyBlock;

        private void Start()
        {
            ControllerContainer.MonoBehaviourRegistry.Register(this);
            m_materialPropertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Triggers the BlinkOnce coroutine and waits for the defined blink cooldown as long as the gameobject is enabled.
        /// </summary>
        private IEnumerator BlinkWithCooldown()
        {
            while (true)
            {
                yield return new WaitForSeconds(m_blinkCooldown);

                m_runningBlinkOnceCoroutine = StartCoroutine(BlinkOnce());
                yield return m_runningBlinkOnceCoroutine;
            }
        }

        /// <summary>
        /// Fades shader, this component is attached to, out once and fades it in again.
        /// </summary>
        private IEnumerator BlinkOnce()
        {
            float alphaClampValue = m_alphaMax;
            bool decreasingValue = true;

            while (true)
            {
                if (decreasingValue)
                {
                    alphaClampValue -= m_blinkTime * Time.deltaTime;

                    if (alphaClampValue <= m_alphaMin)
                    {
                        decreasingValue = false;
                    }
                }
                else
                {
                    alphaClampValue += m_blinkTime * Time.deltaTime;

                    if (alphaClampValue >= m_alphaMax)
                    {
                        yield break;
                    }
                }

                m_materialPropertyBlock.SetFloat("_Alpha", Mathf.Clamp(alphaClampValue, m_alphaMin, m_alphaMax));

                foreach (var rendererToBlink in m_rendererToBlink.Values)
                {
                    rendererToBlink.SetPropertyBlock(m_materialPropertyBlock);
                }

                yield return null;
            }
        }

        /// <summary>
        /// Adds a renderer to blink.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="rendererToBlink">The renderer to blink.</param>
        public void AddRendererToBlink(Vector2 position, Renderer rendererToBlink)
        {
            m_rendererToBlink[position] = rendererToBlink;

            rendererToBlink.GetPropertyBlock(m_materialPropertyBlock);
            m_materialPropertyBlock.SetFloat("_Alpha", m_alphaMax);

            if (!m_blinkingInProgress)
            {
                m_blinkingInProgress = true;
                m_runningBlinkingCoroutine = StartCoroutine(BlinkWithCooldown());
            }

            rendererToBlink.SetPropertyBlock(m_materialPropertyBlock);
        }

        /// <summary>
        /// Removes a renderer.
        /// </summary>
        /// <param name="position">The position.</param>
        public void RemoveRenderer(Vector2 position)
        {
            m_rendererToBlink.Remove(position);

            if (m_rendererToBlink.Count == 0 && m_blinkingInProgress)
            {
                StopCoroutine(m_runningBlinkOnceCoroutine);
                StopCoroutine(m_runningBlinkingCoroutine);
                m_blinkingInProgress = false;
            }
        }
    }
}
