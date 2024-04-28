// <copyright file="BetterBulldozerModSettings.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Settings
{
    using Better_Bulldozer.Systems;
    using Colossal.IO.AssetDatabase;
    using Game.Modding;
    using Game.Settings;
    using Unity.Entities;

    /// <summary>
    /// The mod settings for the Anarchy Mod.
    /// </summary>
    [FileLocation("Mods_Yenyang_Better_Bulldozer")]

    public class BetterBulldozerModSettings : ModSetting
    {
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
        public bool AllowRemovingSubElementNetworks { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to allow removal of upgrades.
        /// </summary>
        public bool AllowRemovingExtensions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically remove manicured grass.
        /// </summary>
        [SettingsUISetter(typeof(BetterBulldozerModSettings), nameof(ManageAutomaticlyRemoveManicuredGrassSystem))]
        public bool AutomaticRemovalManicuredGrass { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically remove fences and hedges.
        /// </summary>
        // public bool AutomaticRemovalFencesAndHedges { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether: Used to force saving of Modsettings if settings would result in empty Json.
        /// </summary>
        [SettingsUIHidden]
        public bool Contra { get; set; }

        /// <summary>
        /// Sets a value indicating whether: a button for Resetting the settings for the Mod.
        /// </summary>
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
        /// Sets a value indicating whether: to safely remove mod.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        public bool SafelyRemove
        {
            set
            {
                SafelyRemoveSystem safelyRemoveSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<SafelyRemoveSystem>();
                safelyRemoveSystem.Enabled = true;
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
            AllowRemovingSubElementNetworks = true;
            AllowRemovingExtensions = true;
            AutomaticRemovalManicuredGrass = false;

            // AutomaticRemovalFencesAndHedges = false;
        }

        private void ManageAutomaticlyRemoveManicuredGrassSystem(bool value) => World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AutomaticallyRemoveManicuredGrassSurfaceSystem>().Enabled = value;
    }
}
