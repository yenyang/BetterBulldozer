// <copyright file="AutomaticallyRemoveFencesAndHedgesSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Better_Bulldozer.Systems
{
    using Better_Bulldozer.Components;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Net;
    using Game.Prefabs;
    using Game.Tools;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// A system that automatically removes fences and hedges from created buildings.
    /// </summary>
    public partial class AutomaticallyRemoveFencesAndHedges : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_UpdatedWithSubLanesQuery;
        private EntityQuery m_FencePrefabEntities;
        private EntityQuery m_HedgePrefabEntities;
        private PrefabSystem m_PrefabSystem;
        private ModificationEndBarrier m_Barrier;
        private ToolSystem m_ToolSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomaticallyRemoveFencesAndHedges"/> class.
        /// </summary>
        public AutomaticallyRemoveFencesAndHedges()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_Log.Info($"{nameof(AutomaticallyRemoveFencesAndHedges)}.{nameof(OnCreate)}.");
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_FencePrefabEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<NetLaneData>(),
                        ComponentType.ReadOnly<UtilityLaneData>(),
                        ComponentType.ReadOnly<SpawnableObjectData>(),
                        ComponentType.ReadOnly<NetLaneGeometryData>(),
                        ComponentType.ReadOnly<NetLaneArchetypeData>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<PlantData>(),
                    },
                },
            });
            m_HedgePrefabEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<NetLaneData>(),
                        ComponentType.ReadOnly<UtilityLaneData>(),
                        ComponentType.ReadOnly<SpawnableObjectData>(),
                        ComponentType.ReadOnly<NetLaneGeometryData>(),
                        ComponentType.ReadOnly<NetLaneArchetypeData>(),
                        ComponentType.ReadOnly<PlantData>(),
                    },
                },
            });
            base.OnCreate();
            Enabled = false;
            m_UpdatedWithSubLanesQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Net.SubLane, Updated>()
                .WithNone<Temp, Deleted, DeleteInXFrames>()
                .Build();

            RequireForUpdate(m_UpdatedWithSubLanesQuery);
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
#if DEBUG
            NativeList<Entity> fencePrefabEntities = m_FencePrefabEntities.ToEntityListAsync(Allocator.Temp, out JobHandle fencePrefabJobHandle);
            NativeList<Entity> hedgePrefabEntities = m_HedgePrefabEntities.ToEntityListAsync(Allocator.Temp, out JobHandle hedgePrefabJobHandle);
            fencePrefabJobHandle.Complete();
            hedgePrefabJobHandle.Complete();

            foreach (Entity fenceEntity in fencePrefabEntities)
            {
                if (m_PrefabSystem.TryGetPrefab(fenceEntity, out PrefabBase prefabBase))
                {
                    m_Log.Debug($"{nameof(AutomaticallyRemoveFencesAndHedges)}.{nameof(OnGameLoadingComplete)} Fence Prefab {prefabBase.name}.");
                }
            }

            foreach (Entity hedgeEntity in hedgePrefabEntities)
            {
                if (m_PrefabSystem.TryGetPrefab(hedgeEntity, out PrefabBase prefabBase))
                {
                    m_Log.Debug($"{nameof(AutomaticallyRemoveFencesAndHedges)}.{nameof(OnGameLoadingComplete)} Hedge Prefab {prefabBase.name}.");
                }
            }

            fencePrefabEntities.Dispose();
            hedgePrefabEntities.Dispose();
#endif
            if (mode.IsGame())
            {
                Enabled = BetterBulldozerMod.Instance.Settings.AutomaticRemovalFencesAndHedges;
            }
            else
            {
                Enabled = false;
                return;
            }

            if (!BetterBulldozerMod.Instance.Settings.AutomaticRemovalFencesAndHedges)
            {
                return;
            }

            EntityQuery subLanesQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Net.SubLane>()
                .WithNone<Temp, Deleted, DeleteInXFrames>()
                .Build();

            NativeList<Entity> fencePrefabEntities = m_FencePrefabEntities.ToEntityListAsync(Allocator.TempJob, out JobHandle fencePrefabJobHandle);
            NativeList<Entity> hedgePrefabEntities = m_HedgePrefabEntities.ToEntityListAsync(Allocator.TempJob, out JobHandle hedgePrefabJobHandle);

            NativeList<Entity> fenceAndHedgeSublanes = new NativeList<Entity>(Allocator.TempJob);

            GatherSubLanesJob gatherSubLanesJob = new GatherSubLanesJob()
            {
                m_FencePrefabs = fencePrefabEntities,
                m_HedgePrefabs = hedgePrefabEntities,
                m_SubLaneType = SystemAPI.GetBufferTypeHandle<Game.Net.SubLane>(isReadOnly: true),
                m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                m_SubLanes = fenceAndHedgeSublanes,
                m_EditorContainerDataLookup = SystemAPI.GetComponentLookup<EditorContainerData>(isReadOnly: true),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
            };

            JobHandle gatherSubLanesJobHandle = gatherSubLanesJob.Schedule(subLanesQuery, JobHandle.CombineDependencies(Dependency, fencePrefabJobHandle, hedgePrefabJobHandle));

            fencePrefabEntities.Dispose(gatherSubLanesJobHandle);
            hedgePrefabEntities.Dispose(gatherSubLanesJobHandle);

            HandleDeleteInXFramesJob handleDeleteInXFramesJob = new HandleDeleteInXFramesJob()
            {
                m_DeleteInXFramesLookup = SystemAPI.GetComponentLookup<DeleteInXFrames>(isReadOnly: true),
                m_SubLanes = fenceAndHedgeSublanes,
                buffer = m_Barrier.CreateCommandBuffer(),
            };

            JobHandle handleDeleteInXFramesJobHandle = handleDeleteInXFramesJob.Schedule(gatherSubLanesJobHandle);
            m_Barrier.AddJobHandleForProducer(handleDeleteInXFramesJobHandle);
            Dependency = handleDeleteInXFramesJobHandle;
            fenceAndHedgeSublanes.Dispose(handleDeleteInXFramesJobHandle);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            NativeList<Entity> fencePrefabEntities = m_FencePrefabEntities.ToEntityListAsync(Allocator.TempJob, out JobHandle fencePrefabJobHandle);
            NativeList<Entity> hedgePrefabEntities = m_HedgePrefabEntities.ToEntityListAsync(Allocator.TempJob, out JobHandle hedgePrefabJobHandle);

            NativeList<Entity> fenceAndHedgeSublanes = new NativeList<Entity>(Allocator.TempJob);

            GatherSubLanesJob gatherSubLanesJob = new GatherSubLanesJob()
            {
                m_FencePrefabs = fencePrefabEntities,
                m_HedgePrefabs = hedgePrefabEntities,
                m_SubLaneType = SystemAPI.GetBufferTypeHandle<Game.Net.SubLane>(isReadOnly: true),
                m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                m_SubLanes = fenceAndHedgeSublanes,
                m_EditorContainerDataLookup = SystemAPI.GetComponentLookup<EditorContainerData>(isReadOnly: true),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
            };

            JobHandle gatherSubLanesJobHandle = gatherSubLanesJob.Schedule(m_UpdatedWithSubLanesQuery, JobHandle.CombineDependencies(Dependency, fencePrefabJobHandle, hedgePrefabJobHandle));

            fencePrefabEntities.Dispose(gatherSubLanesJobHandle);
            hedgePrefabEntities.Dispose(gatherSubLanesJobHandle);

            HandleDeleteInXFramesJob handleDeleteInXFramesJob = new HandleDeleteInXFramesJob()
            {
                m_DeleteInXFramesLookup = SystemAPI.GetComponentLookup<DeleteInXFrames>(isReadOnly: true),
                m_SubLanes = fenceAndHedgeSublanes,
                buffer = m_Barrier.CreateCommandBuffer(),
            };

            JobHandle handleDeleteInXFramesJobHandle = handleDeleteInXFramesJob.Schedule(gatherSubLanesJobHandle);
            m_Barrier.AddJobHandleForProducer(handleDeleteInXFramesJobHandle);
            Dependency = handleDeleteInXFramesJobHandle;
            fenceAndHedgeSublanes.Dispose(handleDeleteInXFramesJobHandle);
        }

#if BURST
        [BurstCompile]
#endif
        private struct GatherSubLanesJob : IJobChunk
        {
            [ReadOnly]
            public BufferTypeHandle<Game.Net.SubLane> m_SubLaneType;
            [ReadOnly]
            public NativeList<Entity> m_FencePrefabs;
            [ReadOnly]
            public NativeList<Entity> m_HedgePrefabs;
            public NativeList<Entity> m_SubLanes;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            [ReadOnly]
            public ComponentLookup<EditorContainerData> m_EditorContainerDataLookup;
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                BufferAccessor<Game.Net.SubLane> subLaneBufferAccessor = chunk.GetBufferAccessor(ref m_SubLaneType);
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity currentEntity = entityNativeArray[i];
                    DynamicBuffer<Game.Net.SubLane> dynamicBuffer = subLaneBufferAccessor[i];
                    if (m_PrefabRefLookup.HasComponent(currentEntity) && m_PrefabRefLookup.TryGetComponent(currentEntity, out PrefabRef ownerPrefabRef) && m_EditorContainerDataLookup.HasComponent(ownerPrefabRef.m_Prefab))
                    {
                        continue;
                    }

                    foreach (Game.Net.SubLane subLane in dynamicBuffer)
                    {
                        if (m_PrefabRefLookup.HasComponent(subLane.m_SubLane) && m_PrefabRefLookup.TryGetComponent(subLane.m_SubLane, out PrefabRef prefabRef) && (m_FencePrefabs.Contains(prefabRef.m_Prefab) || m_HedgePrefabs.Contains(prefabRef.m_Prefab)))
                        {
                            m_SubLanes.Add(subLane.m_SubLane);
                        }
                    }
                }
            }
        }

#if BURST
        [BurstCompile]
#endif
        private struct HandleDeleteInXFramesJob : IJob
        {
            [ReadOnly]
            public NativeList<Entity> m_SubLanes;
            [ReadOnly]
            public ComponentLookup<DeleteInXFrames> m_DeleteInXFramesLookup;
            public EntityCommandBuffer buffer;

            public void Execute()
            {
                foreach (Entity entity in m_SubLanes)
                {
                    if (!m_DeleteInXFramesLookup.HasComponent(entity))
                    {
                        buffer.AddComponent<DeleteInXFrames>(entity);
                    }

                    buffer.SetComponent(entity, new DeleteInXFrames() { m_FramesRemaining = 5 });
                }
            }
        }
    }
}
