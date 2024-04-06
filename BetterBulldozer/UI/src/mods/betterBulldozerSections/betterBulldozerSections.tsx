import { useLocalization } from "cs2/l10n";
import {ModuleRegistryExtend} from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import { VanillaComponentResolver } from "../VanillaComponentResolver/VanillaComponentResolver";
import mod from "../../../mod.json";
import spacesSrc from "./AreaToolBetterBulldozer.svg";
import staticObjectMarkersSrc from "./MarkersIconBetterBulldozer.svg";
import surfacesSrc from "./SurfaceIconBetterBulldozer.svg";
import { tool } from "cs2/bindings";
import locale from "../lang/en-US.json";
import upgradeIsMainSrc from "./SubElementsBetterBulldozer.svg";

// These establishes the binding with C# side. Without C# side game ui will crash.
const gameplayManipulation$ = bindValue<boolean>(mod.id, 'GameplayManipulation');
const bypassConfirmation$ = bindValue<boolean>(mod.id, 'BypassConfirmation');
const raycastTarget$ = bindValue<number>(mod.id, 'RaycastTarget');
const areasFilter$ = bindValue<number>(mod.id, 'AreasFilter');
const markersFilter$ = bindValue<number>(mod.id, 'MarkersFilter');
const upgradeIsMain$ = bindValue<boolean>(mod.id, 'UpgradeIsMain');
const noMainElements$ = bindValue<boolean>(mod.id, 'NoMainElements');

// These contain the coui paths to Unified Icon Library svg assets
const couiStandard =                         "coui://uil/Standard/";
const gameplayManipulationSrc =         couiStandard +  "CubeSimulation.svg";
const bypassConfirmationSrc =           couiStandard +  "BypassQuestionmark.svg";
const lanesSrc =                         couiStandard + "Network.svg";
const networkMarkersSrc =                couiStandard + "DottedLinesMarkers.svg";
const subElementBulldozerSrc =           couiStandard + "HouseAndNetwork.svg";
const subElementsOfMainElementSrc =               couiStandard + "House.svg";

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

// This functions trigger an event on C# side and C# designates the method to implement.
function handleClick(eventName: string) {
    trigger(mod.id, eventName);
}

export const BetterBulldozerComponent: ModuleRegistryExtend = (Component : any) => {
    // I believe you should not put anything here.
    return (props) => {
        // This defines aspects of the components.
        const {children, ...otherProps} = props || {};

        // These get the value of the bindings.
        const subElementBulldozerToolActive = useValue(tool.activeTool$).id == "SubElement Bulldozer Tool";
        const bulldozeToolActive = useValue(tool.activeTool$).id == tool.BULLDOZE_TOOL;
        const gameplayManipulation = useValue(gameplayManipulation$);
        const bypassConfirmation = useValue(bypassConfirmation$);
        const raycastTarget = useValue(raycastTarget$);
        const areasFilter = useValue(areasFilter$);
        const markersFilter = useValue(markersFilter$);
        const upgradeIsMain = useValue(upgradeIsMain$);
        const noMainElements = useValue(noMainElements$);
        
        // translation handling. Translates using locale keys that are defined in C# or fallback string here.
        const { translate } = useLocalization();
        const filterSectionTitle =          translate(sectionTitlePrefix + "Filter",                        locale["YY_BETTER_BULLDOZER.Filter"]);
        const surfacesFilterTooltip =       translate(tooltipDescriptionPrefix + surfacesID,                locale["YY_BETTER_BULLDOZER_DESCRIPTION.RaycastSurfacesButton"]);
        const spacesFilterTooltip =         translate(tooltipDescriptionPrefix + spacesID,                  locale["YY_BETTER_BULLDOZER_DESCRIPTION.SpacesFilterButton"]);
        const staticObjectMarkersTooltip =  translate(tooltipDescriptionPrefix + staticObjectsID,           locale["YY_BETTER_BULLDOZER_DESCRIPTION.StaticObjectsFilterButton"]);
        const markerNetworkTooltip =        translate(tooltipDescriptionPrefix + networksFilterID,          locale["YY_BETTER_BULLDOZER_DESCRIPTION.NetworksFilterButton"]);
        const gameplayManipulationTooltip = translate(tooltipDescriptionPrefix + gameplayManipulationID,    locale["YY_BETTER_BULLDOZER_DESCRIPTION.GameplayManipulationButton"]);
        const bypassConfirmationTooltip =   translate(tooltipDescriptionPrefix + bypassConfirmationID,      locale["YY_BETTER_BULLDOZER_DESCRIPTION.BypassConfirmationButton"]);
        const raycastMarkersTooltip =       translate(tooltipDescriptionPrefix + raycastMarkersID,          locale["YY_BETTER_BULLDOZER_DESCRIPTION.RaycastMarkersButton"]);
        const raycastAreasTooltip =         translate(tooltipDescriptionPrefix + raycastAreasID,            locale["YY_BETTER_BULLDOZER_DESCRIPTION.RaycastAreasButton"]);
        const lanesTooltip =                translate(tooltipDescriptionPrefix + lanesID,                   locale["YY_BETTER_BULLDOZER_DESCRIPTION.RaycastLanesButton"]);

        const subElementBulldozerDescription = translate("YY_BETTER_BULLDOZER_DESCRIPTION.SubElementBulldozerButton" ,locale["YY_BETTER_BULLDOZER_DESCRIPTION.SubElementBulldozerButton"]);
        const subElementsOfMainElementDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[SubElementsOfMainElement]", locale["BetterBulldozer.TOOLTIP_DESCRIPTION[SubElementsOfMainElement]"]);
        const upgradeIsMainDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[UpgradeIsMain]", locale["BetterBulldozer.TOOLTIP_DESCRIPTION[UpgradeIsMain]"]);
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
        if (bulldozeToolActive || subElementBulldozerToolActive) {
            result.props.children?.push(
                /* 
                Adds a section for filters if raycasting areas or markers. Each of those sections has two buttons.
                Adds a section for tool mode with 4 buttons.
                All properties of the buttons and sections have been previously defined in variables above.
                */
               <>
                    { raycastingAreas && (
                        // This section is only showing if Raycasting areas. It includes filters for surfaces and spaces.
                        <VanillaComponentResolver.instance.Section title={filterSectionTitle}>
                                <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={surfacesFilter}    tooltip={surfacesFilterTooltip} onSelect={() => handleClick(surfacesID)}  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}  src={surfacesSrc}></VanillaComponentResolver.instance.ToolButton>
                                <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={spacesFilter}      tooltip={spacesFilterTooltip}   onSelect={() => handleClick(spacesID)}    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}  src={spacesSrc}></VanillaComponentResolver.instance.ToolButton>
                        </VanillaComponentResolver.instance.Section>
                    )}
                    { raycastingMarkers && (
                        // This section is only showing if Raycasting markers. It includes filters for static objects and networks.
                        <VanillaComponentResolver.instance.Section title={filterSectionTitle}>
                                <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={staticObjectMarkersFilter} tooltip={staticObjectMarkersTooltip}    onSelect={() => handleClick(staticObjectsID)}   focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     src={staticObjectMarkersSrc}></VanillaComponentResolver.instance.ToolButton>
                                <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={markerNetworkFilter}       tooltip={markerNetworkTooltip}          onSelect={() => handleClick(networksFilterID)}  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     src={networkMarkersSrc}></VanillaComponentResolver.instance.ToolButton>
                        </VanillaComponentResolver.instance.Section>
                    )}
                    { subElementBulldozerToolActive && (
                        // This section is only showing using Subelement Bulldozer.
                        <VanillaComponentResolver.instance.Section title={translate("BetterBulldozer.SECTION_TITLE[Tier]", locale["BetterBulldozer.SECTION_TITLE[Tier]"])}>
                                <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={!upgradeIsMain}    tooltip={subElementsOfMainElementDescription}   onSelect={() => handleClick("SubElementsOfMainElement")} src={subElementsOfMainElementSrc}      focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}  ></VanillaComponentResolver.instance.ToolButton>
                                <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={upgradeIsMain}      tooltip={upgradeIsMainDescription}             onSelect={() => handleClick("UpgradeIsMain")}            src={upgradeIsMainSrc}                 focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}  ></VanillaComponentResolver.instance.ToolButton>
                                <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={noMainElements}      tooltip={"no main elements description"}      onSelect={() => handleClick("NoMainElements")}           src={subElementBulldozerSrc}                 focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}  ></VanillaComponentResolver.instance.ToolButton>
                       </VanillaComponentResolver.instance.Section>
                    )}
                    <VanillaComponentResolver.instance.Section title={toolModeTitle}>
                            <VanillaComponentResolver.instance.ToolButton  selected={subElementBulldozerToolActive}     tooltip={subElementBulldozerDescription}    onSelect={() => handleClick("SubElementBulldozerButton")}       src={subElementBulldozerSrc}    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={gameplayManipulation}              tooltip={gameplayManipulationTooltip}       onSelect={() => handleClick(gameplayManipulationID)}            src={gameplayManipulationSrc}   focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={bypassConfirmation}                tooltip={bypassConfirmationTooltip}         onSelect={() => handleClick(bypassConfirmationID)}              src={bypassConfirmationSrc}     focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={raycastingLanes}                   tooltip={lanesTooltip}                      onSelect={() => handleClick(lanesID)}                           src={lanesSrc}                  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={raycastingMarkers}                 tooltip={raycastMarkersTooltip}             onSelect={() => handleClick(raycastMarkersID)}                  src={networkMarkersSrc}         focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={raycastingAreas}                   tooltip={raycastAreasTooltip}               onSelect={() => handleClick(raycastAreasID)}                    src={surfacesSrc}               focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                    </VanillaComponentResolver.instance.Section>
                </>
            );
        }

        return result;
    };
}