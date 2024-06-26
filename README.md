﻿Easily delete things that you otherwise could not delete or easily delete.

Sully made a Youtube video about custom car parks (aka Parking Lots) and this functionality helped make that safe.

## Dependencies
Unified Icon Library
I18n Everywhere (Soft dependency)

## Donations
If you want to say thank you with a donation you can do so on Paypal.

## Translations
I am looking for volunteers to help translate the mod into the official languages. For those interested please discuss the translation project in the discord link. CSL2:CODEMODS -> mods-wip -> Better Bulldozer: Translations. CrowdIn Link available there or here.
The mod is fully or partially translated into: Chinese Simplified, German, Korean, Polish, Spanish, Chinese Traditional, French, Italian, Brazilian Portuguese and Russian.

# Detailed Descrption
* Remove Moving Objects and Cims Tool Mode that allows you to use the bulldozer on moving objects such as vehicles or cims.
* Icon for bypass confirmation that disables the prompt for whether you are sure you want to demolish a building.
* Icon and filters to show and EXCLUSIVELY target invisible paths, roads, etc or static object markers with the bulldozer and remove paths, roads, etc or static object markers. See below.
* Icon and filters to EXCLUSIVELY target surfaces or spaces with the bulldozer and remove with one click. Works both inside and outside of assets.
* Icon to EXCLUSIVELY target Net-Lanes and Net-Lane-Geometry prefabs with the bulldozer such as interconnected fences, interconnected hedges, linear street markings. You cannot create these in-game without EDT Mod.
* Automatically manages the "Show Markers" DevUI option while plopping, drawing, and bulldozing applicable objects and networks. 
* Opt-in option to automatically remove manicured grass surfaces during placement or spawning.
* Opt-in option to automatically remove fences and hedges after placement, spawning, and updating. (still visible during placement and while moving)
* Opt-in option to automatically remove branding objects and advertisements after placement, spawning, and updating. (still visible during placement and while moving)
* Remove Sub-Elements Tool Mode See Below.

## Remove Sub-Elements Tool Mode
Removes props, trees, decals, fences, hedges, sub-buildings, extensions, and networks from assets. This tool can break connectivity within assets. Some elements are more safe to remove such as: props, trees, decals, fences, hedges, and sub-buildings. Some elements are less safe to remove such as: networks and extensions. You can prohibit removing those in the settings.

Terminology:
* Sub-Elements is an umbrella term that includes all props, trees, decals, fences, hedges, sub-buildings, extensions, and networks that are part of a building or network asset.
* Sub-buildings are separate buildings attached to the service building’s existing lot.
* Extensions are visible upgrades connected directly to the main building.
* 'Permanently Removes' means that anytime the object would regenerate it will be automatically removed shortly afterwards.

Selection Modes:
* Single Item - similar but more powerful than DevUI: Simulation -> Debug Toggle + UIBindings -> ActionsSection -> Delete. Props, trees, decals, fences, and hedges can/will regenerate later. Sub-buildings, some extensions, and networks do not regenerate.
* Exact Match - Selects all exact matches of that sub-element. (i.e. All oak trees in this park). Networks are not supported. 'Permanently Removes' props, trees, decals, fences, hedges.
* Similar Category - Selects all sub-elements in a similar category within the building, subbuilding, extension or network and 'permanently removes' them. Categories include: trees, plants, street lights, trash bins, branding objects and advertisements, activity locations, and all hedges and fences. (i.e. all trees in this asset).
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
* Klyte45, Algernon - Help with UI, Cooperative Development and Code Sharing
* T.D.W., krzychu124, Triton Supreme, and Quboid - Cooperative Development and Code Sharing
* City Rat - Icons
* Localization - Hendrix (German), RilkeXS (Chinese Simplified), TwotoolusFly_LSh.st (Korean), OWSEEX (Russian), felipecollucci (Brazilian Portuguese), Karmel68 and Lisek (Polish), Citadino (Spanish), Karg (French), yui hei lai (Chinese Traditional)
* Testing, Feedback - Dante, starrysum, HarbourMaster Jay, Dome, Tigon Ologdring, BruceyBoy, RaftermanNZ, Elektrotek, SpaceChad 

This mod is not affiliated with any C:S1 mod with similar titles. 