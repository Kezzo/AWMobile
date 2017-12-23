using System;
using System.Collections.Generic;
using AWM.Enums;
using AWM.System;
using UnityEngine;

namespace AWM.BattleVisuals
{
    /// <summary>
    /// Class to synchronize blinking shader actors.
    /// </summary>
    public class ShaderBlinkOrchestrator : MonoBehaviour
    {
        /// <summary>
        /// Used to setup the shader property to use and the blinking timing per <see cref="ShaderCategory"/>.
        /// </summary>
        [Serializable]
        public class ShaderCategoryBlinkSetting
        {
            public ShaderCategory m_ShaderCategory;

            public float m_MaxValue;
            public float m_MinValue;

            public string m_ShaderPropertyName;

            public float m_BlinkTime;
            public float m_BlinkCooldown;

            public bool m_DecreaseValue;
        }

        [SerializeField]
        private List<ShaderCategoryBlinkSetting> m_shaderCategoryBlinkSettings;

        private Dictionary<ShaderCategory, ShaderBlinkActor> m_shaderBlinkActors = new Dictionary<ShaderCategory, ShaderBlinkActor>();

        private void Start()
        {
            ControllerContainer.MonoBehaviourRegistry.Register(this);
        }


        /// <summary>
        /// Adds a renderer to blink to the correct <see cref="ShaderBlinkActor"/> instance that controls the blinking.
        /// </summary>
        /// <param name="shaderCategory">The shader category the given renderer belongs to. Defines shader property and blink timing to use.</param>
        /// <param name="uniquePosition">The uniquePosition to uniquely identify a renderer.</param>
        /// <param name="rendererToBlink">The renderer to blink.</param>
        public void AddRendererToBlink(ShaderCategory shaderCategory, Vector2 uniquePosition, Renderer rendererToBlink)
        {
            if (!m_shaderBlinkActors.ContainsKey(shaderCategory))
            {
                m_shaderBlinkActors.Add(shaderCategory, new ShaderBlinkActor(GetBlinkSetting(shaderCategory), this));
            }

            m_shaderBlinkActors[shaderCategory].AddRenderer(uniquePosition, rendererToBlink);
        }

        /// <summary>
        /// Removes a renderer from the correct <see cref="ShaderBlinkActor"/> instance that controlled the blinking.
        /// </summary>
        /// <param name="shaderCategory">The shader category the given renderer belongs to. Defines shader property and blink timing to use.</param>
        /// <param name="position">The uniquePosition to uniquely identify a renderer.</param>
        public void RemoveRenderer(ShaderCategory shaderCategory, Vector2 position)
        {
            if (m_shaderBlinkActors.ContainsKey(shaderCategory))
            {
                m_shaderBlinkActors[shaderCategory].RemoveRenderer(position);
            }
        }

        /// <summary>
        /// Returns a serialized <see cref="ShaderCategoryBlinkSetting"/> that belongs to a given <see cref="ShaderCategory"/>.
        /// </summary>
        private ShaderCategoryBlinkSetting GetBlinkSetting(ShaderCategory shaderCategory)
        {
            return m_shaderCategoryBlinkSettings.Find(setting => setting.m_ShaderCategory == shaderCategory);
        }
    }
}
