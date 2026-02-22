# PriorityClean - Universal Patch

This mod patches the capabilities of all pawns (notably Mechanoids), so that if they are capable of performing cleaning tasks, then they are also made capable of performing PriorityClean tasks (and will prioritize them)

## Key Features
- Enables `PriorityCleaning` for mechanoids that already support `Cleaning`.
- Makes colony mechanoids prioritize priority filth before normal cleaning filth.
- Supports vanilla and modded mechs that expose cleaning work through standard work type defs.
- Keeps humanlike PriorityClean behavior aligned with the original mod.
- Should work with any modded pawn/race/mechanoid (testing limited)

## Requirements
- Requires **PriorityClean** (`fyarn.PriorityClean`)
<https://steamcommunity.com/sharedfiles/filedetails/?id=1294779672>

## Load Order
Load this mod **after**:
- `PriorityClean`
- mods that add mechanoids
- mods that alter mechanoid jobs/workgivers

## Can I Add Mid-Game?
- Should be ok. Works in testing.
- May show one-time errors when loading existing saves.
- **Always** back up saves before changing your mod list.

## Removal Safety
- Should be ok. Works in testing.
- If you remove `PriorityClean`, it is recommended to remove this patch mod as well.
- **Always** back up saves before changing your mod list.

## AI Disclosure
This mod was primarily built from AI-generated content. It was vibe-coded, then manually reviewed and tested before release.

## Technical Notes (Short)
This patch applies XML and runtime sync to:
- add `PriorityCleaning` to eligible mech cleaning work type lists,
- route PriorityClean workgiver defs through a mech-compatible class,
- gate regular mech `CleanFilth` work while reachable priority filth exists,
- keep life-stage cleaning restrictions mirrored to `PriorityCleaning` where needed.

### Github: <https://github.com/Gadolinonym/PriorityClean-UniversalPatch>
### Steam Workshop: <https://example.com/>