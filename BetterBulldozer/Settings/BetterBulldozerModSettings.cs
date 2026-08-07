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

        private bool _automaticRemovalManicuredGrass;
        private bool _automaticRemovalFencesAndHedges;
        private bool _automaticRemovalBrandingObjects;
        private bool _automaticRemovalStreetSignObjects;

        /// <summary>
        /// Gets or sets a value indicating whether to automatically remove manicured grass.
        /// </summary>
        public bool AutomaticRemovalManicuredGrass
        {
            get => _automaticRemovalManicuredGrass;
            set
            {
                _automaticRemovalManicuredGrass = value;
                ManageAutomaticallyRemoveManicuredGrassSystem(value);
            }
        }

        /// <summary>
        /// Sets a value indicating whether to remove owned grass surfaces.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        public bool RemovedOwnedGrassSurfaces
        {
            set
            {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<RemoveExistingOwnedGrassSurfaces>().Enabled = true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically remove fences and hedges.
        /// </summary>
        public bool AutomaticRemovalFencesAndHedges
        {
            get => _automaticRemovalFencesAndHedges;
            set
            {
                _automaticRemovalFencesAndHedges = value;
                ManageAutomaticallyRemoveFencesAndHedgesSystem(value);
            }
        }

        /// <summary>
        /// Sets a value indicating whether to restore fences and hedges.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIDisableByCondition(typeof(BetterBulldozerModSettings), nameof(AutomaticRemovalFencesAndHedges))]
        [SettingsUIConfirmation]
        public bool RestoreFencesAndHedges
        {
            set
            {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<RestoreFencesAndHedgesSystem>().Enabled = true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically remove branding objects.
        /// </summary>
        public bool AutomaticRemovalBrandingObjects
        {
            get => _automaticRemovalBrandingObjects;
            set
            {
                _automaticRemovalBrandingObjects = value;
                ManageAutomaticallyRemoveBrandingObjects(value);
            }
        }

        /// <summary>
        /// Sets a value indicating whether to restore branding objects.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIDisableByCondition(typeof(BetterBulldozerModSettings), nameof(AutomaticRemovalBrandingObjects))]
        [SettingsUIConfirmation]
        public bool RestoreBrandingObjects
        {
            set
            {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<RestoreBrandingObjects>().Enabled = true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically remove street sign objects.
        /// </summary>
        public bool AutomaticRemovalStreetSignObjects
        {
            get => _automaticRemovalStreetSignObjects;
            set
            {
                _automaticRemovalStreetSignObjects = value;
                ManageAutomaticallyRemoveStreetSignObjects(value);
            }
        }

        /// <summary>
        /// Sets a value indicating whether to restore branding objects.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIDisableByCondition(typeof(BetterBulldozerModSettings), nameof(AutomaticRemovalStreetSignObjects))]
        [SettingsUIConfirmation]
        public bool RestoreStreetSignObjects
        {
            set
            {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<RestoreStreetSignObjects>().Enabled = true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether: for saving previous selection mode for remove subelement tool mode.
        /// </summary>
        [SettingsUIHidden]
        public BetterBulldozerUISystem.SelectionMode PreviousSelectionMode { get; set; } = BetterBulldozerUISystem.SelectionMode.Matching;

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
        /// Gets a value indicating the version.
        /// </summary>
        public string Version => BetterBulldozerMod.Instance.Version;

        /// <inheritdoc/>
        public override void SetDefaults()
        {
            AllowRemovingSubElementNetworks = true;
            AllowRemovingExtensions = true;
            AutomaticRemovalManicuredGrass = false;
            AutomaticRemovalFencesAndHedges = false;
            AutomaticRemovalBrandingObjects = false;
            AutomaticRemovalStreetSignObjects = false;
        }

        /// <summary>
        /// Sets Enabled for AutomaticallyRemoveManicuredGrassSurfaceSystem.
        /// </summary>
        /// <param name="value">Toggle value.</param>
        public void ManageAutomaticallyRemoveManicuredGrassSystem(bool value)
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AutomaticallyRemoveManicuredGrassSurfaceSystem>();

            if (system != null)
            {
                system.Enabled = value;
            }
        }

        /// <summary>
        /// Sets Enabled for AutomaticallyRemoveFencesAndHedges.
        /// </summary>
        /// <param name="value">Toggle value.</param>
        public void ManageAutomaticallyRemoveFencesAndHedgesSystem(bool value)
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AutomaticallyRemoveFencesAndHedges>();

            if (system != null)
            {
                system.Enabled = value;

                if (value)
                {
                    system.ForceFullScan();
                }
            }
        }

        /// <summary>
        /// Sets Enabled for AutomaticallyRemoveBrandingObjects.
        /// </summary>
        /// <param name="value">Toggle value.</param>
        public void ManageAutomaticallyRemoveBrandingObjects(bool value)
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AutomaticallyRemoveBrandingObjects>();

            if (system != null)
            {
                system.Enabled = value;

                if (value)
                {
                    system.ForceFullScan();
                }
            }
        }

        /// <summary>
        /// Sets Enabled for AutomaticallyRemoveStreetSignObjects.
        /// </summary>
        /// <param name="value">Toggle value.</param>
        public void ManageAutomaticallyRemoveStreetSignObjects(bool value)
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AutomaticallyRemoveStreetSignObjects>();

            if (system != null)
            {
                system.Enabled = value;

                if (value)
                {
                    system.ForceFullScan();
                }
            }
        }

        private bool IsRemovingExtensionsProhibited() => !AllowRemovingExtensions;
    }
}
