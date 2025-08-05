Here's a detailed `.github/copilot-instructions.md` file for the FishIndustry mod project:


# GitHub Copilot Instructions for FishIndustry Mod

## Mod Overview and Purpose

**Mod Name:** FishIndustry (Continued)

**Description:**  
FishIndustry is a mod that expands the culinary and recreational aspects of RimWorld by introducing fish-related resources, buildings, and activities. Originally created by Rikiki, this continued version updates and refines the mod to enhance players' gameplay experience with fishing and aquaculture systems. The primary goal of the mod is to allow players to fully utilize water bodies in their colonies for food production and leisure.

## Key Features and Systems

1. **Fishing Zone:**
   - Designate special zones where colonists can fish. 
   - Beware of dangerous fauna like tailteeths depending on the biome.

2. **Fishing Pier:**
   - A structure that can be built over water for safe fishing. 
   - Adding corn or rice can improve fishing efficiency.

3. **Aquaculture Basin:**
   - Advanced structure for breeding aquatic species.
   - Requires "Aquaculture" research after "Hydroponics."
   - Provides a renewable and high-yield meat source.

4. **New Recipes:**
   - Create grilled fish simple meals at the campfire.

5. **New Items and Activities:**
   - Oyster as a joy food.
   - Meditative activity at the fishing pier to watch the water.

6. **Job Management:**
   - Fishing activities are classified as hunting jobs.
   - Prioritized as: hunting > operating aquaculture basin > fishing at pier.

## Coding Patterns and Conventions

- **Class Naming Conventions:**
  - Classes are prefixed according to their functionality (e.g., `Building_`, `Zone_`, `JobDriver_`).

- **Accessibility:**
  - Internal classes are used for entities not needed outside their defining assembly.

- **Inheritance:**
  - Inherits from relevant base classes such as `Building_WorkTable` for buildings.

- **Method Names:**
  - Descriptive and follow PascalCase.
  - Public methods reflect actions or states (e.g., `TryFeedFishes`, `StartMicroFungusInfestation`).

## XML Integration

- Avoid overwriting default game definitions by using custom XML for new items.
- Utilize XML PatchOperation to modify existing definitions without conflicts (overwriting has been removed in this version).

## Harmony Patching

- Harmony patches should be used to safely alter existing game functionality.
- Ensure each patch is well-documented, indicating what's modified and why.
- Harmony can assist in ensuring compatibility with other mods by non-destructive alterations.

## Suggestions for Copilot

While utilizing GitHub Copilot for this mod, consider the following suggestions:

1. **Pattern Recognition:**
   - Ensure that Copilot follows established naming and coding conventions specific to RimWorld modding.

2. **Harmony Patches:**
   - Help streamline the generation of harmony patches from within methods to encourage non-destructive changes.

3. **XML Patch Creation:**
   - Assist in generating XML patch operations for minor modifications to existing definitions.

4. **New Class Scaffolding:**
   - Aid in scaffolding typical class structures, especially for extending existing RimWorld functionality.

5. **Error Handling:**
   - Propose standard error handling patterns, particularly for catching and dealing with game-specific exceptions.

By adhering to these guidelines, we can ensure that the FishIndustry mod remains a stable and enjoyable addition to any player's mod list, enhancing their RimWorld experience with new aquatic adventures.


This document is structured to provide clear guidelines and suggestions that align with both the technical and conceptual directions of the FishIndustry mod.
