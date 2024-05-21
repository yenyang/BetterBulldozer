// <copyright file="RemoveRegeneratedSubelementPrefabsSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Systems
{
    using Better_Bulldozer.Components;
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Buildings;
    using Game.Common;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Tools;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Removes regenrated subelement prefabs that have been permananetly removed.
    /// </summary>
    public partial class RemoveRegeneratedSubelementPrefabsSystem : GameSystemBase
    {
        private EntityQuery m_PermanentlyRemovedSubObjectQuery;
        private EntityQuery m_PermanentlyRemovedSubLaneQuery;
        private EntityQuery m_RentersUpdatedQuery;
        private ModificationEndBarrier m_ModificationEndBarrier;
        private ToolSystem m_ToolSystem;
        private PrefabSystem m_PrefabSystem;
        private ILog m_Log;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_ModificationEndBarrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_Log.Info($"{nameof(RemoveRegeneratedSubelementPrefabsSystem)}.{nameof(OnCreate)}");


            m_PermanentlyRemovedSubObjectQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Game.Objects.SubObject>(),
                        ComponentType.ReadOnly<PermanentlyRemovedSubElementPrefab>(),
                        ComponentType.ReadOnly<Updated>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    },
                },
            });

            m_PermanentlyRemovedSubLaneQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Game.Net.SubLane>(),
                        ComponentType.ReadOnly<PermanentlyRemovedSubElementPrefab>(),
                        ComponentType.ReadOnly<Updated>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    },
                },
            });

            m_RentersUpdatedQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Event>(),
                        ComponentType.ReadOnly<RentersUpdated>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    },
                },
            });

            RequireAnyForUpdate(m_PermanentlyRemovedSubObjectQuery, m_PermanentlyRemovedSubLaneQuery, m_RentersUpdatedQuery);
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode == GameMode.Game)
            {
                Enabled = true;
            }
            else
            {
                Enabled = false;
            }
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            m_Log.Debug($"{nameof(RemoveRegeneratedSubelementPrefabsSystem)}.{nameof(OnUpdate)}");
            EntityCommandBuffer buffer = m_ModificationEndBarrier.CreateCommandBuffer();

            if (!m_PermanentlyRemovedSubObjectQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entitiesWithSubObjects = m_PermanentlyRemovedSubObjectQuery.ToEntityArray(Allocator.Temp);
                ProcessForSubobjects(entitiesWithSubObjects, ref buffer);
            }

            if (!m_PermanentlyRemovedSubLaneQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entitiesWithSublanes = m_PermanentlyRemovedSubLaneQuery.ToEntityArray(Allocator.Temp);
                ProcessForSublanes(entitiesWithSublanes, ref buffer);
            }

            if (!m_RentersUpdatedQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> eventEntities = m_RentersUpdatedQuery.ToEntityArray(Allocator.Temp);
                NativeList<Entity> actualSubobjectEntitiesList = new NativeList<Entity>(Allocator.Temp);
                NativeList<Entity> actualSublaneEntitiesList = new NativeList<Entity>(Allocator.Temp);
                foreach (Entity entity in eventEntities)
                {
                    if (EntityManager.TryGetComponent(entity, out RentersUpdated rentersUpdated) && EntityManager.HasBuffer<PermanentlyRemovedSubElementPrefab>(rentersUpdated.m_Property))
                    {
                        m_Log.Debug($"{nameof(RemoveRegeneratedSubelementPrefabsSystem)} rentersUpdated includes {rentersUpdated.m_Property.Index}:{rentersUpdated.m_Property.Version}");
                        if (EntityManager.HasBuffer<Game.Objects.SubObject>(rentersUpdated.m_Property))
                        {
                            actualSubobjectEntitiesList.Add(rentersUpdated.m_Property);
                        }
                        else if (EntityManager.HasBuffer<Game.Net.SubLane>(rentersUpdated.m_Property))
                        {
                            actualSublaneEntitiesList.Add(rentersUpdated.m_Property);
                        }

                        continue;
                    }

                    if (EntityManager.TryGetComponent(entity, out SubObjectsUpdated subObjectsUpdated) && EntityManager.HasBuffer<PermanentlyRemovedSubElementPrefab>(subObjectsUpdated.m_Owner))
                    {
                        if (EntityManager.HasBuffer<Game.Objects.SubObject>(subObjectsUpdated.m_Owner))
                        {
                            actualSubobjectEntitiesList.Add(subObjectsUpdated.m_Owner);
                        }
                        else if (EntityManager.HasBuffer<Game.Net.SubLane>(subObjectsUpdated.m_Owner))
                        {
                            actualSublaneEntitiesList.Add(subObjectsUpdated.m_Owner);
                        }
                    }
                }

                ProcessForSubobjects(actualSubobjectEntitiesList.AsArray(), ref buffer);
                ProcessForSublanes(actualSublaneEntitiesList.AsArray(), ref buffer);
                eventEntities.Dispose();
                actualSubobjectEntitiesList.Dispose();
                actualSublaneEntitiesList.Dispose();
            }
        }

        private void ProcessForSubobjects(NativeArray<Entity> entities, ref EntityCommandBuffer buffer)
        {
            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetBuffer(entity, isReadOnly: false, out DynamicBuffer<Game.Objects.SubObject> subObjectBuffer))
                {
                    continue;
                }

                if (!EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<PermanentlyRemovedSubElementPrefab> prefabMatches) || prefabMatches.Length == 0)
                {
                    continue;
                }

                NativeList<Entity> m_RemoveIfMatchingPrefabEntity = new NativeList<Entity>(Allocator.Temp);
                foreach (PermanentlyRemovedSubElementPrefab subElementPrefab in prefabMatches)
                {
                    if (EntityManager.TryGetComponent(subElementPrefab.m_RecordEntity, out PrefabRef prefabRef))
                    {
                        m_RemoveIfMatchingPrefabEntity.Add(prefabRef.m_Prefab);
                    }
                }

                foreach (Game.Objects.SubObject subObject in subObjectBuffer)
                {
                    if (!EntityManager.TryGetComponent(subObject.m_SubObject, out PrefabRef prefabRef))
                    {
                        continue;
                    }

                    if (m_RemoveIfMatchingPrefabEntity.Contains(prefabRef.m_Prefab))
                    {
                        if (!EntityManager.HasComponent<DeleteInXFrames>(subObject.m_SubObject))
                        {
                            buffer.AddComponent<DeleteInXFrames>(subObject.m_SubObject);
                        }

                        buffer.SetComponent(subObject.m_SubObject, new DeleteInXFrames() { m_FramesRemaining = 5 });
                        m_Log.Debug($"{nameof(RemoveRegeneratedSubelementPrefabsSystem)}.{nameof(OnUpdate)} Will delete entity {subObject.m_SubObject.Index}.{subObject.m_SubObject.Version} from {entity.Index}.{entity.Version}");

                        if (EntityManager.TryGetBuffer(subObject.m_SubObject, false, out DynamicBuffer<Game.Objects.SubObject> dynamicBuffer))
                        {
                            foreach (Game.Objects.SubObject deepSubObject in dynamicBuffer)
                            {
                                if (!EntityManager.HasComponent<DeleteInXFrames>(deepSubObject.m_SubObject))
                                {
                                    buffer.AddComponent<DeleteInXFrames>(deepSubObject.m_SubObject);
                                }

                                buffer.SetComponent(deepSubObject.m_SubObject, new DeleteInXFrames() { m_FramesRemaining = 5 });
                            }
                        }
                    }
                }
            }
        }

        private void ProcessForSublanes(NativeArray<Entity> entities, ref EntityCommandBuffer buffer)
        {
            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetBuffer(entity, isReadOnly: false, out DynamicBuffer<Game.Net.SubLane> subLaneBuffer))
                {
                    continue;
                }

                if (!EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<PermanentlyRemovedSubElementPrefab> prefabMatches) || prefabMatches.Length == 0)
                {
                    continue;
                }

                NativeList<Entity> m_RemoveIfMatchingPrefabEntity = new NativeList<Entity>(Allocator.Temp);
                foreach (PermanentlyRemovedSubElementPrefab subElementPrefab in prefabMatches)
                {
                    if (EntityManager.TryGetComponent(subElementPrefab.m_RecordEntity, out PrefabRef prefabRef))
                    {
                        m_RemoveIfMatchingPrefabEntity.Add(prefabRef.m_Prefab);
                    }
                }

                foreach (Game.Net.SubLane subLane in subLaneBuffer)
                {
                    if (!EntityManager.TryGetComponent(subLane.m_SubLane, out PrefabRef prefabRef))
                    {
                        continue;
                    }

                    if (m_RemoveIfMatchingPrefabEntity.Contains(prefabRef.m_Prefab))
                    {
                        if (!EntityManager.HasComponent<DeleteInXFrames>(subLane.m_SubLane))
                        {
                            buffer.AddComponent<DeleteInXFrames>(subLane.m_SubLane);
                        }

                        buffer.SetComponent(subLane.m_SubLane, new DeleteInXFrames() { m_FramesRemaining = 5 });
                        m_Log.Debug($"{nameof(RemoveRegeneratedSubelementPrefabsSystem)}.{nameof(OnUpdate)} Will delete entity {subLane.m_SubLane.Index}.{subLane.m_SubLane.Version} from {entity.Index}.{entity.Version}");
                    }
                }
            }
        }

#if BURST
        [BurstCompile]
#endif
        private struct GatherSubObjectsJob : IJobChunk
        {
            [ReadOnly]
            public BufferTypeHandle<Game.Objects.SubObject> m_SubObjectType;
            [ReadOnly]
            public BufferTypeHandle<PermanentlyRemovedSubElementPrefab> m_PermanentlyRemovedSubElementPrefabType;
            public NativeList<Entity> m_SubObjects;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            [ReadOnly]
            public BufferLookup<Game.Objects.SubObject> m_SubObjectLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                BufferAccessor<Game.Objects.SubObject> subObjectBufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);
                BufferAccessor<PermanentlyRemovedSubElementPrefab> removedBufferAccessor = chunk.GetBufferAccessor(ref m_PermanentlyRemovedSubElementPrefabType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    DynamicBuffer<Game.Objects.SubObject> subObjectBuffer = subObjectBufferAccessor[i];
                    DynamicBuffer<PermanentlyRemovedSubElementPrefab> removedBuffer = removedBufferAccessor[i];

                    if (removedBuffer.Length == 0 || subObjectBuffer.Length == 0)
                    {
                        continue;
                    }

                    NativeList<Entity> prefabEntities = new NativeList<Entity>(Allocator.TempJob);
                    foreach (PermanentlyRemovedSubElementPrefab removedPrefab in removedBuffer)
                    {
                        prefabEntities.Add(removedPrefab.m_RecordEntity);
                    }

                    foreach (Game.Objects.SubObject subObject in subObjectBuffer)
                    {
                        if (!m_PrefabRefLookup.TryGetComponent(subObject.m_SubObject, out PrefabRef prefabRef) || !prefabEntities.Contains(prefabRef.m_Prefab))
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
            public BufferLookup<PermanentlyRemovedSubElementPrefab> m_PermanentlyRemovedSubElementPrefabLookup;
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
                    if (!m_PermanentlyRemovedSubElementPrefabLookup.TryGetBuffer(rentersUpdated.m_Property, out DynamicBuffer<PermanentlyRemovedSubElementPrefab> removedBuffer) || removedBuffer.Length == 0)
                    {
                        continue;
                    }

                    if (!m_SubObjectLookup.TryGetBuffer(rentersUpdated.m_Property, out DynamicBuffer<Game.Objects.SubObject> subObjectBuffer) || subObjectBuffer.Length == 0)
                    {
                        continue;
                    }

                    NativeList<Entity> prefabEntities = new NativeList<Entity>(Allocator.TempJob);
                    foreach (PermanentlyRemovedSubElementPrefab removedPrefab in removedBuffer)
                    {
                        prefabEntities.Add(removedPrefab.m_RecordEntity);
                    }

                    foreach (Game.Objects.SubObject subObject in subObjectBuffer)
                    {
                        if (!m_PrefabRefLookup.TryGetComponent(subObject.m_SubObject, out PrefabRef prefabRef) || !prefabEntities.Contains(prefabRef.m_Prefab))
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


#if BURST
        [BurstCompile]
#endif
        private struct GatherSubLanesJob : IJobChunk
        {
            [ReadOnly]
            public BufferTypeHandle<Game.Net.SubLane> m_SubLaneType;
            [ReadOnly]
            public BufferTypeHandle<PermanentlyRemovedSubElementPrefab> m_PermanentlyRemovedSubElementPrefabType;
            public NativeList<Entity> m_SubLanes;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                BufferAccessor<Game.Net.SubLane> subLaneBufferAccessor = chunk.GetBufferAccessor(ref m_SubLaneType);
                BufferAccessor<PermanentlyRemovedSubElementPrefab> removedBufferAccessor = chunk.GetBufferAccessor(ref m_PermanentlyRemovedSubElementPrefabType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    DynamicBuffer<Game.Net.SubLane> subLaneDynamicBuffer = subLaneBufferAccessor[i];
                    DynamicBuffer<PermanentlyRemovedSubElementPrefab> removedDynamicBuffer = removedBufferAccessor[i];

                    if (removedDynamicBuffer.Length == 0 || subLaneDynamicBuffer.Length == 0)
                    {
                        continue;
                    }

                    NativeList<Entity> prefabEntities = new NativeList<Entity>(Allocator.TempJob);
                    foreach (PermanentlyRemovedSubElementPrefab removedPrefab in removedDynamicBuffer)
                    {
                        prefabEntities.Add(removedPrefab.m_RecordEntity);
                    }

                    foreach (Game.Net.SubLane subLane in subLaneDynamicBuffer)
                    {
                        if (m_PrefabRefLookup.HasComponent(subLane.m_SubLane) && m_PrefabRefLookup.TryGetComponent(subLane.m_SubLane, out PrefabRef prefabRef) && prefabEntities.Contains(prefabRef.m_Prefab))
                        {
                            m_SubLanes.Add(subLane.m_SubLane);
                        }
                    }
                }
            }
        }

#if BURST
        [BurstCompile]
#endif
        private struct GatherSubLanesFromEventsJob : IJobChunk
        {
            [ReadOnly]
            public BufferLookup<Game.Net.SubLane> m_SubLaneLookup;
            [ReadOnly]
            public BufferLookup<PermanentlyRemovedSubElementPrefab> m_PermanentlyRemovedSubElementPrefabLookup;
            public NativeList<Entity> m_SubLanes;
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
                    if (!m_PermanentlyRemovedSubElementPrefabLookup.TryGetBuffer(rentersUpdated.m_Property, out DynamicBuffer<PermanentlyRemovedSubElementPrefab> removedBuffer) || removedBuffer.Length == 0)
                    {
                        continue;
                    }

                    if (!m_SubLaneLookup.TryGetBuffer(rentersUpdated.m_Property, out DynamicBuffer<Game.Net.SubLane> subLaneBuffer) || subLaneBuffer.Length == 0)
                    {
                        continue;
                    }

                    NativeList<Entity> prefabEntities = new NativeList<Entity>(Allocator.TempJob);
                    foreach (PermanentlyRemovedSubElementPrefab removedPrefab in removedBuffer)
                    {
                        prefabEntities.Add(removedPrefab.m_RecordEntity);
                    }

                    foreach (Game.Net.SubLane subObject in subLaneBuffer)
                    {
                        if (!m_PrefabRefLookup.TryGetComponent(subObject.m_SubLane, out PrefabRef prefabRef) || !prefabEntities.Contains(prefabRef.m_Prefab))
                        {
                            continue;
                        }
                    }
                }
            }
        }
    }
}
