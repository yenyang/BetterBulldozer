// <copyright file="AutomaticallyRemoveBrandingObjects.cs" company="Yenyang's Mods. MIT License">
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
                }, new EntityQueryDesc
                {
                    All = new ComponentType[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<RentersUpdated>() },
                    None = new ComponentType[] { ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Deleted>() },
                });
            RequireForUpdate(m_UpdateQuery);
            m_Barrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();
            base.OnCreate();
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
            }
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            m_BrandObjectPrefabQuery = SystemAPI.QueryBuilder()
                .WithAll<BrandObjectData>()
                .Build();

            if (!m_ToolSystem.actionMode.IsGame())
            {
                Enabled = false;
                return;
            }

            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();
            NativeList<Entity> brandingObjectPrefabsEntities = m_BrandObjectPrefabQuery.ToEntityListAsync(Allocator.Temp, out JobHandle brandingObjectJobHandle);
            brandingObjectJobHandle.Complete();

            if (!m_UpdateQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entities = m_UpdateQuery.ToEntityArray(Allocator.Temp);
                ProcessEntities(entities, brandingObjectPrefabsEntities, ref buffer);
                entities.Dispose();
            }

            if (!m_UpdateQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> eventEntities = m_UpdateQuery.ToEntityArray(Allocator.Temp);
                NativeList<Entity> actualEntitiesList = new NativeList<Entity>(Allocator.Temp);
                foreach (Entity entity in eventEntities)
                {
                    if (EntityManager.TryGetComponent(entity, out RentersUpdated rentersUpdated))
                    {
                        m_Log.Debug($"rentersUpdated includes {rentersUpdated.m_Property.Index}:{rentersUpdated.m_Property.Version}");
                        actualEntitiesList.Add(rentersUpdated.m_Property);
                        continue;
                    }

                    if (EntityManager.TryGetComponent(entity, out SubObjectsUpdated subObjectsUpdated))
                    {
                        actualEntitiesList.Add(subObjectsUpdated.m_Owner);
                    }
                }

                ProcessEntities(actualEntitiesList.AsArray(), brandingObjectPrefabsEntities, ref buffer);
                eventEntities.Dispose();
                actualEntitiesList.Dispose();
            }

            brandingObjectPrefabsEntities.Dispose();
        }

        private void ProcessEntities(NativeArray<Entity> entities, NativeList<Entity> prefabEntities, ref EntityCommandBuffer buffer)
        {
            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetBuffer(entity, isReadOnly: false, out DynamicBuffer<Game.Objects.SubObject> dynamicBuffer))
                {
                    continue;
                }

                foreach (Game.Objects.SubObject subObject in dynamicBuffer)
                {
                    if (!EntityManager.TryGetComponent(subObject.m_SubObject, out PrefabRef prefabRef))
                    {
                        continue;
                    }

                    if (prefabEntities.Contains(prefabRef.m_Prefab))
                    {
                        if (!EntityManager.HasComponent<DeleteInXFrames>(subObject.m_SubObject))
                        {
                            buffer.AddComponent<DeleteInXFrames>(subObject.m_SubObject);
                        }

                        buffer.SetComponent(subObject.m_SubObject, new DeleteInXFrames() { m_FramesRemaining = 5 });
                    }

                    if (!EntityManager.TryGetBuffer(subObject.m_SubObject, isReadOnly: false, out DynamicBuffer<Game.Objects.SubObject> deepDynamicBuffer))
                    {
                        continue;
                    }

                    foreach (Game.Objects.SubObject deepSubObject in dynamicBuffer)
                    {
                        if (!EntityManager.TryGetComponent(deepSubObject.m_SubObject, out PrefabRef deepPrefabRef))
                        {
                            continue;
                        }

                        if (prefabEntities.Contains(deepPrefabRef.m_Prefab))
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
}
