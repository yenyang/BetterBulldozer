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

        /// <summary>
        /// Returns the locale key for a warning tooltip.
        /// </summary>
        /// <param name="key">The bracketed portion of locale key.</param>
        /// <returns>Localization key for translations.</returns>
        public static string WarningTooltipKey(string key)
        {
            return $"BetterBulldozer.WARNING_TOOLTIP[{key}]";
        }


        /// <summary>
        /// Returns the locale key for a tooltip title key.
        /// </summary>
        /// <param name="key">The bracketed portion of locale key.</param>
        /// <returns>Localization key for translations.</returns>
        public static string TooltipTitleKey(string key)
        {
            return $"BetterBulldozer.TOOLTIP_TITLE[{key}]";
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
                { m_Setting.GetOptionLabelLocaleID(nameof(BetterBulldozerModSettings.AutomaticRemovalManicuredGrass)), "Automatically Remove Manicured Grass" },
                { m_Setting.GetOptionDescLocaleID(nameof(BetterBulldozerModSettings.AutomaticRemovalManicuredGrass)), "Automitically removes Grass Surface 01 and Grass Surface 02 from buildings plopped and spawned." },

                // { m_Setting.GetOptionLabelLocaleID(nameof(BetterBulldozerModSettings.AutomaticRemovalFencesAndHedges)), "Automatically Remove Fences and Hedges" },
                // { m_Setting.GetOptionDescLocaleID(nameof(BetterBulldozerModSettings.AutomaticRemovalFencesAndHedges)), "Automitically removes fences and hedges from buildings plopped and spawned." },
                { m_Setting.GetOptionLabelLocaleID(nameof(BetterBulldozerModSettings.ResetModSettings)), "Reset Better Bulldozer Settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(BetterBulldozerModSettings.ResetModSettings)), "After confirmation this will reset Better Bulldozer Settings." },
                { m_Setting.GetOptionWarningLocaleID(nameof(BetterBulldozerModSettings.ResetModSettings)), "Reset Better Bulldozer  Settings?" },
                { m_Setting.GetOptionLabelLocaleID(nameof(BetterBulldozerModSettings.SafelyRemove)), "Safely Remove" },
                { m_Setting.GetOptionDescLocaleID(nameof(BetterBulldozerModSettings.SafelyRemove)), "This is only recommended if you have used Matching and Similar selection modes of the Remove Sub-Elements tool mode to 'permanently remove' specific types or categories of sub-elements. This removes all history of what was 'permanently removed' by removing all Better Bulldozer mod components and storage entities from your save file. This cannot be undone." },
                { m_Setting.GetOptionWarningLocaleID(nameof(BetterBulldozerModSettings.SafelyRemove)), "Safely Remove Better Bulldozer mod?" },

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
                { TooltipTitleKey("GameplayManipulationButton"), "Remove Moving Objects and Cims" },
                { TooltipTitleKey("RaycastAreasButton"), "Remove Surfaces and Spaces" },
                { TooltipTitleKey("RaycastMarkersButton"), "Remove Markers" },
                { TooltipTitleKey("BypassConfirmationButton"), "Bypass Confirmation" },
                { TooltipTitleKey("RaycastLanesButton"), "Remove standalone lanes" },
                { TooltipTitleKey("SubElementBulldozerButton"), "Remove Sub-Elements" },
                { TooltipTitleKey("SurfacesFilterButton"), "Surfaces Filter" },
                { TooltipTitleKey("SpacesFilterButton"), "Spaces Filter" },
                { TooltipTitleKey("StaticObjectsFilterButton"), "Static Objects Filter" },
                { TooltipTitleKey("NetworksFilterButton"), "Networks Filter" },
                { TooltipDescriptionKey("SubElementBulldozerButton"), "Removes props, trees, decals, fences, hedges, sub-buildings, extensions, and networks from assets. This tool can break connectivity within assets. Some elements are more safe to remove such as: props, trees, decals, fences, hedges, subbuildings. Some elements are less safe to remove such as: networks and extensions. You can prohibit removing those in the settings." },
                { TooltipTitleKey("SubElementsOfMainElement"), "Sub-Elements of the Main Asset" },
                { TooltipDescriptionKey("SubElementsOfMainElement"), "For removing sub-elements such as props, trees, decals, fences, hedges, sub-buildings, extensions, and networks from the main asset." },
                { TooltipTitleKey("UpgradeIsMain"), "Sub-Elements of Sub-Buildings and Extensions" },
                { TooltipDescriptionKey("UpgradeIsMain"), "For removing sub-elements such as props, trees, decals belonging to sub-buildings and extensions of the main building." },
                { TooltipTitleKey("Single"), "Single Item" },
                { TooltipDescriptionKey("Single"), "Selects a single sub-element at a time. Single removal is not permanent, and will regenerate whenever the asset updates." },
                { TooltipTitleKey("Matching"), "Exact Match" },
                { TooltipDescriptionKey("Matching"), "Selects all exactly matching sub-elements within the building, subbuilding, extension or network and 'permanently removes' them. (i.e. all oak trees in this asset). 'Permanent removal' means whenever they are regenerated they will automatically be removed again soon afterwards." },
                { TooltipTitleKey("Similar"), "Similar Category" },
                { TooltipDescriptionKey("Similar"), "Selects all sub-elements in a similar category within the building, subbuilding, extension or network and 'permanently removes' them. Categories include: trees, plants, street lights, trash bins, branding objects and advertisements, activity locations, all hedges and fences, and anything elevated above the ground. (i.e. all trees in this asset). 'Permanent removal' means whenever they are regenerated they will automatically be removed again soon afterwards." },
                { TooltipTitleKey("Reset"), "Reset Asset" },
                { TooltipDescriptionKey("Reset"), "Reset assets by selecting ones that have any sub-elements 'permanently removed' using Exact Match or Similar Category." },
                { SectionLabel("Tier"), "Tier" },
                { WarningTooltipKey("BulldozeSubelement"), "Bulldoze Sub-Element" },
                { WarningTooltipKey("ExtensionRemovalProhibited"), "Removing extensions has been disabled in the settings." },
                { WarningTooltipKey("RemovingMarkerNetworksProhibited"), "Removing sub-element networks has been disabled in the settings." },
                { WarningTooltipKey("ExtensionRemovalWarning"), "Removing some extensions will break assets." },
                { WarningTooltipKey("SubelementNetworkRemovalWarning"), "Removing sub-element networks may break assets." },
                { WarningTooltipKey("NetworksUseSingleItem"), "Removing multiple sub-element networks is not supported. Use Single Item selection instead." },
                { WarningTooltipKey("RemovingSubelementsFromRoads"), "Removing single sub-elements from roads is not recommended due to frequent regeneration." },
                { WarningTooltipKey("RemovingSubelementsFromGrowable"), "Removing single sub-elements from growables that can level up is not recommended due to level up regenerate." },
                { WarningTooltipKey("RemovingSubelementsFromServiceBuildings"), "Recommend purchasing all upgrades before removing single sub-elements from service buildings to avoid regeneration." },

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

        private string SectionLabel(string key)
        {
            return $"BetterBulldozer.SECTION_TITLE[{key}]";
        }
    }
}