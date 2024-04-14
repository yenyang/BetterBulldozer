// <copyright file="SubElementBulldozerTool.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Tools
{
    using Better_Bulldozer.Settings;
    using Better_Bulldozer.Systems;
    using Colossal.Entities;
    using Colossal.Logging;
    using Game.Buildings;
    using Game.Common;
    using Game.Input;
    using Game.Net;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Tool for removing subelements. --burst-disable-compilation
    /// </summary>
    public partial class SubElementBulldozerTool : ToolBaseSystem
    {
        private ProxyAction m_ApplyAction;
        private OverlayRenderSystem m_OverlayRenderSystem;
        private BetterBulldozerUISystem m_BetterBulldozerUISystem;
        private BulldozeToolSystem m_BulldozeToolSystem;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private EntityQuery m_OwnedQuery;
        private SEBTSelectionMode m_SelectionMode = SEBTSelectionMode.Single;
        private float m_Radius = 100f;
        private ILog m_Log;
        private Entity m_SingleHighlightedEntity = Entity.Null;
        private Entity m_HighlighedSubobjectsEntity = Entity.Null;
        private RenderingSystem m_RenderingSystem;
        private EntityQuery m_HighlightedQuery;
        private SubelementBulldozerWarningTooltipSystem m_WarningTooltipSystem;

        /// <summary>
        /// An enum for the tool mod selection.
        /// </summary>
        public enum SEBTSelectionMode
        {
            /// <summary>
            /// SubElementBulldozerTool will only apply to one subelements inside a net or building.
            /// </summary>
            Single,

            /// <summary>
            /// SubElementBulldozerwill apply to all subelements inside a net, or building within specified radius.
            /// </summary>
            Radius,
        }

        /// <inheritdoc/>
        public override string toolID => m_BulldozeToolSystem.toolID; // This is hack to get the UI use bulldoze cursor and bulldoze bar.

        /// <summary>
        /// Gets or sets the TreeAgeChanger ToolMode.
        /// </summary>
        public SEBTSelectionMode SelectionMode { get => m_SelectionMode; set => m_SelectionMode = value; }

        /// <summary>
        /// Gets or sets the TreeAgeChanger Radius.
        /// </summary>
        public float Radius { get => m_Radius; set => m_Radius = value; }

        /// <inheritdoc/>
        public override void GetAvailableSnapMask(out Snap onMask, out Snap offMask)
        {
            base.GetAvailableSnapMask(out onMask, out offMask);
            onMask |= Snap.ContourLines;
            offMask |= Snap.ContourLines;
        }

        /// <inheritdoc/>
        public override PrefabBase GetPrefab()
        {
            return m_BulldozeToolSystem.GetPrefab();
        }

        /// <inheritdoc/>
        public override bool TrySetPrefab(PrefabBase prefab)
        {
            return false;
        }

        /// <inheritdoc/>
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();

            m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
            if (m_RenderingSystem.markersVisible && m_BetterBulldozerUISystem.SelectedRaycastTarget == BetterBulldozerUISystem.RaycastTarget.Markers)
            {
                m_ToolRaycastSystem.typeMask = m_BetterBulldozerUISystem.MarkersFilter;
                if ((m_BetterBulldozerUISystem.MarkersFilter & TypeMask.Net) == TypeMask.Net)
                {
                    m_ToolRaycastSystem.netLayerMask = Layer.MarkerPathway | Layer.MarkerTaxiway | Layer.PowerlineLow | Layer.PowerlineHigh | Layer.WaterPipe | Layer.SewagePipe;
                    m_ToolRaycastSystem.raycastFlags = RaycastFlags.Markers;
                    m_ToolRaycastSystem.utilityTypeMask = UtilityTypes.LowVoltageLine | UtilityTypes.HighVoltageLine | UtilityTypes.SewagePipe | UtilityTypes.SewagePipe;
                    m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Underground | CollisionMask.Overground;
                }
                else
                {
                    m_ToolRaycastSystem.raycastFlags = RaycastFlags.Markers | RaycastFlags.Decals;
                }
            }
            else
            {
                m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.Lanes;
                m_ToolRaycastSystem.netLayerMask = Layer.Fence;
                m_ToolRaycastSystem.raycastFlags = RaycastFlags.Decals;
                m_ToolRaycastSystem.utilityTypeMask = UtilityTypes.Fence;

                if (BetterBulldozerMod.Instance.Settings.AllowRemovingSubElementNetworks)
                {
                    m_ToolRaycastSystem.typeMask |= TypeMask.Net;
                    m_ToolRaycastSystem.netLayerMask |= Layer.Pathway | Layer.PowerlineHigh | Layer.PowerlineLow | Layer.Taxiway | Layer.PublicTransportRoad | Layer.SubwayTrack | Layer.TrainTrack | Layer.TramTrack | Layer.Waterway | Layer.Road;
                }
            }

            m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements | RaycastFlags.NoMainElements;
            if (m_BetterBulldozerUISystem.UpgradeIsMain)
            {
                m_ToolRaycastSystem.raycastFlags |= RaycastFlags.UpgradeIsMain;
            }
        }

        /// <summary>
        /// For stopping the tool. Probably with esc key.
        /// </summary>
        public void RequestDisable()
        {
            m_ToolSystem.activeTool = m_DefaultToolSystem;
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            Enabled = false;
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_ApplyAction = InputManager.instance.FindAction("Tool", "Apply");
            m_RenderingSystem = World.GetOrCreateSystemManaged<RenderingSystem>();
            m_Log.Info($"[{nameof(SubElementBulldozerTool)}] {nameof(OnCreate)}");
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_BulldozeToolSystem = World.GetOrCreateSystemManaged<BulldozeToolSystem>();
            m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            m_BetterBulldozerUISystem = World.GetOrCreateSystemManaged<BetterBulldozerUISystem>();
            m_WarningTooltipSystem = World.GetOrCreateSystemManaged<SubelementBulldozerWarningTooltipSystem>();
            base.OnCreate();
            m_OwnedQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Owner>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Overridden>(),
                    },
                },
            });
            m_HighlightedQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Highlighted>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Overridden>(),
                    },
                },
            });
            RequireForUpdate(m_OwnedQuery);
        }

        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            m_ApplyAction.shouldBeEnabled = true;
            m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnStartRunning)}");
        }

        /// <inheritdoc/>
        protected override void OnStopRunning()
        {
            m_ApplyAction.shouldBeEnabled = false;
            if (m_SingleHighlightedEntity != Entity.Null && m_HighlighedSubobjectsEntity == Entity.Null)
            {
                m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} removing single highlight.");
                EntityManager.RemoveComponent<Highlighted>(m_SingleHighlightedEntity);
                EntityManager.AddComponent<BatchesUpdated>(m_SingleHighlightedEntity);
                m_SingleHighlightedEntity = Entity.Null;
            }

            if (!m_HighlightedQuery.IsEmptyIgnoreFilter)
            {
                m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} removing multiple highlights.");
                EntityManager.AddComponent<BatchesUpdated>(m_HighlightedQuery);
                EntityManager.RemoveComponent<Highlighted>(m_HighlightedQuery);
                m_HighlighedSubobjectsEntity = Entity.Null;
                m_SingleHighlightedEntity = Entity.Null;
            }

            m_WarningTooltipSystem.ClearTooltips();
            base.OnStopRunning();
        }

        /// <inheritdoc/>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = Dependency;
            bool raycastFlag = GetRaycastResult(out Entity currentEntity, out RaycastHit hit);
            bool hasOwnerComponentFlag = EntityManager.HasComponent<Owner>(currentEntity);
            bool hasExtensionComponentFlag = EntityManager.HasComponent<Extension>(currentEntity);
            bool hasNodeComponentFlag = EntityManager.HasComponent<Game.Net.Node>(currentEntity);
            EntityCommandBuffer buffer = m_ToolOutputBarrier.CreateCommandBuffer();
            bool hasSubObjectsFlag = EntityManager.TryGetBuffer(currentEntity, false, out DynamicBuffer<Game.Objects.SubObject> dynamicBuffer);

            // This section handles highlight removal for single highlighted entity.
            if (m_SingleHighlightedEntity != Entity.Null && m_SingleHighlightedEntity != currentEntity && m_HighlighedSubobjectsEntity == Entity.Null)
            {
                m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} removing single highlight.");
                buffer.RemoveComponent<Highlighted>(m_SingleHighlightedEntity);
                buffer.AddComponent<BatchesUpdated>(m_SingleHighlightedEntity);
                m_SingleHighlightedEntity = Entity.Null;
            }

            // This section handles highlight removal for multiple highlighted entities.
            else if (((raycastFlag == false || !hasOwnerComponentFlag || hasNodeComponentFlag) && !m_HighlightedQuery.IsEmptyIgnoreFilter) || (m_HighlighedSubobjectsEntity != Entity.Null && m_HighlighedSubobjectsEntity != currentEntity))
            {
                m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} removing multiple highlights.");
                EntityManager.AddComponent<BatchesUpdated>(m_HighlightedQuery);
                EntityManager.RemoveComponent<Highlighted>(m_HighlightedQuery);
                m_HighlighedSubobjectsEntity = Entity.Null;
                m_SingleHighlightedEntity = Entity.Null;
            }


            if (!hasExtensionComponentFlag || BetterBulldozerMod.Instance.Settings.AllowRemovingExtensions)
            {
                // This section handles highlighting single elements.
                if (raycastFlag && hasOwnerComponentFlag && !hasSubObjectsFlag && !hasNodeComponentFlag) // Single subelement highlight
                {
                    m_WarningTooltipSystem.RegisterTooltip("BulldozeSubelement", Game.UI.Tooltip.TooltipColor.Info, LocaleEN.WarningTooltipKey("BulldozeSubelement"), "Bulldoze Subelement");
                    if (m_SingleHighlightedEntity == currentEntity && !EntityManager.HasComponent<Highlighted>(currentEntity))
                    {
                        buffer.AddComponent<Highlighted>(currentEntity);
                        buffer.AddComponent<BatchesUpdated>(currentEntity);
                        m_SingleHighlightedEntity = currentEntity;
                        m_HighlighedSubobjectsEntity = Entity.Null;
                        m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} added single highlights.");
                    }
                    else if (m_SingleHighlightedEntity != currentEntity)
                    {
                        buffer.AddComponent<Highlighted>(currentEntity);
                        buffer.RemoveComponent<Highlighted>(m_SingleHighlightedEntity);
                        buffer.AddComponent<BatchesUpdated>(m_SingleHighlightedEntity);
                        buffer.AddComponent<BatchesUpdated>(currentEntity);
                        m_SingleHighlightedEntity = currentEntity;
                        m_HighlighedSubobjectsEntity = Entity.Null;
                        m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} added single highlights and removed old highlight.");
                    }
                }

                // This section handles highlighting subelements with subobjects.
                else if (raycastFlag && hasOwnerComponentFlag && hasSubObjectsFlag && !hasNodeComponentFlag)
                {
                    foreach (Game.Objects.SubObject subObject in dynamicBuffer)
                    {
                        buffer.AddComponent<Highlighted>(subObject.m_SubObject);
                        buffer.AddComponent<BatchesUpdated>(subObject.m_SubObject);
                    }

                    m_WarningTooltipSystem.RegisterTooltip("BulldozeSubelement", Game.UI.Tooltip.TooltipColor.Info, LocaleEN.WarningTooltipKey("BulldozeSubelement"), "Bulldoze Subelement");
                    m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} added muiltiple highlights.");
                    m_SingleHighlightedEntity = Entity.Null;
                    m_HighlighedSubobjectsEntity = currentEntity;
                    buffer.AddComponent<Highlighted>(currentEntity);
                    buffer.AddComponent<BatchesUpdated>(currentEntity);
                }

                // This section removes tooltips.
                else
                {
                    m_WarningTooltipSystem.RemoveTooltip("BulldozeSubelement");
                }

                m_WarningTooltipSystem.RemoveTooltip("ExtensionRemovalProhibited");
            }
            else
            {
                m_WarningTooltipSystem.RemoveTooltip("BulldozeSubelement");
                m_WarningTooltipSystem.RegisterTooltip("ExtensionRemovalProhibited", Game.UI.Tooltip.TooltipColor.Error, LocaleEN.WarningTooltipKey("ExtensionRemovalProhibited"), "Removing extensions has been disabled in the settings.");
            }

            if (raycastFlag && !hasNodeComponentFlag && hasExtensionComponentFlag && BetterBulldozerMod.Instance.Settings.AllowRemovingExtensions)
            {
                m_WarningTooltipSystem.RegisterTooltip("ExtensionRemovalWarning", Game.UI.Tooltip.TooltipColor.Warning, LocaleEN.WarningTooltipKey("ExtensionRemovalWarning"), "Removing some extensions will break assets.");
            }
            else
            {
                m_WarningTooltipSystem.RemoveTooltip("ExtensionRemovalWarning");
            }

            if (raycastFlag && !hasNodeComponentFlag && EntityManager.HasComponent<Edge>(currentEntity) && BetterBulldozerMod.Instance.Settings.AllowRemovingSubElementNetworks)
            {
                m_WarningTooltipSystem.RegisterTooltip("SubelementNetworkRemovalWarning", Game.UI.Tooltip.TooltipColor.Warning, LocaleEN.WarningTooltipKey("SubelementNetworkRemovalWarning"), "Removing subelement networks may break assets.");
            }
            else
            {
                m_WarningTooltipSystem.RemoveTooltip("SubelementNetworkRemovalWarning");
            }

            if (m_BetterBulldozerUISystem.SelectedRaycastTarget == BetterBulldozerUISystem.RaycastTarget.Markers && (m_BetterBulldozerUISystem.MarkersFilter & TypeMask.Net) == TypeMask.Net && !BetterBulldozerMod.Instance.Settings.AllowRemovingSubElementNetworks)
            {
                m_WarningTooltipSystem.RegisterTooltip("RemovingMarkerNetworksProhibited", Game.UI.Tooltip.TooltipColor.Error, LocaleEN.WarningTooltipKey("RemovingMarkerNetworksProhibited"), "Removing subelement networks has been disabled in the settings.");
            }
            else
            {
                m_WarningTooltipSystem.RemoveTooltip("RemovingMarkerNetworksProhibited");
            }

            if (m_ApplyAction.WasPressedThisFrame())
            {
                // This handles deleteting editor contrainter for net lane prefabs placed with EDT.
                if (EntityManager.TryGetComponent(currentEntity, out Owner owner)
                    && EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<Game.Net.SubLane> ownerBuffer)
                    && ownerBuffer.Length == 1
                    && EntityManager.HasComponent<Game.Tools.EditorContainer>(owner.m_Owner))
                {
                    currentEntity = owner.m_Owner;
                    m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} Setting current entity to owner");
                }


                if (EntityManager.TryGetComponent(currentEntity, out Edge segmentEdge))
                {
                    if (EntityManager.TryGetBuffer(segmentEdge.m_Start, false, out DynamicBuffer<ConnectedEdge> startConnectedEdges))
                    {
                        if (startConnectedEdges.Length == 1 && startConnectedEdges[0].m_Edge == currentEntity)
                        {
                            buffer.AddComponent<Deleted>(segmentEdge.m_Start);
                        }
                        else
                        {
                            foreach (ConnectedEdge edge in startConnectedEdges)
                            {
                                if (edge.m_Edge != currentEntity)
                                {
                                    EntityManager.AddComponent<Updated>(edge.m_Edge);
                                    if (EntityManager.TryGetComponent(edge.m_Edge, out Edge distantEdge))
                                    {
                                        EntityManager.AddComponent<Updated>(distantEdge.m_Start);
                                        EntityManager.AddComponent<Updated>(distantEdge.m_End);
                                    }
                                }
                            }

                            EntityManager.AddComponent<Updated>(segmentEdge.m_Start);
                        }
                    }

                    if (EntityManager.TryGetBuffer(segmentEdge.m_End, false, out DynamicBuffer<ConnectedEdge> endConnectedEdges))
                    {
                        if (endConnectedEdges.Length == 1 && endConnectedEdges[0].m_Edge == currentEntity)
                        {
                            buffer.AddComponent<Deleted>(segmentEdge.m_End);
                        }
                        else
                        {
                            foreach (ConnectedEdge edge in endConnectedEdges)
                            {
                                if (edge.m_Edge != currentEntity)
                                {
                                    EntityManager.AddComponent<Updated>(edge.m_Edge);
                                    if (EntityManager.TryGetComponent(edge.m_Edge, out Edge distantEdge))
                                    {
                                        EntityManager.AddComponent<Updated>(distantEdge.m_Start);
                                        EntityManager.AddComponent<Updated>(distantEdge.m_End);
                                    }
                                }
                            }

                            EntityManager.AddComponent<Updated>(segmentEdge.m_End);
                        }
                    }
                }

                if (hasSubObjectsFlag && (!hasExtensionComponentFlag || BetterBulldozerMod.Instance.Settings.AllowRemovingExtensions))
                {
                    foreach (Game.Objects.SubObject subObject in dynamicBuffer)
                    {
                        buffer.AddComponent<Deleted>(subObject.m_SubObject);
                    }
                }

                if ((raycastFlag && hasOwnerComponentFlag && !hasExtensionComponentFlag && !hasNodeComponentFlag) || (raycastFlag && hasOwnerComponentFlag && BetterBulldozerMod.Instance.Settings.AllowRemovingExtensions && !hasNodeComponentFlag))
                {
                    buffer.AddComponent<Deleted>(currentEntity);
                }
            }

            return inputDeps;
        }
    }
}
