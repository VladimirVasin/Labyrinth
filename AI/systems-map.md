# System Owner Map

Detailed file-owner navigation map for AI agents working on `Labyrinth`.

Use `AI/system-tree.md` first for broad, ambiguous, architectural, or cross-system work, then use this file before broad code searches and implementation. Owner cards are starting points, not hard boundaries. If code disagrees with this map, trust code, then update this file.

## Update Rules

- Update this file when adding, moving, deleting, splitting, or substantially changing source files.
- Update this file when a feature changes ownership, responsibilities, or cross-system dependencies.
- Update this file when a new gameplay, UI, simulation, rendering, or infrastructure system appears.
- Keep this file navigational. Do not paste code here.
- Also add a short note to `AI/work-log.md` after meaningful implementation work.

## Navigation Rules

- For implementation, bugfix, refactor, or investigation tasks, consult the relevant owner cards here before running broad `rg` searches.
- For broad or unclear work, consult `AI/system-tree.md` first to identify affected systems, then inspect the relevant owner cards here.
- If a task crosses systems, inspect every relevant card and the cross-system links section.
- If the task is unclear, start with `Game Bootstrap / Runtime Orchestration`, then follow the related cards.
- For generated Unity project file changes, keep `Assembly-CSharp.csproj` in sync with added or removed `.cs` files.

## Owner Cards

### Game Bootstrap / Runtime Orchestration

Responsibility:
- Creates and wires the runtime scene, game mode, generated dungeon, base, HUDs, controllers, and services.
- Coordinates system-level events: start game, level switching, selected hero visibility, building completion, combat checks, objectives, and global state.

Primary files:
- `Assets/Scripts/Core/GameBootstrap.cs`
- `Assets/Scripts/Core/GameBootstrap.StartGame.cs`
- `Assets/Scripts/Core/GameBootstrap.Hud.cs`
- `Assets/Scripts/Core/GameBootstrap.Selection.cs`
- `Assets/Scripts/Core/GameBootstrap.Visibility.cs`
- `Assets/Scripts/Core/GameBootstrap.Map.cs`
- `Assets/Scripts/Core/GameBootstrap.DungeonLevels.cs`
- `Assets/Scripts/Core/GameBootstrap.BuildCosts.cs`
- `Assets/Scripts/Core/RuntimeBootstrapper.cs`

Related systems:
- Base development, heroes, maze generation/rendering, mobs, combat, objectives, UI, audio, time scale.

Check when:
- Changing game start/reset, pause/resume state, generated maps, dungeon level switching, active controllers, visibility refresh, selected hero behavior, or top-level event flow.

### Base Development / Economy / Building Unlocks

Responsibility:
- Owns base building state, counts, positions, costs, upgrade levels, construction unlock gates, storage capacities, and base resource production rules.

Primary files:
- `Assets/Scripts/Core/BaseDevelopment.cs`
- `Assets/Scripts/Core/BaseDevelopment.Balance.cs`
- `Assets/Scripts/Core/BuildingCost.cs`
- `Assets/Scripts/Core/BuildingUpgradeType.cs`
- `Assets/Scripts/Core/GameBootstrap.BuildCosts.cs`
- `Assets/Scripts/Core/GameBootstrap.Upgrades.cs`
- `Assets/Scripts/Core/ResourceWallet.cs`
- `Assets/Scripts/Core/ResourceProductionController.cs`
- `Assets/Scripts/Core/MarketExchange.cs`

Building action partials:
- `Assets/Scripts/Core/GameBootstrap.Construction.cs`
- `Assets/Scripts/Core/GameBootstrap.LumberjackCamp.cs`
- `Assets/Scripts/Core/GameBootstrap.CartographerHouse.cs`
- `Assets/Scripts/Core/GameBootstrap.Forge.cs`
- `Assets/Scripts/Core/GameBootstrap.Infirmary.cs`
- `Assets/Scripts/Core/GameBootstrap.Chapel.cs`
- `Assets/Scripts/Core/GameBootstrap.MinersGuild.cs`
- `Assets/Scripts/Core/GameBootstrap.Market.cs`
- `Assets/Scripts/Core/GameBootstrap.Antiquary.cs`
- `Assets/Scripts/Core/GameBootstrap.HeroesGuild.cs`
- `Assets/Scripts/Core/GameBootstrap.PeasantHuts.cs`

Related renderers:
- `Assets/Scripts/Base/BaseView.cs`
- `Assets/Scripts/Base/BuildingView.cs`
- Building-specific renderers under `Assets/Scripts/Maze/*Renderer.cs`

Check when:
- Changing costs, resource flows, storage, construction prerequisites, building status text, building upgrades, service availability, peasant huts, market trades, or production carts.

### Base Construction / Roads / City Ambience

Responsibility:
- Manages construction sites, builder workers, road building, bridges, city walkers, tax collectors, base carts, and building road/path registration.

Primary files:
- `Assets/Scripts/Core/BaseConstructionController.cs`
- `Assets/Scripts/Core/BaseAmbienceController.cs`
- `Assets/Scripts/Core/BaseAmbienceController.Runtime.cs`
- `Assets/Scripts/Core/CityAmbienceController.cs`
- `Assets/Scripts/Core/TaxCollectorController.cs`
- `Assets/Scripts/Core/HeroHouseFundCourierController.cs`
- `Assets/Scripts/Core/AmbientWalkerMoveAnimator.cs`
- `Assets/Scripts/Core/SubCellPathBuilder.cs`

Related files:
- `Assets/Scripts/Core/GameBootstrap.Construction.cs`
- `Assets/Scripts/Core/GameBootstrap.HeroCommute.cs`
- `Assets/Scripts/Core/GameBootstrap.PeasantHuts.cs`
- `Assets/Scripts/Maze/TerrainDecorationController.cs`
- `Assets/Scripts/Maze/TerrainDecorationController.Async.cs`
- `Assets/Scripts/Maze/TerrainDecorationController.Materials.cs`

Check when:
- Changing road timing/direction, bridge passability, construction worker behavior, caravan movement, city walkers, tax collectors, path smoothing, river blocking, or construction-site visuals.

### Heroes / Exploration / Progression

Responsibility:
- Owns hero model state, movement, memory, visit-aware patrol routing, visibility, inventory, injuries, lineage, blessings, vengeance quests, exploration target selection, entrance commute, and progression tuning.

Primary files:
- `Assets/Scripts/Hero/HeroController.cs`
- `Assets/Scripts/Hero/HeroModel.cs`
- `Assets/Scripts/Hero/HeroModel.Injuries.cs`
- `Assets/Scripts/Hero/HeroExplorer.cs`
- `Assets/Scripts/Hero/HeroExplorer.Pathfinding.cs`
- `Assets/Scripts/Hero/HeroExplorer.Patrol.cs`
- `Assets/Scripts/Hero/HeroExplorer.NearbyInteractions.cs`
- `Assets/Scripts/Hero/HeroExplorationCoordinator.cs`
- `Assets/Scripts/Hero/HeroMemory.cs`
- `Assets/Scripts/Hero/HeroVisibility.cs`
- `Assets/Scripts/Hero/HeroInventory.cs`
- `Assets/Scripts/Hero/HeroLineageState.cs`
- `Assets/Scripts/Hero/HeroVengeance.cs`
- `Assets/Scripts/Hero/HeroInjuryState.cs`
- `Assets/Scripts/Hero/HeroBlessings.cs`
- `Assets/Scripts/Hero/HeroBlessingCatalog.cs`
- `Assets/Scripts/Hero/HeroBlessingDefinition.cs`
- `Assets/Scripts/Hero/HeroBlessingType.cs`
- `Assets/Scripts/Hero/HeroKnightNameCatalog.cs`
- `Assets/Scripts/Hero/HeroState.cs`

Bootstrap integration:
- `Assets/Scripts/Core/GameBootstrap.Heroes.cs`
- `Assets/Scripts/Core/GameBootstrap.HeroCommute.cs`
- `Assets/Scripts/Core/GameBootstrap.Cartography.cs`
- `Assets/Scripts/Core/HeroConsumableAutomation.cs`

Visual files:
- `Assets/Scripts/Hero/HeroView.cs`
- `Assets/Scripts/Hero/HeroMemoryView.cs`
- `Assets/Scripts/Hero/HeroVisibilityView.cs`

Check when:
- Changing exploration AI, stale-route patrol behavior, common map usage, hero spawn/commute, stamina, XP, level scaling, item pickup, return behavior, combat wounds, death/rebirth, blessings, lineage, vengeance, or hero HUD stats.

### Combat

Responsibility:
- Resolves hero-vs-mob combat rounds, initiative, actions, damage, armor, guard, stamina, wounds, retreat, rewards, defeat events, and combat feedback text.

Primary files:
- `Assets/Scripts/Combat/CombatController.cs`
- `Assets/Scripts/Combat/CombatController.Round.cs`
- `Assets/Scripts/Combat/CombatController.Injuries.cs`
- `Assets/Scripts/Combat/DamageNumberView.cs`

Related systems:
- `Assets/Scripts/Hero/HeroModel.cs`
- `Assets/Scripts/Hero/HeroModel.Injuries.cs`
- `Assets/Scripts/Mobs/MobModel.cs`
- `Assets/Scripts/Mobs/MobController.cs`
- `Assets/Scripts/Core/GameBootstrap.Heroes.cs`
- `Assets/Scripts/Core/HeroGuildQuestController.cs`

Check when:
- Changing combat balance, action selection, armor/damage formula, mob rewards, injuries/scars, retreat/defeat flow, combat pacing, or combat UI feedback.

### Mobs / Encounters / Respawn

Responsibility:
- Spawns mobs, chooses mob species, handles density/respawn, displayed mob danger levels, wandering state, encounter detection, awareness, and mob queries.

Primary files:
- `Assets/Scripts/Mobs/MobManager.cs`
- `Assets/Scripts/Mobs/MobManager.SpawnSelection.cs`
- `Assets/Scripts/Mobs/MobManager.Queries.cs`
- `Assets/Scripts/Mobs/MobManager.EncounterAwareness.cs`
- `Assets/Scripts/Mobs/MobController.cs`
- `Assets/Scripts/Mobs/MobModel.cs`
- `Assets/Scripts/Mobs/MobState.cs`
- `Assets/Scripts/Mobs/MobView.cs`

Related systems:
- Combat, hero exploration, maze generation, visibility/fog, Heroes Guild quests.

Check when:
- Changing mob counts, spawn placement, darkness respawn, species weighting, displayed mob levels/labels, mob wandering, encounter stalls, visibility hiding, or mob stats.

### Maze Generation / Dungeon Topology

Responsibility:
- Builds the procedural dungeon grid, cells, caves, dedicated boss cave, branches, central room routes, stairs, keys, doors, ores, and validation.

Primary files:
- `Assets/Scripts/Maze/MazeGenerator.cs`
- `Assets/Scripts/Maze/MazeGenerator.Caves.cs`
- `Assets/Scripts/Maze/MazeBranchCarver.cs`
- `Assets/Scripts/Maze/MazeBranchCarver.Spacious.cs`
- `Assets/Scripts/Maze/MazeGenerationResult.cs`
- `Assets/Scripts/Maze/MazeGrid.cs`
- `Assets/Scripts/Maze/MazeCell.cs`
- `Assets/Scripts/Maze/MazeCellType.cs`
- `Assets/Scripts/Maze/MazeDirections.cs`
- `Assets/Scripts/Maze/MazeValidation.cs`
- `Assets/Scripts/Core/MazeGenerationSettings.cs`
- `Assets/Scripts/Core/MazeSizePreset.cs`

Related model files:
- `Assets/Scripts/Maze/ChestModel.cs`
- `Assets/Scripts/Maze/GoldIngotModel.cs`
- `Assets/Scripts/Maze/OreDepositModel.cs`

Check when:
- Changing map shape, guaranteed routes, central room, boss cave, stairs, caves, chests, ores, keys/doors, seed behavior, validation, or dungeon progression.

### Maze Rendering / Terrain / Voxel Visuals

Responsibility:
- Renders dungeon cells, terrain, base buildings, object models, voxel materials, generated textures, shadows, underlay, async rendering, and object visibility hooks.

Primary files:
- `Assets/Scripts/Maze/MazeRenderer.cs`
- `Assets/Scripts/Maze/MazeRenderer.Async.cs`
- `Assets/Scripts/Maze/MazeRenderer.Voxels.cs`
- `Assets/Scripts/Maze/MazeRenderer.Visibility.cs`
- `Assets/Scripts/Maze/MazeRenderer.Lighting.cs`
- `Assets/Scripts/Maze/MazeRenderer.Keys.cs`
- `Assets/Scripts/Maze/MazeRenderer.Underlay.cs`
- `Assets/Scripts/Maze/MazeTerrain.cs`
- `Assets/Scripts/Maze/MazeTerrain.Async.cs`
- `Assets/Scripts/Maze/TerrainDecorationController.cs`
- `Assets/Scripts/Maze/TerrainDecorationController.Async.cs`
- `Assets/Scripts/Maze/TerrainDecorationController.Materials.cs`

Voxel infrastructure:
- `Assets/Scripts/Core/VoxelVisuals.cs`
- `Assets/Scripts/Core/VoxelVisuals.Mesh.cs`
- `Assets/Scripts/Core/VoxelVisuals.Patterns.cs`
- `Assets/Scripts/Core/VoxelVisuals.Shadows.cs`
- `Assets/Scripts/Core/VoxelBurstView.cs`
- `Assets/Scripts/Core/VoxelFigurePartAnimator.cs`
- `Assets/Scripts/Core/GeneratedTextureLibrary.cs`

Building/object renderers:
- `Assets/Scripts/Maze/BuildingDetailRenderer.cs`
- `Assets/Scripts/Maze/BuildingUpgradeVisuals.cs`
- `Assets/Scripts/Maze/ChestView.cs`
- `Assets/Scripts/Maze/DungeonStairsRenderer.cs`
- `Assets/Scripts/Maze/OreDepositRenderer.cs`
- `Assets/Scripts/Maze/LumberjackCampRenderer.cs`
- `Assets/Scripts/Maze/PeasantHutRenderer.cs`
- `Assets/Scripts/Maze/CartographerHouseRenderer.cs`
- `Assets/Scripts/Maze/ForgeRenderer.cs`
- `Assets/Scripts/Maze/InfirmaryRenderer.cs`
- `Assets/Scripts/Maze/ChapelRenderer.cs`
- `Assets/Scripts/Maze/MinersGuildRenderer.cs`
- `Assets/Scripts/Maze/MarketRenderer.cs`
- `Assets/Scripts/Maze/AntiquaryRenderer.cs`
- `Assets/Scripts/Maze/HeroesGuildRenderer.cs`

Check when:
- Changing dungeon visuals, base visuals, terrain/rivers/bridges, object renderers, lighting shader inputs, voxel mesh/material generation, async rendering, or visibility masking of rendered objects.

### Lighting / Visibility / Fog / Maps

Responsibility:
- Handles hero sight, dungeon darkness, object visibility in lighting modes, minimap/expanded map, common cartographer memory, fog-of-war visuals, and lighting masks.

Primary files:
- `Assets/Scripts/Core/GameBootstrap.Visibility.cs`
- `Assets/Scripts/Core/GameBootstrap.Map.cs`
- `Assets/Scripts/Core/GameBootstrap.Cartography.cs`
- `Assets/Scripts/Core/FogOfWarView.cs`
- `Assets/Scripts/Hero/HeroVisibility.cs`
- `Assets/Scripts/Hero/HeroVisibilityView.cs`
- `Assets/Scripts/Hero/HeroMemory.cs`
- `Assets/Scripts/Hero/HeroMemoryView.cs`
- `Assets/Scripts/Maze/MazeRenderer.Visibility.cs`
- `Assets/Scripts/Maze/MazeRenderer.Lighting.cs`
- `Assets/Scripts/UI/MapHudView.cs`

Check when:
- Changing F2/lighting mode, fog, minimap, shared map memory, current hero visibility, hidden object rendering, torch/lantern light origins, or map overlays.

### Objectives / Carryables / Rewards

Responsibility:
- Owns gold ingots, death tokens, carried keys/items, objective delivery rewards, common-map gold reward, and quest progress hooks.

Primary files:
- `Assets/Scripts/Core/GameBootstrap.CarryItems.cs`
- `Assets/Scripts/Core/GoldIngotManager.cs`
- `Assets/Scripts/Core/HeroDeathTokenManager.cs`
- `Assets/Scripts/Core/HeroDeathTokenModel.cs`
- `Assets/Scripts/Maze/GoldIngotModel.cs`
- `Assets/Scripts/Hero/HeroInventory.cs`
- `Assets/Scripts/Hero/HeroVengeance.cs`

Related systems:
- Hero exploration, combat rewards, cartographer memory, HUD tooltips, maze renderer tracking.

Check when:
- Changing pickup/delivery logic, inventory slots, reward XP/gold, dropped death tokens, carried keys, objective labels/tooltips, or item visibility.

### Mines / Dungeon Outposts / Dungeon Fortification

Responsibility:
- Manages mine and outpost discovery, route-first underground construction, center build progress, mine workers, mine carts, ore storage/delivery, mine upgrades, cave paths, mine/outpost lighting, and dungeon fortification routes.

Primary files:
- `Assets/Scripts/Core/MineConstructionController.cs`
- `Assets/Scripts/Core/MineConstructionController.Runtime.cs`
- `Assets/Scripts/Core/MineConstructionController.Caves.cs`
- `Assets/Scripts/Core/MineConstructionController.Paths.cs`
- `Assets/Scripts/Core/MineConstructionController.Workers.cs`
- `Assets/Scripts/Core/MineConstructionController.Visibility.cs`
- `Assets/Scripts/Core/MineConstructionRenderer.cs`
- `Assets/Scripts/Core/DungeonFortificationController.cs`
- `Assets/Scripts/Core/DungeonFortificationRenderer.cs`
- `Assets/Scripts/Core/GameBootstrap.Mines.cs`
- `Assets/Scripts/Core/GameBootstrap.DungeonFortification.cs`

Related files:
- `Assets/Scripts/Maze/OreDepositModel.cs`
- `Assets/Scripts/Maze/OreDepositRenderer.cs`
- `Assets/Scripts/Core/ResourceWallet.cs`
- `Assets/Scripts/Core/BaseAmbienceController.cs`
- `Assets/Scripts/Maze/MazeRenderer.Lighting.cs`

Check when:
- Changing mine/outpost construction, cave selection rules, mine worker routing, mine cart routing, ore production/delivery, mine upgrades, mine/outpost light origins, fortified-cell behavior, or dungeon torch placement.

### Heroes Guild / Contracts

Responsibility:
- Generates, filters, displays, assigns, tracks, and pays clearing contracts for heroes.
- Gates contract targets through mob discovery/progression checks and hero readiness before assignment.

Primary files:
- `Assets/Scripts/Core/HeroGuildQuestController.cs`
- `Assets/Scripts/Core/HeroGuildQuestModel.cs`
- `Assets/Scripts/Core/GameBootstrap.HeroesGuild.cs`
- `Assets/Scripts/Maze/HeroesGuildRenderer.cs`

Related files:
- `Assets/Scripts/UI/BuildingMicroHudView.cs`
- `Assets/Scripts/UI/HeroHudView.cs`
- `Assets/Scripts/UI/HeroHudView.Text.cs`
- `Assets/Scripts/Mobs/MobManager.Queries.cs`
- `Assets/Scripts/Combat/CombatController.cs`

Check when:
- Changing quest generation, target availability, auto-assignment readiness, reward reservation/payment, mob defeat progress, hero HUD quest display, or building micro-HUD services.

### UI / HUD / Menus

Responsibility:
- Owns immediate-mode HUD surfaces, runtime panels, top resources, base building cards, hero/mob panels, map UI, object micro-HUDs, main/pause menu, transitions, and service buttons.

Primary files:
- `Assets/Scripts/UI/MainMenuUI.cs`
- `Assets/Scripts/UI/TopHudView.cs`
- `Assets/Scripts/UI/BaseHudView.cs`
- `Assets/Scripts/UI/HeroHudView.cs`
- `Assets/Scripts/UI/HeroHudView.Panel.cs`
- `Assets/Scripts/UI/HeroHudView.Text.cs`
- `Assets/Scripts/UI/HeroHudView.Icons.cs`
- `Assets/Scripts/UI/HeroHudView.Tooltips.cs`
- `Assets/Scripts/UI/MobHudView.cs`
- `Assets/Scripts/UI/MapHudView.cs`
- `Assets/Scripts/UI/DungeonLevelHudView.cs`
- `Assets/Scripts/UI/VictoryHudView.cs`
- `Assets/Scripts/UI/BuildingMicroHudView.cs`
- `Assets/Scripts/UI/ObjectMicroHudView.cs`
- `Assets/Scripts/UI/BuildingServiceCatalog.cs`
- `Assets/Scripts/UI/BuildingServiceEntry.cs`
- `Assets/Scripts/UI/HeroLineageHudView.cs`
- `Assets/Scripts/UI/GuiHudTransition.cs`
- `Assets/Scripts/Core/ObjectMicroHudTarget.cs`

Bootstrap integration:
- `Assets/Scripts/Core/GameBootstrap.Hud.cs`
- `Assets/Scripts/Core/GameBootstrap.Selection.cs`
- `Assets/Scripts/Core/GameBootstrap.Map.cs`
- `Assets/Scripts/Core/GameBootstrap.DungeonLevels.cs`

Check when:
- Changing visible text, buttons, disabled states, tooltips, panels, map/minimap UI, pause menu, input shortcuts, building micro-HUD services, or selected-object/hero display.

### Audio / Debug / Time

Responsibility:
- Provides generated SFX/music, debug log routing, verbose trace gates, runtime time scale, and background simulation support.

Primary files:
- `Assets/Scripts/Core/GameAudioController.cs`
- `Assets/Scripts/Core/GameDebugLog.cs`
- `Assets/Scripts/Core/TimeScaleController.cs`

Related files:
- `Assets/Scripts/Core/GameBootstrap.cs`
- `Assets/Scripts/Core/GameBootstrap.StartGame.cs`
- Systems that emit `GameDebugLog` or play `GameSfx`.

Check when:
- Changing sound events, debug verbosity, log spam, x1/x3 simulation behavior, pause/menu runtime state, or background execution.

### Camera

Responsibility:
- Controls the isometric camera rig and camera interaction.

Primary files:
- `Assets/Scripts/Camera/LabyrinthCameraController.cs`

Related systems:
- Game bootstrap scene setup, UI input capture, selected target readability.

Check when:
- Changing camera movement, zoom/framing, input handling, or generated-scene focus.

## Cross-System Links

- Building order touches `BaseDevelopment`, `GameBootstrap.* building partials`, `BaseHudView`, `BaseConstructionController`, `BaseAmbienceController`, and building renderers.
- Road/bridge passability touches `BaseAmbienceController`, `TerrainDecorationController`, `TaxCollectorController`, city walkers, production carts, and hero entrance commute.
- Hero exploration touches `HeroExplorer`, `HeroExplorationCoordinator`, `HeroMemory`, `MobManager`, `CombatController`, objectives, cartography, visibility, and map HUD.
- Combat balance touches `CombatController.*`, `HeroModel`, `HeroModel.Injuries`, `MobModel`, `MobManager.SpawnSelection`, rewards, quests, and HUD display.
- Mob density/respawn touches `MobManager`, `MobManager.SpawnSelection`, maze topology, hero visibility, combat, and Heroes Guild contracts.
- Mine and outpost gameplay touches `MineConstructionController.*`, `MineConstructionRenderer`, `MazeRenderer.Lighting`, `ResourceWallet`, `BaseAmbienceController`, `BaseHudView`, and `GameBootstrap.Mines`.
- Lighting/F2 changes often need `HeroVisibilityView`, `MazeRenderer.Lighting`, `MazeRenderer.Visibility`, object managers, torches/mines, and fog/map UI checked together.
- Carryable reward changes touch `HeroInventory`, `GoldIngotManager`, `HeroDeathTokenManager`, `HeroExplorer`, `GameBootstrap.CarryItems`, HUD text, and visibility tracking.
- New `.cs` files must be added to `Assembly-CSharp.csproj`; removed/split files must be removed from it.
