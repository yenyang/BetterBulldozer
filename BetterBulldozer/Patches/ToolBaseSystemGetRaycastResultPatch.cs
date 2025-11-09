// <copyright file="ToolBaseSystemGetRaycastResultPatch.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Patches
{
    using System;
    using Better_Bulldozer.Systems;
    using Colossal.Entities;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Tools;
    using HarmonyLib;
    using Unity.Entities;

    /// <summary>
    /// Patches ToolBaseSystem GetRaycastResult to alter raycast results.
    /// </summary>
    [HarmonyPatch(typeof(ToolBaseSystem), "GetRaycastResult", new Type[] { typeof(Entity), typeof(RaycastHit) }, new ArgumentType[] { ArgumentType.Out, ArgumentType.Out })]

    public class ToolBaseSystemGetRaycastResultPatch
    {
        /// <summary>
        /// Patches ToolBaseSystem GetRaycastResult to alter raycast results.
        /// </summary>
        /// <param name="entity">Entity that is being raycasted.</param>
        /// <param name="hit">The resulting raycast hit.</param>
        /// <returns>True is not actually patching method. False if patching method.</returns>
        public static bool Prefix(out Entity entity, out RaycastHit hit)
        {
            BetterBulldozerUISystem betterBulldozerUISystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<BetterBulldozerUISystem>();
            if (betterBulldozerUISystem.SelectedRaycastTarget != BetterBulldozerUISystem.RaycastTarget.Lanes && betterBulldozerUISystem.SelectedRaycastTarget != BetterBulldozerUISystem.RaycastTarget.Vanilla)
            {
                entity = Entity.Null;
                hit = default;
                return true;
            }

            ToolSystem toolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            BulldozeToolSystem bulldozeToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<BulldozeToolSystem>();
            if (toolSystem.activeTool != bulldozeToolSystem || !toolSystem.actionMode.IsGame())
            {
                entity = Entity.Null;
                hit = default;
                return true;
            }

            ToolRaycastSystem toolRaycastSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolRaycastSystem>();
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            bool raycastHitSomething = toolRaycastSystem.GetRaycastResult(out var result);

            if (raycastHitSomething
                && !toolSystem.EntityManager.HasComponent<Deleted>(result.m_Owner)
                && !toolSystem.EntityManager.HasComponent<Game.Tools.EditorContainer>(result.m_Hit.m_HitEntity)
                && betterBulldozerUISystem.SelectedRaycastTarget == BetterBulldozerUISystem.RaycastTarget.Lanes)
            {
                entity = Entity.Null;
                hit = default;
                BetterBulldozerMod.Instance.Logger.Debug($"{nameof(ToolBaseSystemGetRaycastResultPatch)}.{nameof(Prefix)} skipped method.");
                return false;
            }

            if (raycastHitSomething
                && !toolSystem.EntityManager.HasComponent<Deleted>(result.m_Owner)
                && betterBulldozerUISystem.SelectedRaycastTarget == BetterBulldozerUISystem.RaycastTarget.Vanilla)
            {
                if (((betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Buildings) != BetterBulldozerUISystem.VanillaFilters.Buildings
                    && (toolSystem.EntityManager.HasComponent<Game.Buildings.Building>(result.m_Hit.m_HitEntity)
                    || toolSystem.EntityManager.HasComponent<Game.Buildings.Building>(result.m_Owner)))
                    || ((betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Trees) != BetterBulldozerUISystem.VanillaFilters.Trees
                    && toolSystem.EntityManager.HasComponent<Game.Objects.Tree>(result.m_Hit.m_HitEntity))
                    || ((betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Plants) != BetterBulldozerUISystem.VanillaFilters.Plants
                    && toolSystem.EntityManager.HasComponent<Game.Objects.Plant>(result.m_Hit.m_HitEntity)
                    && !toolSystem.EntityManager.HasComponent<Game.Objects.Tree>(result.m_Hit.m_HitEntity))
                    || ((betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Decals) != BetterBulldozerUISystem.VanillaFilters.Decals
                    && toolSystem.EntityManager.TryGetComponent(result.m_Hit.m_HitEntity, out PrefabRef prefabRef)
                    && toolSystem.EntityManager.TryGetBuffer(prefabRef, isReadOnly: true, out DynamicBuffer<SubMesh> submeshes)
                    && submeshes.Length > 0 && toolSystem.EntityManager.TryGetComponent(submeshes[0].m_SubMesh, out MeshData meshData)
                    && (meshData.m_State & MeshFlags.Decal) == MeshFlags.Decal))
                {
                    entity = Entity.Null;
                    hit = default;
                    BetterBulldozerMod.Instance.Logger.Debug($"{nameof(ToolBaseSystemGetRaycastResultPatch)}.{nameof(Prefix)} skipped method.");
                    return false;
                }

                if ((betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Props) != BetterBulldozerUISystem.VanillaFilters.Props &&
                   ((toolSystem.EntityManager.HasComponent<Game.Objects.Object>(result.m_Hit.m_HitEntity) &&
                     toolSystem.EntityManager.HasComponent<Game.Objects.Static>(result.m_Hit.m_HitEntity)) ||
                    (toolSystem.EntityManager.HasComponent<Game.Objects.Object>(result.m_Owner) &&
                     toolSystem.EntityManager.HasComponent<Game.Objects.Static>(result.m_Owner))))
                {
                    if (((betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Buildings) == BetterBulldozerUISystem.VanillaFilters.Buildings
                    && (toolSystem.EntityManager.HasComponent<Game.Buildings.Building>(result.m_Hit.m_HitEntity)
                    || toolSystem.EntityManager.HasComponent<Game.Buildings.Building>(result.m_Owner)))
                    || ((betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Trees) == BetterBulldozerUISystem.VanillaFilters.Trees
                    && toolSystem.EntityManager.HasComponent<Game.Objects.Tree>(result.m_Hit.m_HitEntity))
                    || ((betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Plants) == BetterBulldozerUISystem.VanillaFilters.Plants
                    && toolSystem.EntityManager.HasComponent<Game.Objects.Plant>(result.m_Hit.m_HitEntity)
                    && !toolSystem.EntityManager.HasComponent<Game.Objects.Tree>(result.m_Hit.m_HitEntity))
                    || ((betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Decals) == BetterBulldozerUISystem.VanillaFilters.Decals
                    && toolSystem.EntityManager.TryGetComponent(result.m_Hit.m_HitEntity, out PrefabRef prefabRef1)
                    && toolSystem.EntityManager.TryGetBuffer(prefabRef1, isReadOnly: true, out DynamicBuffer<SubMesh> submeshes1)
                    && submeshes1.Length > 0 && toolSystem.EntityManager.TryGetComponent(submeshes1[0].m_SubMesh, out MeshData meshData1)
                    && (meshData1.m_State & MeshFlags.Decal) == MeshFlags.Decal))
                    {
                        entity = result.m_Owner;
                        hit = result.m_Hit;
                        return true;
                    }
                    else
                    {
                        entity = Entity.Null;
                        hit = default;
                        BetterBulldozerMod.Instance.Logger.Debug($"{nameof(ToolBaseSystemGetRaycastResultPatch)}.{nameof(Prefix)} skipped method.");
                        return false;
                    }
                }
            }

            if (raycastHitSomething && !toolSystem.EntityManager.HasComponent<Deleted>(result.m_Owner))
            {
                entity = result.m_Owner;
                hit = result.m_Hit;
                return true;
            }

            entity = Entity.Null;
            hit = default;
            return true;
        }
    }
}
