// <copyright file="CleanUpOwnerRecordsSystem.cs" company="Yenyang's Mods. MIT License">
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
    public partial class CleanUpOwnerRecordsSystem : GameSystemBase
    {
        /// <summary>
        /// Relates to the update interval although the GetUpdateInterval isn't even using this.
        /// </summary>
        public const int UPDATES_PER_DAY = 32;

        private EntityQuery m_OwnerRecordQuery;
        private ILog m_Log;
        private EndFrameBarrier m_EndFrameBarrier;

        /// <inheritdoc/>
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 512;
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_Log.Info($"{nameof(CleanUpOwnerRecordsSystem)}.{nameof(OnCreate)}");
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            base.OnCreate();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            m_OwnerRecordQuery = SystemAPI.QueryBuilder()
                .WithAll<OwnerRecord>()
                .WithNone<Temp, Deleted>()
                .Build();

            RequireForUpdate(m_OwnerRecordQuery);

            CleanUpOwnerRecordsJob cleanUpOwnerRecordsJob = new CleanUpOwnerRecordsJob()
            {
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_OwnerRecordTyp = SystemAPI.GetComponentTypeHandle<OwnerRecord>(isReadOnly: true),
                m_PermanentlyRemovedSubElementPrefabLookup = SystemAPI.GetBufferLookup<PermanentlyRemovedSubElementPrefab>(isReadOnly: true),
                buffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
            };
            JobHandle jobHandle = cleanUpOwnerRecordsJob.ScheduleParallel(m_OwnerRecordQuery, Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
        }

#if BURST
        [BurstCompile]
#endif
        private struct CleanUpOwnerRecordsJob : IJobChunk
        {
            [ReadOnly]
            public BufferLookup<PermanentlyRemovedSubElementPrefab> m_PermanentlyRemovedSubElementPrefabLookup;
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<OwnerRecord> m_OwnerRecordTyp;
            public EntityCommandBuffer.ParallelWriter buffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<OwnerRecord> ownerRecordNativeArray = chunk.GetNativeArray(ref m_OwnerRecordTyp);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity currentEntity = entityNativeArray[i];
                    OwnerRecord ownerRecord = ownerRecordNativeArray[i];
                    if (!m_PermanentlyRemovedSubElementPrefabLookup.HasBuffer(ownerRecord.m_Owner))
                    {
                        buffer.DestroyEntity(unfilteredChunkIndex, currentEntity);
                    }
                }
            }
        }
    }
}
