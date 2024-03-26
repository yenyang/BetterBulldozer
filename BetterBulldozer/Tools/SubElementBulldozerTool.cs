// <copyright file="SubElementBulldozerTool.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Tools
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Game.Buildings;
    using Game.Common;
    using Game.Input;
    using Game.Net;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Tool for removing subelements.
    /// </summary>
    public partial class SubElementBulldozerTool : ToolBaseSystem
    {
        private ProxyAction m_ApplyAction;
        private OverlayRenderSystem m_OverlayRenderSystem;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private EntityQuery m_OwnedQuery;
        private SEBTSelectionMode m_SelectionMode = SEBTSelectionMode.Single;
        private float m_Radius = 100f;
        private ILog m_Log;
        private Entity m_SingleHighlightedEntity = Entity.Null;
        private bool m_HighlightingSubobjects = false;
        private RenderingSystem m_RenderingSystem;
        private EntityQuery m_HighlightedQuery;

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
        public override string toolID => "SubElement Bulldozer Tool";

        /// <summary>
        /// Gets or sets the TreeAgeChanger ToolMode.
        /// </summary>
        public SEBTSelectionMode SelectionMode { get => m_SelectionMode; set => m_SelectionMode = value; }

        /// <summary>
        /// Gets or sets the TreeAgeChanger Radius.
        /// </summary>
        public float Radius { get => m_Radius; set => m_Radius = value; }

        /// <inheritdoc/>
        public override PrefabBase GetPrefab()
        {
            return null;
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
            m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.Lanes;
            m_ToolRaycastSystem.netLayerMask = Layer.Fence;
            m_ToolRaycastSystem.raycastFlags = RaycastFlags.SubElements | RaycastFlags.Decals | RaycastFlags.NoMainElements;
            m_ToolRaycastSystem.utilityTypeMask = UtilityTypes.Fence;

            if (BetterBulldozerMod.Instance.Settings.AllowRemovingSubElementNetworks)
            {
                m_ToolRaycastSystem.typeMask |= TypeMask.Net;
                m_ToolRaycastSystem.netLayerMask |= Layer.Pathway | Layer.PowerlineHigh | Layer.PowerlineLow | Layer.Taxiway | Layer.PublicTransportRoad | Layer.SubwayTrack | Layer.TrainTrack | Layer.TramTrack | Layer.Waterway | Layer.Road;
            }

            if (m_RenderingSystem.markersVisible && BetterBulldozerMod.Instance.Settings.AllowRemovingSubElementNetworks)
            {
                m_ToolRaycastSystem.netLayerMask |= Layer.MarkerPathway | Layer.MarkerTaxiway;
                m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Markers;
            }

            // m_ToolRaycastSystem.raycastFlags |= RaycastFlags.UpgradeIsMain; This needs a seperate toggle.
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
            m_RenderingSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<RenderingSystem>();
            m_Log.Info($"[{nameof(SubElementBulldozerTool)}] {nameof(OnCreate)}");
            m_ToolOutputBarrier = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_OverlayRenderSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<OverlayRenderSystem>();
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
            if (m_SingleHighlightedEntity != Entity.Null)
            {
                EntityManager.RemoveComponent<Highlighted>(m_SingleHighlightedEntity);
                EntityManager.AddComponent<BatchesUpdated>(m_SingleHighlightedEntity);
                m_SingleHighlightedEntity = Entity.Null;
            }
        }

        /// <inheritdoc/>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = Dependency;
            bool raycastFlag = GetRaycastResult(out Entity currentEntity, out RaycastHit hit);
            bool hasOwnerComponentFlag = EntityManager.HasComponent<Owner>(currentEntity);
            bool hasExtensionComponentFlag = EntityManager.HasComponent<Extension>(currentEntity);
            bool hasServiceUpgradeComponentFlag = EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(currentEntity);
            Owner ownerComponent = EntityManager.GetComponentData<Owner>(currentEntity);
            if (EntityManager.TryGetComponent(ownerComponent.m_Owner, out PrefabRef prefabRef))
            {
                if (!EntityManager.HasComponent<BuildingData>(prefabRef))
                {
                    return inputDeps;
                }
            }

            /* Saved for later
            if (m_Radius > 0 && raycastFlag && m_SelectionMode == SEBTSelectionMode.Radius) // Radius Circle
            {
                ToolRadiusJob toolRadiusJob = new ()
                {
                    m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out JobHandle outJobHandle),
                    m_Position = new Vector3(hit.m_HitPosition.x, hit.m_Position.y, hit.m_HitPosition.z),
                    m_Radius = m_Radius,
                };
                inputDeps = IJobExtensions.Schedule(toolRadiusJob, JobHandle.CombineDependencies(inputDeps, outJobHandle));
                m_OverlayRenderSystem.AddBufferWriter(inputDeps);
            } */

            if (m_ApplyAction.WasPressedThisFrame())
            {
                if (m_SelectionMode == SEBTSelectionMode.Single)
                {
                    if (EntityManager.TryGetComponent(currentEntity, out Edge segmentEdge))
                    {
                        if (EntityManager.TryGetBuffer(segmentEdge.m_Start, true, out DynamicBuffer<ConnectedEdge> startConnectedEdges))
                        {
                            if (startConnectedEdges.Length == 1 && startConnectedEdges[0].m_Edge == currentEntity)
                            {
                                EntityManager.AddComponent<Deleted>(segmentEdge.m_Start);
                            }
                            else
                            {
                                foreach (ConnectedEdge edge in startConnectedEdges)
                                {
                                    if (edge.m_Edge != currentEntity)
                                    {
                                        EntityManager.AddComponent<Updated>(edge.m_Edge);
                                    }
                                }

                                EntityManager.AddComponent<Updated>(segmentEdge.m_Start);
                            }
                        }

                        if (EntityManager.TryGetBuffer(segmentEdge.m_End, true, out DynamicBuffer<ConnectedEdge> endConnectedEdges))
                        {
                            if (endConnectedEdges.Length == 1 && endConnectedEdges[0].m_Edge == currentEntity)
                            {
                                EntityManager.AddComponent<Deleted>(segmentEdge.m_End);
                            }
                            else
                            {
                                foreach (ConnectedEdge edge in endConnectedEdges)
                                {
                                    if (edge.m_Edge != currentEntity)
                                    {
                                        EntityManager.AddComponent<Updated>(edge.m_Edge);
                                    }
                                }

                                EntityManager.AddComponent<Updated>(segmentEdge.m_End);
                            }
                        }
                    }

                    if ((raycastFlag && hasOwnerComponentFlag && !hasExtensionComponentFlag) || (raycastFlag && hasOwnerComponentFlag && BetterBulldozerMod.Instance.Settings.AllowRemovingExtensions))
                    {
                        EntityManager.AddComponent<Deleted>(currentEntity);
                    }
                }
            }
            else if (m_ApplyAction.IsPressed() && m_SelectionMode == SEBTSelectionMode.Radius && raycastFlag)
            {
                /*
                    TreeChangerWithinRadius changeTreeAgeWithinRadiusJob = new()
                    {
                        m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                        m_Position = hit.m_HitPosition,
                        m_Radius = m_Radius,
                        m_Ages = m_SelectedTreeStates,
                        m_TransformType = __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle,
                        m_TreeType = __TypeHandle.__Game_Object_Tree_RW_ComponentTypeHandle,
                        buffer = m_ToolOutputBarrier.CreateCommandBuffer().AsParallelWriter(),
                        m_PrefabRefType = __TypeHandle.__PrefabRef_RW_ComponentTypeHandle,
                        m_OverrideState = m_AtLeastOneAgeSelected,
                        m_OverridePrefab = overridePrefab,
                        m_Random = new((uint)UnityEngine.Random.Range(1, 100000)),
                        m_PrefabEntities = m_SelectedTreePrefabEntities,
                    };
                    inputDeps = JobChunkExtensions.ScheduleParallel(changeTreeAgeWithinRadiusJob, m_TreeQuery, JobHandle.CombineDependencies(inputDeps, treePrefabJobHandle));
                    m_ToolOutputBarrier.AddJobHandleForProducer(inputDeps);
                */
            }

            if (raycastFlag && hasOwnerComponentFlag && !hasExtensionComponentFlag && !hasServiceUpgradeComponentFlag) // Single subelement highlight
            {
                if (m_SingleHighlightedEntity == currentEntity && !EntityManager.HasComponent<Highlighted>(currentEntity))
                {
                    EntityManager.AddComponent<Highlighted>(currentEntity);
                    EntityManager.AddComponent<BatchesUpdated>(currentEntity);
                    m_SingleHighlightedEntity = currentEntity;
                }
                else if (m_SingleHighlightedEntity != currentEntity)
                {
                    EntityManager.AddComponent<Highlighted>(currentEntity);
                    EntityManager.RemoveComponent<Highlighted>(m_SingleHighlightedEntity);
                    EntityManager.AddComponent<BatchesUpdated>(m_SingleHighlightedEntity);
                    EntityManager.AddComponent<BatchesUpdated>(currentEntity);
                    m_SingleHighlightedEntity = currentEntity;
                }
            }
            else if ((raycastFlag && hasOwnerComponentFlag && hasExtensionComponentFlag && BetterBulldozerMod.Instance.Settings.AllowRemovingExtensions) || (raycastFlag && hasOwnerComponentFlag && hasServiceUpgradeComponentFlag && !hasExtensionComponentFlag))
            {
                if (EntityManager.TryGetBuffer(currentEntity, false, out DynamicBuffer<Game.Objects.SubObject> buffer))
                {
                    foreach (Game.Objects.SubObject subObject in buffer)
                    {
                        EntityManager.AddComponent<Highlighted>(subObject.m_SubObject);
                        EntityManager.AddComponent<BatchesUpdated>(subObject.m_SubObject);
                    }

                    m_HighlightingSubobjects = true;
                }
            }
            else if (m_SingleHighlightedEntity != Entity.Null && m_HighlightingSubobjects == false)
            {
                EntityManager.RemoveComponent<Highlighted>(m_SingleHighlightedEntity);
                EntityManager.AddComponent<BatchesUpdated>(m_SingleHighlightedEntity);
                m_SingleHighlightedEntity = Entity.Null;
            }
            else if (m_HighlightingSubobjects)
            {
                EntityManager.RemoveComponent<Highlighted>(m_HighlightedQuery);
                EntityManager.AddComponent<BatchesUpdated>(m_HighlightedQuery);
                m_HighlightingSubobjects = false;
                m_SingleHighlightedEntity = Entity.Null;
            }

            return inputDeps;
        }

        /* Saved for later
        private struct SubElementBulldozerWithinRadius : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;
            public EntityCommandBuffer.ParallelWriter buffer;
            public float m_Radius;
            public float3 m_Position;

            /// <summary>
            /// Executes job which will change state or prefab for trees within a radius.
            /// </summary>
            /// <param name="chunk">ArchteypeChunk of IJobChunk.</param>
            /// <param name="unfilteredChunkIndex">Use for EntityCommandBuffer.ParralelWriter.</param>
            /// <param name="useEnabledMask">Part of IJobChunk. Unsure what it does.</param>
            /// <param name="chunkEnabledMask">Part of IJobChunk. Not sure what it does.</param>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Game.Objects.Transform> transformNativeArray = chunk.GetNativeArray(ref m_TransformType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (RadiusCheck(m_Position, transformNativeArray[i].m_Position, m_Radius))
                    {
                        Entity currentEntity = entityNativeArray[i];
                        buffer.AddComponent<Deleted>(unfilteredChunkIndex, currentEntity);
                    }
                }
            }

            /// <summary>
            /// Checks the radius and position and returns true if tree is there.
            /// </summary>
            /// <param name="cursorPosition">Float3 from Raycast.</param>
            /// <param name="position">Float3 position from Transform.</param>
            /// <param name="radius">Radius usually passed from settings.</param>
            /// <returns>True if tree position is within radius of position. False if not.</returns>
            private bool RadiusCheck(float3 cursorPosition, float3 position, float radius)
            {
                float minRadius = 5f;
                radius = Mathf.Max(radius, minRadius);
                if (Unity.Mathematics.math.distance(cursorPosition, position) < radius)
                {
                    return true;
                }

                return false;
            }
        }

        private struct ToolRadiusJob : IJob
        {
            public OverlayRenderSystem.Buffer m_OverlayBuffer;
            public float3 m_Position;
            public float m_Radius;

            /// <summary>
            /// Draws tool radius.
            /// </summary>
            public void Execute()
            {
                m_OverlayBuffer.DrawCircle(new UnityEngine.Color(.52f, .80f, .86f, 1f), default, m_Radius / 20f, 0, new float2(0, 1), m_Position, m_Radius * 2f);
            }
        }
        */
    }
}
