﻿// <copyright file="RemoveRegeneratedSubelementPrefabsSystem.cs" company="Yenyang's Mods. MIT License">
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
    using Game.Objects;
    using Game.Prefabs;
    using Game.Tools;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using UnityEngine;

    /// <summary>
    /// Removes regenrated subelement prefabs that have been permananetly removed.
    /// </summary>
    public partial class RemoveRegeneratedSubelementPrefabsSystem : GameSystemBase
    {
        private EntityQuery m_PermanentlyRemovedSubObjectQuery;
        private EntityQuery m_PermanentlyRemovedSubLaneQuery;
        private EntityQuery m_RentersUpdatedQuery;
        private ModificationEndBarrier m_Barrier;
        private ToolSystem m_ToolSystem;
        private PrefabSystem m_PrefabSystem;
        private ILog m_Log;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_Barrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();
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
                        ComponentType.ReadOnly<Game.Common.Event>(),
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
            NativeList<Entity> subElements = new NativeList<Entity>(Allocator.TempJob);

            GatherSubObjectsJob gatherSubObjectsJob = new GatherSubObjectsJob()
            {
                m_PermanentlyRemovedSubElementPrefabType = SystemAPI.GetBufferTypeHandle<PermanentlyRemovedSubElementPrefab>(isReadOnly: true),
                m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                m_SubObjectLookup = SystemAPI.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true),
                m_SubObjects = subElements,
                m_SubObjectType = SystemAPI.GetBufferTypeHandle<Game.Objects.SubObject>(isReadOnly: true),
            };

            Dependency = gatherSubObjectsJob.Schedule(m_PermanentlyRemovedSubObjectQuery, Dependency);

            GatherSubObjectsFromEventsJob gatherSubObjectsFromEventsJob = new GatherSubObjectsFromEventsJob()
            {
                m_PermanentlyRemovedSubElementPrefabLookup = SystemAPI.GetBufferLookup<PermanentlyRemovedSubElementPrefab>(isReadOnly: true),
                m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                m_RentersUpdatedType = SystemAPI.GetComponentTypeHandle<RentersUpdated>(isReadOnly: true),
                m_SubObjectLookup = SystemAPI.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true),
                m_SubObjects = subElements,
            };

            Dependency = gatherSubObjectsFromEventsJob.Schedule(m_RentersUpdatedQuery, Dependency);

            GatherSubLanesJob gatherSubLanesJob = new GatherSubLanesJob()
            {
                m_PermanentlyRemovedSubElementPrefabType = SystemAPI.GetBufferTypeHandle<PermanentlyRemovedSubElementPrefab>(isReadOnly: true),
                m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                m_SubLanes = subElements,
                m_SubLaneType = SystemAPI.GetBufferTypeHandle<Game.Net.SubLane>(isReadOnly: true),
            };

            Dependency = gatherSubLanesJob.Schedule(m_PermanentlyRemovedSubLaneQuery, Dependency);

            GatherSubLanesFromEventsJob gatherSubLanesFromEventsJob = new GatherSubLanesFromEventsJob()
            {
                m_PermanentlyRemovedSubElementPrefabLookup = SystemAPI.GetBufferLookup<PermanentlyRemovedSubElementPrefab>(isReadOnly: true),
                m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                m_RentersUpdatedType = SystemAPI.GetComponentTypeHandle<RentersUpdated>(isReadOnly: true),
                m_SubLaneLookup = SystemAPI.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true),
                m_SubLanes = subElements,
            };

            Dependency = gatherSubLanesFromEventsJob.Schedule(m_RentersUpdatedQuery, Dependency);

            HandleDeleteInXFramesJob handleDeleteInXFramesJob = new HandleDeleteInXFramesJob()
            {
                m_DeleteInXFramesLookup = SystemAPI.GetComponentLookup<DeleteInXFrames>(isReadOnly: true),
                m_TransformLookup = SystemAPI.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true),
                m_Entities = subElements,
                buffer = m_Barrier.CreateCommandBuffer(),
                m_PillarLookup = SystemAPI.GetComponentLookup<Game.Objects.Pillar>(isReadOnly: true),
            };

            JobHandle handleDeleteInXFramesJobHandle = handleDeleteInXFramesJob.Schedule(Dependency);
            m_Barrier.AddJobHandleForProducer(handleDeleteInXFramesJobHandle);
            Dependency = handleDeleteInXFramesJobHandle;

            subElements.Dispose(Dependency);
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
                        if (!m_PrefabRefLookup.TryGetComponent(removedPrefab.m_RecordEntity, out PrefabRef prefabRef))
                        {
                            continue;
                        }

                        prefabEntities.Add(prefabRef.m_Prefab);
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

                    prefabEntities.Dispose();
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
                        if (!m_PrefabRefLookup.TryGetComponent(removedPrefab.m_RecordEntity, out PrefabRef prefabRef))
                        {
                            continue;
                        }

                        prefabEntities.Add(prefabRef.m_Prefab);
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

                    prefabEntities.Dispose();
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
            [ReadOnly]
            public ComponentLookup<Pillar> m_PillarLookup;
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

                    if (!m_PillarLookup.HasComponent(entity) && m_TransformLookup.HasComponent(entity) && m_TransformLookup.TryGetComponent(entity, out Game.Objects.Transform transform) && transform.m_Position.y > 0)
                    {
                        transform.m_Position.y = 0;
                        buffer.SetComponent(entity, transform);
                        buffer.AddComponent<Updated>(entity);
                    }
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
                        if (!m_PrefabRefLookup.TryGetComponent(removedPrefab.m_RecordEntity, out PrefabRef prefabRef))
                        {
                            continue;
                        }

                        prefabEntities.Add(prefabRef.m_Prefab);
                    }

                    foreach (Game.Net.SubLane subLane in subLaneDynamicBuffer)
                    {
                        if (m_PrefabRefLookup.HasComponent(subLane.m_SubLane) && m_PrefabRefLookup.TryGetComponent(subLane.m_SubLane, out PrefabRef prefabRef) && prefabEntities.Contains(prefabRef.m_Prefab))
                        {
                            m_SubLanes.Add(subLane.m_SubLane);
                        }
                    }

                    prefabEntities.Dispose();
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
                        if (!m_PrefabRefLookup.TryGetComponent(removedPrefab.m_RecordEntity, out PrefabRef prefabRef))
                        {
                            continue;
                        }

                        prefabEntities.Add(prefabRef.m_Prefab);
                    }

                    foreach (Game.Net.SubLane subObject in subLaneBuffer)
                    {
                        if (!m_PrefabRefLookup.TryGetComponent(subObject.m_SubLane, out PrefabRef prefabRef) || !prefabEntities.Contains(prefabRef.m_Prefab))
                        {
                            continue;
                        }
                    }

                    prefabEntities.Dispose();
                }
            }
        }
    }
}
