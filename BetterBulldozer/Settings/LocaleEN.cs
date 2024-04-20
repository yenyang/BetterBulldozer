﻿// <copyright file="LocaleEN.cs" company="Yenyang's Mods. MIT License">
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

        /// <summary>
        /// Returns the locale key for a warning tooltip.
        /// </summary>
        /// <param name="key">The bracketed portion of locale key.</param>
        /// <returns>Localization key for translations.</returns>
        public static string WarningTooltipKey(string key)
        {
            return $"BetterBulldozer.WARNING_TOOLTIP[{key}]";
        }

        /// <inheritdoc/>
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Better Bulldozer" },
                { m_Setting.GetOptionLabelLocaleID(nameof(BetterBulldozerModSettings.AllowRemovingSubElementNetworks)), "Allow Removing Subelement Networks" },
                { m_Setting.GetOptionDescLocaleID(nameof(BetterBulldozerModSettings.AllowRemovingSubElementNetworks)), "Allow removing networks such as roads, paths, markers, etc from buildings. Removing subelement networks may break connectivity within an asset." },
                { m_Setting.GetOptionLabelLocaleID(nameof(BetterBulldozerModSettings.AllowRemovingExtensions)), "Allow Removing Extensions" },
                { m_Setting.GetOptionDescLocaleID(nameof(BetterBulldozerModSettings.AllowRemovingExtensions)), "Extensions are building upgrades that do not within the footprint of the main building lot. This will allow you to remove them, but removing some extensions may break connectivity within the asset." },
                { m_Setting.GetOptionLabelLocaleID(nameof(BetterBulldozerModSettings.ResetModSettings)), "Reset Better Bulldozer Settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(BetterBulldozerModSettings.ResetModSettings)), "After confirmation this will reset Better Bulldozer Settings." },
                { m_Setting.GetOptionWarningLocaleID(nameof(BetterBulldozerModSettings.ResetModSettings)), "Reset Better Bulldozer  Settings?" },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.RaycastMarkersButton", "Shows and EXCLUSIVELY targets static object markers or invisible networks. With this enabled you can demolish invisible networks, invisible parking decals, various spots, points, and spawners, but SAVE FIRST! You cannot demolish these within buildings." },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.GameplayManipulationButton", "Allows you to use the bulldozer on moving objects such as vehicles or cims." },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.BypassConfirmationButton", "Disables the prompt for whether you are sure you want to demolish a building." },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.RaycastAreasButton", "Makes the bulldozer EXCLUSIVELY target surfaces or spaces inside or outside of buildings so you can remove them in one click.You must turn this off to bulldoze anything else." },
                { "YY_BETTER_BULLDOZER.Filter", "Filter" },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.SurfacesFilterButton", "For removing surfaces inside or outside of buildings in one click." },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.SpacesFilterButton", "For removing spaces including: Walking, Park, and Hangout areas. They are not currently visible with this tool, but will be highlighted when hovered. With this enabled you can target them inside or outside buildings and remove with one click." },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.StaticObjectsFilterButton", " For removing invisible parking decals, various spots, points, and spawners. Only those outside buildings can be removed. Trying to target those inside buildings will remove the building!" },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.NetworksFilterButton", "For removing invisible networks. Only those outside buildings can be removed. Trying to target those inside buildings will have no effect." },
                { "YY_BETTER_BULLDOZER_DESCRIPTION.RaycastLanesButton", "For removing standalone lanes such as interconnected fences, interconnected hedges, linear street markings, vehicle and pedestrian lanes. Trying to target those inside networks will remove the network! You cannot create these in-game without a mod for it." },
                { TooltipTitleKey("GameplayManipulationButton"), "Gameplay Manipulation" },
                { TooltipTitleKey("RaycastAreasButton"), "Target Surfaces and Spaces" },
                { TooltipTitleKey("RaycastMarkersButton"), "Target Markers" },
                { TooltipTitleKey("BypassConfirmationButton"), "Bypass Confirmation" },
                { TooltipTitleKey("RaycastLanesButton"), "Target standalone lanes" },
                { TooltipTitleKey("SubElementBulldozerButton"), "Subelement Bulldozer" },
                { TooltipTitleKey("SurfacesFilterButton"), "Surfaces Filter" },
                { TooltipTitleKey("SpacesFilterButton"), "Spaces Filter" },
                { TooltipTitleKey("StaticObjectsFilterButton"), "Static Objects Filter" },
                { TooltipTitleKey("NetworksFilterButton"), "Networks Filter" },
                { TooltipDescriptionKey("SubElementBulldozerButton"), "Custom bulldozer for removing props, trees, decals, fences, hedges, subbuildings, extensions, and networks from buildings. This tool can break connectivity within assets. Some elements are more safe to remove such as: props that cims do not use, trees, decals, fences, hedges, subbuildings. Use with caution with props that cims use. Some elements are less safe to remove such as: networks and extensions. You can prohibit removing those in the settings." },
                { TooltipTitleKey("SubElementsOfMainElement"), "Subelements of the Main Asset" },
                { TooltipDescriptionKey("SubElementsOfMainElement"), "For removing subelements such as props, trees, decals, fences, hedges, subbuildings, extensions, and networks from the main asset." },
                { TooltipTitleKey("UpgradeIsMain"), "Subelements of Subbuildings and Extensions" },
                { TooltipDescriptionKey("UpgradeIsMain"), "For removing subelements such as props, trees, decals belonging to subbuildings and extensions of the main building." },
                { TooltipTitleKey("Single"), "Single Item" },
                { TooltipDescriptionKey("Single"), "Selects a single subelement at a time." },
                { TooltipTitleKey("Matching"), "Exact Match" },
                { TooltipDescriptionKey("Matching"), "Selects all exactly matching subelements within the building, subbuilding, extension or network. (i.e. all oak trees in this asset)" },
                { TooltipTitleKey("Similar"), "Similar Category" },
                { TooltipDescriptionKey("Similar"), "Selects all subelements in a similar category within the building, subbuilding, extension or network. Categories include: trees, plants, street lights, trash bins, branding objects and advertisements, activity locations, all hedges and fences, and anything elevated above the ground. (i.e. all trees in this asset)" },
                { SectionLabel("Tier"), "Tier" },
                { WarningTooltipKey("BulldozeSubelement"), "Bulldoze Subelement" },
                { WarningTooltipKey("ExtensionRemovalProhibited"), "Removing extensions has been disabled in the settings." },
                { WarningTooltipKey("RemovingMarkerNetworksProhibited"), "Removing subelement networks has been disabled in the settings." },
                { WarningTooltipKey("ExtensionRemovalWarning"), "Removing some extensions will break assets." },
                { WarningTooltipKey("SubelementNetworkRemovalWarning"), "Removing subelement networks may break assets." },
                { WarningTooltipKey("NetworksUseSingleItem"), "Removing multiple subelement networks is not supported. Use Single Item selection instead." },
                { WarningTooltipKey("RemovingSubelementsFromRoads"), "Removing subelements from roads is not recommended because roads update frequently and the subelements will regenerate." },
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