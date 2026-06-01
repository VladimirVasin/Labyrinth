# System Tree

High-level conceptual map for `Labyrinth`.

Use this before the owner map when a task is broad, ambiguous, architectural, or crosses multiple systems. This file answers "what systems exist and how they depend on each other"; `AI/systems-map.md` answers "which files own them".

## Update Rules

- Update this file when adding, removing, renaming, or substantially changing a gameplay system, simulation system, UI surface, rendering layer, generated-map feature, or cross-system dependency.
- Keep this file conceptual and navigational. Do not paste code or long file lists here.
- Keep file ownership details in `AI/systems-map.md`.
- If a feature crosses systems, update both the relevant tree leaves here and the cross-system links in this file.
- If this tree disagrees with code, trust code, then update the tree.

## System Tree

### Runtime Shell

- Application bootstrap
  - Creates core services, controllers, HUDs, camera, audio, time scale, and runtime state.
  - Owns game states: main menu, generation, playing, base HUD, pause.
- Expedition lifecycle
  - Starts a new generated map.
  - Clears previous runtime systems.
  - Initializes terrain, dungeon, base, mobs, items, heroes, city systems, and underground systems.
- Dungeon level lifecycle
  - Switches between generated dungeon levels.
  - Restores level-one base systems when returning to the city level.

### Generated World

- Maze topology
  - Grid, walkable cells, walls, caves, boss cave, central room, central doors, keys, stairs, chests, ores.
  - Validation keeps required routes possible.
- Terrain and base placement
  - Outdoor terrain around the base.
  - Rivers, bridges, roads, building placement blockers, decorative terrain.
- Dungeon progression topology
  - Level-one city/dungeon entry.
  - Central room gate and mini-boss route.
  - Down/up stairs and later dungeon levels.

### Rendering And Visibility

- Maze rendering
  - Dungeon cells, floors, walls, underlay, object visibility tracking, async render path.
- Voxel visuals
  - Shared voxel/block styling, generated textures, shadows, figure parts, burst effects.
- Lighting and fog
  - Hero lanterns, torches, mine/outpost lights, dungeon light mask, fog-of-war, map visibility.
- World labels and micro HUD targets
  - Base building labels.
  - Construction-site labels.
  - Elite mob level labels.
  - Dungeon object labels and click targets.

### Idle City

- Base development
  - Building state, unlock order, costs, levels, storage limits, production tuning.
- Base construction
  - Construction sites, castle builders, staged building visuals.
  - Road and bridge construction before buildings become functional.
- Resource economy
  - Food, gold, wood, iron.
  - Farms, lumber camps, peasant huts, market exchange, tax collectors.
- City logistics and ambience
  - Production carts, tax collectors, city walkers, hero-house couriers.
  - Road network and bridge passability.

### Underground Infrastructure

- Dungeon fortification
  - Manual/automatic fortified cells.
  - Torch placement, wall reinforcements, dungeon light origins.
- Mines
  - Select known ore caves.
  - Fortify route from dungeon entrance.
  - Deliver materials to cave center.
  - Build mine, fortify 3x3 cave footprint, produce ore, dispatch/return mine carts.
- Outposts
  - Select known intermediate non-ore caves.
  - Fortify route from dungeon entrance.
  - Deliver materials to cave center.
  - Build outpost, fortify 3x3 cave footprint, provide stationary dungeon light and safe area.

### Heroes

- Hero creation and lineage
  - Hero houses, generation names, death/rebirth flow, house fund couriers.
- Hero commute
  - Newly built or reborn heroes leave their houses and travel to the labyrinth entrance through city roads.
- Exploration AI
  - Frontier selection, shared reservations, remembered paths, visit-aware stale-route patrols, central-room/mini-boss routing.
- Memory and cartography
  - Personal memory, shared cartographer memory, minimap/world map visibility.
- Inventory and carry objectives
  - Gold ingots, death tokens, keys, return stones, consumables.
- Hero progression and condition
  - XP, levels, stamina, equipment, blessings, injuries, scars, wounds, vengeance state.

### Mobs And Combat

- Mob population
  - Spawn selection, species weighting, density limits, displayed danger levels, dark respawn.
- Mob movement and encounters
  - Wandering, awareness, adjacent encounter checks, stuck/blocked movement guards.
- Combat resolution
  - Initiative, attacks, armor, stamina, wounds, retreat, death, rewards, combat text.
- Boss progression
  - Mini-boss/central-room routing, boss-cave encounter routing, and level gate rewards.

### Objectives And Rewards

- Carry objectives
  - Gold ingots, death tokens, keys, central-room/descent objectives.
- Heroes Guild contracts
  - Contract generation, auto-assignment, progress tracking, payment/failure.
  - Target species are gated by visible/defeated discoveries, dungeon progression, and hero readiness before assignment.
- Economy feedback loops
  - Hero personal gold.
  - Treasury resources.
  - Objective XP and building-service costs.

### UI, Input, Audio, Time

- Main menu and expedition setup
  - Map size, seed, start/regenerate expedition.
- Runtime HUDs
  - Top HUD, base HUD, building micro HUD, object micro HUD, hero HUD, mob HUD, lineage HUD, level HUD, map HUD, victory HUD.
- Input modes
  - Camera controls, selection clicks, castle hotkey, pause, F2 visibility mode, F9 debug building mode.
- Audio and time scale
  - UI/game SFX, music, x1/x3 time scale, runtime simulation while HUDs are open.

## Cross-System Links

- New expedition: runtime shell -> world generation -> terrain/maze rendering -> base/city/underground initialization -> mobs/items/heroes/HUD.
- Level switching: dungeon level lifecycle -> renderer reset -> current memory selection -> mobs/items reset -> level-one city restoration when applicable.
- Visibility: heroes and shared memory define visible cells; torches, mines, outposts, and active mine workers/carts add dungeon light; object renderers are hidden outside lighting visibility.
- Respawn safety: hero sight, fortified/torch-lit cells, mine/outpost lit cells, and dungeon infrastructure block unsafe dark respawns.
- City roads: construction, bridges, carts, tax collectors, hero commute, and mine workers all depend on completed road paths.
- Mines/outposts: base HUD starts cave selection; cartographer shared memory determines selectable caves; mine workers spend wood and use city roads plus fortified dungeon routes; completed structures feed lighting and safe cells.
- Hero exploration: exploration AI uses maze topology, personal/common memory, cell visit history, mob queries, item objectives, stamina, and combat state.
- Combat rewards: combat updates hero XP/injuries, mob population, Heroes Guild contracts, carry rewards, and progression gates.
- Heroes Guild target gating: contracts depend on mob visibility/discovery, central-room/dungeon progression, and per-hero combat readiness so early idle heroes do not receive unreachable clearing targets.
- Economy: base buildings, production carts, market/taxes, mines, hero services, construction, and objective deliveries all write into or spend from resource wallets.
