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
        [SettingsUISetter(typeof(BetterBulldozerModSettings), nameof(ManageAutomaticallyRemoveManicuredGrassSystem))]
        public bool AutomaticRemovalManicuredGrass { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically remove fences and hedges.
        /// </summary>
        [SettingsUISetter(typeof(BetterBulldozerModSettings), nameof(ManageAutomaticallyRemoveFencesAndHedgesSystem))]
        public bool AutomaticRemovalFencesAndHedges { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically remove branding objects.
        /// </summary>
        [SettingsUISetter(typeof(BetterBulldozerModSettings), nameof(ManageAutomaticallyRemoveBrandingObjects))]
        public bool AutomaticRemovalBrandingObjects { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether: Used to force saving of Modsettings if settings would result in empty Json.
        /// </summary>
        [SettingsUIHidden]
        public bool Contra { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether: Used to force saving of Modsettings if settings would result in empty Json.
        /// </summary>
        [SettingsUIHidden]
        public BetterBulldozerUISystem.SelectionMode PreviousSelectionMode { get; set; }

        /// <summary>
        /// Sets a value indicating whether: a button for Resetting the settings for the Mod.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        public bool ResetModSettings
        {
            set
            {
                BetterBulldozerUISystem.SelectionMode mode = PreviousSelectionMode;
                SetDefaults();
                Contra = false;
                PreviousSelectionMode = mode;
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
            PreviousSelectionMode = BetterBulldozerUISystem.SelectionMode.Matching;
            AutomaticRemovalFencesAndHedges = false;
        }

        /// <summary>
        /// Sets Enabled for AutomaticallyRemoveManicuredGrassSurfaceSystem.
        /// </summary>
        /// <param name="value">Toggle value.</param>
        public void ManageAutomaticallyRemoveManicuredGrassSystem(bool value) => World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AutomaticallyRemoveManicuredGrassSurfaceSystem>().Enabled = value;

        /// <summary>
        /// Sets Enabled for AutomaticallyRemoveFencesAndHedges.
        /// </summary>
        /// <param name="value">Toggle value.</param>
        public void ManageAutomaticallyRemoveFencesAndHedgesSystem(bool value) => World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AutomaticallyRemoveFencesAndHedges>().Enabled = value;

        /// <summary>
        /// Sets Enabled for AutomaticallyRemoveBrandingObjects.
        /// </summary>
        /// <param name="value">Toggle value.</param>
        public void ManageAutomaticallyRemoveBrandingObjects(bool value) => World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AutomaticallyRemoveBrandingObjects>().Enabled = value;
    }
}
