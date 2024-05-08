// <copyright file="SafelyRemoveSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Better_Bulldozer.Systems
{
    using Better_Bulldozer.Components;
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Tools;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Cleans up old entities after owners were deleted periodicly.
    /// </summary>
    public partial class SafelyRemoveSystem : GameSystemBase
    {
        private EntityQuery m_PermanentlyRemovedSubElementPrefabQuery;
        private ILog m_Log;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private EntityQuery m_OwnerRecordQuery;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_Log.Info($"{nameof(SafelyRemoveSystem)}.{nameof(OnCreate)}");
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            base.OnCreate();
            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            m_PermanentlyRemovedSubElementPrefabQuery = SystemAPI.QueryBuilder()
                .WithAll<PermanentlyRemovedSubElementPrefab>()
                .WithNone<Deleted>()
                .Build();

            m_OwnerRecordQuery = SystemAPI.QueryBuilder()
                .WithAll<OwnerRecord>()
                .WithNone<Temp, Deleted>()
                .Build();

            RequireAnyForUpdate(m_PermanentlyRemovedSubElementPrefabQuery, m_OwnerRecordQuery);

            RemoveCustomBufferJob removeCustomBufferJob = new RemoveCustomBufferJob()
            {
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                buffer = m_ToolOutputBarrier.CreateCommandBuffer().AsParallelWriter(),
            };
            JobHandle removeBufferJobHandle = removeCustomBufferJob.ScheduleParallel(m_PermanentlyRemovedSubElementPrefabQuery, Dependency);
            m_ToolOutputBarrier.AddJobHandleForProducer(removeBufferJobHandle);

            CleanUpOwnerRecordsJob cleanUpOwnerRecordsJob = new CleanUpOwnerRecordsJob()
            {
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                buffer = m_ToolOutputBarrier.CreateCommandBuffer().AsParallelWriter(),
            };
            JobHandle jobHandle = cleanUpOwnerRecordsJob.ScheduleParallel(m_OwnerRecordQuery, JobHandle.CombineDependencies(Dependency, removeBufferJobHandle));
            m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
            Enabled = false;
        }

#if BURST
        [BurstCompile]
#endif
        private struct CleanUpOwnerRecordsJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public EntityCommandBuffer.ParallelWriter buffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity currentEntity = entityNativeArray[i];
                    buffer.DestroyEntity(unfilteredChunkIndex, currentEntity);
                }
            }
        }

#if BURST
        [BurstCompile]
#endif
        private struct RemoveCustomBufferJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public EntityCommandBuffer.ParallelWriter buffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity currentEntity = entityNativeArray[i];
                    buffer.RemoveComponent<PermanentlyRemovedSubElementPrefab>(unfilteredChunkIndex, currentEntity);
                }
            }
        }
    }
}
