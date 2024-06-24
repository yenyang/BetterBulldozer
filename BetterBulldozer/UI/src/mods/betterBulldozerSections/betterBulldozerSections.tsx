import { useLocalization } from "cs2/l10n";
import {ModuleRegistryExtend} from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import { VanillaComponentResolver } from "../VanillaComponentResolver/VanillaComponentResolver";
import mod from "../../../mod.json";
import { tool } from "cs2/bindings";
import locale from "../lang/en-US.json";
import { getModule } from "cs2/modding";

// These establishes the binding with C# side. Without C# side game ui will crash.
const vechicleCimAnimalSelectionMode$ = bindValue<number>(mod.id, 'VehicleCimsAnimalsSelectionMode');
const bypassConfirmation$ = bindValue<boolean>(mod.id, 'BypassConfirmation');
const raycastTarget$ = bindValue<number>(mod.id, 'RaycastTarget');
const areasFilter$ = bindValue<number>(mod.id, 'AreasFilter');
const isGame$ = bindValue<boolean>(mod.id, 'IsGame');
const markersFilter$ = bindValue<number>(mod.id, 'MarkersFilter');
const upgradeIsMain$ = bindValue<boolean>(mod.id, 'UpgradeIsMain');
const subElementBulldozeToolActive$ = bindValue<boolean>(mod.id, 'SubElementBulldozeToolActive');
const selectionMode$ = bindValue<number>(mod.id, "SelectionMode");
const selectionRadius$ = bindValue<number>(mod.id, "SelectionRadius");
const selectedVanillaFilters$ = bindValue<VanillaFilters>(mod.id, "SelectedVanillaFilters");

// These contain the coui paths to Unified Icon Library svg assets
const uilStandard =                         "coui://uil/Standard/";
const gameplayManipulationSrc =         uilStandard +  "CubeSimulation.svg";
const bypassConfirmationSrc =           uilStandard +  "BypassQuestionmark.svg";
const lanesSrc =                         uilStandard + "Lanes.svg";
const networkMarkersSrc =                uilStandard + "DottedLinesMarkers.svg";
const subElementBulldozerSrc =           uilStandard + "Jackhammer.svg";

const singleSrc =                        uilStandard + "SingleRhombus.svg";
const matchingSrc =                     uilStandard + "SameRhombus.svg";
const similarSrc =                      uilStandard + "SimilarRhombus.svg";
const resetSrc =                        uilStandard + "Reset.svg";

const singleVehicleCimAnimalSrc =          uilStandard + "Dot.svg";
const radiusSrc =                          uilStandard + "Circle.svg";

const subElementsOfMainElementSrc =      uilStandard + "HouseMainElements.svg";
const surfacesSrc =                     uilStandard + "ShovelSurface.svg";
const upgradeIsMainSrc =                uilStandard + "HouseSmallSubElements.svg";
const staticObjectMarkersSrc =          uilStandard + "MarkerSpawner.svg";
const spacesSrc =                       uilStandard + "PeopleAreaTool.svg";

const arrowDownSrc =         uilStandard +  "ArrowDownThickStroke.svg";
const arrowUpSrc =           uilStandard +  "ArrowUpThickStroke.svg";

const allSrc =              uilStandard + "StarAll.svg";
const networkSrc =         uilStandard +  "Network.svg";
const decalsSrc =           uilStandard +  "Decals.svg";
const treeSrc =           uilStandard +  "TreeAdult.svg";
const plantSrc =           uilStandard +  "FlowerPot.svg";
const buildingSrc =         uilStandard + "House.svg";
const propsSrc =            uilStandard + "BenchAndLampProps.svg"; 

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

// This functions trigger an event on C# side and C# designates the method to implement.
function changeSelection(mode: SelectionMode) {
    trigger(mod.id, "ChangeSelectionMode", mode);
}

// This functions trigger an event on C# side and C# designates the method to implement.
function changeVCAselection(mode: VCAselectionMode) {
    trigger(mod.id, "ChangeVCAselectionMode", mode);
}

// This functions trigger an event on C# side and C# designates the method to implement.
function changeSelectedVanillaFilter(filter: VanillaFilters) {
    trigger(mod.id, "ChangeVanillaFilter", filter);
}

enum SelectionMode 
{
    Single = 0,
    Matching = 1,
    Similar = 2,
    Reset = 3,
}

enum VCAselectionMode 
{
    Single = 0,
    Radius = 1,
}

enum VanillaFilters 
{
    None = 0,
    Networks = 1,
    Buildings = 2,
    Trees = 4,
    Plants = 8,
    Decals = 16,
    Props = 32,
    All = 64,
}

const descriptionToolTipStyle = getModule("game-ui/common/tooltip/description-tooltip/description-tooltip.module.scss", "classes");
    

// This is working, but it's possible a better solution is possible.
function descriptionTooltip(tooltipTitle: string | null, tooltipDescription: string | null) : JSX.Element {
    return (
        <>
            <div className={descriptionToolTipStyle.title}>{tooltipTitle}</div>
            <div className={descriptionToolTipStyle.content}>{tooltipDescription}</div>
        </>
    );
}

export const BetterBulldozerComponent: ModuleRegistryExtend = (Component : any) => {
    // I believe you should not put anything here.
    return (props) => {
        // This defines aspects of the components.
        const {children, ...otherProps} = props || {};

        // These get the value of the bindings.
        const subElementBulldozerToolActive = useValue(subElementBulldozeToolActive$);
        const bulldozeToolActive = useValue(tool.activeTool$).id == tool.BULLDOZE_TOOL;
        const bypassConfirmation = useValue(bypassConfirmation$);
        const raycastTarget = useValue(raycastTarget$);
        const areasFilter = useValue(areasFilter$);
        const markersFilter = useValue(markersFilter$);
        const upgradeIsMain = useValue(upgradeIsMain$);
        const isGame = useValue(isGame$);
        const selectionMode = useValue(selectionMode$) as SelectionMode;
        const vechicleCimAnimalSelectionMode = useValue(vechicleCimAnimalSelectionMode$) as VCAselectionMode;
        const selectionRadius = useValue(selectionRadius$);
        const selectedVanillaFilters = useValue(selectedVanillaFilters$);
        
        // translation handling. Translates using locale keys that are defined in C# or fallback string here.
        const { translate } = useLocalization();
        const filterSectionTitle =          translate(sectionTitlePrefix + "Filter",                        locale["YY_BETTER_BULLDOZER.Filter"]);
        const surfacesFilterTooltip =       translate(tooltipDescriptionPrefix + surfacesID,                locale["YY_BETTER_BULLDOZER_DESCRIPTION.SurfacesFilterButton"]);
        const spacesFilterTooltip =         translate(tooltipDescriptionPrefix + spacesID,                  locale["YY_BETTER_BULLDOZER_DESCRIPTION.SpacesFilterButton"]);
        const staticObjectMarkersTooltip =  translate(tooltipDescriptionPrefix + staticObjectsID,           locale["YY_BETTER_BULLDOZER_DESCRIPTION.StaticObjectsFilterButton"]);
        const markerNetworkTooltip =        translate(tooltipDescriptionPrefix + networksFilterID,          locale["YY_BETTER_BULLDOZER_DESCRIPTION.NetworksFilterButton"]);
        const gameplayManipulationTooltip = translate(tooltipDescriptionPrefix + gameplayManipulationID,    locale["YY_BETTER_BULLDOZER_DESCRIPTION.GameplayManipulationButton"]);
        const bypassConfirmationTooltip =   translate(tooltipDescriptionPrefix + bypassConfirmationID,      locale["YY_BETTER_BULLDOZER_DESCRIPTION.BypassConfirmationButton"]);
        const raycastMarkersTooltip =       translate(tooltipDescriptionPrefix + raycastMarkersID,          locale["YY_BETTER_BULLDOZER_DESCRIPTION.RaycastMarkersButton"]);
        const raycastAreasTooltip =         translate(tooltipDescriptionPrefix + raycastAreasID,            locale["YY_BETTER_BULLDOZER_DESCRIPTION.RaycastAreasButton"]);
        const lanesTooltip =                translate(tooltipDescriptionPrefix + lanesID,                   locale["YY_BETTER_BULLDOZER_DESCRIPTION.RaycastLanesButton"]);

        const subElementBulldozerDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[SubElementBulldozerButton]" ,locale["BetterBulldozer.TOOLTIP_DESCRIPTION[SubElementBulldozerButton]"]);
        const subElementsOfMainElementDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[SubElementsOfMainElement]", locale["BetterBulldozer.TOOLTIP_DESCRIPTION[SubElementsOfMainElement]"]);
        const upgradeIsMainDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[UpgradeIsMain]", locale["BetterBulldozer.TOOLTIP_DESCRIPTION[UpgradeIsMain]"]);
        const toolModeTitle =               translate("Toolbar.TOOL_MODE_TITLE", "Tool Mode");

        const surfacesFilterTitle =         translate("BetterBulldozer.TOOLTIP_TITLE[SurfacesFilterButton]" ,locale["BetterBulldozer.TOOLTIP_TITLE[SurfacesFilterButton]"]);
        const spacesFilterTitle =           translate("BetterBulldozer.TOOLTIP_TITLE[SpacesFilterButton]",locale["BetterBulldozer.TOOLTIP_TITLE[SpacesFilterButton]"]);
        const staticObjectsFilterTitle =    translate("BetterBulldozer.TOOLTIP_TITLE[StaticObjectsFilterButton]" ,locale["BetterBulldozer.TOOLTIP_TITLE[StaticObjectsFilterButton]"]);
        const markerNetworkFilterTitle =    translate("BetterBulldozer.TOOLTIP_TITLE[NetworksFilterButton]" ,locale["BetterBulldozer.TOOLTIP_TITLE[NetworksFilterButton]"]);
        const gameplayManipulationTitle =   translate("BetterBulldozer.TOOLTIP_TITLE[GameplayManipulationButton]" ,locale["BetterBulldozer.TOOLTIP_TITLE[GameplayManipulationButton]"]);
        const bypassConfirmationTitle =     translate("BetterBulldozer.TOOLTIP_TITLE[BypassConfirmationButton]" ,locale["BetterBulldozer.TOOLTIP_TITLE[BypassConfirmationButton]"]);
        const raycastMarkersTitle =         translate("BetterBulldozer.TOOLTIP_TITLE[RaycastMarkersButton]" ,locale["BetterBulldozer.TOOLTIP_TITLE[RaycastMarkersButton]"]);
        const raycastAreasTitle =           translate("BetterBulldozer.TOOLTIP_TITLE[RaycastAreasButton]" ,locale["BetterBulldozer.TOOLTIP_TITLE[RaycastAreasButton]"]);
        const lanesTitle =                  translate("BetterBulldozer.TOOLTIP_TITLE[RaycastLanesButton]" ,locale["BetterBulldozer.TOOLTIP_TITLE[RaycastLanesButton]"]);
        const subElementBulldozerTitle =    translate("BetterBulldozer.TOOLTIP_TITLE[SubElementBulldozerButton]" ,locale["BetterBulldozer.TOOLTIP_TITLE[SubElementBulldozerButton]"]);
        const subElementsofMainElementTitle =   translate("BetterBulldozer.TOOLTIP_TITLE[SubElementsOfMainElement]" ,locale["BetterBulldozer.TOOLTIP_TITLE[SubElementsOfMainElement]"]);
        const upgradeIsMainTitle =              translate("BetterBulldozer.TOOLTIP_TITLE[UpgradeIsMain]" ,locale["BetterBulldozer.TOOLTIP_TITLE[UpgradeIsMain]"]);
        const singleTooltipTitle =          translate("BetterBulldozer.TOOLTIP_TITLE[Single]" ,locale["BetterBulldozer.TOOLTIP_TITLE[Single]"]);
        const singleTooltipDescription =          translate( "BetterBulldozer.TOOLTIP_DESCRIPTION[Single]",locale["BetterBulldozer.TOOLTIP_DESCRIPTION[Single]"]);
        const matchingTooltipTitle =        translate("BetterBulldozer.TOOLTIP_TITLE[Matching]" ,locale["BetterBulldozer.TOOLTIP_TITLE[Matching]"]);
        const matchingTooltipDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[Matching]", locale["BetterBulldozer.TOOLTIP_DESCRIPTION[Matching]"]);
        const similarTooltipTitle =         translate("BetterBulldozer.TOOLTIP_TITLE[Similar]" ,locale["BetterBulldozer.TOOLTIP_TITLE[Similar]"]);
        const similarTooltipDescription =   translate("BetterBulldozer.TOOLTIP_DESCRIPTION[Similar]", locale["BetterBulldozer.TOOLTIP_DESCRIPTION[Similar]"]);
        const resetTooltipTitle =           translate("BetterBulldozer.TOOLTIP_TITLE[Reset]" ,locale["BetterBulldozer.TOOLTIP_TITLE[Reset]"]);
        const resetTooltipDescription =     translate("BetterBulldozer.TOOLTIP_DESCRIPTION[Reset]" ,locale["BetterBulldozer.TOOLTIP_DESCRIPTION[Reset]"]);
        const singleOrDragTooltipTitle =    translate("BetterBulldozer.TOOLTIP_TITLE[SingleMovingObject]" , locale["BetterBulldozer.TOOLTIP_TITLE[SingleMovingObject]"]);
        const singleOrDragTooltipDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[SingleMovingObject]" ,locale["BetterBulldozer.TOOLTIP_DESCRIPTION[SingleMovingObject]"]);
        const radiusSelectionTooltipTitle =     translate("BetterBulldozer.TOOLTIP_TITLE[RadiusSelection]" ,locale["BetterBulldozer.TOOLTIP_TITLE[RadiusSelection]"]);
        const radiusSelectionTooltipDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[RadiusSelection]" ,locale["BetterBulldozer.TOOLTIP_DESCRIPTION[RadiusSelection]"]);
        const increaseRadiusDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[IncreaseRadius]" , locale["BetterBulldozer.TOOLTIP_DESCRIPTION[IncreaseRadius]"]);
        const decreaseRadiusDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[DecreaseRadius]", locale["BetterBulldozer.TOOLTIP_DESCRIPTION[DecreaseRadius]"]);
        const allFiltersTitle = translate("BetterBulldozer.TOOLTIP_TITLE[AllFilters]" ,locale["BetterBulldozer.TOOLTIP_TITLE[AllFilters]"]);
        const allFiltersDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[AllFilters]" ,locale["BetterBulldozer.TOOLTIP_DESCRIPTION[AllFilters]"]);
        const vanillaNetworksFilterTitle = translate("BetterBulldozer.TOOLTIP_TITLE[VanillaNetworksFilter]" ,locale["BetterBulldozer.TOOLTIP_TITLE[VanillaNetworksFilter]"]);
        const vanillaNetworksFilterDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[VanillaNetworksFilter]" ,locale["BetterBulldozer.TOOLTIP_DESCRIPTION[VanillaNetworksFilter]"]);
        const buildingFilterTitle = translate("BetterBulldozer.TOOLTIP_TITLE[BuildingFilter]" ,locale["BetterBulldozer.TOOLTIP_TITLE[BuildingFilter]"]);
        const buildingFilterDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[BuildingFilter]" ,locale["BetterBulldozer.TOOLTIP_DESCRIPTION[BuildingFilter]"]);
        const treeFilterTitle = translate("BetterBulldozer.TOOLTIP_TITLE[TreeFilter]" , locale["BetterBulldozer.TOOLTIP_TITLE[TreeFilter]"]);
        const treeFilterDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[TreeFilter]" ,locale["BetterBulldozer.TOOLTIP_DESCRIPTION[TreeFilter]"]);
        const plantFilterTitle = translate("BetterBulldozer.TOOLTIP_TITLE[PlantFilter]" ,locale["BetterBulldozer.TOOLTIP_TITLE[PlantFilter]"]);
        const plantFilterDescription = translate( "BetterBulldozer.TOOLTIP_DESCRIPTION[PlantFilter]",locale["BetterBulldozer.TOOLTIP_DESCRIPTION[PlantFilter]"]);
        const decalFilterTitle = translate("BetterBulldozer.TOOLTIP_TITLE[DecalFilter]" ,locale["BetterBulldozer.TOOLTIP_TITLE[DecalFilter]"]);
        const decalFilterDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[DecalFilter]" ,locale["BetterBulldozer.TOOLTIP_DESCRIPTION[DecalFilter]"]);
        const propFilterTitle = translate("BetterBulldozer.TOOLTIP_TITLE[PropFilter]" ,locale["BetterBulldozer.TOOLTIP_TITLE[PropFilter]"]);
        const propFilterDescription = translate("BetterBulldozer.TOOLTIP_DESCRIPTION[PropFilter]" ,locale["BetterBulldozer.TOOLTIP_DESCRIPTION[PropFilter]"]);

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
        if (bulldozeToolActive && isGame) {
            result.props.children?.push(
                /* 
                Adds a section for filters if raycasting areas or markers. Each of those sections has two buttons.
                Adds a section for tool mode with buttons.
                All properties of the buttons and sections have been previously defined in variables above.
                */
               <>
                    { raycastingAreas && (
                        // This section is only showing if Raycasting areas. It includes filters for surfaces and spaces.
                        <VanillaComponentResolver.instance.Section title={filterSectionTitle}>
                                <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={surfacesFilter}    tooltip={descriptionTooltip(surfacesFilterTitle ,surfacesFilterTooltip)} onSelect={() => handleClick(surfacesID)}  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}  src={surfacesSrc}></VanillaComponentResolver.instance.ToolButton>
                                <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={spacesFilter}      tooltip={descriptionTooltip(spacesFilterTitle ,spacesFilterTooltip)}   onSelect={() => handleClick(spacesID)}    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}  src={spacesSrc}></VanillaComponentResolver.instance.ToolButton>
                        </VanillaComponentResolver.instance.Section>
                    )}
                    { raycastingMarkers && (
                        // This section is only showing if Raycasting markers. It includes filters for static objects and networks.
                        <VanillaComponentResolver.instance.Section title={filterSectionTitle}>
                                <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={staticObjectMarkersFilter} tooltip={descriptionTooltip(staticObjectsFilterTitle ,staticObjectMarkersTooltip)}    onSelect={() => handleClick(staticObjectsID)}   focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     src={staticObjectMarkersSrc}></VanillaComponentResolver.instance.ToolButton>
                                <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={markerNetworkFilter}       tooltip={descriptionTooltip(markerNetworkFilterTitle ,markerNetworkTooltip)}          onSelect={() => handleClick(networksFilterID)}  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     src={networkMarkersSrc}></VanillaComponentResolver.instance.ToolButton>
                        </VanillaComponentResolver.instance.Section>
                    )}
                    { subElementBulldozerToolActive && (
                        // This section is only showing while using Subelement Bulldozer.
                        <>
                            <VanillaComponentResolver.instance.Section title={translate("BetterBulldozer.SECTION_TITLE[Selection]" ,locale["BetterBulldozer.SECTION_TITLE[Selection]"])}>
                                    <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={selectionMode == SelectionMode.Single}         tooltip={descriptionTooltip(singleTooltipTitle, singleTooltipDescription)}                          onSelect={() => changeSelection(SelectionMode.Single)}              src={singleSrc}         focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}  ></VanillaComponentResolver.instance.ToolButton>
                                    <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={selectionMode == SelectionMode.Matching}       tooltip={descriptionTooltip(matchingTooltipTitle, matchingTooltipDescription)}                          onSelect={() => changeSelection(SelectionMode.Matching)}            src={matchingSrc}                 focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}  ></VanillaComponentResolver.instance.ToolButton>
                                    <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={selectionMode == SelectionMode.Similar}        tooltip={descriptionTooltip(similarTooltipTitle, similarTooltipDescription)}                          onSelect={() => changeSelection(SelectionMode.Similar)}            src={similarSrc}                 focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}  ></VanillaComponentResolver.instance.ToolButton>
                                    <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={selectionMode == SelectionMode.Reset}          tooltip={descriptionTooltip(resetTooltipTitle, resetTooltipDescription)}                          onSelect={() => changeSelection(SelectionMode.Reset)}            src={resetSrc}                 focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}  ></VanillaComponentResolver.instance.ToolButton>
                            </VanillaComponentResolver.instance.Section>
                            <VanillaComponentResolver.instance.Section title={translate("BetterBulldozer.SECTION_TITLE[Tier]", locale["BetterBulldozer.SECTION_TITLE[Tier]"])}>
                                    <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={!upgradeIsMain}    tooltip={descriptionTooltip(subElementsofMainElementTitle ,subElementsOfMainElementDescription)}   onSelect={() => handleClick("SubElementsOfMainElement")} src={subElementsOfMainElementSrc}      focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}  ></VanillaComponentResolver.instance.ToolButton>
                                    <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={upgradeIsMain}      tooltip={descriptionTooltip(upgradeIsMainTitle ,upgradeIsMainDescription)}             onSelect={() => handleClick("UpgradeIsMain")}            src={upgradeIsMainSrc}                 focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}  ></VanillaComponentResolver.instance.ToolButton>
                            </VanillaComponentResolver.instance.Section>
                        </>
                    )}
                    { raycastTarget == 4 && (
                        // This section is only showing while using Removing Vehicles Cims and Animals.
                        <>
                            { vechicleCimAnimalSelectionMode == VCAselectionMode.Radius && (
                            <VanillaComponentResolver.instance.Section title={translate("BetterBulldozer.SECTION_TITLE[Radius]",locale["BetterBulldozer.SECTION_TITLE[Radius]"])}>
                                <VanillaComponentResolver.instance.ToolButton tooltip={decreaseRadiusDescription} onSelect={() => handleClick("DecreaseRadius")} src={arrowDownSrc} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.mouseToolOptionsTheme.startButton}></VanillaComponentResolver.instance.ToolButton>
                                <div className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}>{ selectionRadius + " m"}</div>
                                <VanillaComponentResolver.instance.ToolButton tooltip={increaseRadiusDescription} onSelect={() => handleClick("IncreaseRadius")} src={arrowUpSrc} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.mouseToolOptionsTheme.endButton} ></VanillaComponentResolver.instance.ToolButton>
                            </VanillaComponentResolver.instance.Section>
                            )}
                            <VanillaComponentResolver.instance.Section title={translate("BetterBulldozer.SECTION_TITLE[Selection]" ,locale["BetterBulldozer.SECTION_TITLE[Selection]"])}>
                                        <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={vechicleCimAnimalSelectionMode == VCAselectionMode.Single}         tooltip={descriptionTooltip(singleOrDragTooltipTitle, singleOrDragTooltipDescription )}                          onSelect={() => changeVCAselection(VCAselectionMode.Single)}              src={singleVehicleCimAnimalSrc}         focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}  ></VanillaComponentResolver.instance.ToolButton>
                                        <VanillaComponentResolver.instance.ToolButton className={VanillaComponentResolver.instance.toolButtonTheme.button} selected={vechicleCimAnimalSelectionMode == VCAselectionMode.Radius}       tooltip={descriptionTooltip(radiusSelectionTooltipTitle, radiusSelectionTooltipDescription)}                          onSelect={() => changeVCAselection(VCAselectionMode.Radius)}            src={radiusSrc}                 focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}  ></VanillaComponentResolver.instance.ToolButton>
                            </VanillaComponentResolver.instance.Section>
                        </>
                    )}
                    { raycastTarget == 0 && !subElementBulldozerToolActive && (
                        // This section is only showing while using vanilla bulldozer.
                        <VanillaComponentResolver.instance.Section title={filterSectionTitle}>
                            <VanillaComponentResolver.instance.ToolButton  selected={(selectedVanillaFilters & VanillaFilters.All) == VanillaFilters.All}               tooltip={descriptionTooltip(allFiltersTitle ,allFiltersDescription)}                        src={allSrc}            onSelect={() => changeSelectedVanillaFilter(VanillaFilters.All)}        className={VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     ></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={(selectedVanillaFilters & VanillaFilters.Networks) == VanillaFilters.Networks}     tooltip={descriptionTooltip(vanillaNetworksFilterTitle ,vanillaNetworksFilterDescription)}  src={networkSrc}        onSelect={() => changeSelectedVanillaFilter(VanillaFilters.Networks)}   className={VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     ></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={(selectedVanillaFilters & VanillaFilters.Buildings) == VanillaFilters.Buildings}   tooltip={descriptionTooltip(buildingFilterTitle ,buildingFilterDescription)}                src={buildingSrc}       onSelect={() => changeSelectedVanillaFilter(VanillaFilters.Buildings)}  className={VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     ></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={(selectedVanillaFilters & VanillaFilters.Trees) == VanillaFilters.Trees}           tooltip={descriptionTooltip(treeFilterTitle ,treeFilterDescription)}                        src={treeSrc}           onSelect={() => changeSelectedVanillaFilter(VanillaFilters.Trees)}      className={VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     ></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={(selectedVanillaFilters & VanillaFilters.Plants) == VanillaFilters.Plants}         tooltip={descriptionTooltip(plantFilterTitle ,plantFilterDescription)}                      src={plantSrc}          onSelect={() => changeSelectedVanillaFilter(VanillaFilters.Plants)}     className={VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     ></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={(selectedVanillaFilters & VanillaFilters.Decals) == VanillaFilters.Decals}         tooltip={descriptionTooltip(decalFilterTitle ,decalFilterDescription)}                      src={decalsSrc}         onSelect={() => changeSelectedVanillaFilter(VanillaFilters.Decals)}     className={VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     ></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={(selectedVanillaFilters & VanillaFilters.Props) == VanillaFilters.Props}           tooltip={descriptionTooltip(propFilterTitle ,propFilterDescription)}                        src={propsSrc}          onSelect={() => changeSelectedVanillaFilter(VanillaFilters.Props)}      className={VanillaComponentResolver.instance.toolButtonTheme.button} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     ></VanillaComponentResolver.instance.ToolButton>
                        </VanillaComponentResolver.instance.Section>
                    )}
                    <VanillaComponentResolver.instance.Section title={toolModeTitle}>
                            <VanillaComponentResolver.instance.ToolButton  selected={subElementBulldozerToolActive}     tooltip={descriptionTooltip(subElementBulldozerTitle ,subElementBulldozerDescription)}    onSelect={() => handleClick("SubElementBulldozerButton")}       src={subElementBulldozerSrc}    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={raycastTarget == 4}              tooltip={descriptionTooltip(gameplayManipulationTitle, gameplayManipulationTooltip)}       onSelect={() => handleClick(gameplayManipulationID)}            src={gameplayManipulationSrc}   focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={bypassConfirmation}                tooltip={descriptionTooltip(bypassConfirmationTitle, bypassConfirmationTooltip)}         onSelect={() => handleClick(bypassConfirmationID)}              src={bypassConfirmationSrc}     focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={raycastingLanes}                   tooltip={descriptionTooltip(lanesTitle, lanesTooltip)}                      onSelect={() => handleClick(lanesID)}                           src={lanesSrc}                  focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={raycastingMarkers}                 tooltip={descriptionTooltip(raycastMarkersTitle, raycastMarkersTooltip)}             onSelect={() => handleClick(raycastMarkersID)}                  src={networkMarkersSrc}         focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                            <VanillaComponentResolver.instance.ToolButton  selected={raycastingAreas}                   tooltip={descriptionTooltip(raycastAreasTitle, raycastAreasTooltip)}               onSelect={() => handleClick(raycastAreasID)}                    src={surfacesSrc}               focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}     className={VanillaComponentResolver.instance.toolButtonTheme.button}></VanillaComponentResolver.instance.ToolButton>
                    </VanillaComponentResolver.instance.Section>
                </>
            );
        }

        return result;
    };
}