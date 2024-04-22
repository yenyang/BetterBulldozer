// <copyright file="RemoveRegeneratedSubelementPrefabsSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Systems
{
    using Better_Bulldozer.Components;
    using Colossal.Entities;
    using Colossal.Logging;
    using Game;
    using Game.Buildings;
    using Game.Common;
    using Game.Prefabs;
    using Game.Tools;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Removes regenrated subelement prefabs that have been permananetly removed.
    /// </summary>
    public partial class RemoveRegeneratedSubelementPrefabsSystem : GameSystemBase
    {
        private EntityQuery m_PermanentlyRemovedSubObjectQuery;
        private EntityQuery m_PermanentlyRemovedSubLaneQuery;
        private ModificationEndBarrier m_ModificationEndBarrier;
        private PrefabSystem m_PrefabSystem;
        private ILog m_Log;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_ModificationEndBarrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_Log.Info($"{nameof(RemoveRegeneratedSubelementPrefabsSystem)}.{nameof(OnCreate)}");
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            m_PermanentlyRemovedSubObjectQuery = SystemAPI.QueryBuilder()
                .WithAll<PermanentlyRemovedSubElementPrefab, Updated>()
                .WithAny<Game.Objects.SubObject>()
                .WithNone<Temp, Deleted>()
                .Build();

            m_PermanentlyRemovedSubLaneQuery = SystemAPI.QueryBuilder()
                .WithAll<PermanentlyRemovedSubElementPrefab, Updated>()
                .WithAny<Game.Net.SubLane>()
                .WithNone<Temp, Deleted>()
                .Build();

            RequireAnyForUpdate(m_PermanentlyRemovedSubObjectQuery, m_PermanentlyRemovedSubLaneQuery);

            m_Log.Debug($"{nameof(RemoveRegeneratedSubelementPrefabsSystem)}.{nameof(OnUpdate)}");
            EntityCommandBuffer buffer = m_ModificationEndBarrier.CreateCommandBuffer();

            if (!m_PermanentlyRemovedSubObjectQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entitiesWithSubObjects = m_PermanentlyRemovedSubObjectQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in entitiesWithSubObjects)
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
                        m_RemoveIfMatchingPrefabEntity.Add(subElementPrefab.m_PrefabEntity);
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

            if (!m_PermanentlyRemovedSubLaneQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entitiesWithSublanes = m_PermanentlyRemovedSubLaneQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in entitiesWithSublanes)
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
                        m_RemoveIfMatchingPrefabEntity.Add(subElementPrefab.m_PrefabEntity);
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
        }

    }
}
