// <copyright file="RemoveVehiclesCimsAndAnimalsTool.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

// #define VERBOSE
#define BURST
namespace Better_Bulldozer.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Better_Bulldozer.Systems;
    using Colossal.Annotations;
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Citizens;
    using Game.Common;
    using Game.Creatures;
    using Game.Input;
    using Game.Net;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Game.Vehicles;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.InputSystem;


    /// <summary>
    /// Tool for controlling removing vehicles cims and animals.
    /// </summary>
    public partial class RemoveVehiclesCimsAndAnimalsTool : ToolBaseSystem
    {
        private ProxyAction m_ApplyAction;
        private OverlayRenderSystem m_OverlayRenderSystem;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private BulldozeToolSystem m_BulldozeToolSystem;
        private EntityQuery m_MovingObjectsQuery;
        private EntityQuery m_ParkedObjectsQuery;
        private ILog m_Log;
        private BetterBulldozerUISystem m_BetterBulldozerUISystem;

        /// <inheritdoc/>
        public override string toolID => m_BulldozeToolSystem.toolID; // This is hack to get the UI use bulldoze cursor and bulldoze bar.

        /// <inheritdoc/>
        public override PrefabBase GetPrefab()
        {
            return null;
        }

        /// <inheritdoc/>
        public override bool TrySetPrefab(PrefabBase prefab)
        {
            return false;
        }

        /// <inheritdoc/>
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            m_ToolRaycastSystem.typeMask = TypeMask.Terrain;
        }

        /// <summary>
        /// For stopping the tool. Probably with esc key.
        /// </summary>
        public void RequestDisable()
        {
            m_ToolSystem.activeTool = m_DefaultToolSystem;
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            Enabled = false;
            m_Log = BetterBulldozerMod.Instance.Logger;
            m_Log.Info($"[{nameof(RemoveVehiclesCimsAndAnimalsTool)}] {nameof(OnCreate)}");
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            m_BulldozeToolSystem = World.GetOrCreateSystemManaged<BulldozeToolSystem>();
            m_BetterBulldozerUISystem = World.GetOrCreateSystemManaged<BetterBulldozerUISystem>();
            base.OnCreate();

            m_MovingObjectsQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<InterpolatedTransform>(),
                    },
                    Any = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Vehicle>(),
                        ComponentType.ReadOnly<Animal>(),
                        ComponentType.ReadOnly<Human>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                    },
                },
            });

            m_ParkedObjectsQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Game.Objects.Transform>(),
                    },
                    Any = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Vehicle>(),
                        ComponentType.ReadOnly<Animal>(),
                        ComponentType.ReadOnly<Human>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<InterpolatedTransform>(),
                    },
                },
            });

            RequireForUpdate(m_MovingObjectsQuery);

            m_ApplyAction = BetterBulldozerMod.Instance.Settings.GetAction(BetterBulldozerMod.ApplyMimicAction);
            var builtInApplyAction = InputManager.instance.FindAction(InputManager.kToolMap, "Apply");
            var mimicApplyBinding = m_ApplyAction.bindings.FirstOrDefault(b => b.group == nameof(Mouse));
            var builtInApplyBinding = builtInApplyAction.bindings.FirstOrDefault(b => b.group == nameof(Mouse));
            var applyWatcher = new ProxyBinding.Watcher(builtInApplyBinding, binding => SetMimic(mimicApplyBinding, binding));
            SetMimic(mimicApplyBinding, applyWatcher.binding);
        }

        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            m_ApplyAction.shouldBeEnabled = true;
            m_Log.Debug($"{nameof(RemoveVehiclesCimsAndAnimalsTool)}.{nameof(OnStartRunning)}");
        }

        /// <inheritdoc/>
        protected override void OnStopRunning()
        {
            m_ApplyAction.shouldBeEnabled = false;
        }

        /// <inheritdoc/>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = Dependency;
            bool raycastFlag = GetRaycastResult(out Entity e, out RaycastHit hit);

            if (hit.m_HitPosition.x == 0 && hit.m_HitPosition.y == 0 && hit.m_HitPosition.z == 0)
            {
                return inputDeps;
            }

            float radius = m_BetterBulldozerUISystem.SelectionRadius;
            ToolRadiusJob toolRadiusJob = new ()
            {
                m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out JobHandle outJobHandle),
                m_Position = new Vector3(hit.m_HitPosition.x, hit.m_Position.y, hit.m_HitPosition.z),
                m_Radius = radius,
            };
            inputDeps = IJobExtensions.Schedule(toolRadiusJob, JobHandle.CombineDependencies(inputDeps, outJobHandle));
            m_OverlayRenderSystem.AddBufferWriter(inputDeps);

            if (m_ApplyAction.IsPressed())
            {
                    RemoveVehiclesCimsAndAnimalsWithRadius removeVCAWithinRadiusJob = new ()
                    {
                        m_EntityType = SystemAPI.GetEntityTypeHandle(),
                        m_Position = hit.m_HitPosition,
                        m_Radius = radius,
                        m_InterpolatedTransformType = SystemAPI.GetComponentTypeHandle<InterpolatedTransform>(isReadOnly: true),
                        buffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                    };
                    inputDeps = JobChunkExtensions.Schedule(removeVCAWithinRadiusJob, m_MovingObjectsQuery, inputDeps);
                    m_ToolOutputBarrier.AddJobHandleForProducer(inputDeps);

                    RemoveStationaryVehiclesCimsAndAnimalsWithRadius removeStationaryVCAwithinRadiusJob = new()
                    {
                        m_EntityType = SystemAPI.GetEntityTypeHandle(),
                        m_Position = hit.m_HitPosition,
                        m_Radius = radius,
                        m_TransformType = SystemAPI.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true),
                        buffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                    };
                    inputDeps = JobChunkExtensions.Schedule(removeStationaryVCAwithinRadiusJob, m_ParkedObjectsQuery, inputDeps);
                    m_ToolOutputBarrier.AddJobHandleForProducer(inputDeps);
            }

            return inputDeps;
        }

        /// <inheritdoc/>
        protected override void OnGameLoaded(Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);
        }

        /// <inheritdoc/>
        protected override void OnGamePreload(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void SetMimic(ProxyBinding mimic, ProxyBinding buildIn)
        {
            var newMimicBinding = mimic.Copy();
            newMimicBinding.path = buildIn.path;
            newMimicBinding.modifiers = buildIn.modifiers;
            InputManager.instance.SetBinding(newMimicBinding, out _);
        }

#if BURST
        [BurstCompile]
#endif
        private struct RemoveVehiclesCimsAndAnimalsWithRadius : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<InterpolatedTransform> m_InterpolatedTransformType;
            public EntityCommandBuffer buffer;
            public float m_Radius;
            public float3 m_Position;

            /// <summary>
            /// Executes job which will change state or prefab for trees within a radius.
            /// </summary>
            /// <param name="chunk">ArchteypeChunk of IJobChunk.</param>
            /// <param name="unfilteredChunkIndex">Use for EntityCommandBuffer.ParralelWriter.</param>
            /// <param name="useEnabledMask">Part of IJobChunk. Unsure what it does.</param>
            /// <param name="chunkEnabledMask">Part of IJobChunk. Not sure what it does.</param>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<InterpolatedTransform> interpolatedTransformNativeArray = chunk.GetNativeArray(ref m_InterpolatedTransformType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (CheckForWithinRadius(m_Position, interpolatedTransformNativeArray[i].m_Position, m_Radius))
                    {
                        Entity currentEntity = entityNativeArray[i];
                        buffer.AddComponent<Deleted>(currentEntity);
                    }
                }
            }

            /// <summary>
            /// Checks the radius and position and returns true if tree is there.
            /// </summary>
            /// <param name="cursorPosition">Float3 from Raycast.</param>
            /// <param name="position">Float3 position from InterploatedTransform.</param>
            /// <param name="radius">Radius usually passed from settings.</param>
            /// <returns>True if tree position is within radius of position. False if not.</returns>
            private bool CheckForWithinRadius(float3 cursorPosition, float3 position, float radius)
            {
                float minRadius = 10f;
                radius = Mathf.Max(radius, minRadius);
                position.y = cursorPosition.y;
                if (Unity.Mathematics.math.distance(cursorPosition, position) < radius)
                {
                    return true;
                }

                return false;
            }
        }

#if BURST
        [BurstCompile]
#endif
        private struct RemoveStationaryVehiclesCimsAndAnimalsWithRadius : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;
            public EntityCommandBuffer buffer;
            public float m_Radius;
            public float3 m_Position;

            /// <summary>
            /// Executes job which will change state or prefab for trees within a radius.
            /// </summary>
            /// <param name="chunk">ArchteypeChunk of IJobChunk.</param>
            /// <param name="unfilteredChunkIndex">Use for EntityCommandBuffer.ParralelWriter.</param>
            /// <param name="useEnabledMask">Part of IJobChunk. Unsure what it does.</param>
            /// <param name="chunkEnabledMask">Part of IJobChunk. Not sure what it does.</param>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Game.Objects.Transform> transformNativeArray = chunk.GetNativeArray(ref m_TransformType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (CheckForWithinRadius(m_Position, transformNativeArray[i].m_Position, m_Radius))
                    {
                        Entity currentEntity = entityNativeArray[i];
                        buffer.AddComponent<Deleted>(currentEntity);
                    }
                }
            }

            /// <summary>
            /// Checks the radius and position and returns true if tree is there.
            /// </summary>
            /// <param name="cursorPosition">Float3 from Raycast.</param>
            /// <param name="position">Float3 position from InterploatedTransform.</param>
            /// <param name="radius">Radius usually passed from settings.</param>
            /// <returns>True if tree position is within radius of position. False if not.</returns>
            private bool CheckForWithinRadius(float3 cursorPosition, float3 position, float radius)
            {
                float minRadius = 10f;
                radius = Mathf.Max(radius, minRadius);
                position.y = cursorPosition.y;
                if (Unity.Mathematics.math.distance(cursorPosition, position) < radius)
                {
                    return true;
                }

                return false;
            }
        }

#if BURST
        [BurstCompile]
#endif
        private struct ToolRadiusJob : IJob
        {
            public OverlayRenderSystem.Buffer m_OverlayBuffer;
            public float3 m_Position;
            public float m_Radius;

            /// <summary>
            /// Draws tool radius.
            /// </summary>
            public void Execute()
            {
                m_OverlayBuffer.DrawCircle(new UnityEngine.Color(.52f, .80f, .86f, 1f), default, m_Radius / 20f, 0, new float2(0, 1), m_Position, m_Radius * 2f);
            }
        }
    }
}
