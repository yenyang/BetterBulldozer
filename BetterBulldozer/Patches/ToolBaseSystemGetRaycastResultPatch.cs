// <copyright file="ToolBaseSystemGetRaycastResultPatch.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Patches
{
    using System;
    using Better_Bulldozer.Systems;
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
            if (betterBulldozerUISystem.SelectedRaycastTarget != BetterBulldozerUISystem.RaycastTarget.Lanes)
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
            if (toolRaycastSystem.GetRaycastResult(out var result1)
                && !toolSystem.EntityManager.HasComponent<Deleted>(result1.m_Owner)
                && !toolSystem.EntityManager.HasComponent<Game.Tools.EditorContainer>(result1.m_Hit.m_HitEntity))
            {
                entity = Entity.Null;
                hit = default;
                BetterBulldozerMod.Instance.Logger.Debug($"{nameof(ToolBaseSystemGetRaycastResultPatch)}.{nameof(Prefix)} skipped method.");
                return false;
            }

            if (toolRaycastSystem.GetRaycastResult(out var result) && !toolSystem.EntityManager.HasComponent<Deleted>(result.m_Owner))
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
