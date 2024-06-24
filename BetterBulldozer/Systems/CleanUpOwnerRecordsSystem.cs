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
    using Game.Serialization;
    using Game.Tools;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Cleans up old entities after owners were deleted on deserialize.
    /// </summary>
    public partial class CleanUpOwnerRecordsSystem : GameSystemBase
    {

        private EntityQuery m_OwnerRecordQuery;
        private ILog m_Log;
        private DeserializationBarrier m_Barrier;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_Log.Info($"{nameof(CleanUpOwnerRecordsSystem)}.{nameof(OnCreate)}");
            m_Barrier = World.GetOrCreateSystemManaged<DeserializationBarrier>();
            base.OnCreate();
            m_OwnerRecordQuery = SystemAPI.QueryBuilder()
                .WithAll<OwnerRecord>()
                .WithNone<Temp, Deleted>()
                .Build();

            RequireForUpdate(m_OwnerRecordQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            m_Log.Debug($"{nameof(CleanUpOwnerRecordsSystem)}.{nameof(OnUpdate)}");
            CleanUpOwnerRecordsJob cleanUpOwnerRecordsJob = new CleanUpOwnerRecordsJob()
            {
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_OwnerRecordTyp = SystemAPI.GetComponentTypeHandle<OwnerRecord>(isReadOnly: true),
                m_PermanentlyRemovedSubElementPrefabLookup = SystemAPI.GetBufferLookup<PermanentlyRemovedSubElementPrefab>(isReadOnly: true),
                buffer = m_Barrier.CreateCommandBuffer(),
            };
            JobHandle jobHandle = cleanUpOwnerRecordsJob.Schedule(m_OwnerRecordQuery, Dependency);
            m_Barrier.AddJobHandleForProducer(jobHandle);
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
            public EntityCommandBuffer buffer;

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
                        buffer.AddComponent<Deleted>(currentEntity);
                    }
                }
            }
        }
    }
}
