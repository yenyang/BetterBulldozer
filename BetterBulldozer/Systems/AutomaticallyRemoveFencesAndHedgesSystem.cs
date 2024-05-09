// <copyright file="AutomaticallyRemoveFencesAndHedges.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Systems
{
    using Better_Bulldozer.Components;
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Tools;
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
            Enabled = BetterBulldozerMod.Instance.Settings.AutomaticRemovalFencesAndHedges;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            m_UpdatedWithSubLanesQuery = SystemAPI.QueryBuilder()
                .WithAllRW<Game.Net.SubLane>()
                .WithAll<Updated>()
                .WithNone<Temp, Deleted, DeleteInXFrames>()
                .Build();

            RequireForUpdate(m_UpdatedWithSubLanesQuery);

            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();
            NativeArray<Entity> entities = m_UpdatedWithSubLanesQuery.ToEntityArray(Allocator.Temp);
            NativeList<Entity> fencePrefabEntities = m_FencePrefabEntities.ToEntityListAsync(Allocator.Temp, out JobHandle fencePrefabJobHandle);
            NativeList<Entity> hedgePrefabEntities = m_HedgePrefabEntities.ToEntityListAsync(Allocator.Temp, out JobHandle hedgePrefabJobHandle);
            fencePrefabJobHandle.Complete();
            hedgePrefabJobHandle.Complete();



            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetComponent(entity, out PrefabRef ownerPrefabRef))
                {
                    continue;
                }

                if (EntityManager.HasComponent<EditorContainerData>(ownerPrefabRef.m_Prefab))
                {
                    continue;
                }

                if (!EntityManager.TryGetBuffer(entity, isReadOnly: false, out DynamicBuffer<Game.Net.SubLane> dynamicBuffer))
                {
                    continue;
                }

                foreach (Game.Net.SubLane subLane in dynamicBuffer)
                {
                    if (!EntityManager.TryGetComponent(subLane.m_SubLane, out PrefabRef prefabRef))
                    {
                        continue;
                    }

                    if (fencePrefabEntities.Contains(prefabRef.m_Prefab) || hedgePrefabEntities.Contains(prefabRef.m_Prefab))
                    {
                        if (!EntityManager.HasComponent<DeleteInXFrames>(subLane.m_SubLane))
                        {
                            buffer.AddComponent<DeleteInXFrames>(subLane.m_SubLane);
                        }

                        buffer.SetComponent(subLane.m_SubLane, new DeleteInXFrames() { m_FramesRemaining = 5 });
                    }
                }
            }

            fencePrefabEntities.Dispose();
            hedgePrefabEntities.Dispose();
            entities.Dispose();
        }
    }
}
