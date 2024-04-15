# Rich FEZence

Discord Rich Presence for FEZ!

## Features
- Map preview image
- "Level name" tooltip (roughly cleaned up from level names in code)
- Pause state ("Exploring" or "Paused")
- Cube shards + anticubes count
- Some special cases...

## Limitations
- Any mod levels (or levels that don't have map previews or hardcoded special cases) won't display their map preview image.
	- Preview images can't be uploaded dynamically - they're part of the Discord "application".
	- The hardcoded client ID is one I maintain that has all the base-game and included map preview images uploaded already.
- Cannot be run from a ZIP file - it must be an unpacked mod folder. 
	- This is due to the Discord Game SDK being a separate, non-.NET library which cannot be loaded from memory (without some temporary file gymnastics).