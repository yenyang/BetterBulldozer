// <copyright file="LocaleEN.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Settings
{
    using System.Collections.Generic;
    using Colossal;
    using Colossal.IO.AssetDatabase.Internal;

    /// <summary>
    /// Localization for <see cref="BetterBulldozerMod"/> mod in English.
    /// </summary>
    public class LocaleEN : IDictionarySource
    {
        private readonly BetterBulldozerModSettings m_Setting;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocaleEN"/> class.
        /// </summary>
        /// <param name="setting">Settings class.</param>
        public LocaleEN(BetterBulldozerModSettings setting)
        {
            m_Setting = setting;
        }

        /// <inheritdoc/>
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetOptionLabelLocaleID(nameof(BetterBulldozerModSettings.AllowRemovingSubElementNetworks)), "Allow Removing SubElement Networks" },
                { m_Setting.GetOptionDescLocaleID(nameof(BetterBulldozerModSettings.AllowRemovingSubElementNetworks)), "Allow removing SubElement networks such as roads, paths, markers, etc. Removing paths and especially markers will sometimes result in CTD." },
                { m_Setting.GetOptionLabelLocaleID(nameof(BetterBulldozerModSettings.AllowRemovingExtensions)), "Allow Removing Upgrades" },
                { m_Setting.GetOptionDescLocaleID(nameof(BetterBulldozerModSettings.AllowRemovingExtensions)), "Upgrades are building upgrades that do not extend the footprint of the lot. This will allow you to remove them, but other aspects of the building may not reset properly." },
                { m_Setting.GetOptionLabelLocaleID(nameof(BetterBulldozerModSettings.UpdateBuildingAfterRemovingExtension)), "Refresh Building after removing Upgrade." },
                { m_Setting.GetOptionDescLocaleID(nameof(BetterBulldozerModSettings.UpdateBuildingAfterRemovingExtension)), "Refreshes all Building props, trees, markers, etc after removing a building Upgrade." },
                { "YY_BETTER_BULLDOZER.ToolMode", "Tool Mode" },
                { "YY_BETTER_BULLDOZER.RaycastSurfacesButton", "Target Surfaces and Spaces" },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.RaycastSurfacesButton", "Makes the bulldozer EXCLUSIVELY target surfaces and spaces inside or outside of buildings so you can remove them in one click. You must turn this off to bulldoze anything else." },
                { "YY_BETTER_BULLDOZER.RaycastMarkersButton", "Target Markers" },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.RaycastMarkersButton", "Shows and EXCLUSIVELY targets static object markers or invisible networks. With this enabled you can demolish invisible networks, invisible parking decals, various spots, points, and spawners, but SAVE FIRST! You cannot demolish these within buildings." },
                { "YY_BETTER_BULLDOZER.GameplayManipulationButton", "Gameplay Manipulation" },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.GameplayManipulationButton", "Allows you to use the bulldozer on moving objects such as vehicles or cims." },
                { "YY_BETTER_BULLDOZER.BypassConfirmationButton", "Bypass Confirmation" },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.BypassConfirmationButton", "Disables the prompt for whether you are sure you want to demolish a building." },
                { "YY_BETTER_BULLDOZER.SubElementBulldozerButton", "Sub-Element Bulldozer" },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.SubElementBulldozerButton", "Custom bulldozer for removing props, trees, decals, fences, hedges from buildings. Currently you cannot delete nets or any type of service upgrade." },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.RaycastAreasButton", "Makes the bulldozer EXCLUSIVELY target surfaces or spaces inside or outside of buildings so you can remove them in one click.You must turn this off to bulldoze anything else." },
                { "YY_BETTER_BULLDOZER.Filter", "Filter" },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.SurfacesFilterButton", "For removing surfaces inside or outside of buildings in one click." },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.SpacesFilterButton", "For removing spaces including: Walking, Park, and Hangout areas. They are not currently visible with this tool, but will be highlighted when hovered. With this enabled you can target them inside or outside buildings and remove with one click." },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.StaticObjectsFilterButton", " For removing invisible parking decals, various spots, points, and spawners. Only those outside buildings can be removed. Trying to target those inside buildings will remove the building!" },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.NetworksFilterButton", "For removing invisible networks. Only those outside buildings can be removed.Trying to target those inside buildings will have no effect." },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.RaycastLanesButton", "For removing standalone lanes such as interconnected fences, interconnected hedges, linear street markings, vehicle and pedestrian lanes. Trying to target those inside networks will remove the network! You cannot create these in-game without a mod for it." },
                { TooltipDescriptionKey("SubElementsOfMainElement"), "For removing subelements such as props, trees, decals, and subbuildings from buildings." },
                { TooltipDescriptionKey("UpgradeIsMain"), "For removing subelements such as props, trees, and decals of subbuildings." },
                { SectionLabel("Tier"), "Tier" },
            };
        }


        /// <inheritdoc/>
        public void Unload()
        {
        }

        private string TooltipDescriptionKey(string key)
        {
            return $"BetterBulldozer.TOOLTIP_DESCRIPTION[{key}]";
        }

        private string TooltipTitleKey(string key)
        {
            return $"BetterBulldozer.TOOLTIP_TITLE[{key}]";
        }

        private string SectionLabel(string key)
        {
            return $"BetterBulldozer.SECTION_TITLE[{key}]";
        }
    }
}