// <copyright file="BetterBulldozerUISystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

// #define VERBOSE
namespace Better_Bulldozer.Systems
{
    using Better_Bulldozer.Helpers;
    using Better_Bulldozer.Tools;
    using Colossal.Logging;
    using Colossal.UI.Binding;
    using Game.Areas;
    using Game.Common;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Game.UI;
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
        private ValueBinding<bool> m_GameplayManipulation;
        private ValueBinding<bool> m_UpgradeIsMain;
        private ValueBindingHelper<int> m_SelectionMode;
        private ToolBaseSystem m_PreviousBulldozeToolSystem;
        private ToolBaseSystem m_PreviousToolSystem;
        private bool m_ToolModeToggledRecently;
        private PrefabBase m_PreviousPrefab;
        private bool m_SwitchToSubElementBulldozeToolOnUpdate;
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
        /// Gets a value indicating the active selection mode for subelement bulldozer.
        /// </summary>
        public SelectionMode ActiveSelectionMode { get => (SelectionMode)m_SelectionMode.Value; }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_BulldozeToolSystem = World.GetOrCreateSystemManaged<BulldozeToolSystem>();
            m_RenderingSystem = World.GetOrCreateSystemManaged<RenderingSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ObjectToolSystem = World.GetOrCreateSystemManaged<ObjectToolSystem>();
            m_SubElementBulldozeToolSystem = World.GetOrCreateSystemManaged<SubElementBulldozerTool>();
            m_NetToolSystem = World.GetOrCreateSystemManaged<NetToolSystem>();
            m_ToolSystem.EventToolChanged += OnToolChanged;
            m_DefaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            m_ToolSystem.EventPrefabChanged += OnPrefabChanged;
            m_PreviousBulldozeToolSystem = m_BulldozeToolSystem;

            // These establish binding with UI.
            AddBinding(m_RaycastTarget = new ValueBinding<int>(ModId, "RaycastTarget", (int)RaycastTarget.Vanilla));
            AddBinding(m_AreasFilter = new ValueBinding<int>(ModId, "AreasFilter", (int)AreaTypeMask.Surfaces));
            AddBinding(m_MarkersFilter = new ValueBinding<int>(ModId, "MarkersFilter", (int)TypeMask.Net));
            AddBinding(m_BypassConfirmation = new ValueBinding<bool>(ModId, "BypassConfirmation", false));
            AddBinding(m_GameplayManipulation = new ValueBinding<bool>(ModId, "GameplayManipulation", false));
            AddBinding(m_UpgradeIsMain = new ValueBinding<bool>(ModId, "UpgradeIsMain", false));
            AddBinding(m_SubElementBulldozeToolActive = new ValueBinding<bool>(ModId, "SubElementBulldozeToolActive", false));
            m_SelectionMode = CreateBinding("SelectionMode", (int)SelectionMode.Single);

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
            AddBinding(new TriggerBinding(ModId, "UpgradeIsMain", UpgradeIsMainToggled));
            AddBinding(new TriggerBinding(ModId, "SubElementsOfMainElement", SubElementsOfMainElementToggled));
            CreateTrigger("ChangeSelectionMode", (int value) => m_SelectionMode.UpdateCallback(value));
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

            if (m_ActivatePrefabToolOnUpdate)
            {
                m_ActivatePrefabToolOnUpdate = false;
                m_ToolSystem.ActivatePrefabTool(m_PreviousPrefab);
            }

            if (m_BulldozeToolSystem.debugBypassBulldozeConfirmation != m_BypassConfirmation.value)
            {
                m_BypassConfirmation.Update(m_BulldozeToolSystem.debugBypassBulldozeConfirmation);
            }

            if (m_GameplayManipulation.value != m_BulldozeToolSystem.allowManipulation)
            {
                m_GameplayManipulation.Update(m_BulldozeToolSystem.allowManipulation);
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
            m_GameplayManipulation.Update(!m_GameplayManipulation.value);
            m_BulldozeToolSystem.allowManipulation = m_GameplayManipulation.value;
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

            if (m_ToolSystem.activeTool == m_SubElementBulldozeToolSystem)
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

            if (m_ToolSystem.activeTool == m_SubElementBulldozeToolSystem)
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
            if (m_ToolSystem.activeTool == m_BulldozeToolSystem)
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

        /// <summary>
        /// For setting upgrade is main when button is pressed.
        /// </summary>
        private void UpgradeIsMainToggled() => m_UpgradeIsMain.Update(true);

        /// <summary>
        /// For unsetting upgrade is main when subeleemnts of main element button pressed.
        /// </summary>
        private void SubElementsOfMainElementToggled() => m_UpgradeIsMain.Update(false);
    }
}
