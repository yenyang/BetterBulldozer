// <copyright file="BetterBulldozerModSettings.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Settings
{
    using Colossal.IO.AssetDatabase;
    using Game.Modding;
    using Game.Settings;

    /// <summary>
    /// The mod settings for the Anarchy Mod.
    /// </summary>
    [FileLocation("Mods_Yenyang_Better_Bulldozer")]
    [SettingsUISection(General, Experimental)]
    [SettingsUIGroupOrder(SubElementBulldozer)]
    [SettingsUIShowGroupName(SubElementBulldozer)]

    public class BetterBulldozerModSettings : ModSetting
    {
        /// <summary>
        /// This is for settings not flagged as experimental.
        /// </summary>
        public const string General = "General";

        /// <summary>
        /// This is for experimental settings that should be used with caution.
        /// </summary>
        public const string Experimental = "Experimental";

        /// <summary>
        /// This is for options related to the sub element bulldozer.
        /// </summary>
        public const string SubElementBulldozer = "SubElement Bulldozer";

        /// <summary>
        /// This is for the reset button(s).
        /// </summary>
        public const string Reset = "Reset";

        /// <summary>
        /// Initializes a new instance of the <see cref="BetterBulldozerModSettings"/> class.
        /// </summary>
        /// <param name="mod">Better Bulldozer mod.</param>
        public BetterBulldozerModSettings(IMod mod)
            : base(mod)
        {
            SetDefaults();
        }

        /// <summary>
        /// Gets or sets a value indicating whether to allow removing sub element networks.
        /// </summary>
        [SettingsUISection(Experimental, SubElementBulldozer)]
        public bool AllowRemovingSubElementNetworks { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to allow removal of upgrades.
        /// </summary>
        [SettingsUISection(Experimental, SubElementBulldozer)]
        public bool AllowRemovingExtensions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether after removing an upgrade should the building be updated.
        /// </summary>
        [SettingsUISection(Experimental, SubElementBulldozer)]
        [SettingsUIHideByCondition(typeof(BetterBulldozerModSettings), nameof(IsRemovingExtensionsProhibited))]
        public bool UpdateBuildingAfterRemovingExtension { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether: Used to force saving of Modsettings if settings would result in empty Json.
        /// </summary>
        [SettingsUIHidden]
        public bool Contra { get; set; }

        /// <summary>
        /// Sets a value indicating whether: a button for Resetting the settings for the Mod.
        /// </summary>
        [SettingsUISection(General, Reset)]
        [SettingsUIButton]
        [SettingsUIConfirmation]
        public bool ResetModSettings
        {
            set
            {
                SetDefaults();
                Contra = false;
                ApplyAndSave();
            }
        }

        /// <summary>
        /// Checks if prevent accidental allow removing upgrades is off or on.
        /// </summary>
        /// <returns>Opposite of AllowRemovingExtensions.</returns>
        public bool IsRemovingExtensionsProhibited() => !AllowRemovingExtensions;

        /// <inheritdoc/>
        public override void SetDefaults()
        {
            Contra = true;
            AllowRemovingSubElementNetworks = false;
            AllowRemovingExtensions = false;
            UpdateBuildingAfterRemovingExtension = true;
        }
    }
}
