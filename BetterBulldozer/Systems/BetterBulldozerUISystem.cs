// <copyright file="BetterBulldozerUISystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

// #define VERBOSE
namespace Better_Bulldozer.Systems
{
    using System;
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
        private ToolSystem m_ToolSystem;
        private ILog m_Log;
        private RenderingSystem m_RenderingSystem;
        private PrefabSystem m_PrefabSystem;
        private BulldozeToolSystem m_BulldozeToolSystem;
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
        private ValueBinding<bool> m_BulldozeToolActive;

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

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_ToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            m_BulldozeToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<BulldozeToolSystem>();
            m_RenderingSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<RenderingSystem>();
            m_PrefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            m_ObjectToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ObjectToolSystem>();
            m_NetToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<NetToolSystem>();
            ToolSystem toolSystem = m_ToolSystem; // I don't know why vanilla game did this.
            m_ToolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Combine(toolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChanged));
            m_DefaultToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<DefaultToolSystem>();
            ToolSystem toolSystem2 = m_ToolSystem;
            toolSystem2.EventPrefabChanged = (Action<PrefabBase>)Delegate.Combine(toolSystem2.EventPrefabChanged, new Action<PrefabBase>(OnPrefabChanged));

            // This binding communicates what the Raycast target is.
            AddBinding(m_RaycastTarget = new ValueBinding<int>("BetterBulldozer", "RaycastTarget", (int)RaycastTarget.Vanilla));

            // This binding communicates what the Area filter is.
            AddBinding(m_AreasFilter = new ValueBinding<int>("BetterBulldozer", "AreasFilter", (int)AreaTypeMask.Surfaces));

            // This binding communicates what the Markers filter is.
            AddBinding(m_MarkersFilter = new ValueBinding<int>("BetterBulldozer", "MarkersFilter", (int)TypeMask.Net));

            // This binding communicates whether bypass confirmation is toggled.
            AddBinding(m_BypassConfirmation = new ValueBinding<bool>("BetterBulldozer", "BypassConfirmation", false));

            // This binding communicates whether gameplay manipulation is toggled.
            AddBinding(m_GameplayManipulation = new ValueBinding<bool>("BetterBulldozer", "GameplayManipulation", false));

            // This binding communicates whether the bulldoze tool is active.
            AddBinding(m_BulldozeToolActive = new ValueBinding<bool>("BetterBulldozer", "BulldozeToolActive", false));

            // This binding listens for whether the BypassConfirmationToggled tool icon has been toggled.
            AddBinding(new TriggerBinding("BetterBulldozer", "BypassConfirmationToggled", BypassConfirmationToggled));

            // This binding listens for whether the GameplayManipulationToggled tool icon has been toggled.
            AddBinding(new TriggerBinding("BetterBulldozer", "GameplayManipulationToggled", GameplayManipulationToggled));

            // This binding listens for whether the RaycastMarkersButtonToggled tool icon has been toggled.
            AddBinding(new TriggerBinding("BetterBulldozer", "RaycastMarkersButtonToggled", RaycastMarkersButtonToggled));

            // This binding listens for whether the SurfacesFilterToggled tool icon has been toggled.
            AddBinding(new TriggerBinding("BetterBulldozer", "SurfacesFilterToggled", SurfacesFilterToggled));

            // This binding listens for whether the SpacesFilterToggled tool icon has been toggled.
            AddBinding(new TriggerBinding("BetterBulldozer", "SpacesFilterToggled", SpacesFilterToggled));

            // This binding listens for whether the StaticObjectsFilterToggled tool icon has been toggled.
            AddBinding(new TriggerBinding("BetterBulldozer", "StaticObjectsFilterToggled", StaticObjectsFilterToggled));

            // This binding listens for whether the NetworksFilterToggled tool icon has been toggled.
            AddBinding(new TriggerBinding("BetterBulldozer", "NetworksFilterToggled", NetworksFilterToggled));

            // This binding listens for whether the RaycastAreasButtonToggled tool icon has been toggled.
            AddBinding(new TriggerBinding("BetterBulldozer", "RaycastAreasButtonToggled", RaycastAreasButtonToggled));
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

        private void HandleShowMarkers(PrefabBase prefab)
        {
            if (prefab != null && m_PrefabSystem.TryGetEntity(prefab, out Entity prefabEntity) && m_ToolSystem.activeTool != m_DefaultToolSystem)
            {
                if (EntityManager.HasComponent<MarkerNetData>(prefabEntity) || prefab is MarkerObjectPrefab || (prefab is BulldozePrefab /*&& m_RaycastTarget.value == RaycastTarget.Markers*/))
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

                    // m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(HandleShowMarkers)}  m_RaycastTarget == RaycastTarget.Markers: {m_RaycastTarget.value == RaycastTarget.Markers}");
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
            if (tool == null || m_ToolSystem.activePrefab == null)
            {
                m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(OnToolChanged)} something is null.");
                return;
            }

            m_Log.Debug($"{nameof(BetterBulldozerUISystem)}.{nameof(OnToolChanged)} {tool.toolID} {m_ToolSystem.activePrefab?.GetPrefabID()} {tool.GetPrefab()?.GetPrefabID()}");

            bool flag = tool == m_BulldozeToolSystem;
            if (m_BulldozeToolActive.value != flag)
            {
                m_BulldozeToolActive.Update(flag);
            }

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
    }
}
