// <copyright file="AutomaticallyRemoveStreetSignObjects.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Better_Bulldozer.Systems
{
    using Better_Bulldozer.Components;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Buildings;
    using Game.Common;
    using Game.Prefabs;
    using Game.Tools;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// A system that automatically removes branding objects.
    /// </summary>
    public partial class AutomaticallyRemoveStreetSignObjects : GameSystemBase
    {
        private ILog m_Log;

        private EntityQuery m_UpdateQuery;
        private EntityQuery m_StreetSignPrefabQuery;

        private ModificationEndBarrier m_Barrier;
        private bool m_JustLoaded = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomaticallyRemoveStreetSignObjects"/> class.
        /// </summary>
        public AutomaticallyRemoveStreetSignObjects()
        {
        }

        public void ForceFullScan()
        {
            if (Enabled)
            {
                m_JustLoaded = true;
            }
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_Log.Info($"{nameof(AutomaticallyRemoveStreetSignObjects)}.{nameof(OnCreate)}.");

            m_UpdateQuery = GetEntityQuery (
                new EntityQueryDesc
                {
                    All = new ComponentType[] { ComponentType.ReadOnly<Game.Objects.SubObject>(), ComponentType.ReadOnly<Updated>() },
                    None = new ComponentType[] { ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Deleted>() },
                });

            m_Barrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();

            base.OnCreate();

            m_StreetSignPrefabQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficSignData>()
                .Build();

            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode.IsGame())
            {
               Enabled = BetterBulldozerMod.Instance.Settings.AutomaticRemovalStreetSignObjects;
            }
            else
            {
                Enabled = false;
                return;
            }

            if (!BetterBulldozerMod.Instance.Settings.AutomaticRemovalStreetSignObjects)
            {
                return;
            }

            m_JustLoaded = true;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            EntityQuery subObjectQuery = m_UpdateQuery;

            if (m_JustLoaded)
            {
                subObjectQuery = SystemAPI.QueryBuilder()
                    .WithAll<Game.Objects.SubObject>()
                    .WithNone<Temp, Deleted>()
                    .Build();

                m_JustLoaded = false;
            }

            NativeList<Entity> streetSignPrefabs = m_StreetSignPrefabQuery.ToEntityListAsync(Allocator.TempJob, out JobHandle streetSignJobHandle);

            NativeList<Entity> streetSignsSubObjects = new NativeList<Entity>(Allocator.TempJob);

            if (!subObjectQuery.IsEmptyIgnoreFilter)
            {
                FilterAndGatherTrafficSignsJob trafficSignsJob = new FilterAndGatherTrafficSignsJob()
                {
                    m_SubObjectType = SystemAPI.GetBufferTypeHandle<Game.Objects.SubObject>(isReadOnly: true),
                    m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                    m_TrafficSignLookup = SystemAPI.GetComponentLookup<TrafficSignData>(isReadOnly: true),
                    m_StreetSignPrefabs = streetSignPrefabs,
                    m_SubObjects = streetSignsSubObjects,
                };

                Dependency = trafficSignsJob.Schedule(subObjectQuery, JobHandle.CombineDependencies(Dependency, streetSignJobHandle));
            }

            streetSignPrefabs.Dispose(Dependency);

            HandleDeleteInXFramesJob handleDeleteInXFramesJob = new HandleDeleteInXFramesJob()
            {
                m_DeleteInXFramesLookup = SystemAPI.GetComponentLookup<DeleteInXFrames>(isReadOnly: true),
                m_Entities = streetSignsSubObjects,
                buffer = m_Barrier.CreateCommandBuffer(),
                m_TransformLookup = SystemAPI.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true),
            };

            JobHandle handleDeleteInXFramesJobHandle = handleDeleteInXFramesJob.Schedule(Dependency);
            m_Barrier.AddJobHandleForProducer(handleDeleteInXFramesJobHandle);
            Dependency = handleDeleteInXFramesJobHandle;

            streetSignsSubObjects.Dispose(handleDeleteInXFramesJobHandle);
        }

#if BURST
        [BurstCompile]
#endif
        private struct FilterAndGatherTrafficSignsJob : IJobChunk
        {
            [ReadOnly]
            public BufferTypeHandle<Game.Objects.SubObject> m_SubObjectType;

            [ReadOnly]
            public ComponentLookup<TrafficSignData> m_TrafficSignLookup;

            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefLookup;

            [ReadOnly]
            public NativeList<Entity> m_StreetSignPrefabs;

            public NativeList<Entity> m_SubObjects;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                BufferAccessor<Game.Objects.SubObject> subObjectBufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    DynamicBuffer<Game.Objects.SubObject> dynamicBuffer = subObjectBufferAccessor[i];

                    foreach (Game.Objects.SubObject subObject in dynamicBuffer)
                    {
                        if (!m_PrefabRefLookup.TryGetComponent(subObject.m_SubObject, out PrefabRef prefabRef) || !m_StreetSignPrefabs.Contains(prefabRef.m_Prefab))
                        {
                            continue;
                        }

                        /// <summary>
                        /// In order to correctly identify StreetSign, TrafficSignData.m_TypeMask must be 256
                        /// </summary>
                        if (m_TrafficSignLookup.TryGetComponent(prefabRef.m_Prefab, out TrafficSignData signData))
                        {
                            if (signData.m_TypeMask == 256)
                            {
                                m_SubObjects.Add(subObject.m_SubObject);
                            }
                        }
                    }
                }
            }
        }

#if BURST
        [BurstCompile]
#endif
        private struct HandleDeleteInXFramesJob : IJob
        {
            [ReadOnly]
            public NativeList<Entity> m_Entities;
            [ReadOnly]
            public ComponentLookup<DeleteInXFrames> m_DeleteInXFramesLookup;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_TransformLookup;
            public EntityCommandBuffer buffer;

            public void Execute()
            {
                foreach (Entity entity in m_Entities)
                {
                    if (!m_DeleteInXFramesLookup.HasComponent(entity))
                    {
                        buffer.AddComponent<DeleteInXFrames>(entity);
                    }

                    buffer.SetComponent(entity, new DeleteInXFrames() { m_FramesRemaining = 30 });

                    if (m_TransformLookup.HasComponent(entity))
                    {
                        if (m_TransformLookup.TryGetComponent(entity, out Game.Objects.Transform transform) && transform.m_Position.y > 0)
                        {
                            transform.m_Position.y = 0;
                            buffer.SetComponent(entity, transform);
                            buffer.AddComponent<Updated>(entity);
                        }
                    }
                }
            }
        }
    }
}
