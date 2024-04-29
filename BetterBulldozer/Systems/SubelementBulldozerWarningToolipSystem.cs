// <copyright file="SubelementBulldozerWarningToolipSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Systems
{
    using System.Collections.Generic;
    using Better_Bulldozer.Tools;
    using Colossal.Logging;
    using Game.Tools;
    using Game.UI.Localization;
    using Game.UI.Tooltip;

    /// <summary>
    /// Adds warning tooltips about safety or usefullness of removing subelements.
    /// </summary>
    public partial class SubelementBulldozerWarningTooltipSystem : TooltipSystemBase
    {
        private ToolSystem m_ToolSystem;
        private ILog m_Log;
        private Dictionary<string, StringTooltip> m_Tooltips;
        private SubElementBulldozerTool m_SubElementBulldozerTool;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubelementBulldozerWarningTooltipSystem"/> class.
        /// </summary>
        public SubelementBulldozerWarningTooltipSystem()
        {
        }

        /// <summary>
        /// Registers a string tooltip to be displayed while using Subelement bulldozer tool.
        /// </summary>
        /// <param name="path">Unique string path for tooltip.</param>
        /// <param name="color">Tooltip color.</param>
        /// <param name="localeKey">Localization key.</param>
        /// <param name="fallback">Fallback string if localization key is not found.</param>
        /// <returns>True if tooltip added. False if already exists.</returns>
        public bool RegisterTooltip(string path, TooltipColor color, string localeKey, string fallback)
        {
            if (m_Tooltips.ContainsKey(path))
            {
                return false;
            }

            m_Log.Debug($"{nameof(SubelementBulldozerWarningTooltipSystem)}.{nameof(RegisterTooltip)} Registering new tooltip {path}.");
            m_Tooltips.Add(path, new StringTooltip() { path = path, value = LocalizedString.IdWithFallback(localeKey, fallback), color = color });
            return true;
        }

        /// <summary>
        /// Removes a tooltip from registry if valid path.
        /// </summary>
        /// <param name="path">Unique string path for tooltip.</param>
        public void RemoveTooltip(string path)
        {
            if (m_Tooltips.ContainsKey(path))
            {
                m_Log.Debug($"{nameof(SubelementBulldozerWarningTooltipSystem)}.{nameof(RemoveTooltip)} Removing tooltip {path}.");
                m_Tooltips.Remove(path);
            }
        }

        /// <summary>
        /// Removes all tooltips from registry.
        /// </summary>
        public void ClearTooltips()
        {
            m_Log.Debug($"{nameof(SubelementBulldozerWarningTooltipSystem)}.{nameof(ClearTooltips)}");
            m_Tooltips.Clear();
        }


        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_SubElementBulldozerTool = World.GetOrCreateSystemManaged<SubElementBulldozerTool>();
            m_Tooltips = new Dictionary<string, StringTooltip>();
            m_Log.Info($"{nameof(SubelementBulldozerWarningTooltipSystem)} Created.");
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (m_ToolSystem.activeTool == m_SubElementBulldozerTool)
            {
                foreach (StringTooltip stringTooltip in m_Tooltips.Values)
                {
                    AddMouseTooltip(stringTooltip);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
