// <copyright file="HandleDeleteInXFramesSystem.cs" company="Yenyang's Mods. MIT License">
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
    /// A system that deletes something in a certain amount of frames.
    /// </summary>
    public partial class HandleDeleteInXFramesSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_DeleteNextFrameQuery;
        private ToolOutputBarrier m_Barrier;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandleDeleteInXFramesSystem"/> class.
        /// </summary>
        public HandleDeleteInXFramesSystem()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_Log.Info($"{nameof(HandleDeleteInXFramesSystem)} Created.");
            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            base.OnCreate();
            m_DeleteNextFrameQuery = SystemAPI.QueryBuilder()
                .WithAll<DeleteInXFrames, Owner>()
                .WithNone<Temp, Deleted>()
                .Build();

            RequireForUpdate(m_DeleteNextFrameQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();

            HandleDeleteInXFramesJob handleDeleteInXFramesJob = new HandleDeleteInXFramesJob()
            {
                buffer = buffer,
                m_DeleteInXFramesType = SystemAPI.GetComponentTypeHandle<DeleteInXFrames>(isReadOnly: true),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_OwnerType = SystemAPI.GetComponentTypeHandle<Owner>(isReadOnly: true),
                m_UpdatedLookup = SystemAPI.GetComponentLookup<Updated>(isReadOnly: true),
            };

            JobHandle jobHandle = handleDeleteInXFramesJob.Schedule(m_DeleteNextFrameQuery, Dependency);
            m_Barrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
        }

#if BURST
        [BurstCompile]
#endif
        private struct HandleDeleteInXFramesJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<DeleteInXFrames> m_DeleteInXFramesType;
            [ReadOnly]
            public ComponentTypeHandle<Owner> m_OwnerType;
            [ReadOnly]
            public ComponentLookup<Updated> m_UpdatedLookup;
            public EntityCommandBuffer buffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Owner> ownerNativeArray = chunk.GetNativeArray(ref m_OwnerType);
                NativeArray<DeleteInXFrames> deleteInXFramesNativeArray = chunk.GetNativeArray(ref m_DeleteInXFramesType);
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (m_UpdatedLookup.HasComponent(ownerNativeArray[i].m_Owner))
                    {
                        buffer.SetComponent(entityNativeArray[i], new DeleteInXFrames { m_FramesRemaining = 30 });
                    }
                    else
                    {
                        DeleteInXFrames deleteInXFrames = deleteInXFramesNativeArray[i];
                        if (deleteInXFrames.m_FramesRemaining <= 0)
                        {
                            buffer.AddComponent<Deleted>(entityNativeArray[i]);
                        }
                        else
                        {
                            deleteInXFrames.m_FramesRemaining--;
                            buffer.SetComponent(entityNativeArray[i], deleteInXFrames);
                        }
                    }
                }
            }
        }
    }
}
