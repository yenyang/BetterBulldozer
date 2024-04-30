// <copyright file="AutomaticallyRemoveManicuredGrassSurface.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Systems
{
    using System.Collections.Generic;
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Tools;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// A system that automatically removes grass surfaces from created buildings.
    /// </summary>
    public partial class AutomaticallyRemoveManicuredGrassSurfaceSystem : GameSystemBase
    {
        private readonly List<PrefabID> m_GrassSurfacePrefabIDs = new ()
        {
            new ("SurfacePrefab", "Grass Surface 01"),
            new ("SurfacePrefab", "Grass Surface 02"),
        };

        private ILog m_Log;
        private EntityQuery m_CreationDefinitionQuery;
        private NativeList<Entity> m_GrassSurfacePrefabEntities;
        private ToolSystem m_ToolSystem;
        private AreaToolSystem m_AreaToolSystem;
        private PrefabSystem m_PrefabSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomaticallyRemoveManicuredGrassSurfaceSystem"/> class.
        /// </summary>
        public AutomaticallyRemoveManicuredGrassSurfaceSystem()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_Log.Info($"{nameof(AutomaticallyRemoveManicuredGrassSurfaceSystem)}.{nameof(OnCreate)}.");
            m_ToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            m_AreaToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<AreaToolSystem>();
            m_PrefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            m_CreationDefinitionQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<CreationDefinition>(),
                        ComponentType.ReadOnly<Updated>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Overridden>(),
                    },
                },
            });
            m_GrassSurfacePrefabEntities = new NativeList<Entity>(m_GrassSurfacePrefabIDs.Count, Allocator.Persistent);
            RequireForUpdate(m_CreationDefinitionQuery);
            base.OnCreate();
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            if (!m_GrassSurfacePrefabEntities.IsEmpty)
            {
                return;
            }

            foreach (PrefabID prefabID in m_GrassSurfacePrefabIDs)
            {
                if (m_PrefabSystem.TryGetPrefab(prefabID, out PrefabBase prefab) && prefab != null)
                {
                    if (m_PrefabSystem.TryGetEntity(prefab, out Entity entity))
                    {
                        if (entity != Entity.Null)
                        {
                            m_GrassSurfacePrefabEntities.Add(entity);

                            // m_Log.Debug($"{nameof(AutomaticallyRemoveManicuredGrassSurfaceSystem)}.{nameof(OnGameLoadingComplete)} added entity {entity.Index}:{entity.Version}");
                        }
                    }
                }
            }

            Enabled = BetterBulldozerMod.Instance.Settings.AutomaticRemovalManicuredGrass;
            base.OnGameLoadingComplete(purpose, mode);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (m_GrassSurfacePrefabEntities.IsEmpty)
            {
                m_Log.Warn($"{nameof(AutomaticallyRemoveManicuredGrassSurfaceSystem)}.{nameof(OnUpdate)} m_GrassSurfacePrefabEntities.IsEmpty");
                return;
            }

            if (m_ToolSystem.activePrefab != null && m_ToolSystem.activeTool == m_AreaToolSystem && m_PrefabSystem.TryGetEntity(m_ToolSystem.activePrefab, out Entity activePrefabEntity))
            {
                if (m_GrassSurfacePrefabEntities.Contains(activePrefabEntity))
                {
                    m_Log.Debug($"{nameof(AutomaticallyRemoveManicuredGrassSurfaceSystem)}.{nameof(OnUpdate)} Actively drawing manicured grass with area tool.");
                    return;
                }
            }

            NativeArray<Entity> entities = m_CreationDefinitionQuery.ToEntityArray(Allocator.Temp);

            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetComponent(entity, out CreationDefinition currentCreationDefinition))
                {
                    entities.Dispose();
                    m_Log.Warn($"{nameof(AutomaticallyRemoveManicuredGrassSurfaceSystem)}.{nameof(OnUpdate)} couldn't find current creation definition.");
                    return;
                }

                if (m_GrassSurfacePrefabEntities.Contains(currentCreationDefinition.m_Prefab) && m_ToolSystem.activeTool != m_AreaToolSystem)
                {
                    // m_Log.Debug($"{nameof(AutomaticallyRemoveManicuredGrassSurfaceSystem)}.{nameof(OnUpdate)} found creation data.");
                    EntityManager.DestroyEntity(entity);
                }
            }

            entities.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            m_GrassSurfacePrefabEntities.Dispose();
            base.OnDestroy();
        }
    }
}
