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
        private EntityQuery m_LoadedQuery;

        private ModificationEndBarrier m_Barrier;

        private bool m_JustLoaded = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomaticallyRemoveBrandingObjects"/> class.
        /// </summary>
        public AutomaticallyRemoveBrandingObjects()
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
            m_Log.Info($"{nameof(AutomaticallyRemoveBrandingObjects)}.{nameof(OnCreate)}.");

            m_UpdateQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Objects.SubObject, Updated>()
                .WithNone<Temp, Deleted>()
                .AddAdditionalQuery()
                .WithAll<Event, RentersUpdated>()
                .WithNone<Temp, Deleted>()
                .Build();

            m_LoadedQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Objects.SubObject>()
                .WithNone<Temp, Deleted>()
                .AddAdditionalQuery()
                .WithAll<RentersUpdated>()
                .WithNone<Temp, Deleted>()
                .Build();


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
            EntityQuery subObjectQuery = m_JustLoaded ? m_LoadedQuery : m_UpdateQuery;
            m_JustLoaded = false;

            if (subObjectQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            int prefabCount = m_BrandObjectPrefabQuery.CalculateEntityCount();

            NativeParallelHashSet<Entity> brandingPrefabsSet = new NativeParallelHashSet<Entity>(prefabCount, Allocator.TempJob);

            JobHandle setSetupHandle = new CreateHashSetJob
            {
                EntityType = SystemAPI.GetEntityTypeHandle(),
                Set = brandingPrefabsSet.AsParallelWriter(),
            }.ScheduleParallel(m_BrandObjectPrefabQuery, Dependency);

            JobHandle upstreamDependency = JobHandle.CombineDependencies(Dependency, setSetupHandle);

            // Create a single concurrent command buffer for processing directly in parallel jobs
            EntityCommandBuffer.ParallelWriter ecbWriter = m_Barrier.CreateCommandBuffer().AsParallelWriter();

            GatherSubObjectsJob gatherSubObjectsJob = new GatherSubObjectsJob()
            {
                m_BrandingObjectPrefabs = brandingPrefabsSet,

                m_RentersUpdatedType = SystemAPI.GetComponentTypeHandle<RentersUpdated>(isReadOnly: true),
                m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                m_SubObjectLookup = SystemAPI.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true),
                m_SubObjectType = SystemAPI.GetBufferTypeHandle<Game.Objects.SubObject>(isReadOnly: true),
                m_DeleteInXFramesLookup = SystemAPI.GetComponentLookup<DeleteInXFrames>(isReadOnly: true),

                m_OwnerLookup = SystemAPI.GetComponentLookup<Game.Common.Owner>(isReadOnly: true),

                m_SavedTransformLookup = SystemAPI.GetComponentLookup<SavedOwnerBuildingTag>(isReadOnly: true),

                m_CommandBuffer = ecbWriter,
            };

            JobHandle finalHandle = gatherSubObjectsJob.ScheduleParallel(subObjectQuery, upstreamDependency);

            brandingPrefabsSet.Dispose(finalHandle);

            m_Barrier.AddJobHandleForProducer(finalHandle);
            Dependency = finalHandle;
        }

#if BURST
        [BurstCompile]
#endif
        private struct CreateHashSetJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle EntityType;
            public NativeParallelHashSet<Entity>.ParallelWriter Set;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    Set.Add(entities[i]);
                }
            }
        }

#if BURST
        [BurstCompile]
#endif
        private struct GatherSubObjectsJob : IJobChunk
        {
            [ReadOnly] public NativeParallelHashSet<Entity> m_BrandingObjectPrefabs;

            [ReadOnly] public BufferTypeHandle<Game.Objects.SubObject> m_SubObjectType;
            [ReadOnly] public ComponentTypeHandle<RentersUpdated> m_RentersUpdatedType;
            [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            [ReadOnly] public BufferLookup<Game.Objects.SubObject> m_SubObjectLookup;
            [ReadOnly] public ComponentLookup<DeleteInXFrames> m_DeleteInXFramesLookup;
            [ReadOnly] public ComponentLookup<SavedOwnerBuildingTag> m_SavedTransformLookup;

            // 1. Add Owner lookup
            [ReadOnly] public ComponentLookup<Game.Common.Owner> m_OwnerLookup;

            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (chunk.Has(ref m_RentersUpdatedType))
                {
                    NativeArray<RentersUpdated> rentersUpdatedNativeArray = chunk.GetNativeArray(ref m_RentersUpdatedType);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        RentersUpdated rentersUpdated = rentersUpdatedNativeArray[i];

                        if (!m_SubObjectLookup.TryGetBuffer(rentersUpdated.m_Property, out DynamicBuffer<Game.Objects.SubObject> dynamicBuffer))
                        {
                            continue;
                        }

                        ProcessSubObjectBuffer(dynamicBuffer, unfilteredChunkIndex);
                    }
                }
                else if (chunk.Has(ref m_SubObjectType))
                {
                    BufferAccessor<Game.Objects.SubObject> subObjectBufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        DynamicBuffer<Game.Objects.SubObject> dynamicBuffer = subObjectBufferAccessor[i];

                        ProcessSubObjectBuffer(dynamicBuffer, unfilteredChunkIndex);
                    }
                }
            }

            private void ProcessSubObjectBuffer(DynamicBuffer<Game.Objects.SubObject> dynamicBuffer, int sortKey)
            {
                foreach (Game.Objects.SubObject subObject in dynamicBuffer)
                {
                    if (!m_PrefabRefLookup.TryGetComponent(subObject.m_SubObject, out PrefabRef prefabRef) || !m_BrandingObjectPrefabs.Contains(prefabRef.m_Prefab))
                    {
                        continue;
                    }

                    ProcessEntity(subObject.m_SubObject, sortKey);

                    if (m_SubObjectLookup.TryGetBuffer(subObject.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer))
                    {
                        foreach (Game.Objects.SubObject deepSubObject in deepSubObjectBuffer)
                        {
                            ProcessEntity(deepSubObject.m_SubObject, sortKey);
                        }
                    }
                }
            }

            private void ProcessEntity(Entity entity, int sortKey)
            {
                if (m_OwnerLookup.TryGetComponent(entity, out Game.Common.Owner owner) && owner.m_Owner != Entity.Null)
                {
                    Entity ownerEntity = owner.m_Owner;

                    // Check m_SavedTransformLookup against ownerEntity (NOT entity)
                    if (!m_SavedTransformLookup.HasComponent(ownerEntity))
                    {
                        m_CommandBuffer.AddComponent(sortKey, ownerEntity, new SavedOwnerBuildingTag());
                    }
                }

                if (!m_DeleteInXFramesLookup.HasComponent(entity))
                {
                    m_CommandBuffer.AddComponent(sortKey, entity, new DeleteInXFrames() { m_FramesRemaining = 30 });
                }
                else
                {
                    m_CommandBuffer.SetComponent(sortKey, entity, new DeleteInXFrames() { m_FramesRemaining = 30 });
                }
            }
        }
    }
}