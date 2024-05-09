// <copyright file="RemoveExistingOwnedGrassSurfaces.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Better_Bulldozer.Systems
{
    using System.Collections.Generic;
    using Better_Bulldozer.Components;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Areas;
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
    public partial class RemoveExistingOwnedGrassSurfaces : GameSystemBase
    {
        private readonly List<PrefabID> m_GrassSurfacePrefabIDs = new()
        {
            new ("SurfacePrefab", "Grass Surface 01"),
            new ("SurfacePrefab", "Grass Surface 02"),
        };

        private NativeList<Entity> m_GrassSurfacePrefabEntities;
        private ILog m_Log;
        private EntityQuery m_OwnedAreaQuery;
        private PrefabSystem m_PrefabSystem;
        private ToolOutputBarrier m_Barrier;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveExistingOwnedGrassSurfaces"/> class.
        /// </summary>
        public RemoveExistingOwnedGrassSurfaces()
        {
        }


        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            if (!m_GrassSurfacePrefabEntities.IsEmpty)
            {
                return;
            }

            foreach (PrefabID prefabID in m_GrassSurfacePrefabIDs)
            {
                if (m_PrefabSystem.TryGetPrefab(prefabID, out PrefabBase prefab) && prefab != null)
                {
                    if (m_PrefabSystem.TryGetEntity(prefab, out Entity entity))
                    {
                        if (entity != Entity.Null)
                        {
                            m_GrassSurfacePrefabEntities.Add(entity);

                            // m_Log.Debug($"{nameof(AutomaticallyRemoveManicuredGrassSurfaceSystem)}.{nameof(OnGameLoadingComplete)} added entity {entity.Index}:{entity.Version}");
                        }
                    }
                }
            }

            base.OnGameLoadingComplete(purpose, mode);
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_Log.Info($"{nameof(RemoveExistingOwnedGrassSurfaces)}.{nameof(OnCreate)}.");
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_GrassSurfacePrefabEntities = new NativeList<Entity>(m_GrassSurfacePrefabIDs.Count, Allocator.Persistent);
            base.OnCreate();
            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            m_OwnedAreaQuery = SystemAPI.QueryBuilder()
                .WithAll<Owner, Area, Surface, PrefabRef>()
                .WithNone<Temp, Deleted, DeleteInXFrames>()
                .Build();

            RequireForUpdate(m_OwnedAreaQuery);

            if (m_GrassSurfacePrefabEntities.IsEmpty)
            {
                Enabled = false;
                return;
            }

            DeleteOwnedGrassSurfacesJob deleteOwnedGrassSurfacesJob = new DeleteOwnedGrassSurfacesJob()
            {
                buffer = m_Barrier.CreateCommandBuffer().AsParallelWriter(),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_GrassPrefabs = m_GrassSurfacePrefabEntities,
                m_PrefabRefType = SystemAPI.GetComponentTypeHandle<PrefabRef>(),
            };
            JobHandle jobHandle = deleteOwnedGrassSurfacesJob.ScheduleParallel(m_OwnedAreaQuery, Dependency);
            m_Barrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            m_GrassSurfacePrefabEntities.Dispose();
            base.OnDestroy();
        }

#if BURST
        [BurstCompile]
#endif
        private struct DeleteOwnedGrassSurfacesJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public EntityCommandBuffer.ParallelWriter buffer;
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabRefType;
            [ReadOnly]
            public NativeList<Entity> m_GrassPrefabs;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<PrefabRef> prefabRefNativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity currentEntity = entityNativeArray[i];
                    PrefabRef prefabRef = prefabRefNativeArray[i];
                    if (m_GrassPrefabs.Contains(prefabRef.m_Prefab))
                    {
                        buffer.AddComponent<DeleteInXFrames>(unfilteredChunkIndex, currentEntity);
                        DeleteInXFrames delete = new DeleteInXFrames { m_FramesRemaining = 3 };
                        buffer.SetComponent(unfilteredChunkIndex, currentEntity, delete);
                    }
                }
            }
        }
    }
}
