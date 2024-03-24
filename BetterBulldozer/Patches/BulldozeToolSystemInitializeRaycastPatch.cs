// <copyright file="BulldozeToolSystemInitializeRaycastPatch.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Patches
{
    using Better_Bulldozer.Systems;
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
            else if (betterBulldozerUISystem.SelectedRaycastTarget == BetterBulldozerUISystem.RaycastTarget.Icons)
            {
                toolRaycastSystem.typeMask = TypeMask.Icons;
                toolRaycastSystem.utilityTypeMask = UtilityTypes.LowVoltageLine | UtilityTypes.HighVoltageLine | UtilityTypes.SewagePipe | UtilityTypes.SewagePipe;
                toolRaycastSystem.iconLayerMask = Game.Notifications.IconLayerMask.Marker | Game.Notifications.IconLayerMask.Default;
            }
        }
    }
}
