import { ModRegistrar } from "cs2/modding";
import { BetterBulldozerComponent } from "mods/betterBulldozerSections/betterBulldozerSections";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import mod from "../mod.json";

const register: ModRegistrar = (moduleRegistry) => {
     // To find modules in the registry un comment the next line and go to the console on localhost:9444. You must have -uiDeveloperMode launch option enabled.
     // console.log('mr', moduleRegistry);

     // The vanilla component resolver is a singleton that helps extract and maintain components from game that were not specifically exposed.
     VanillaComponentResolver.setRegistry(moduleRegistry);

     // This extends mouse tooltip options with mod component. It may or may not work with gamepads.
     moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', BetterBulldozerComponent);

     // This is just to verify using UI console that all the component registriations was completed.
     console.log(mod.id + " UI module registrations completed.");
}

export default register;