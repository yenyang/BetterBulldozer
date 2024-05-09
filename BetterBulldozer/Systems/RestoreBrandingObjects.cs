// <copyright file="RestoreBrandingObjects.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Systems
{
    using Better_Bulldozer.Components;
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Tools;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// A system that automatically removes fences and hedges from created buildings.
    /// </summary>
    public partial class RestoreBrandingObjects : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_SubObjectQuery;
        private PrefabSystem m_PrefabSystem;
        private ToolOutputBarrier m_Barrier;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestoreBrandingObjects"/> class.
        /// </summary>
        public RestoreBrandingObjects()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_Log.Info($"{nameof(AutomaticallyRemoveBrandingObjects)}.{nameof(OnCreate)}.");
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            base.OnCreate();
            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            m_SubObjectQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Objects.SubObject>()
                .WithNone<Temp, Deleted, DeleteInXFrames>()
                .Build();

            RequireForUpdate(m_SubObjectQuery);

            AddUpdatedJob addUpdatedJob = new AddUpdatedJob()
            {
                buffer = m_Barrier.CreateCommandBuffer().AsParallelWriter(),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
            };
            JobHandle jobHandle = addUpdatedJob.ScheduleParallel(m_SubObjectQuery, Dependency);
            m_Barrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
            Enabled = false;
        }

#if BURST
        [BurstCompile]
#endif
        private struct AddUpdatedJob : IJobChunk
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
                    buffer.AddComponent<Updated>(unfilteredChunkIndex, currentEntity);
                }
            }
        }
    }
}
