# GitHub Copilot Instructions for FishIndustry (Continued) Mod

Welcome to the FishIndustry (Continued) mod for RimWorld! This mod adds various fish-related resources, buildings, and activities to your RimWorld game, allowing you to catch and breed fish, create new recipes, and experience new joy activities.

## Mod Overview and Purpose

FishIndustry (Continued) aims to enhance the fishing experience in RimWorld by adding several new elements:

- **Fishing Zone**: A zone where fishers can catch fish from the water, with varying dangers depending on the biome.
- **Fishing Pier**: A simple building that allows for safe fishing with the option to use corn or rice to enhance fish catch rates.
- **Aquaculture Basin**: An advanced building used for breeding aquatic species, providing a renewable source of meat.
- **New Recipes**: Added recipes at the campfire to cook grilled fish.
- **Joy Food**: Oyster as a joy food.
- **Meditative Joy Activity**: Watching shiny water on the fishing pier.

## Key Features and Systems

### Fishing Zone
Players can designate a zone where fishers can fish. The danger level of the waters varies based on the biome, affecting the success rate of fishing.

### Fishing Pier
A simple building built on water that allows hunters to catch fish safely. Using corn or rice can increase the fish catch rate.

### Aquaculture Basin
An advanced building used to breed aquatic species. It requires the "Aquaculture" research after "Hydroponics" and provides a renewable source of meat.

### New Recipes
Several new recipes are added at the campfire, allowing players to cook grilled fish.

### Joy Food
Oyster is introduced as a joy food in the game.

### Joy Activity
A new meditative joy activity is added on the fishing pier, allowing players to watch the water.

## Coding Patterns and Conventions

### C# Files
The C# files in this mod follow standard RimWorld conventions. Each file typically contains a class that extends or modifies existing RimWorld classes. For example:
- `Alert_AquacultureBasinCriticalIssue.cs` defines an alert for issues with the aquaculture basin.
- `Building_FishingPier.cs` defines the fishing pier building with methods for its functionality.

### XML Files
The XML files are used to define new game elements such as job definitions, joy giver definitions, terrain definitions, and thing definitions. For example:
- `Jobs_Joy.xml` defines new jobs related to joy.
- `JoyGivers.xml` defines new joy giver definitions.
- `Terrain_FishingPierFloor.xml` defines the terrain for the fishing pier floor.
- `Building_FishingPier.xml` defines the fishing pier building.

## XML Integration

XML files are crucial for defining new game elements. They should be placed in the appropriate directories within the mod's structure. Here is an example of how to structure an XML file:

xml
<Defs>
  <JoyGiverDef>
    <defName>WatchShinyWater</defName>
    <label>Watch shiny water</label>
    <description>Stand by the water and watch the shimmering waves.</description>
    <jobDef>Job_Joy_WatchShinyWater</jobDef>
    <requireCapablePawn>true</requireCapablePawn>
    <targetFinderClass>TargetFinder_ShinyWater</targetFinderClass>
  </JoyGiverDef>
</Defs>


## Harmony Patching

Harmony patching is used to modify existing RimWorld code. Patches are typically defined in C# files using the Harmony library. Here is an example of a Harmony patch:

csharp
using HarmonyLib;
using RimWorld;
using Verse;

namespace FishIndustry
{
    [HarmonyPatch(typeof(FishPier), "SpawnSetup")]
    public static class Patch_FishPier_SpawnSetup
    {
        [HarmonyPostfix]
        public static void Postfix(FishPier __instance)
        {
            // Custom logic after the original SpawnSetup method
        }
    }
}


## Suggestions for Copilot

When using GitHub Copilot for this mod, keep the following in mind:

- **Contextual Understanding**: Copilot can generate code snippets, but it may not always understand the full context of the mod. Review and test generated code thoroughly.
- **Customization**: Copilot may suggest code that aligns with common patterns but may need customization for specific mod requirements.
- **Integration**: Ensure that generated code integrates correctly with existing mod structures, especially XML and C# files.
- **Testing**: Always test generated code in the game to ensure it works as expected and does not cause errors.

Feel free to ask for help on the Discord-channel if you encounter any issues or have specific questions about the mod's functionality.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

