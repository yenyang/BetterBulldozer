// <copyright file="BulldozeToolSystemInitializeRaycastPatch.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Patches
{
    using Better_Bulldozer.Systems;
    using Game;
    using Game.Areas;
    using Game.Common;
    using Game.Net;
    using Game.Rendering;
    using Game.Tools;
    using HarmonyLib;
    using Unity.Entities;

    /// <summary>
    /// Patches Bulldoze Tool System Inititialize Raycast to add Markers as something to raycast.
    /// </summary>
    [HarmonyPatch(typeof(BulldozeToolSystem), "InitializeRaycast")]
    public class BulldozeToolSystemInitializeRaycastPatch
    {
        /// <summary>
        /// Patches Bulldoze Tool System Inititialize Raycast to add Markers as something to raycast.
        /// </summary>
        public static void Postfix()
        {
            ToolSystem toolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            if (!toolSystem.actionMode.IsGame())
            {
                return;
            }

            ToolRaycastSystem toolRaycastSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolRaycastSystem>();
            BetterBulldozerUISystem betterBulldozerUISystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<BetterBulldozerUISystem>();
            RenderingSystem renderingSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<RenderingSystem>();
            if (renderingSystem.markersVisible && betterBulldozerUISystem.SelectedRaycastTarget == BetterBulldozerUISystem.RaycastTarget.Markers)
            {
                toolRaycastSystem.typeMask = betterBulldozerUISystem.MarkersFilter;
                if ((betterBulldozerUISystem.MarkersFilter & TypeMask.Net) == TypeMask.Net)
                {
                    toolRaycastSystem.netLayerMask = Layer.MarkerPathway | Layer.MarkerTaxiway | Layer.PowerlineLow | Layer.PowerlineHigh | Layer.WaterPipe | Layer.SewagePipe;
                    toolRaycastSystem.raycastFlags = RaycastFlags.Markers;
                    toolRaycastSystem.utilityTypeMask = UtilityTypes.LowVoltageLine | UtilityTypes.HighVoltageLine | UtilityTypes.SewagePipe | UtilityTypes.SewagePipe;
                    toolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Underground | CollisionMask.Overground;
                }
                else
                {
                    toolRaycastSystem.raycastFlags = RaycastFlags.Markers | RaycastFlags.Decals;
                }
            }
            else if (betterBulldozerUISystem.SelectedRaycastTarget == BetterBulldozerUISystem.RaycastTarget.Areas)
            {
                toolRaycastSystem.typeMask = TypeMask.Areas;
                toolRaycastSystem.areaTypeMask = betterBulldozerUISystem.AreasFilter;
                toolRaycastSystem.raycastFlags |= RaycastFlags.SubElements;
            }
            else if (betterBulldozerUISystem.SelectedRaycastTarget == BetterBulldozerUISystem.RaycastTarget.Lanes)
            {
                toolRaycastSystem.typeMask = TypeMask.Net;
                toolRaycastSystem.netLayerMask = Layer.Fence | Layer.LaneEditor;
                toolRaycastSystem.raycastFlags |= RaycastFlags.Markers | RaycastFlags.EditorContainers;
            }
            else if (betterBulldozerUISystem.SelectedRaycastTarget == BetterBulldozerUISystem.RaycastTarget.VehiclesCimsAndAnimals)
            {
                toolRaycastSystem.typeMask = TypeMask.MovingObjects;
            }
            else if (betterBulldozerUISystem.SelectedRaycastTarget == BetterBulldozerUISystem.RaycastTarget.Vanilla)
            {
                if ((betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Networks) != BetterBulldozerUISystem.VanillaFilters.Networks)
                {
                    toolRaycastSystem.typeMask &= ~TypeMask.Net;
                }

                if ((betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Surfaces) != BetterBulldozerUISystem.VanillaFilters.Surfaces)
                {
                    toolRaycastSystem.areaTypeMask &= ~AreaTypeMask.Surfaces;
                }

                if ((betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Decals) != BetterBulldozerUISystem.VanillaFilters.Decals)
                {
                    toolRaycastSystem.raycastFlags &= ~RaycastFlags.Decals;
                }

                if ((betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Buildings) != BetterBulldozerUISystem.VanillaFilters.Buildings
                    && (betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Trees) != BetterBulldozerUISystem.VanillaFilters.Trees
                    && (betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Plants) != BetterBulldozerUISystem.VanillaFilters.Plants
                    && (betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Props) != BetterBulldozerUISystem.VanillaFilters.Props
                    && (betterBulldozerUISystem.SelectedVanillaFilters & BetterBulldozerUISystem.VanillaFilters.Decals) != BetterBulldozerUISystem.VanillaFilters.Decals)
                {
                    toolRaycastSystem.typeMask &= ~TypeMask.StaticObjects;
                }
            }
        }
    }
}
