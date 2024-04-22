// <copyright file="AutomaticallyRemoveFencesAndHedges.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Systems
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Net;
    using Game.Prefabs;
    using Game.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// A system that automatically removes fences and hedges from created buildings.
    /// </summary>
    public partial class AutomaticallyRemoveFencesAndHedges : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_TempHedgesQuery;
        private EntityQuery m_FencePrefabEntities;
        private EntityQuery m_HedgePrefabEntities;
        private PrefabSystem m_PrefabSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomaticallyRemoveFencesAndHedges"/> class.
        /// </summary>
        public AutomaticallyRemoveFencesAndHedges()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_Log.Info($"{nameof(AutomaticallyRemoveFencesAndHedges)}.{nameof(OnCreate)}.");
            m_PrefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            m_TempHedgesQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Temp>(),
                        ComponentType.ReadOnly<Game.Net.UtilityLane>(),
                        ComponentType.ReadOnly<Lane>(),
                        ComponentType.ReadOnly<Game.Objects.Plant>(),
                        ComponentType.ReadOnly<PrefabRef>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Overridden>(),
                    },
                },
            });
            m_FencePrefabEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<NetLaneData>(),
                        ComponentType.ReadOnly<UtilityLaneData>(),
                        ComponentType.ReadOnly<SpawnableObjectData>(),
                        ComponentType.ReadOnly<NetLaneGeometryData>(),
                        ComponentType.ReadOnly<NetLaneArchetypeData>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<PlantData>(),
                    },
                },
            });
            m_HedgePrefabEntities = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<NetLaneData>(),
                        ComponentType.ReadOnly<UtilityLaneData>(),
                        ComponentType.ReadOnly<SpawnableObjectData>(),
                        ComponentType.ReadOnly<NetLaneGeometryData>(),
                        ComponentType.ReadOnly<NetLaneArchetypeData>(),
                        ComponentType.ReadOnly<PlantData>(),
                    },
                },
            });

            RequireForUpdate(m_TempHedgesQuery);
            base.OnCreate();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = m_TempHedgesQuery.ToEntityArray(Allocator.Temp);
            NativeList<Entity> fencePrefabEntities = m_FencePrefabEntities.ToEntityListAsync(Allocator.Temp, out JobHandle fencePrefabJobHandle);
            NativeList<Entity> hedgePrefabEntities = m_HedgePrefabEntities.ToEntityListAsync(Allocator.Temp, out JobHandle hedgePrefabJobHandle);
            fencePrefabJobHandle.Complete();
            hedgePrefabJobHandle.Complete();

            m_Log.Debug($"{nameof(AutomaticallyRemoveFencesAndHedges)}.{nameof(OnUpdate)}.");

            foreach (Entity fenceEntity in fencePrefabEntities)
            {
                if (m_PrefabSystem.TryGetPrefab(fenceEntity, out PrefabBase prefabBase))
                {
                    m_Log.Debug($"{nameof(AutomaticallyRemoveFencesAndHedges)}.{nameof(OnUpdate)} Fence Prefab {prefabBase.name}.");
                }
            }

            foreach (Entity hedgeEntity in hedgePrefabEntities)
            {
                if (m_PrefabSystem.TryGetPrefab(hedgeEntity, out PrefabBase prefabBase))
                {
                    m_Log.Debug($"{nameof(AutomaticallyRemoveFencesAndHedges)}.{nameof(OnUpdate)} Hedge Prefab {prefabBase.name}.");
                }
            }

            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetComponent(entity, out PrefabRef currentPrefabRef))
                {
                    entities.Dispose();
                    m_Log.Warn($"{nameof(AutomaticallyRemoveFencesAndHedges)}.{nameof(OnUpdate)} couldn't find current creation definition.");
                    return;
                }

                if (fencePrefabEntities.Contains(currentPrefabRef.m_Prefab) || hedgePrefabEntities.Contains(currentPrefabRef.m_Prefab))
                {
                    m_Log.Debug($"{nameof(AutomaticallyRemoveFencesAndHedges)}.{nameof(OnUpdate)} found creation data.");
                    EntityManager.DestroyEntity(entity);
                }

                if (m_PrefabSystem.TryGetPrefab(currentPrefabRef.m_Prefab, out PrefabBase prefabBase))
                {
                    m_Log.Debug($"{nameof(AutomaticallyRemoveFencesAndHedges)}.{nameof(OnUpdate)} Prefab {prefabBase.name}.");
                }
            }

            fencePrefabEntities.Dispose();
            hedgePrefabEntities.Dispose();
            entities.Dispose();
        }
    }
}
