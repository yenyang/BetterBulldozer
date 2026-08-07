// <copyright file="RestoreBrandingObjects.cs" company="Yenyang's Mods. MIT License">
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
    /// A system that automatically removes fences and hedges from created buildings.
    /// </summary>
    public partial class RestoreBrandingObjects : GameSystemBase
    {
        private ILog m_Log;

        private EntityQuery m_SavedBrandingQuery;

        private ToolSystem m_ToolSystem;
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

            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_ToolSystem = World.GetExistingSystemManaged<ToolSystem>();

            base.OnCreate();

            Enabled = false;

            m_SavedBrandingQuery = SystemAPI.QueryBuilder()
                .WithAll<SavedOwnerBuildingTag>()
                .WithNone<Temp, Deleted>()
                .Build();

            RequireForUpdate(m_SavedBrandingQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (!m_ToolSystem.actionMode.IsGame())
            {
                Enabled = false;
                return;
            }

            EntityCommandBuffer.ParallelWriter ecbWriter = m_Barrier.CreateCommandBuffer().AsParallelWriter();

            RestoreBrandingJob restoreJob = new RestoreBrandingJob()
            {
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_SavedOwnerBuildingTagType = SystemAPI.GetComponentTypeHandle<SavedOwnerBuildingTag>(),
                m_CommandBuffer = ecbWriter,
            };

            JobHandle finalHandle = restoreJob.ScheduleParallel(m_SavedBrandingQuery, Dependency);

            m_Barrier.AddJobHandleForProducer(finalHandle);
            Dependency = finalHandle;

            Enabled = false;
        }

#if BURST
        [BurstCompile]
#endif
        private struct RestoreBrandingJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle m_EntityType;

            public ComponentTypeHandle<SavedOwnerBuildingTag> m_SavedOwnerBuildingTagType;

            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityType);

                m_CommandBuffer.RemoveComponent<SavedOwnerBuildingTag>(unfilteredChunkIndex, entities);
                m_CommandBuffer.AddComponent<Updated>(unfilteredChunkIndex, entities);
            }
        }
    }
}