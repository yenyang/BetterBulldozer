// <copyright file="BetterBulldozerModSettings.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Settings
{
    using Better_Bulldozer.Systems;
    using Colossal.IO.AssetDatabase;
    using Colossal.PSI.Common;
    using Game;
    using Game.Input;
    using Game.Modding;
    using Game.Settings;
    using Game.Tools;
    using Unity.Entities;
    using static Game.Prefabs.CompositionFlags;

    /// <summary>
    /// The mod settings for the Anarchy Mod.
    /// </summary>
    [FileLocation("Mods_Yenyang_Better_Bulldozer")]
    [SettingsUIMouseAction(BetterBulldozerMod.RSEApplyMimicAction, "RemoveSubElementsApplyAction")]
    [SettingsUIMouseAction(BetterBulldozerMod.VCAApplyMimicAction, "RemoveVCAApplyAction")]
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
        [SettingsUISetter(typeof(BetterBulldozerModSettings), nameof(ManageAutomaticallyRemoveFencesAndHedgesSystem))]
        public bool AutomaticRemovalFencesAndHedges { get; set; }

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
        [SettingsUISetter(typeof(BetterBulldozerModSettings), nameof(ManageAutomaticallyRemoveBrandingObjects))]
        public bool AutomaticRemovalBrandingObjects { get; set; }

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
        /// Gets or sets hidden keybinding for Remove Sub Elements apply action.
        /// </summary>
        [SettingsUIMouseBinding(BetterBulldozerMod.RSEApplyMimicAction)]
        [SettingsUIHidden]
        public ProxyBinding RSEApplyMimic { get; set; }

        /// <summary>
        /// Gets or sets hidden keybinding for Remove Vehicle Cims and Animals action.
        /// </summary>
        [SettingsUIMouseBinding(BetterBulldozerMod.VCAApplyMimicAction)]
        [SettingsUIHidden]
        public ProxyBinding VCAApplyMimic { get; set; }


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
        public void ManageAutomaticallyRemoveFencesAndHedgesSystem(bool value)
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AutomaticallyRemoveFencesAndHedges>().Enabled = value;
            if (!value && World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ToolSystem>().actionMode.IsGame())
            {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<RestoreFencesAndHedgesSystem>().Enabled = true;
            }
        }

        /// <summary>
        /// Sets Enabled for AutomaticallyRemoveBrandingObjects.
        /// </summary>
        /// <param name="value">Toggle value.</param>
        public void ManageAutomaticallyRemoveBrandingObjects(bool value)
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AutomaticallyRemoveBrandingObjects>().Enabled = value;
            if (!value && World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ToolSystem>().actionMode.IsGame())
            {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<RestoreBrandingObjects>().Enabled = true;
            }
        }

        private bool IsRemovingExtensionsProhibited() => !AllowRemovingExtensions;
    }
}
