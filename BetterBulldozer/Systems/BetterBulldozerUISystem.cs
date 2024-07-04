// <copyright file="BetterBulldozerUISystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

// #define VERBOSE
namespace Better_Bulldozer.Systems
{
    using System;
    using System.Security.Cryptography;
    using Better_Bulldozer.Extensions;
    using Better_Bulldozer.Tools;
    using Better_Bulldozer.Utils;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Colossal.UI.Binding;
    using Game;
    using Game.Areas;
    using Game.Common;
    using Game.Debug;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Unity.Entities;

    /// <summary>
    /// UI system for Better Bulldozer extensions to the bulldoze tool.
    /// </summary>
    public partial class BetterBulldozerUISystem : ExtendedUISystemBase
    {
        private const string ModId = "BetterBulldozer";

        private ToolSystem m_ToolSystem;
        private ILog m_Log;
        private RenderingSystem m_RenderingSystem;
        private PrefabSystem m_PrefabSystem;
        private BulldozeToolSystem m_BulldozeToolSystem;
        private SubElementBulldozerTool m_SubElementBulldozeToolSystem;
        private bool m_RecordedShowMarkers;
        private bool m_PrefabIsMarker = false;
        private NetToolSystem m_NetToolSystem;
        private ObjectToolSystem m_ObjectToolSystem;
        private DefaultToolSystem m_DefaultToolSystem;
        private ValueBinding<bool> m_SubElementBulldozeToolActive;
        private ValueBinding<int> m_RaycastTarget;
        private ValueBinding<int> m_AreasFilter;
        private ValueBinding<int> m_MarkersFilter;
        private ValueBinding<bool> m_BypassConfirmation;
        private ValueBinding<bool> m_UpgradeIsMain;
        private ValueBindingHelper<bool> m_IsGame;
        private ValueBindingHelper<int> m_SelectionMode;
        private ValueBindingHelper<int> m_VehicleCimsAnimalsSelectionMode;
        private ValueBindingHelper<int> m_SelectionRadius;
        private ValueBindingHelper<VanillaFilters> m_SelectedVanillaFilters;
        private ToolBaseSystem m_PreviousBulldozeToolSystem;
        private ToolBaseSystem m_PreviousToolSystem;
        private bool m_ToolModeToggledRecently;
        private RemoveVehiclesCimsAndAnimalsTool m_RemoveVehiclesCimsAndAnimalsTool;
        private PrefabBase m_PreviousPrefab;
        private bool m_SwitchToSubElementBulldozeToolOnUpdate;
        private bool m_SwitchToRemoveVehilcesCimsAndAnimalsToolOnUpdate;
        private bool m_ActivatePrefabToolOnUpdate;

        /// <summary>
        /// An enum to handle different raycast target options.
        /// </summary>
        public enum RaycastTarget
        {
            /// <summary>
            /// Do not change the raycast targets.
            /// </summary>
            Vanilla,

            /// <summary>
            /// Exclusively target surfaces and spaces
            /// </summary>
            Areas,

            /// <summary>
            /// Exclusively target markers.
            /// </summary>
            Markers,

            /// <summary>
            /// Exclusively target standalone lanes such as fences, hedges, street markings, or vehicle lanes.
            /// </summary>
            Lanes,

            /// <summary>
            /// Exclusively target vehciles, cims and animals.
            /// </summary>
            VehiclesCimsAndAnimals,
        }

        /// <summary>
        /// Selection mode for subelement bulldozer.
        /// </summary>
        public enum SelectionMode
        {
            /// <summary>
            /// One item at a time.
            /// </summary>
            Single,

            /// <summary>
            /// All exact match of prefab with same owner.
            /// </summary>
            Matching,

            /// <summary>
            /// Same family of prefab with same owner.
            /// </summary>
            Similar,

            /// <summary>
            /// Handles reseting assets.
            /// </summary>
            Reset,
        }

        /// <summary>
        /// Selection mode for removing vehicles, cims, and animals.
        /// </summary>
        public enum VCAselectionMode
        {
            /// <summary>
            /// One item at a time.
            /// </summary>
            Single,

            /// <summary>
            /// uses a radius and can delete broken ones.
            /// </summary>
            Radius,
        }

        /// <summary>
        /// An enum used to communicate filters for vanilla bulldozer.
        /// </summary>
        public enum VanillaFilters
        {
            /// <summary>
            /// Nothing selected.
            /// </summary>
            None = 0,

            /// <summary>
            /// Roads, tracks, etc.
            /// </summary>
            Networks = 1,

            /// <summary>
            /// Things with building data.
            /// </summary>
            Buildings = 2,

            /// <summary>
            /// Trees and wild bushes.
            /// </summary>
            Trees = 4,

            /// <summary>
            /// Cultivated plants and potted plants.
            /// </summary>
            Plants = 8,

            /// <summary>
            /// Decals.
            /// </summary>
            Decals = 16,

            /// <summary>
            /// Static objects that are not anything else.
            /// </summary>
            Props = 32,

            /// <summary>
            /// Vanilla bulldozer, no filters.
            /// </summary>
            All = 64,
        }

        /// <summary>
        /// Gets a value indicating what to raycast.
        /// </summary>
        public RaycastTarget SelectedRaycastTarget { get => (RaycastTarget)m_RaycastTarget.value; }

        /// <summary>
        /// Gets a value indicating the filter to apply to areas.
        /// </summary>
        public AreaTypeMask AreasFilter { get => (AreaTypeMask)m_AreasFilter.value; }

        /// <summary>
        /// Gets a value indicating the filter to apply to Markers.
        /// </summary>
        public TypeMask MarkersFilter { get => (TypeMask)m_MarkersFilter.value; }

        /// <summary>
        /// Gets a value indicating whether UpgradeIsMain.
        /// </summary>
        public bool UpgradeIsMain { get => m_UpgradeIsMain.value; }

        /// <summary>
        /// Gets a value indicating the selection radius.
        /// </summary>
        public int SelectionRadius { get => m_SelectionRadius.Value; }

        /// <summary>
        /// Gets a value indicating the selected vanilla bulldoze tool filters.
        /// </summary>
        public VanillaFilters SelectedVanillaFilters { get => m_SelectedVanillaFilters.Value; }

        /// <summary>
        /// Gets a value indicating the active selection mode for subelement bulldozer.
        /// </summary>
        public SelectionMode ActiveSelectionMode { get => (SelectionMode)m_SelectionMode.Value; }

        /// <summary>
        /// Gets a value indicating the active selection mode for subelement bulldozer.
        /// </summary>
        public VCAselectionMode VehicleCimsAnimalsSelectionMode { get => (VCAselectionMode)m_VehicleCimsAnimalsSelectionMode.Value; }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_Log.Info($"{nameof(BetterBulldozerUISystem)}.{nameof(OnCreate)}");
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_BulldozeToolSystem = World.GetOrCreateSystemManaged<BulldozeToolSystem>();
            m_RenderingSystem = World.GetOrCreateSystemManaged<RenderingSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ObjectToolSystem = World.GetOrCreateSystemManaged<ObjectToolSystem>();
            m_SubElementBulldozeToolSystem = World.GetOrCreateSystemManaged<SubElementBulldozerTool>();
            m_NetToolSystem = World.GetOrCreateSystemManaged<NetToolSystem>();
            m_ToolSystem.EventToolChanged += OnToolChanged;
            m_DefaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            m_RemoveVehiclesCimsAndAnimalsTool = World.GetOrCreateSystemManaged<RemoveVehiclesCimsAndAnimalsTool>();
            m_ToolSystem.EventPrefabChanged += OnPrefabChanged;
            m_PreviousBulldozeToolSystem = m_BulldozeToolSystem;

            // These establish binding with UI.
            AddBinding(m_RaycastTarget = new ValueBinding<int>(ModId, "RaycastTarget", (int)RaycastTarget.Vanilla));
            AddBinding(m_AreasFilter = new ValueBinding<int>(ModId, "AreasFilter", (int)AreaTypeMask.Surfaces));
            AddBinding(m_MarkersFilter = new ValueBinding<int>(ModId, "MarkersFilter", (int)TypeMask.Net));
            AddBinding(m_BypassConfirmation = new ValueBinding<bool>(ModId, "BypassConfirmation", false));
            AddBinding(m_UpgradeIsMain = new ValueBinding<bool>(ModId, "UpgradeIsMain", false));
            AddBinding(m_SubElementBulldozeToolActive = new ValueBinding<bool>(ModId, "SubElementBulldozeToolActive", false));
            m_SelectionMode = CreateBinding("SelectionMode", (int)BetterBulldozerMod.Instance.Settings.PreviousSelectionMode);
            m_IsGame = CreateBinding("IsGame", false);
            m_VehicleCimsAnimalsSelectionMode = CreateBinding("VehicleCimsAnimalsSelectionMode", (int)VCAselectionMode.Single);
            m_SelectionRadius = CreateBinding("SelectionRadius", 10);
            m_SelectedVanillaFilters = CreateBinding("SelectedVanillaFilters", VanillaFilters.Networks | VanillaFilters.Buildings | VanillaFilters.Trees | VanillaFilters.Plants | VanillaFilters.Decals | VanillaFilters.Props | VanillaFilters.All);

            // These handle events activating actions triggered by clicking buttons in the UI.
            AddBinding(new TriggerBinding(ModId, "BypassConfirmationButton", BypassConfirmationToggled));
            AddBinding(new TriggerBinding(ModId, "GameplayManipulationButton", GameplayManipulationToggled));
            AddBinding(new TriggerBinding(ModId, "RaycastMarkersButton", RaycastMarkersButtonToggled));
            AddBinding(new TriggerBinding(ModId, "SurfacesFilterButton", SurfacesFilterToggled));
            AddBinding(new TriggerBinding(ModId, "SpacesFilterButton", SpacesFilterToggled));
            AddBinding(new TriggerBinding(ModId, "StaticObjectsFilterButton", StaticObjectsFilterToggled));
            AddBinding(new TriggerBinding(ModId, "NetworksFilterButton", NetworksFilterToggled));
            AddBinding(new TriggerBinding(ModId, "RaycastAreasButton", RaycastAreasButtonToggled));
            AddBinding(new TriggerBinding(ModId, "RaycastLanesButton", RaycastLanesButtonToggled));
            AddBinding(new TriggerBinding(ModId, "SubElementBulldozerButton", SubElementBulldozerButtonToggled));
            AddBinding(new TriggerBinding(ModId, "UpgradeIsMain", () => m_UpgradeIsMain.Update(true)));
            AddBinding(new TriggerBinding(ModId, "SubElementsOfMainElement", () => m_UpgradeIsMain.Update(false)));
            CreateTrigger("ChangeSelectionMode", (int value) => ChangeSelectionMode(value));
            CreateTrigger("ChangeVCAselectionMode", (int value) => ChangeVCAselectionMode(value) );
            CreateTrigger("IncreaseRadius", () => m_SelectionRadius.Value = Math.Min(m_SelectionRadius.Value + 10, 100));
            CreateTrigger("DecreaseRadius", () => m_SelectionRadius.Value = Math.Max(m_SelectionRadius.Value - 10, 10));
            CreateTrigger("ChangeVanillaFilter", (int value) => ChangeVanillaFilters((VanillaFilters)value));
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode == GameMode.Game)
            {
                m_IsGame.Value = true;
                return;
            }

            m_IsGame.Value = false;
            return;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (m_SwitchToSubElementBulldozeToolOnUpdate)
            {
                m_SwitchToSubElementBulldozeToolOnUpdate = false;
                m_ToolSystem.activeTool = m_SubElementBulldozeToolSystem;
            }
            else if (m_SwitchToRemoveVehilcesCimsAndAnimalsToolOnUpdate)
            {
                m_SwitchToRemoveVehilcesCimsAndAnimalsToolOnUpdate = false;
                m_ToolSystem.activeTool = m_RemoveVehiclesCimsAndAnimalsTool;
            }

            if (m_ActivatePrefabToolOnUpdate)
            {
                m_ActivatePrefabToolOnUpdate = false;
                m_ToolSystem.ActivatePrefabTool(m_PreviousPrefab);
            }

            if (m_BulldozeToolSystem.debugBypassBulldozeConfirmation != m_BypassConfirmation.value)
            {
                m_BypassConfirmation.Update(m_BulldozeToolSystem.debugBypassBulldozeConfirmation);
            }

            if (SelectedRaycastTarget == RaycastTarget.Areas && AreasFilter == AreaTypeMask.Spaces)
            {
                AreaTypeMask areaTypeMask = m_BulldozeToolSystem.requireAreas;
                areaTypeMask |= AreaTypeMask.Spaces;
                areaTypeMask &= ~AreaTypeMask.Surfaces;
                m_BulldozeToolSystem.SetMemberValue("requireAreas", areaTypeMask);
            }
            else if (SelectedRaycastTarget == RaycastTarget.Areas)
            {
                AreaTypeMask areaTypeMask = m_BulldozeToolSystem.requireAreas;
                areaTypeMask |= AreaTypeMask.Surfaces;
                areaTypeMask &= ~AreaTypeMask.Spaces;
                m_BulldozeToolSystem.SetMemberValue("requireAreas", areaTypeMask);
            }
        }

        /// <summary>
        /// C# event handler for event callback from UI JavaScript. Toggles the bypassConfirmation field of the bulldozer system.
        /// </summary>
        /// <param name="flag">A bool for what to set the field to.</param>
        private void BypassConfirmationToggled()
        {
            m_BypassConfirmation.Update(!m_BypassConfirmation.value);
            m_BulldozeToolSystem.debugBypassBulldozeConfirmation = m_BypassConfirmation.value;
        }

        /// <summary>
        /// C# event handler for event callback from UI JavaScript. Toggles the game playmanipulation field of the bulldozer system.
        /// </summary>
        private void GameplayManipulationToggled()
        {
            if (m_RaycastTarget.value != (int)RaycastTarget.VehiclesCimsAndAnimals)
            {
                m_RaycastTarget.Update((int)RaycastTarget.VehiclesCimsAndAnimals);
            }
            else
            {
                m_RaycastTarget.Update((int)RaycastTarget.Vanilla);
            }

            if (m_VehicleCimsAnimalsSelectionMode.Value == (int)VCAselectionMode.Radius && m_ToolSystem.activeTool != m_RemoveVehiclesCimsAndAnimalsTool)
            {
                m_PreviousBulldozeToolSystem = m_RemoveVehiclesCimsAndAnimalsTool;
                m_ToolSystem.activeTool = m_RemoveVehiclesCimsAndAnimalsTool;
            }
            else if (m_ToolSystem.activeTool == m_RemoveVehiclesCimsAndAnimalsTool)
            {
                m_PreviousBulldozeToolSystem = m_BulldozeToolSystem;
                m_ToolModeToggledRecently = true;
                m_ToolSystem.activeTool = m_BulldozeToolSystem;
            }

            HandleShowMarkers(m_ToolSystem.activePrefab);
        }

        private void ChangeVCAselectionMode(int mode)
        {
            m_VehicleCimsAnimalsSelectionMode.Value = mode;

            if (m_VehicleCimsAnimalsSelectionMode.Value == (int)VCAselectionMode.Radius && m_ToolSystem.activeTool != m_RemoveVehiclesCimsAndAnimalsTool)
            {
                m_PreviousBulldozeToolSystem = m_RemoveVehiclesCimsAndAnimalsTool;
                m_ToolSystem.activeTool = m_RemoveVehiclesCimsAndAnimalsTool;
            }
            else if (m_ToolSystem.activeTool == m_RemoveVehiclesCimsAndAnimalsTool && m_VehicleCimsAnimalsSelectionMode.Value == (int)VCAselectionMode.Single)
            {
                m_PreviousBulldozeToolSystem = m_BulldozeToolSystem;
                m_ToolModeToggledRecently = true;
                m_ToolSystem.activeTool = m_BulldozeToolSystem;
            }

            HandleShowMarkers(m_ToolSystem.activePrefab);
        }

        private void ChangeVanillaFilters(VanillaFilters toggledFilter)
        {
            if (toggledFilter != VanillaFilters.All && (m_SelectedVanillaFilters.Value & VanillaFilters.All) == VanillaFilters.All)
            {
                m_SelectedVanillaFilters.Value &= ~VanillaFilters.All;
            }
            else if (toggledFilter == VanillaFilters.All && m_SelectedVanillaFilters.Value != VanillaFilters.None)
            {
                m_SelectedVanillaFilters.Value = VanillaFilters.None;
                return;
            }
            else if (toggledFilter == VanillaFilters.All && m_SelectedVanillaFilters.Value == VanillaFilters.None)
            {
                m_SelectedVanillaFilters.Value |= VanillaFilters.Networks | VanillaFilters.Buildings | VanillaFilters.Trees | VanillaFilters.Plants | VanillaFilters.Decals | VanillaFilters.Props | VanillaFilters.All;
                return;
            }

            if ((m_SelectedVanillaFilters.Value & toggledFilter) == toggledFilter)
            {
                m_SelectedVanillaFilters.Value &= ~toggledFilter;
            }
            else
            {
                m_SelectedVanillaFilters.Value |= toggledFilter;
            }

            if ((int)m_SelectedVanillaFilters.Value == 63)
            {
                m_SelectedVanillaFilters.Value |= VanillaFilters.All;
            }
        }

        /// <summary>
        /// C# event handler for event callback from UI JavaScript. Toggles the m_RenderingSystem.MarkersVisible.
        /// </summary>
        private void RaycastMarkersButtonToggled()
        {
            if (SelectedRaycastTarget != RaycastTarget.Markers)
            {
                m_RaycastTarget.Update((int)RaycastTarget.Markers);
            }
            else
            {
                m_RaycastTarget.Update((int)RaycastTarget.Vanilla);
            }

            HandleShowMarkers(m_ToolSystem.activePrefab);
        }

        /// <summary>
        /// C# event handler for event callback from UI JavaScript. For filtering for surfaces.
        /// </summary>
        private void SurfacesFilterToggled()
        {
            if (AreasFilter != AreaTypeMask.Surfaces)
            {
                m_AreasFilter.Update((int)AreaTypeMask.Surfaces);
            }
            else
            {
                m_AreasFilter.Update((int)AreaTypeMask.Spaces);
            }
        }

        /// <summary>
        /// C# event handler for event callback from UI JavaScript. For filtering for spaces.
        /// </summary>
        private void SpacesFilterToggled()
        {
            if (AreasFilter != AreaTypeMask.Spaces)
            {
                m_AreasFilter.Update((int)AreaTypeMask.Spaces);
            }
            else
            {
                m_AreasFilter.Update((int)AreaTypeMask.Surfaces);
            }
        }

        /// <summary>
        /// C# event handler for event callback from UI JavaScript. For filtering for static objects.
        /// </summary>
        private void StaticObjectsFilterToggled()
        {
            if (MarkersFilter != TypeMask.StaticObjects)
            {
                m_MarkersFilter.Update((int)TypeMask.StaticObjects);
            }
            else
            {
                m_MarkersFilter.Update((int)TypeMask.Net);
            }
        }

        /// <summary>
        /// C# event handler for event callback from UI JavaScript. For filtering for nets.
        /// </summary>
        private void NetworksFilterToggled()
        {
            if (MarkersFilter != TypeMask.Net)
            {
                m_MarkersFilter.Update((int)TypeMask.Net);
            }
            else
            {
                m_MarkersFilter.Update((int)TypeMask.StaticObjects);
            }
        }

        /// <summary>
        /// C# event handler for event callback from UI JavaScript. Toggles the m_RaycastAreas.
        /// </summary>
        private void RaycastAreasButtonToggled()
        {
            if (SelectedRaycastTarget != RaycastTarget.Areas)
            {
                m_RaycastTarget.Update((int)RaycastTarget.Areas);
            }
            else
            {
                m_RaycastTarget.Update((int)RaycastTarget.Vanilla);
            }

            if (m_ToolSystem.activeTool != m_BulldozeToolSystem)
            {
                m_PreviousBulldozeToolSystem = m_BulldozeToolSystem;
                m_ToolModeToggledRecently = true;
                m_ToolSystem.activeTool = m_BulldozeToolSystem;
            }

            HandleShowMarkers(m_ToolSystem.activePrefab);
        }

        /// <summary>
        /// C# event handler for event callback from UI JavaScript. Toggles the m_RaycastAreas.
        /// </summary>
        private void RaycastLanesButtonToggled()
        {
            if (SelectedRaycastTarget != RaycastTarget.Lanes)
            {
                m_RaycastTarget.Update((int)RaycastTarget.Lanes);
            }
            else
            {
                m_RaycastTarget.Update((int)RaycastTarget.Vanilla);
            }

            if (m_ToolSystem.activeTool != m_BulldozeToolSystem)
            {
                m_PreviousBulldozeToolSystem = m_BulldozeToolSystem;
                m_ToolModeToggledRecently = true;
                m_ToolSystem.activeTool = m_BulldozeToolSystem;
            }

            HandleShowMarkers(m_ToolSystem.activePrefab);
        }

        /// <summary>
        /// C# event handler for event callback from UI JavaScript. Toggles the m_RaycastAreas.
        /// </summary>
        private void SubElementBulldozerButtonToggled()
        {
            if (m_ToolSystem.activeTool != m_SubElementBulldozeToolSystem)
            {
                m_PreviousBulldozeToolSystem = m_SubElementBulldozeToolSystem;
                m_ToolSystem.activeTool = m_SubElementBulldozeToolSystem;
                if (m_RaycastTarget.value != (int)RaycastTarget.Vanilla && m_RaycastTarget.value != (int)RaycastTarget.Markers)
                {
                    m_RaycastTarget.Update((int)RaycastTarget.Vanilla);
                }
            }
            else if (m_ToolSystem.activeTool == m_SubElementBulldozeToolSystem)
            {
                m_PreviousBulldozeToolSystem = m_BulldozeToolSystem;
                m_ToolModeToggledRecently = true;
                m_ToolSystem.activeTool = m_BulldozeToolSystem;
            }

            HandleShowMarkers(m_ToolSystem.activePrefab);
        }


        private void HandleShowMarkers(PrefabBase prefab)
        {
            if (prefab != null && m_PrefabSystem.TryGetEntity(prefab, out Entity prefabEntity) && m_ToolSystem.activeTool != m_DefaultToolSystem)
            {
                if (EntityManager.HasComponent<MarkerNetData>(prefabEntity)
                 || prefab is MarkerObjectPrefab || prefab is NetLaneGeometryPrefab || prefab is NetLanePrefab || prefab is TransformPrefab
                 || (prefab is BulldozePrefab && SelectedRaycastTarget == RaycastTarget.Markers)
                 || (prefab is BulldozePrefab && SelectedRaycastTarget == RaycastTarget.Lanes))
                {
                    if (!m_PrefabIsMarker)
                    {
                        m_RecordedShowMarkers = m_RenderingSystem.markersVisible;
                        m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(HandleShowMarkers)} m_RecordedShowMarkers = {m_RecordedShowMarkers}");
                    }

                    m_RenderingSystem.markersVisible = true;
                    m_PrefabIsMarker = true;
                    m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(HandleShowMarkers)} m_PrefabIsMarker = {m_PrefabIsMarker}");
                }
                else if (m_PrefabIsMarker)
                {
                    m_PrefabIsMarker = false;
                    m_RenderingSystem.markersVisible = m_RecordedShowMarkers;
                    m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(HandleShowMarkers)} EntityManager.HasComponent<MarkerNetData>(prefabEntity) : {EntityManager.HasComponent<MarkerNetData>(prefabEntity)}");
                    m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(HandleShowMarkers)} prefab is MarkerObjectPrefab : {prefab is MarkerObjectPrefab}");
                    m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(HandleShowMarkers)} prefab is BulldozePrefab : {prefab is BulldozePrefab}");

                    m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(HandleShowMarkers)}  m_RaycastTarget == RaycastTarget.Markers: {SelectedRaycastTarget == RaycastTarget.Markers}");
                }
            }
            else if (m_PrefabIsMarker)
            {
                m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(HandleShowMarkers)} prefab != null : {prefab != null}");
                if (prefab != null)
                {
                    m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(HandleShowMarkers)} m_PrefabSystem.TryGetEntity(prefab, out Entity prefabEntity) : {m_PrefabSystem.TryGetEntity(prefab, out Entity prefabEntity2)}");
                }

                m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(HandleShowMarkers)} m_ToolSystem.activeTool != m_DefaultToolSystem : {m_ToolSystem.activeTool != m_DefaultToolSystem}");
                m_PrefabIsMarker = false;
                m_RenderingSystem.markersVisible = m_RecordedShowMarkers;
            }
        }

        private void OnToolChanged(ToolBaseSystem tool)
        {
            if (tool == null)
            {
                m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(OnToolChanged)} something is null.");
                return;
            }

            if (tool == m_BulldozeToolSystem && m_PreviousBulldozeToolSystem == m_SubElementBulldozeToolSystem && m_PreviousToolSystem != m_SubElementBulldozeToolSystem)
            {
                m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(OnToolChanged)} Setting tool to SubElementBulldoze tool since that was previous tool mode.");
                m_SwitchToSubElementBulldozeToolOnUpdate = true;
            }
            else if (m_PreviousToolSystem == m_SubElementBulldozeToolSystem && (tool == m_BulldozeToolSystem || tool == m_DefaultToolSystem) && !m_ToolModeToggledRecently)
            {
                m_PreviousToolSystem = null;
                m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(OnToolChanged)} Activating prefab tool since subelement bulldoze tool was closed without changing tool mode.");
                m_ActivatePrefabToolOnUpdate = true;
            }

            if (tool == m_BulldozeToolSystem && m_PreviousBulldozeToolSystem == m_RemoveVehiclesCimsAndAnimalsTool && m_PreviousToolSystem != m_RemoveVehiclesCimsAndAnimalsTool)
            {
                m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(OnToolChanged)} Setting tool to m_RemoveVehiclesCimsAndAnimalsTool tool since that was previous tool mode.");
                m_SwitchToRemoveVehilcesCimsAndAnimalsToolOnUpdate = true;
            }
            else if (m_PreviousToolSystem == m_RemoveVehiclesCimsAndAnimalsTool && (tool == m_BulldozeToolSystem || tool == m_DefaultToolSystem) && !m_ToolModeToggledRecently)
            {
                m_PreviousToolSystem = null;
                m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(OnToolChanged)} Activating prefab tool since m_RemoveVehiclesCimsAndAnimalsTool was closed without changing tool mode.");
                m_ActivatePrefabToolOnUpdate = true;
            }

            m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(OnToolChanged)} {tool.toolID} {m_ToolSystem.activePrefab?.GetPrefabID()} {tool.GetPrefab()?.GetPrefabID()}");

            HandleShowMarkers(m_ToolSystem.activePrefab);
            if (m_ToolSystem.activePrefab is not BulldozePrefab)
            {
                m_PreviousPrefab = m_ToolSystem.activePrefab;
            }

            m_PreviousToolSystem = tool;
            m_ToolModeToggledRecently = false;

            if (tool == m_SubElementBulldozeToolSystem)
            {
                m_SubElementBulldozeToolActive.Update(true);
            }
            else if (m_SubElementBulldozeToolActive.value)
            {
                m_SubElementBulldozeToolActive.Update(false);
            }
        }

        /// <summary>
        /// Method implemented by event triggered by prefab changing.
        /// </summary>
        /// <param name="prefab">The new prefab.</param>
        private void OnPrefabChanged(PrefabBase prefab)
        {
            if (prefab == null)
            {
                return;
            }

            m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(OnPrefabChanged)} {prefab.GetPrefabID()}");
            HandleShowMarkers(prefab);
            if (prefab is not BulldozePrefab)
            {
                m_PreviousPrefab = prefab;
            }
        }

        private void ChangeSelectionMode(int value)
        {
            m_SelectionMode.Value = value;
            BetterBulldozerMod.Instance.Settings.PreviousSelectionMode = (SelectionMode)value;
            BetterBulldozerMod.Instance.Settings.ApplyAndSave();
        }
    }
}
