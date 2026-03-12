# Copilot Instructions for Spawn Mod Content

## Mod Overview and Purpose

**Mod Name:** Spawn Mod Content

The Spawn Mod Content mod for RimWorld is designed to enhance development testing by offering an extended developer tool. This mod introduces a feature that allows modders to spawn in-game content from selected mods. It is particularly useful for checking and reviewing mod content such as items, buildings, and terrains before release. This functionality surpasses the limitations of the vanilla "Make colony" action by ensuring comprehensive testing regardless of map size.

## Key Features and Systems

- **Mod Selection for Load:** Users can choose which mods to load items from, facilitating targeted testing and evaluation of specific mod content.

- **Complete Content Spawning:** Unlike the vanilla option, this mod spawns a detailed environment:
  - **Power Grid:** A power grid spans the entire map.
  - **Testing Power Node:** Includes a power node supplying 900,000 watts.
  - **All Items and Buildings:** Every item and building from the selected mod appears, with all workbenches equipped with their respective recipes.
  - **Diverse Pawn Roster:** Spawns a pawn from each defined race in the selected mod.
  - **Terrains and Plants:** Each terrain definition and plant type is placed in individual zones, allowing for detailed landscape and flora inspection.

## Coding Patterns and Conventions

- **Namespace Naming:** Ensure that namespaces follow the convention: `SpawnModContent.[SubmoduleName]`.
- **Class Naming:** Classes use PascalCase for clear readability and differentiation from methods.
- **Method Naming:** Methods use PascalCase to differentiate them from variables, which are in camelCase.
- **Comments:** Use XML comments for method descriptions to support in-editor documentation lookup.

## XML Integration

- **XML Def Files:** Utilize RimWorld's XML Def system to define new items, buildings, and races. Ensure Defs are well-structured and validated against RimWorld's schema standards.
- **XML Patching:** Leverage XML patching to modify existing game content without overriding base files, ensuring mod compatibility.

## Harmony Patching

- **Harmony Version:** Ensure you are using the compatible version of Harmony for RimWorld modding.
- **Patch Application:** Use Harmony for runtime method injection, allowing changes to vanilla methods or adding new behavior.
- **Safe Patching Practice:** Always use Prefix and Postfix where applicable to maintain game stability and consider using transpilers only when necessary and with sufficient testing.

## Suggestions for Copilot

- **Contextual Assistance:** Allow Copilot to assist by providing suggestions for common practices in C# and RimWorld modding, such as naming conventions and Linq queries for handling collections.
- **Pattern Recognition:** Let Copilot predict code structure based on recognized design patterns, especially when dealing with Harmony patches or XML handling.
- **Error Fix Suggestions:** Enable Copilot to suggest fixes for common C# compile errors or exceptions related to RimWorld-specific coding environments, helping streamline the debugging process.
- **Automation of Repetitive Code:** Utilize Copilot to automate repetitive code segments, such as property definitions or iterative item spawning logic.

By following these guidelines, developers and modders can leverage GitHub Copilot effectively to streamline the development process of the Spawn Mod Content mod, ensuring robust and maintainable code.
