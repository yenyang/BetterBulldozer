// <copyright file="HandleDeleteInXFramesSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Systems
{
    using Better_Bulldozer.Components;
    using Colossal.Entities;
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Simulation;
    using Game.Tools;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// A system that deletes something in a certain amount of frames.
    /// </summary>
    public partial class HandleDeleteInXFramesSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_DeleteNextFrameQuery;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private SimulationSystem m_SimulationSystem;

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
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            base.OnCreate();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            m_DeleteNextFrameQuery = SystemAPI.QueryBuilder()
                .WithAll<DeleteInXFrames>()
                .WithNone<Temp, Deleted>()
                .Build();

            RequireForUpdate(m_DeleteNextFrameQuery);

            EntityCommandBuffer buffer = m_ToolOutputBarrier.CreateCommandBuffer();

            NativeArray<Entity> entities = m_DeleteNextFrameQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                if (EntityManager.TryGetComponent(entity, out DeleteInXFrames counter))
                {
                    if (m_SimulationSystem.selectedSpeed > 0)
                    {
                        uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, BuildingUpkeepSystem.kUpdatesPerDay, 16);
                        if (EntityManager.HasComponent<UpdateFrame>(entity))
                        {
                            UpdateFrame frame = EntityManager.GetSharedComponentManaged<UpdateFrame>(entity);
                            if (frame.m_Index == updateFrame)
                            {
                                m_Log.Debug("Skipping due to update frame.");
                                continue;
                            }
                        }


                        if (EntityManager.TryGetComponent(entity, out Owner owner) && EntityManager.HasComponent<UpdateFrame>(owner.m_Owner))
                        {
                            UpdateFrame frame = EntityManager.GetSharedComponentManaged<UpdateFrame>(owner.m_Owner);
                            if (frame.m_Index == updateFrame)
                            {
                                m_Log.Debug("Skipping due to owner update frame.");
                                continue;
                            }
                        }
                    }

                    if (counter.m_FramesRemaining == 0)
                    {
                        buffer.AddComponent<Deleted>(entity);
                    }
                    else
                    {
                        counter.m_FramesRemaining--;
                        buffer.SetComponent(entity, counter);
                    }
                }
            }
        }
    }
}
