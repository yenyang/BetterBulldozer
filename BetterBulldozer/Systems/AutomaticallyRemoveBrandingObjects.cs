// <copyright file="AutomaticallyRemoveBrandingObjects.cs" company="Yenyang's Mods. MIT License">
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
    public partial class AutomaticallyRemoveBrandingObjects : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_UpdateQuery;
        private EntityQuery m_BrandObjectPrefabQuery;
        private EntityQuery m_UpdateEventQuery;
        private PrefabSystem m_PrefabSystem;
        private ModificationEndBarrier m_Barrier;
        private ToolSystem m_ToolSystem;
        private bool m_JustLoaded = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomaticallyRemoveBrandingObjects"/> class.
        /// </summary>
        public AutomaticallyRemoveBrandingObjects()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_Log.Info($"{nameof(AutomaticallyRemoveBrandingObjects)}.{nameof(OnCreate)}.");
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_UpdateQuery = GetEntityQuery (
                new EntityQueryDesc
                {
                    All = new ComponentType[] { ComponentType.ReadOnly<Game.Objects.SubObject>(), ComponentType.ReadOnly<Updated>() },
                    None = new ComponentType[] { ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Deleted>() },
                });

            m_UpdateEventQuery = GetEntityQuery (
                new EntityQueryDesc
                {
                    All = new ComponentType[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<RentersUpdated>() },
                    None = new ComponentType[] { ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Deleted>() },
                });

            m_Barrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();
            base.OnCreate();
            m_BrandObjectPrefabQuery = SystemAPI.QueryBuilder()
                .WithAll<BrandObjectData>()
                .Build();
            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode.IsGame())
            {
               Enabled = BetterBulldozerMod.Instance.Settings.AutomaticRemovalBrandingObjects;
            }
            else
            {
                Enabled = false;
                return;
            }

            if (!BetterBulldozerMod.Instance.Settings.AutomaticRemovalBrandingObjects)
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


            NativeList<Entity> brandingObjectPrefabsEntities = m_BrandObjectPrefabQuery.ToEntityListAsync(Allocator.TempJob, out JobHandle brandingObjectJobHandle);

            NativeList<Entity> brandingSubObjects = new NativeList<Entity>(Allocator.TempJob);

            if (!subObjectQuery.IsEmptyIgnoreFilter)
            {
                GatherSubObjectsJob gatherSubObjectsJob = new GatherSubObjectsJob()
                {
                    m_BrandingObjectPrefabs = brandingObjectPrefabsEntities,
                    m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                    m_SubObjectLookup = SystemAPI.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true),
                    m_SubObjects = brandingSubObjects,
                    m_SubObjectType = SystemAPI.GetBufferTypeHandle<Game.Objects.SubObject>(isReadOnly: true),
                };

                Dependency = gatherSubObjectsJob.Schedule(subObjectQuery, JobHandle.CombineDependencies(Dependency, brandingObjectJobHandle));
            }

            if (!m_UpdateEventQuery.IsEmptyIgnoreFilter)
            {
                GatherSubObjectsFromEventsJob gatherSubObjectsFromEventsJob = new GatherSubObjectsFromEventsJob()
                {
                    m_BrandingObjectPrefabs = brandingObjectPrefabsEntities,
                    m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                    m_RentersUpdatedType = SystemAPI.GetComponentTypeHandle<RentersUpdated>(isReadOnly: true),
                    m_SubObjectLookup = SystemAPI.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true),
                    m_SubObjects = brandingSubObjects,
                };

                Dependency = gatherSubObjectsFromEventsJob.Schedule(m_UpdateEventQuery, Dependency);
            }

            brandingObjectPrefabsEntities.Dispose(Dependency);
            HandleDeleteInXFramesJob handleDeleteInXFramesJob = new HandleDeleteInXFramesJob()
            {
                m_DeleteInXFramesLookup = SystemAPI.GetComponentLookup<DeleteInXFrames>(isReadOnly: true),
                m_Entities = brandingSubObjects,
                buffer = m_Barrier.CreateCommandBuffer(),
            };

            JobHandle handleDeleteInXFramesJobHandle = handleDeleteInXFramesJob.Schedule(Dependency);
            m_Barrier.AddJobHandleForProducer(handleDeleteInXFramesJobHandle);
            Dependency = handleDeleteInXFramesJobHandle;
            brandingSubObjects.Dispose(handleDeleteInXFramesJobHandle);
        }

#if BURST
        [BurstCompile]
#endif
        private struct GatherSubObjectsJob : IJobChunk
        {
            [ReadOnly]
            public BufferTypeHandle<Game.Objects.SubObject> m_SubObjectType;
            [ReadOnly]
            public NativeList<Entity> m_BrandingObjectPrefabs;
            public NativeList<Entity> m_SubObjects;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            [ReadOnly]
            public BufferLookup<Game.Objects.SubObject> m_SubObjectLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                BufferAccessor<Game.Objects.SubObject> subObjectBufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    DynamicBuffer<Game.Objects.SubObject> dynamicBuffer = subObjectBufferAccessor[i];
                    foreach (Game.Objects.SubObject subObject in dynamicBuffer)
                    {
                        if (!m_PrefabRefLookup.TryGetComponent(subObject.m_SubObject, out PrefabRef prefabRef) || !m_BrandingObjectPrefabs.Contains(prefabRef.m_Prefab))
                        {
                            continue;
                        }

                        m_SubObjects.Add(subObject.m_SubObject);
                        if (!m_SubObjectLookup.TryGetBuffer(subObject.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer))
                        {
                            continue;
                        }

                        foreach (Game.Objects.SubObject deepSubObject in deepSubObjectBuffer)
                        {
                            m_SubObjects.Add(subObject.m_SubObject);
                        }
                    }
                }
            }
        }

#if BURST
        [BurstCompile]
#endif
        private struct GatherSubObjectsFromEventsJob : IJobChunk
        {
            [ReadOnly]
            public BufferLookup<Game.Objects.SubObject> m_SubObjectLookup;
            [ReadOnly]
            public NativeList<Entity> m_BrandingObjectPrefabs;
            public NativeList<Entity> m_SubObjects;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            [ReadOnly]
            public ComponentTypeHandle<RentersUpdated> m_RentersUpdatedType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<RentersUpdated> rentersUpdatedNativeArray = chunk.GetNativeArray(ref m_RentersUpdatedType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    RentersUpdated rentersUpdated = rentersUpdatedNativeArray[i];
                    if (!m_SubObjectLookup.TryGetBuffer(rentersUpdated.m_Property, out DynamicBuffer<Game.Objects.SubObject> dynamicBuffer))
                    {
                        continue;
                    }

                    foreach (Game.Objects.SubObject subObject in dynamicBuffer)
                    {
                        if (!m_PrefabRefLookup.TryGetComponent(subObject.m_SubObject, out PrefabRef prefabRef) || !m_BrandingObjectPrefabs.Contains(prefabRef.m_Prefab))
                        {
                            continue;
                        }

                        m_SubObjects.Add(subObject.m_SubObject);
                        if (!m_SubObjectLookup.TryGetBuffer(subObject.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer))
                        {
                            continue;
                        }

                        foreach (Game.Objects.SubObject deepSubObject in deepSubObjectBuffer)
                        {
                            m_SubObjects.Add(subObject.m_SubObject);
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
            public EntityCommandBuffer buffer;

            public void Execute()
            {
                foreach (Entity entity in m_Entities)
                {
                    if (!m_DeleteInXFramesLookup.HasComponent(entity))
                    {
                        buffer.AddComponent<DeleteInXFrames>(entity);
                    }

                    buffer.SetComponent(entity, new DeleteInXFrames() { m_FramesRemaining = 5 });
                }
            }
        }
    }
}
