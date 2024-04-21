// <copyright file="SubElementBulldozerTool.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Tools
{
    using System;
    using System.Reflection;
    using Better_Bulldozer.Components;
    using Better_Bulldozer.Settings;
    using Better_Bulldozer.Systems;
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Buildings;
    using Game.Common;
    using Game.Input;
    using Game.Net;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using static Game.Prefabs.TriggerPrefabData;

    /// <summary>
    /// Tool for removing subelements. For debuggin use --burst-disable-compilation launch parameter.
    /// </summary>
    public partial class SubElementBulldozerTool : ToolBaseSystem
    {
        private ProxyAction m_ApplyAction;
        private OverlayRenderSystem m_OverlayRenderSystem;
        private BetterBulldozerUISystem m_BetterBulldozerUISystem;
        private BulldozeToolSystem m_BulldozeToolSystem;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private EntityQuery m_OwnedQuery;
        private float m_Radius = 100f;
        private ILog m_Log;
        private Entity m_PreviousRaycastedEntity;
        private RenderingSystem m_RenderingSystem;
        private EntityQuery m_HighlightedQuery;
        private SubelementBulldozerWarningTooltipSystem m_WarningTooltipSystem;
        private NativeList<Entity> m_MainEntities;
        private NativeList<Entity> m_PrefabEntities;
        private ComponentType m_LevelLockedComponentType;
        private bool m_FoundPlopTheGrowables;

        /// <inheritdoc/>
        public override string toolID => m_BulldozeToolSystem.toolID; // This is hack to get the UI use bulldoze cursor and bulldoze bar.

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
            m_MainEntities = new NativeList<Entity>(Allocator.Persistent);
            m_PrefabEntities = new NativeList<Entity>(Allocator.Persistent);
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
            EntityManager.AddComponent<BatchesUpdated>(m_HighlightedQuery);
            EntityManager.RemoveComponent<Highlighted>(m_HighlightedQuery);
            m_PreviousRaycastedEntity = Entity.Null;
            m_WarningTooltipSystem.ClearTooltips();
            base.OnStopRunning();
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_MainEntities.Dispose();
            m_PrefabEntities.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                Type type = assembly.GetType("PlopTheGrowables.LevelLocked");
                if (type != null)
                {
                    m_Log.Info($"Found {type.FullName} in {type.Assembly.FullName}. ");
                    m_LevelLockedComponentType = ComponentType.ReadOnly(type);
                    m_FoundPlopTheGrowables = true;
                }
            }

        }

        /// <inheritdoc/>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = Dependency;
            bool raycastFlag = GetRaycastResult(out Entity currentRaycastEntity, out RaycastHit hit);
            bool hasOwnerComponentFlag = EntityManager.TryGetComponent(currentRaycastEntity, out Owner owner);
            bool hasExtensionComponentFlag = EntityManager.HasComponent<Extension>(currentRaycastEntity);
            bool hasNodeComponentFlag = EntityManager.HasComponent<Game.Net.Node>(currentRaycastEntity);
            EntityCommandBuffer buffer = m_ToolOutputBarrier.CreateCommandBuffer();


            // This section handles highlight removal.
            if (m_PreviousRaycastedEntity != currentRaycastEntity || !raycastFlag || currentRaycastEntity == Entity.Null)
            {
                EntityManager.AddComponent<BatchesUpdated>(m_HighlightedQuery);
                EntityManager.RemoveComponent<Highlighted>(m_HighlightedQuery);
                m_PreviousRaycastedEntity = currentRaycastEntity;
                m_MainEntities.Clear();
                m_PrefabEntities.Clear();
            }

            if (!hasExtensionComponentFlag || BetterBulldozerMod.Instance.Settings.AllowRemovingExtensions)
            {
                if (m_HighlightedQuery.IsEmptyIgnoreFilter && raycastFlag && hasOwnerComponentFlag && !hasNodeComponentFlag)
                {
                    m_WarningTooltipSystem.RegisterTooltip("BulldozeSubelement", Game.UI.Tooltip.TooltipColor.Info, LocaleEN.WarningTooltipKey("BulldozeSubelement"), "Bulldoze Subelement");
                    buffer.AddComponent<Highlighted>(currentRaycastEntity);
                    buffer.AddComponent<BatchesUpdated>(currentRaycastEntity);
                    m_MainEntities.Add(currentRaycastEntity);
                    m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} Added to main entities {currentRaycastEntity.Index} {currentRaycastEntity.Version}");
                    if (EntityManager.TryGetComponent(currentRaycastEntity, out PrefabRef prefabRef) && m_PrefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefabBase))
                    {
                        if (m_BetterBulldozerUISystem.ActiveSelectionMode == BetterBulldozerUISystem.SelectionMode.Matching)
                        {
                            m_PrefabEntities.Add(prefabRef.m_Prefab);
                            if (prefabBase is StaticObjectPrefab && EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> ownerSubobjects))
                            {
                                foreach (Game.Objects.SubObject subObject in ownerSubobjects)
                                {
                                    if (EntityManager.TryGetComponent(subObject.m_SubObject, out PrefabRef subObjectPrefabRef) && subObjectPrefabRef.m_Prefab == prefabRef.m_Prefab)
                                    {
                                        buffer.AddComponent<Highlighted>(subObject.m_SubObject);
                                        buffer.AddComponent<BatchesUpdated>(subObject.m_SubObject);
                                        m_MainEntities.Add(subObject.m_SubObject);

                                        m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} Added to main entities {subObject.m_SubObject.Index} {subObject.m_SubObject.Version}");
                                    }
                                }
                            }
                            else if ((prefabBase is NetLanePrefab || prefabBase is NetLaneGeometryPrefab) && EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<Game.Net.SubLane> ownerSublanes))
                            {
                                foreach (Game.Net.SubLane subLane in ownerSublanes)
                                {
                                    if (EntityManager.TryGetComponent(subLane.m_SubLane, out PrefabRef subLanePrefabRef) && subLanePrefabRef.m_Prefab == prefabRef.m_Prefab)
                                    {
                                        buffer.AddComponent<Highlighted>(subLane.m_SubLane);
                                        buffer.AddComponent<BatchesUpdated>(subLane.m_SubLane);
                                        m_MainEntities.Add(subLane.m_SubLane);

                                        m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} Added to main entities {subLane.m_SubLane.Index} {subLane.m_SubLane.Version}");
                                    }
                                }
                            }

                            if (prefabBase is NetPrefab || prefabBase is RoadPrefab)
                            {
                                m_WarningTooltipSystem.RegisterTooltip("NetworksUseSingleItem", Game.UI.Tooltip.TooltipColor.Warning, LocaleEN.WarningTooltipKey("NetworksUseSingleItem"), "Removing multiple subelement networks is not supported. Use Single Item selection instead.");
                            }
                            else
                            {
                                m_WarningTooltipSystem.RemoveTooltip("NetworksUseSingleItem");
                            }
                        }
                        else if (m_BetterBulldozerUISystem.ActiveSelectionMode == BetterBulldozerUISystem.SelectionMode.Similar)
                        {
                            m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} similar.");
                            if (prefabBase is StaticObjectPrefab && EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> ownerSubobjects))
                            {
                                if (EntityManager.HasComponent<Tree>(currentRaycastEntity))
                                {
                                    CheckForSimilarSubObjects(ComponentType.ReadOnly<Tree>(), ownerSubobjects, ref buffer);
                                }
                                else if (EntityManager.HasComponent<Plant>(currentRaycastEntity))
                                {
                                    CheckForSimilarSubObjects(ComponentType.ReadOnly<Plant>(), ComponentType.ReadOnly<Tree>(), ownerSubobjects, ref buffer);
                                }
                                else if (EntityManager.HasComponent<Game.Objects.StreetLight>(currentRaycastEntity))
                                {
                                    CheckForSimilarSubObjects(ComponentType.ReadOnly<Game.Objects.StreetLight>(), ownerSubobjects, ref buffer);
                                }
                                else if (EntityManager.HasComponent<Game.Objects.Quantity>(currentRaycastEntity))
                                {
                                    CheckForSimilarSubObjects(ComponentType.ReadOnly<Game.Objects.Quantity>(), ownerSubobjects, ref buffer);
                                }
                                else if (EntityManager.HasComponent<Game.Prefabs.BrandObjectData>(prefabRef.m_Prefab))
                                {
                                    CheckForSimilarSubObjectsPrefabs(ComponentType.ReadOnly<Game.Prefabs.BrandObjectData>(), ownerSubobjects, ref buffer);
                                }
                                else if (EntityManager.HasComponent<Game.Objects.ActivityLocation>(currentRaycastEntity))
                                {
                                    CheckForSimilarSubObjects(ComponentType.ReadOnly<Game.Objects.ActivityLocation>(), ownerSubobjects, ref buffer);
                                }
                                else if (EntityManager.HasComponent<Game.Objects.Elevation>(currentRaycastEntity))
                                {
                                    CheckForSimilarSubObjects(ComponentType.ReadOnly<Game.Objects.Elevation>(), ownerSubobjects, ref buffer);
                                }
                                else
                                {
                                    m_PrefabEntities.Add(prefabRef.m_Prefab);
                                    foreach (Game.Objects.SubObject subObject in ownerSubobjects)
                                    {
                                        if (EntityManager.TryGetComponent(subObject.m_SubObject, out PrefabRef subObjectPrefabRef) && subObjectPrefabRef.m_Prefab == prefabRef.m_Prefab)
                                        {
                                            buffer.AddComponent<Highlighted>(subObject.m_SubObject);
                                            buffer.AddComponent<BatchesUpdated>(subObject.m_SubObject);
                                            m_MainEntities.Add(subObject.m_SubObject);

                                            m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} Added to main entities {subObject.m_SubObject.Index} {subObject.m_SubObject.Version}");
                                        }
                                    }
                                }
                            }
                            else if ((prefabBase is NetLanePrefab || prefabBase is NetLaneGeometryPrefab) && EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<Game.Net.SubLane> ownerSublanes))
                            {
                                foreach (Game.Net.SubLane subLane in ownerSublanes)
                                {
                                    if (EntityManager.TryGetComponent(subLane.m_SubLane, out PrefabRef fencePrefabEntity) && EntityManager.TryGetComponent(fencePrefabEntity.m_Prefab, out UtilityLaneData utilityLaneData) && (utilityLaneData.m_UtilityTypes & UtilityTypes.Fence) == UtilityTypes.Fence)
                                    {
                                        buffer.AddComponent<Highlighted>(subLane.m_SubLane);
                                        buffer.AddComponent<BatchesUpdated>(subLane.m_SubLane);
                                        m_MainEntities.Add(subLane.m_SubLane);
                                        m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(CheckForSimilarSubObjects)} Added to main entities {subLane.m_SubLane} {subLane.m_SubLane}");
                                        m_PrefabEntities.Add(fencePrefabEntity.m_Prefab);
                                    }
                                }
                            }

                            if (prefabBase is NetPrefab || prefabBase is RoadPrefab)
                            {
                                m_WarningTooltipSystem.RegisterTooltip("NetworksUseSingleItem", Game.UI.Tooltip.TooltipColor.Warning, LocaleEN.WarningTooltipKey("NetworksUseSingleItem"), "Removing multiple subelement networks is not supported. Use Single Item selection instead.");
                            }
                            else
                            {
                                m_WarningTooltipSystem.RemoveTooltip("NetworksUseSingleItem");
                            }
                        }
                    }

                    foreach (Entity entity in m_MainEntities)
                    {
                        if (EntityManager.TryGetBuffer(currentRaycastEntity, false, out DynamicBuffer<Game.Objects.SubObject> dynamicBuffer))
                        {
                            foreach (Game.Objects.SubObject subObject in dynamicBuffer)
                            {
                                buffer.AddComponent<Highlighted>(subObject.m_SubObject);
                                buffer.AddComponent<BatchesUpdated>(subObject.m_SubObject);

                                if (EntityManager.TryGetBuffer(subObject.m_SubObject, false, out DynamicBuffer<Game.Objects.SubObject> deepDynamicBuffer))
                                {
                                    foreach (Game.Objects.SubObject deepSubObject in deepDynamicBuffer)
                                    {
                                        buffer.AddComponent<Highlighted>(subObject.m_SubObject);
                                        buffer.AddComponent<BatchesUpdated>(subObject.m_SubObject);
                                    }
                                }
                            }
                        }
                    }
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

            if (raycastFlag && !hasNodeComponentFlag && EntityManager.HasComponent<Edge>(currentRaycastEntity) && BetterBulldozerMod.Instance.Settings.AllowRemovingSubElementNetworks)
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
            /*
            if (EntityManager.TryGetComponent(owner.m_Owner, out PrefabRef prefabRef1) && m_PrefabSystem.TryGetPrefab(prefabRef1.m_Prefab, out PrefabBase prefabBase1))
            {
                if (prefabBase1 is RoadPrefab)
                {
                    m_WarningTooltipSystem.RegisterTooltip("RemovingSubelementsFromRoads", Game.UI.Tooltip.TooltipColor.Warning, LocaleEN.WarningTooltipKey("RemovingSubelementsFromRoads"), "Removing subobjects from roads is not recommended because roads update frequently and the subobjects will regenerate.");
                }
                else
                {
                    m_WarningTooltipSystem.RemoveTooltip("RemovingSubelementsFromRoads");
                }

                if (EntityManager.TryGetComponent(prefabRef1.m_Prefab, out SpawnableBuildingData spawnableBuildingData) && spawnableBuildingData.m_Level < 5 && m_FoundPlopTheGrowables && !EntityManager.HasComponent(owner.m_Owner, m_LevelLockedComponentType))
                {
                    m_WarningTooltipSystem.RegisterTooltip("RemovingSubelementsFromGrowable", Game.UI.Tooltip.TooltipColor.Warning, LocaleEN.WarningTooltipKey("RemovingSubelementsFromGrowable"), "Removing subelements from growables that can level up is not recommended because when they level up all the subobjects, fences, and hedges will regenerate.");
                }
                else
                {
                    m_WarningTooltipSystem.RemoveTooltip("RemovingSubelementsFromGrowable");
                }

                if (EntityManager.HasComponent<ServiceObjectData>(prefabRef1.m_Prefab))
                {
                    m_WarningTooltipSystem.RegisterTooltip("RemovingSubelementsFromServiceBuildings", Game.UI.Tooltip.TooltipColor.Info, LocaleEN.WarningTooltipKey("RemovingSubelementsFromServiceBuildings"), "Removing subelements from service buildings should be done after all upgrades are purchased since all the subobjects, fences, and hedges will regenerate when an upgrade is applied.");
                }
                else
                {
                    m_WarningTooltipSystem.RemoveTooltip("RemovingSubelementsFromServiceBuildings");
                }
            }*/


            if (m_ApplyAction.WasPressedThisFrame())
            {
                foreach (Entity entity in m_MainEntities)
                {
                    Entity currentEntity = entity;
                    m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} starting {currentEntity.Index} {currentEntity.Version}.");
                    if (EntityManager.TryGetComponent(currentEntity, out Owner currentOwner)
                    && EntityManager.TryGetBuffer(currentOwner.m_Owner, isReadOnly: true, out DynamicBuffer<Game.Net.SubLane> ownerBuffer)
                    && ownerBuffer.Length == 1
                    && EntityManager.HasComponent<Game.Tools.EditorContainer>(currentOwner.m_Owner))
                    {
                        currentEntity = currentOwner.m_Owner;
                        m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} Setting current entity to owner");
                    }

                    m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} starting network stuff.");
                    if (EntityManager.TryGetComponent(currentEntity, out Edge segmentEdge))
                    {
                        m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} 1");
                        if (EntityManager.TryGetBuffer(segmentEdge.m_Start, false, out DynamicBuffer<ConnectedEdge> startConnectedEdges))
                        {
                            m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} 2");
                            if (startConnectedEdges.Length == 1 && startConnectedEdges[0].m_Edge == currentEntity)
                            {
                                buffer.AddComponent<Deleted>(segmentEdge.m_Start);
                                m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} deleted segment edge start");
                            }
                            else
                            {
                                m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} 3");
                                foreach (ConnectedEdge edge in startConnectedEdges)
                                {
                                    if (edge.m_Edge != currentEntity)
                                    {
                                        buffer.AddComponent<Updated>(edge.m_Edge);
                                        if (EntityManager.TryGetComponent(edge.m_Edge, out Edge distantEdge))
                                        {
                                            buffer.AddComponent<Updated>(distantEdge.m_Start);
                                            buffer.AddComponent<Updated>(distantEdge.m_End);
                                        }
                                    }
                                }
                                m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} 6");
                                buffer.AddComponent<Updated>(segmentEdge.m_Start);
                            }
                        }
                        m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} 7");
                        if (EntityManager.TryGetBuffer(segmentEdge.m_End, false, out DynamicBuffer<ConnectedEdge> endConnectedEdges))
                        {
                            m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} 8");
                            if (endConnectedEdges.Length == 1 && endConnectedEdges[0].m_Edge == currentEntity)
                            {
                                buffer.AddComponent<Deleted>(segmentEdge.m_End);
                                m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} deleted segment edge end");
                            }
                            else
                            {
                                foreach (ConnectedEdge edge in endConnectedEdges)
                                {
                                    m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} 9");
                                    if (edge.m_Edge != currentEntity)
                                    {
                                        buffer.AddComponent<Updated>(edge.m_Edge);
                                        if (EntityManager.TryGetComponent(edge.m_Edge, out Edge distantEdge))
                                        {
                                            m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} 10");
                                            buffer.AddComponent<Updated>(distantEdge.m_Start);
                                            buffer.AddComponent<Updated>(distantEdge.m_End);
                                        }
                                    }
                                }

                                m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} 11");
                                buffer.AddComponent<Updated>(segmentEdge.m_End);
                            }
                        }
                    }

                    m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} finished network stuff.");
                    if (EntityManager.TryGetBuffer(currentEntity, false, out DynamicBuffer<Game.Objects.SubObject> dynamicBuffer) && (!EntityManager.HasComponent<Extension>(currentEntity) || BetterBulldozerMod.Instance.Settings.AllowRemovingExtensions))
                    {
                        foreach (Game.Objects.SubObject subObject in dynamicBuffer)
                        {
                            buffer.AddComponent<Deleted>(subObject.m_SubObject);
                        }
                    }

                    if ((!EntityManager.HasComponent<Extension>(currentEntity) && !EntityManager.HasComponent<Game.Net.Node>(currentEntity)) || (BetterBulldozerMod.Instance.Settings.AllowRemovingExtensions && !EntityManager.HasComponent<Game.Net.Node>(currentEntity)))
                    {
                        buffer.AddComponent<Deleted>(currentEntity);
                        m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(OnUpdate)} Deleted {currentEntity.Index} {currentEntity.Version}");
                    }
                }

                if (m_PrefabEntities.Length > 0 && !EntityManager.HasBuffer<PermanentlyRemovedSubElementPrefab>(owner.m_Owner))
                {
                    EntityManager.AddBuffer<PermanentlyRemovedSubElementPrefab>(owner.m_Owner);
                }

                if (m_PrefabEntities.Length > 0 && EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: false, out DynamicBuffer<PermanentlyRemovedSubElementPrefab> removedPrefabBuffer))
                {
                    foreach (Entity entity in m_PrefabEntities)
                    {
                        removedPrefabBuffer.Add(new PermanentlyRemovedSubElementPrefab(entity));
                    }
                }
            }

            return inputDeps;
        }

        private void CheckForSimilarSubObjects(ComponentType necessaryComponent, ComponentType excludeComponent, DynamicBuffer<Game.Objects.SubObject> subObjects, ref EntityCommandBuffer buffer)
        {
            foreach (Game.Objects.SubObject subObject in subObjects)
            {
                if (EntityManager.HasComponent(subObject.m_SubObject, excludeComponent))
                {
                    continue;
                }

                if (EntityManager.HasComponent(subObject.m_SubObject, necessaryComponent))
                {
                    buffer.AddComponent<Highlighted>(subObject.m_SubObject);
                    buffer.AddComponent<BatchesUpdated>(subObject.m_SubObject);
                    m_MainEntities.Add(subObject.m_SubObject);
                    m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(CheckForSimilarSubObjects)} Added to main entities {subObject.m_SubObject} {subObject.m_SubObject}");
                    if (EntityManager.TryGetComponent(subObject.m_SubObject, out PrefabRef prefabRef) && !m_PrefabEntities.Contains(prefabRef.m_Prefab))
                    {
                        m_PrefabEntities.Add(prefabRef.m_Prefab);
                    }
                }
            }
        }

        private void CheckForSimilarSubObjects(ComponentType necessaryComponent, DynamicBuffer<Game.Objects.SubObject> subObjects, ref EntityCommandBuffer buffer)
        {
            foreach (Game.Objects.SubObject subObject in subObjects)
            {
                if (EntityManager.HasComponent(subObject.m_SubObject, necessaryComponent))
                {
                    buffer.AddComponent<Highlighted>(subObject.m_SubObject);
                    buffer.AddComponent<BatchesUpdated>(subObject.m_SubObject);
                    m_MainEntities.Add(subObject.m_SubObject);
                    m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(CheckForSimilarSubObjects)} Added to main entities {subObject.m_SubObject} {subObject.m_SubObject}");
                    if (EntityManager.TryGetComponent(subObject.m_SubObject, out PrefabRef prefabRef) && !m_PrefabEntities.Contains(prefabRef.m_Prefab))
                    {
                        m_PrefabEntities.Add(prefabRef.m_Prefab);
                    }
                }
            }
        }

        private void CheckForSimilarSubObjectsPrefabs(ComponentType necessaryComponent, DynamicBuffer<Game.Objects.SubObject> subObjects, ref EntityCommandBuffer buffer)
        {
            foreach (Game.Objects.SubObject subObject in subObjects)
            {
                if (EntityManager.TryGetComponent(subObject.m_SubObject, out PrefabRef prefabRef) && EntityManager.HasComponent(prefabRef.m_Prefab, necessaryComponent))
                {
                    buffer.AddComponent<Highlighted>(subObject.m_SubObject);
                    buffer.AddComponent<BatchesUpdated>(subObject.m_SubObject);
                    m_MainEntities.Add(subObject.m_SubObject);
                    m_Log.Debug($"{nameof(SubElementBulldozerTool)}.{nameof(CheckForSimilarSubObjects)} Added to main entities {subObject.m_SubObject} {subObject.m_SubObject}");
                    if (!m_PrefabEntities.Contains(prefabRef.m_Prefab))
                    {
                        m_PrefabEntities.Add(prefabRef.m_Prefab);
                    }
                }
            }
        }
    }
}
