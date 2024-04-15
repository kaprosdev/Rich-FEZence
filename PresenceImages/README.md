# Presence Images

## What is this?
The images in this folder are extra game screenshots to stand in for presence states that don't have map previews in the base game.

If you're running this using a different application's ID, these are the only images you'd need that don't come from unpacking the game.

## Readying assets for a Discord application
Aside from these images, all of the images this mod expects in the connected Discord application are in the `other textures/map_screens` subdirectory of a FEZ unpack (excluding `shine_rays.png`).

Discord requires assets to be 512x512 at minimum, so resize all of those images using Nearest Neighbor (I used https://redketchup.io/bulk-image-resizer). 
Don't rename them - names should be all-lowercase `snake_case` corresponding to the name of the room they belong to.

All of the extra images included here should already be 512x512.

## What are *these specific* images for?
Spoilers (for the mod and FEZ):
- `start_menus.png` is displayed in the normal main menu and in levels that do not have a corresponding image.
- `start_menus_glitch.png` [eventually] is displayed in the glitched main menu.
- `elders.png` should be self-explanatory (elders isn't in the map and therefore doesn't have a preview).
- `elders_glitch.png` [eventually] is displayed between the Hexahedron breaking and the reboot.
- `descending.png` is displayed during the 32-bit ending cutscene.
- `transcending.jpg` is displayed during the 64-bit ending cutscene.
- `newgameplus.png` is a small icon that will appear beside the main level image if you're in NG+ (i.e. have first-person mode).
- `newgameplusplus.png` is a small icon that will appear beside the main level image if you have anaglyph 3D unlocked - I couldn't remember if this was contingent on being an NG+, so the name is whatever