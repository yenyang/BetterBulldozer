﻿Easily delete things that you otherwise could not delete or easily delete.

CloverPie made a instructional video about most of the features of Better Bulldozer. You can find it on Youtube.
Sully made a Youtube video about custom car parks (aka Parking Lots) and this functionality helped make that safe.

## Dependencies
Unified Icon Library

## Donations
If you want to say thank you with a donation you can do so on Paypal.

## Translations
I am looking for volunteers to help translate the mod into the other languages. For those interested please discuss the translation project in the discord link. CSL2:CODEMODS -> mods-wip -> Better Bulldozer: Translations. CrowdIn Link available there or here.
The mod is fully or partially translated into: Chinese Simplified, German, Korean, Polish, Spanish, Chinese Traditional, French, Italian, Brazilian Portuguese, Russian, and Portuguese. 

## Supplemental Mods
I18n Everywhere and European Portuguese Localization (Only needed for European Portuguese Translations)

# Detailed Description
* Remove Moving Objects and Cims Tool Mode that allows you to use the bulldozer on moving objects such as vehicles or cims including broken, stuck, and vehicles placed by the player. Use radius selection to remove any that you cannot target with single selection. 
* Icon for bypass confirmation that disables the prompt for whether you are sure you want to demolish a building.
* Icon and filters to show and EXCLUSIVELY target invisible paths, roads, etc or static object markers with the bulldozer and remove paths, roads, etc or static object markers. See below.
* Icon and filters to EXCLUSIVELY target surfaces or spaces with the bulldozer and remove with one click. Works both inside and outside of assets.
* Icon to EXCLUSIVELY target Net-Lanes and Net-Lane-Geometry prefabs with the bulldozer such as interconnected fences, interconnected hedges, linear street markings. You cannot create these in-game without EDT Mod.
* Automatically manages the "Show Markers" DevUI option while plopping, drawing, and bulldozing applicable objects and networks. 
* Opt-in option to automatically remove manicured grass surfaces during placement or spawning.
* Opt-in option to automatically remove fences and hedges after placement, spawning, updating, and when a save is loaded. (still visible during placement and while moving). Intended for players who want to draw their own fences everywhere and do not want to delete them from each asset.
* Opt-in option to automatically remove branding objects and advertisements after placement, spawning, updating, and when a save is loaded. (still visible during placement and while moving). Intended for players who do not want any branding objects anywhere and may want control over where and when they occur.
* Remove Sub-Elements Tool Mode See Below.
* Options menu button to restore fences and hedges that were automatically removed. This restoration takes time.
* Options menu button to restore branding objects and adverstisements that were automatically removed. This restoration takes time.
* Options menu button to remove all manicured grass surfaces from buildings.
* Filters for the vanilla bulldoze tool with no tool modes selected including: Networks, Buildings, Trees, Plants, Decals, and Props. Toggling these filters off makes it so the bulldoze tool cannot remove those items.

## Remove Sub-Elements Tool Mode
Removes props, trees, decals, fences, hedges, sub-buildings, extensions, and networks from assets. This tool can break connectivity within assets. Some elements are more safe to remove such as: props, trees, decals, fences, hedges, and sub-buildings. Some elements are less safe to remove such as: networks and extensions. You can prohibit removing those in the settings.

Terminology:
* Sub-Elements is an umbrella term that includes all props, trees, decals, fences, hedges, sub-buildings, extensions, and networks that are part of a building or network asset.
* Sub-buildings are separate buildings attached to the service building’s existing lot.
* Extensions are visible upgrades connected directly to the main building.
* 'Permanently Removes' means that anytime the object would regenerate it will be automatically removed shortly afterwards.

Selection Modes:
* Single Item - similar but more powerful than DevUI: Simulation -> Debug Toggle + UIBindings -> ActionsSection -> Delete. Props, trees, decals, fences, and hedges can/will REGENERATE later. Networks do not regenerate.
* Exact Match - Selects all exact matches of that sub-element. (i.e. All oak trees in this park). Networks are not supported. 'Permanently Removes' props, trees, decals, fences, hedges.
* Similar Category - Selects all sub-elements in a similar category within the building, subbuilding, extension or network and 'permanently removes' them. Categories include: trees, plants, street lights, trash bins, branding objects and advertisements, activity locations, and all hedges and fences. (i.e. all trees in this asset). 'Permanently Removes' props, trees, decals, fences, hedges.
* Reset Asset - Reset assets by selecting ones that have any sub-elements 'permanently removed' using Exact Match or Similar Category.

## Safely Remove Button
There is a safely remove button in the options menu. It is only recommeneded if you have been using Exact Match and Similar Category to remove sub-elements. Pressing this button and confirming it will reset all assets that have had sub-elements 'permanently removed'.

## Invisible Paths and Markers
Drawing invisible paths and markers is an unsupported feature of the game. You need DevUI to access the 'Add Object' menu via the home button to draw invisible paths and markers, unless another mod makes this more convenient.

With the Bulldoze tool you can select an icon and filters to show and EXCLUSIVELY demolish invisible networks or invisible parking decals, various spots, points, and spawners, BUT SAVE BEFORE HAND!

One configuration I built/demolished resulted in a crash to desktop because I left a segment that was too short. Deleting them in a different order worked.

# Support
I will respond on the code modding channels on **Cities: Skylines Modding Discord**

# Credits 
* yenyang - Mod Author
* Chameleon TBN - Testing, Feedback, Icons, and Logo
* Sully - Testing, Feedback, and Promotional Material.
* CloverPie - Promotional Material.
* Klyte45, T.D.W., Algernon - Help with UI, Cooperative Development and Code Sharing
* krzychu124, Triton Supreme, and Quboid - Cooperative Development and Code Sharing
* City Rat - Icons
* Localization - Hendrix (German), RilkeXS (Chinese Simplified), TwotoolusFly_LSh.st (Korean), OWSEEX and krugl1y (Russian), felipecollucci (Brazilian Portuguese), Karmel68 and Lisek (Polish), Citadino (Spanish), Karg and Morgan (French), yui hei lai and allegretic (Chinese Traditional), Furios (Italian), Ti4goc (Portuguese)
* Testing, Feedback - Dante, starrysum, HarbourMaster Jay, Dome, Tigon Ologdring, BruceyBoy, RaftermanNZ, Elektrotek, SpaceChad, CanadianMoosePlays

This mod is not affiliated with any C:S1 mod with similar titles. 