# Copilot Instructions for "Spawn Mod Content" Mod

## Mod Overview and Purpose

The "Spawn Mod Content" mod, crafted primarily for personal use by the developer Mlie, serves as a powerful tool for mod developers and testers in RimWorld. Its primary goal is to facilitate the bulk spawning of mod items and entities on the game map, which aids in comprehensive testing and verification before mod publishing. This mod surpasses the vanilla "Make colony" action by allowing the selection of specific mods to load items from, making it a valuable resource for reviewing mod content quickly and efficiently.

## Key Features and Systems

- **Selectable Mod Content Loading**: Users can choose which mod's content to spawn, assisting in targeted testing.
- **Comprehensive Spawning**: 
  - Generates a power grid covering the entire map.
  - Includes a powerful power-node connected to this grid with 900,000W output for robust testing conditions.
  - Spawns all items, buildings (with recipes added to workbenches), a pawn from each race, all terrain definitions, and all plants—each in unique zones.
- **Map-Size Independence**: The mod functionality is consistent across different map sizes and is not limited to a confined area like the vanilla actions.

## Coding Patterns and Conventions

- **Namespace and Class Organization**: The mod maintains clear organization with a primary namespace encapsulating mod logic, ensuring logical grouping of functionalities.
- **Method Naming**: Methods use descriptive, camelCase naming conventions, reflecting their purposes, e.g., `clearAllHomeArea`, `DoSpawnModContent`.
- **Code Readability**: Maintain readability with meaningful variable names and comments where complex logic is implemented, aiding other developers in understanding the flow.

## XML Integration

- **Definitions**: Utilizes XML files to define ThingDefs critical to the mod's functionality, such as `PowerNode.xml` located in the `Defs` directory.
- **Translation Templates**: Organized translation templates under `Source\TranslationTemplate\ThingDef\PowerNode.xml` to manage localization demands, ensuring mod content is accessible in multiple languages.

## Harmony Patching

- **Method Overriding**: Uses Harmony to override or extend base game methods where necessary, allowing for seamless integration of new functionalities introduced by the mod.
- **Patch Management**: Ensure that patches are well-documented and minimal to reduce conflicts with other mods and future updates of RimWorld.

## Suggestions for Copilot

- **AI-Assisted Code Suggestions**: Enable suggestions for C# that leverage existing method patterns and naming conventions for consistency across the codebase.
- **XML Schema Awareness**: Ensure AI understands the structure and potential extensions in `ThingDef` files for XML-based suggestions.
- **Harmony Integration**: Leverage Copilot to suggest elegant Harmony patch structures and potential hooks for efficiently extending game methods without redundancies or conflicts.
- **Refactor Proposals**: Use Copilot to propose refactoring options for improved efficiency and readability, especially in complex logic within `DebugAutotests.cs`.
- **Testing Scenarios**: Suggest comprehensive testing scripts and scenarios that span the functionality covered by the mod to help streamline debugging and verification efforts.

By adhering to these guidelines and utilizing Copilot effectively, contributors can successfully navigate and enhance the "Spawn Mod Content" mod for RimWorld.

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

