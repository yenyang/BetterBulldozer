import { ModRegistrar } from "modding/types";
import { BetterBulldozerComponent } from "mods/betterBulldozerSection";

const register: ModRegistrar = (moduleRegistry) => {
     // console.log('mr', moduleRegistry);
     moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', BetterBulldozerComponent(moduleRegistry));
}

export default register;