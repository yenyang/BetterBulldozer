// <copyright file="RestoreFencesAndHedgesSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Better_Bulldozer.Systems
{
    using Better_Bulldozer.Components;
    using Colossal.Logging;
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
    /// A system that restores fences and hedges from buildings.
    /// </summary>
    public partial class RestoreFencesAndHedgesSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_SubLanesQuery;
        private PrefabSystem m_PrefabSystem;
        private ToolOutputBarrier m_Barrier;
        private ToolSystem m_ToolSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestoreFencesAndHedgesSystem"/> class.
        /// </summary>
        public RestoreFencesAndHedgesSystem()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_Log.Info($"{nameof(RestoreFencesAndHedgesSystem)}.{nameof(OnCreate)}.");
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            base.OnCreate();
            Enabled = false;

            m_SubLanesQuery = SystemAPI.QueryBuilder()
                .WithAllRW<Game.Net.SubLane>()
                .WithNone<Temp, Deleted, DeleteInXFrames>()
                .Build();

            RequireForUpdate(m_SubLanesQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (!m_ToolSystem.actionMode.IsGame())
            {
                Enabled = false;
                return;
            }

            AddUpdatedJob addUpdatedJob = new AddUpdatedJob()
            {
                buffer = m_Barrier.CreateCommandBuffer().AsParallelWriter(),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
            };
            JobHandle jobHandle = addUpdatedJob.ScheduleParallel(m_SubLanesQuery, Dependency);
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
