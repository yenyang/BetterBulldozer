import { useLocalization } from "cs2/l10n";
import {ModuleRegistryExtend} from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import { VanillaComponentResolver } from "../VanillaComponentResolver/VanillaComponentResolver";
import mod from "../../../mod.json";
import spacesSrc from "./AreaToolBetterBulldozer.svg";
import bypassConfirmationSrc from "./BypassQuestionmarkBB.svg";
import gameplayManipulationSrc from "./CubeSimulationBB.svg";
import staticObjectMarkersSrc from "./MarkersIconBetterBulldozer.svg";
import lanesSrc from "./NetworkBB.svg";
import networkMarkersSrc from "./DottedLinesMarkersBB.svg";
import surfacesSrc from "./SurfaceIconBetterBulldozer.svg";
import { tool } from "cs2/bindings";

// These establishes the binding with C# side. Without C# side game ui will crash.
// export const bulldozeToolActive$ = bindValue<boolean>(mod.id, 'BulldozeToolActive');
export const gameplayManipulation$ = bindValue<boolean>(mod.id, 'GameplayManipulation');
export const bypassConfirmation$ = bindValue<boolean>(mod.id, 'BypassConfirmation');
export const raycastTarget$ = bindValue<number>(mod.id, 'RaycastTarget');
export const areasFilter$ = bindValue<number>(mod.id, 'AreasFilter');
export const markersFilter$ = bindValue<number>(mod.id, 'MarkersFilter');

// This functions trigger an event on C# side and C# designates the method to implement.
export function handleClick(eventName: string) {
    trigger(mod.id, eventName);
}

export const BetterBulldozerComponent: ModuleRegistryExtend = (Component : any) => {
    // I believe you should not put anything here.
    return (props) => {
        // This defines aspects of the components.
        const {children, ...otherProps} = props || {};

        // These get the value of the bindings.
        const bulldozeToolActive = useValue(tool.activeTool$).id == tool.BULLDOZE_TOOL;
        const gameplayManipulation = useValue(gameplayManipulation$);
        const bypassConfirmation = useValue(bypassConfirmation$);
        const raycastTarget = useValue(raycastTarget$);
        const areasFilter = useValue(areasFilter$);
        const markersFilter = useValue(markersFilter$);
        
        // Saving strings for events and translations.
        const surfacesID =              "SurfacesFilterButton";
        const spacesID =                "SpacesFilterButton";
        const staticObjectsID =         "StaticObjectsFilterButton";
        const networksFilterID =        "NetworksFilterButton";
        const gameplayManipulationID =  "GameplayManipulationButton";
        const bypassConfirmationID =    "BypassConfirmationButton";
        const raycastMarkersID =        "RaycastMarkersButton";
        const raycastAreasID =          "RaycastAreasButton";
        const lanesID =                 "RaycastLanesButton";
        const tooltipDescriptionPrefix ="YY_BETTER_BULLDOZER_DESCRIPTION.";
        const sectionTitlePrefix =      "YY_BETTER_BULLDOZER.";

        // translation handling. Translates using locale keys that are defined in C# or fallback string here.
        const { translate } = useLocalization();
        const filterSectionTitle =          translate(sectionTitlePrefix + "Filter",                        "Filter");
        const surfacesFilterTooltip =       translate(tooltipDescriptionPrefix + surfacesID,                "For removing surfaces inside or outside of buildings in one click.");
        const spacesFilterTooltip =         translate(tooltipDescriptionPrefix + spacesID,                  "For removing spaces including: Walking, Park, and Hangout areas. They are not currently visible with this tool, but will be highlighted when hovered. With this enabled you can target them inside or outside buildings and remove with one click.");
        const staticObjectMarkersTooltip =  translate(tooltipDescriptionPrefix + staticObjectsID,           "For removing invisible parking decals, various spots, points, and spawners. Only those outside buildings can be removed. Trying to target those inside buildings will remove the building!");
        const markerNetworkTooltip =        translate(tooltipDescriptionPrefix + networksFilterID,          "For removing invisible networks. Only those outside buildings can be removed. Trying to target those inside buildings will have no effect.");
        const gameplayManipulationTooltip = translate(tooltipDescriptionPrefix + gameplayManipulationID,    "Allows you to use the bulldozer on moving objects such as vehicles or cims.");
        const bypassConfirmationTooltip =   translate(tooltipDescriptionPrefix + bypassConfirmationID,      "Disables the prompt for whether you are sure you want to demolish a building.");
        const raycastMarkersTooltip =       translate(tooltipDescriptionPrefix + raycastMarkersID,          "Shows and EXCLUSIVELY targets static object markers or invisible networks. With this enabled you can demolish invisible networks, invisible parking decals, various spots, points, and spawners, but SAVE FIRST! You cannot demolish these within buildings.");
        const raycastAreasTooltip =         translate(tooltipDescriptionPrefix + raycastAreasID,            "Makes the bulldozer EXCLUSIVELY target surfaces or spaces inside or outside of buildings so you can remove them in one click. You must turn this off to bulldoze anything else.");
        const lanesTooltip =                translate(tooltipDescriptionPrefix + lanesID,                   "For removing standalone lanes such as interconnected fences, interconnected hedges, street markings, and vehicle lanes. Trying to target those inside networks will remove the network! You cannot create these in-game without a mod for it.")
        const toolModeTitle =               translate("Toolbar.TOOL_MODE_TITLE", "Tool Mode");

        // These convert integer casts of Enums into booleans.
        const raycastingMarkers : boolean = raycastTarget == 2;
        const raycastingAreas : boolean = raycastTarget == 1;
        const raycastingLanes : boolean = raycastTarget == 3;
        const surfacesFilter : boolean = areasFilter == 16; 
        const spacesFilter : boolean = areasFilter == 8;
        const staticObjectMarkersFilter : boolean = markersFilter == 2; 
        const markerNetworkFilter : boolean = markersFilter == 8;

        // This gets the original component that we may alter and return.
        var result : JSX.Element = Component();
        // It is important that we coordinate how to handle the tool options panel because it is possibile to create a mod that works for your mod but prevents others from doing the same thing.
        // If bulldoze tool active add better bulldozer sections.
        if (bulldozeToolActive) {
            result.props.children?.unshift(
                /* 
                Adds a section for filters if raycasting areas or markers. Each of those sections has two buttons.
                Adds a section for tool mode with 4 buttons.
                All properties of the buttons and sections have been previously defined in variables above.
                */
               <>
                    { raycastingAreas ?
                        // This section is only showing if Raycasting areas. It includes filters for surfaces and spaces.
                        <VanillaComponentResolver.instance.Section title={filterSectionTitle}>
                            <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={surfacesFilter}    tooltip={surfacesFilterTooltip} onSelect={() => handleClick(surfacesID)}    src={surfacesSrc}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={spacesFilter}      tooltip={spacesFilterTooltip}   onSelect={() => handleClick(spacesID)}      src={spacesSrc}></VanillaComponentResolver.instance.ToolButton>
                        </VanillaComponentResolver.instance.Section>
                        : <></>
                    }
                    { raycastingMarkers ? 
                        // This section is only showing if Raycasting markers. It includes filters for static objects and networks.
                        <VanillaComponentResolver.instance.Section title={filterSectionTitle}>
                            <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={staticObjectMarkersFilter} tooltip={staticObjectMarkersTooltip}    onSelect={() => handleClick(staticObjectsID)}   src={staticObjectMarkersSrc}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={markerNetworkFilter}       tooltip={markerNetworkTooltip}          onSelect={() => handleClick(networksFilterID)}  src={networkMarkersSrc}></VanillaComponentResolver.instance.ToolButton>
                        </VanillaComponentResolver.instance.Section>
                        : <></>
                    }
                     <VanillaComponentResolver.instance.Section title={toolModeTitle}>
                        <VanillaComponentResolver.instance.ToolButton  selected={raycastingLanes}       tooltip={lanesTooltip}                  onSelect={() => handleClick(lanesID)}                   src={lanesSrc}                  className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                        <VanillaComponentResolver.instance.ToolButton  selected={gameplayManipulation}  tooltip={gameplayManipulationTooltip}   onSelect={() => handleClick(gameplayManipulationID)}    src={gameplayManipulationSrc}   className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                        <VanillaComponentResolver.instance.ToolButton  selected={bypassConfirmation}    tooltip={bypassConfirmationTooltip}     onSelect={() => handleClick(bypassConfirmationID)}      src={bypassConfirmationSrc}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                        <VanillaComponentResolver.instance.ToolButton  selected={raycastingMarkers}     tooltip={raycastMarkersTooltip}         onSelect={() => handleClick(raycastMarkersID)}          src={networkMarkersSrc}         className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                        <VanillaComponentResolver.instance.ToolButton  selected={raycastingAreas}       tooltip={raycastAreasTooltip}           onSelect={() => handleClick(raycastAreasID)}            src={surfacesSrc}               className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                     </VanillaComponentResolver.instance.Section>
                </>
            );
        }

        return result;
    };
}