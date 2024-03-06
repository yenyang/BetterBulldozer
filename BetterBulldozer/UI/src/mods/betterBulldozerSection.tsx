import { useModding } from "modding/modding-context";
import { ModuleRegistry } from "modding/types";
import { MouseEvent, useCallback } from "react";

export const BetterBulldozerComponent = (moduleRegistry: ModuleRegistry) => (Component: any) => {
    // The module registrys are found by logging console.log('mr', moduleRegistry); in the index file and finding appropriate one.
    const toolMouseModule = moduleRegistry.registry.get("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx");
    const toolButtonModule = moduleRegistry.registry.get("game-ui/game/components/tool-options/tool-button/tool-button.tsx")!!;
    const theme = moduleRegistry.registry.get("game-ui/game/components/tool-options/tool-button/tool-button.module.scss")?.classes;
    // These are found in the minified JS file after searching for module.
    const Section: any = toolMouseModule?.Section;
    const ToolButton: any = toolButtonModule?.ToolButton;

    return (props: any) => {
        const { children, ...otherProps} = props || {};
        const { api: { api: { useValue, bindValue, trigger } } } = useModding();
        const { engine } = useModding();

        // These establish the bindings with C# side. Without C# side game ui will crash.

        // This binding is for whether the tool is active.
        const bulldozeToolActive$ = bindValue<boolean>('BetterBulldozer', 'BulldozeToolActive');
        const bulldozeToolActive = useValue(bulldozeToolActive$);

        // This binding is for whether game manipulation is toggled.
        const gameplayManipulation$ = bindValue<boolean>('BetterBulldozer', 'GameplayManipulation');
        const gameplayManipulation = useValue(gameplayManipulation$);

        // This binding is for whether bypass confirmation is toggled.
        const bypassConfirmation$ = bindValue<boolean>('BetterBulldozer', 'BypassConfirmation');
        const bypassConfirmation = useValue(bypassConfirmation$);

        // This binding is for what type of raycast target is selected.
        const raycastTarget$ = bindValue<number>('BetterBulldozer', 'RaycastTarget');
        const raycastTarget = useValue(raycastTarget$);

        // This binding is for what area filter is selected.
        const areasFilter$ = bindValue<number>('BetterBulldozer', 'AreasFilter');
        const areasFilter = useValue(areasFilter$);

        // This binding is for what markers filter is selected.
        const markersFilter$ = bindValue<number>('BetterBulldozer', 'MarkersFilter');
        const markersFilter = useValue(markersFilter$);

        const surfacesFilterToggled = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("BetterBulldozer", "SurfacesFilterToggled");
        }, []);

        const spacesFilterToggled = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("BetterBulldozer", "SpacesFilterToggled");
        }, []);

        const networksFilterToggled = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("BetterBulldozer", "NetworksFilterToggled");
        }, []);

        const staticObjectsFilterToggled = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("BetterBulldozer", "StaticObjectsFilterToggled");
        }, []);

        const raycastAreasButtonToggled = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("BetterBulldozer", "RaycastAreasButtonToggled");
        }, []);

        const raycastMarkersButtonToggled = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("BetterBulldozer", "RaycastMarkersButtonToggled");
        }, []);

        const bypassConfirmationToggled = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("BetterBulldozer", "BypassConfirmationToggled");
        }, []);

        const gameplayManipulationToggled = useCallback ((ev: MouseEvent<HTMLButtonElement>) => {
            // This triggers an event on C# side and C# designates the method to implement.
            trigger("BetterBulldozer", "GameplayManipulationToggled");
        }, []);
        
        // These convert integer casts of Enums into booleans.
        const raycastingMarkers : boolean = raycastTarget == 2;
        const raycastingAreas : boolean = raycastTarget == 1;
        const surfacesFilter : boolean = areasFilter == 16; 
        const spacesFilter : boolean = areasFilter == 8;
        const staticObjectMarkersFilter : boolean = markersFilter == 2; 
        const markerNetworkFilter : boolean = markersFilter == 8;

        var result = Component();
        if (bulldozeToolActive) {
            result.props.children?.unshift
            (
                /* 
                Add a new section before other tool options sections with translated title based of this localization key. Localization key defined in C#.
                Add a new Tool button into that section. Selected is based on Anarchy Enabled binding. 
                Tooltip is translated based on localization key. OnSelect run callback fucntion here to trigger event. 
                Anarchy specific image source changes bases on Anarchy Enabled binding. 
                */
                <>
                    { raycastingAreas ?
                    // This section is only showing if Raycasting areas.
                    <Section title={engine.translate("YY_BETTER_BULLDOZER.Filter")}>
                        <ToolButton className = {theme.button} selected={surfacesFilter} tooltip = {"YY_BETTER_BULLDOZER_DESCRIPTION.SurfacesFilterButton"} onSelect={surfacesFilterToggled} src="coui://ui-mods/images/SurfaceIconBetterBulldozer.svg"></ToolButton>
                        <ToolButton className = {theme.button} selected={spacesFilter} tooltip = {"YY_BETTER_BULLDOZER_DESCRIPTION.SpacesFilterButton"} onSelect={spacesFilterToggled} src="coui://ui-mods/images/AreaToolBetterBulldozer.svg"></ToolButton>
                    </Section>
                    : <></>
                    }
                    { raycastingMarkers ?
                    // This section is only showing if Raycasting markers.
                    <Section title={engine.translate("YY_BETTER_BULLDOZER.Filter")}>
                        <ToolButton className = {theme.button} selected={staticObjectMarkersFilter} tooltip = {"YY_BETTER_BULLDOZER_DESCRIPTION.StaticObjectsFilterButton"} onSelect={staticObjectsFilterToggled} src="coui://ui-mods/images/MarkersIconBetterBulldozer.svg"></ToolButton>
                        <ToolButton className = {theme.button} selected={markerNetworkFilter} tooltip = {"YY_BETTER_BULLDOZER_DESCRIPTION.NetworksFilterButton"} onSelect={networksFilterToggled} src="coui://uil/Standard/DottedLinesMarkers.svg"></ToolButton>
                    </Section>
                    : <></>
                    }
                    <Section title={engine.translate("Toolbar.TOOL_MODE_TITLE")}>
                        <ToolButton className = {theme.button} selected={gameplayManipulation} tooltip = {"YY_BETTER_BULLDOZER_DESCRIPTION.GameplayManipulationButton"} onSelect={gameplayManipulationToggled} src="coui://uil/Standard/CubeSimulation.svg"></ToolButton>
                        <ToolButton className = {theme.button} selected={bypassConfirmation} tooltip = {"YY_BETTER_BULLDOZER_DESCRIPTION.BypassConfirmationButton"} onSelect={bypassConfirmationToggled} src="coui://uil/Standard/BypassQuestionmark.svg"></ToolButton>
                        <ToolButton className = {theme.button} selected={raycastingMarkers} tooltip = {"YY_BETTER_BULLDOZER.RaycastMarkersButton"} onSelect={raycastMarkersButtonToggled} src="coui://uil/Standard/DottedLinesMarkers.svg"></ToolButton>
                        <ToolButton className = {theme.button} selected={raycastingAreas} tooltip = {"YY_BETTER_BULLDOZER.RaycastAreasButton"} onSelect={raycastAreasButtonToggled} src="coui://ui-mods/images/SurfaceIconBetterBulldozer.svg"></ToolButton>
                    </Section>
                </>
            );
        }
        return result;
    };
}
