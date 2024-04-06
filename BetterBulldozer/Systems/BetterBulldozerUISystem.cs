// <copyright file="BetterBulldozerUISystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

// #define VERBOSE
namespace Better_Bulldozer.Systems
{
    using System;
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
    public partial class BetterBulldozerUISystem : UISystemBase
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
        private ValueBinding<int> m_RaycastTarget;
        private ValueBinding<int> m_AreasFilter;
        private ValueBinding<int> m_MarkersFilter;
        private ValueBinding<bool> m_BypassConfirmation;
        private ValueBinding<bool> m_GameplayManipulation;
        private ValueBinding<bool> m_UpgradeIsMain;
        private ValueBinding<bool> m_NoMainElements;

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
        /// Gets a value indicating whether to have NoMainElements.
        /// </summary>
        public bool NoMainElements { get => m_NoMainElements.value; }

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

            // This binding communicates what the Raycast target is.
            AddBinding(m_RaycastTarget = new ValueBinding<int>(ModId, "RaycastTarget", (int)RaycastTarget.Vanilla));

            // This binding communicates what the Area filter is.
            AddBinding(m_AreasFilter = new ValueBinding<int>(ModId, "AreasFilter", (int)AreaTypeMask.Surfaces));

            // This binding communicates what the Markers filter is.
            AddBinding(m_MarkersFilter = new ValueBinding<int>(ModId, "MarkersFilter", (int)TypeMask.Net));

            // This binding communicates whether bypass confirmation is toggled.
            AddBinding(m_BypassConfirmation = new ValueBinding<bool>(ModId, "BypassConfirmation", false));

            // This binding communicates whether gameplay manipulation is toggled.
            AddBinding(m_GameplayManipulation = new ValueBinding<bool>(ModId, "GameplayManipulation", false));

            // This binding communicates whether UpgradeIsMain is toggled.
            AddBinding(m_UpgradeIsMain = new ValueBinding<bool>(ModId, "UpgradeIsMain", false));

            // This binding communicates whether m_NoMainElements is toggled.
            AddBinding(m_NoMainElements = new ValueBinding<bool>(ModId, "NoMainElements", false));

            // This binding listens for whether the BypassConfirmation tool icon has been toggled.
            AddBinding(new TriggerBinding(ModId, "BypassConfirmationButton", BypassConfirmationToggled));

            // This binding listens for whether the GameplayManipulation tool icon has been toggled.
            AddBinding(new TriggerBinding(ModId, "GameplayManipulationButton", GameplayManipulationToggled));

            // This binding listens for whether the RaycastMarkersButton tool icon has been toggled.
            AddBinding(new TriggerBinding(ModId, "RaycastMarkersButton", RaycastMarkersButtonToggled));

            // This binding listens for whether the SurfacesFilter tool icon has been toggled.
            AddBinding(new TriggerBinding(ModId, "SurfacesFilterButton", SurfacesFilterToggled));

            // This binding listens for whether the SpacesFilter tool icon has been toggled.
            AddBinding(new TriggerBinding(ModId, "SpacesFilterButton", SpacesFilterToggled));

            // This binding listens for whether the StaticObjectsFilter tool icon has been toggled.
            AddBinding(new TriggerBinding(ModId, "StaticObjectsFilterButton", StaticObjectsFilterToggled));

            // This binding listens for whether the NetworksFilter tool icon has been toggled.
            AddBinding(new TriggerBinding(ModId, "NetworksFilterButton", NetworksFilterToggled));

            // This binding listens for whether the RaycastAreasButton tool icon has been toggled.
            AddBinding(new TriggerBinding(ModId, "RaycastAreasButton", RaycastAreasButtonToggled));

            // This binding listens for whether the RaycastLabesButton tool icon has been toggled.
            AddBinding(new TriggerBinding(ModId, "RaycastLanesButton", RaycastLanesButtonToggled));

            // This binding listens for whether the SubElementBulldozerButton tool icon has been toggled.
            AddBinding(new TriggerBinding(ModId, "SubElementBulldozerButton", SubElementBulldozerButtonToggled));

            // This binding listens for whether the UpgradeIsMain or SubElementOfMainElement tool icon has been toggled.
            AddBinding(new TriggerBinding(ModId, "UpgradeIsMain", UpgradeIsMainToggled));

            // This binding listens for whether the UpgradeIsMain or SubElementOfMainElement tool icon has been toggled.
            AddBinding(new TriggerBinding(ModId, "SubElementsOfMainElement", SubElementsOfMainElementToggled));

            // This binding listens for whether the UpgradeIsMain or SubElementOfMainElement tool icon has been toggled.
            AddBinding(new TriggerBinding(ModId, "NoMainElements", NoMainElementToggled));
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

            HandleShowMarkers(m_ToolSystem.activePrefab);
        }

        /// <summary>
        /// C# event handler for event callback from UI JavaScript. Toggles the m_RaycastAreas.
        /// </summary>
        private void SubElementBulldozerButtonToggled()
        {
            if (m_ToolSystem.activeTool == m_BulldozeToolSystem)
            {
                m_ToolSystem.activeTool = m_SubElementBulldozeToolSystem;
            }
            else if (m_ToolSystem.activeTool == m_SubElementBulldozeToolSystem)
            {
                m_ToolSystem.activeTool = m_BulldozeToolSystem;
            }

            HandleShowMarkers(m_ToolSystem.activePrefab);
        }


        private void HandleShowMarkers(PrefabBase prefab)
        {
            if (prefab != null && m_PrefabSystem.TryGetEntity(prefab, out Entity prefabEntity) && m_ToolSystem.activeTool != m_DefaultToolSystem)
            {
                if (EntityManager.HasComponent<MarkerNetData>(prefabEntity)
                 || prefab is MarkerObjectPrefab || prefab is NetLaneGeometryPrefab || prefab is NetLanePrefab
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

            m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(OnToolChanged)} {tool.toolID} {m_ToolSystem.activePrefab?.GetPrefabID()} {tool.GetPrefab()?.GetPrefabID()}");

            HandleShowMarkers(m_ToolSystem.activePrefab);
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
        }

        /// <summary>
        /// For setting upgrade is main when button is pressed.
        /// </summary>
        private void UpgradeIsMainToggled() => m_UpgradeIsMain.Update(true);

        /// <summary>
        /// For unsetting upgrade is main when subeleemnts of main element button pressed.
        /// </summary>
        private void SubElementsOfMainElementToggled() => m_UpgradeIsMain.Update(false);

        /// <summary>
        /// For unsetting upgrade is main when subeleemnts of main element button pressed.
        /// </summary>
        private void NoMainElementToggled() => m_NoMainElements.Update(!m_NoMainElements.value);

    }
}
