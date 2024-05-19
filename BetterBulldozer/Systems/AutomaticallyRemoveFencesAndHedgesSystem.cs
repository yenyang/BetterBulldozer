// <copyright file="AutomaticallyRemoveFencesAndHedgesSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

// #define BURST
namespace Better_Bulldozer.Systems
{
    using Better_Bulldozer.Components;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
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
                    m_Log.Debug($"{nameof(AutomaticallyRemoveFencesAndHedges)}.{nameof(OnUpdate)} Fence Prefab {prefabBase.name}.");
                }
            }

            foreach (Entity hedgeEntity in hedgePrefabEntities)
            {
                if (m_PrefabSystem.TryGetPrefab(hedgeEntity, out PrefabBase prefabBase))
                {
                    m_Log.Debug($"{nameof(AutomaticallyRemoveFencesAndHedges)}.{nameof(OnUpdate)} Hedge Prefab {prefabBase.name}.");
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
            }
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            m_UpdatedWithSubLanesQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Net.SubLane, Updated>()
                .WithNone<Temp, Deleted, DeleteInXFrames>()
                .Build();

            RequireForUpdate(m_UpdatedWithSubLanesQuery);

            NativeArray<Entity> entities = m_UpdatedWithSubLanesQuery.ToEntityArray(Allocator.Temp);
            NativeList<Entity> fencePrefabEntities = m_FencePrefabEntities.ToEntityListAsync(Allocator.Temp, out JobHandle fencePrefabJobHandle);
            NativeList<Entity> hedgePrefabEntities = m_HedgePrefabEntities.ToEntityListAsync(Allocator.Temp, out JobHandle hedgePrefabJobHandle);

            NativeList<Entity> fenceAndHedgeSublanes = new NativeList<Entity>(Allocator.TempJob);

            GatherSubLanesJob gatherSubLanesJob = new GatherSubLanesJob()
            {
                m_FencePrefabs = fencePrefabEntities,
                m_HedgePrefabs = hedgePrefabEntities,
                m_OwnerType = SystemAPI.GetComponentTypeHandle<Owner>(),
                m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(),
                m_SubLanes = fenceAndHedgeSublanes,
            };

            JobHandle gatherSubLanesJobHandle = gatherSubLanesJob.Schedule(m_UpdatedWithSubLanesQuery, JobHandle.CombineDependencies(Dependency, fencePrefabJobHandle, hedgePrefabJobHandle));

            fencePrefabEntities.Dispose(gatherSubLanesJobHandle);
            hedgePrefabEntities.Dispose(gatherSubLanesJobHandle);
            entities.Dispose(gatherSubLanesJobHandle);

            HandleDeleteInXFramesJob handleDeleteInXFramesJob = new HandleDeleteInXFramesJob()
            {
                m_DeleteInXFramesLookup = SystemAPI.GetComponentLookup<DeleteInXFrames>(),
                m_SubLanes = fenceAndHedgeSublanes,
                buffer = m_Barrier.CreateCommandBuffer(),
            };

            JobHandle handleDeleteInXFramesJobHandle = handleDeleteInXFramesJob.Schedule(fenceAndHedgeSublanes.Length, gatherSubLanesJobHandle);
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
            public ComponentTypeHandle<Owner> m_OwnerType;
            [ReadOnly]
            public NativeList<Entity> m_FencePrefabs;
            [ReadOnly]
            public NativeList<Entity> m_HedgePrefabs;
            public NativeList<Entity> m_SubLanes;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Owner> ownerNativeArray = chunk.GetNativeArray(ref m_OwnerType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Owner owner = ownerNativeArray[i];
                    if (m_PrefabRefLookup.HasComponent(owner.m_Owner) && m_PrefabRefLookup.TryGetComponent(owner.m_Owner, out PrefabRef prefabRef) && (m_FencePrefabs.Contains(prefabRef.m_Prefab) || m_HedgePrefabs.Contains(prefabRef.m_Prefab)))
                    {
                        m_SubLanes.Add(owner.m_Owner);
                    }
                }
            }
        }

#if BURST
        [BurstCompile]
#endif
        private struct HandleDeleteInXFramesJob : IJobFor
        {
            [ReadOnly]
            public NativeList<Entity> m_SubLanes;
            [ReadOnly]
            public ComponentLookup<DeleteInXFrames> m_DeleteInXFramesLookup;
            public EntityCommandBuffer buffer;


            public void Execute(int index)
            {
                if (!m_DeleteInXFramesLookup.HasComponent(m_SubLanes.ElementAt(index)))
                {
                    buffer.AddComponent<DeleteInXFrames>(m_SubLanes.ElementAt(index));
                }

                buffer.SetComponent(m_SubLanes.ElementAt(index), new DeleteInXFrames() { m_FramesRemaining = 5 });
            }
        }
    }
}
