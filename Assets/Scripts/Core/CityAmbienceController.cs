using System.Collections.Generic;
using Labyrinth.Base;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed class CityAmbienceController : MonoBehaviour
    {
        private const float WalkerYOffset = 0.06f;
        private const float WalkerSpeedCellsPerSecond = 0.72f;
        private const float WalkerVisualScale = 1.34f;
        private const int AnchorsPerBuilding = 6;
        private const int MaxCityWalkers = 42;

        private enum CityWalkerRole
        {
            Guard,
            Farmer,
            Lumberjack,
            Squire,
            Villager,
            Alchemist,
            TavernWorker,
            Smith,
            Healer,
            Cartographer,
            Acolyte
        }

        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private readonly List<CityBuilding> buildings = new List<CityBuilding>();
        private readonly List<CityWalker> walkers = new List<CityWalker>();
        private readonly List<WalkerAnchor> walkerAnchors = new List<WalkerAnchor>();

        private MazeGenerationResult result;
        private MazeRenderer mazeRenderer;
        private Transform root;
        private Material villagerBodyMaterial;
        private Material villagerHeadMaterial;
        private Material villagerPackMaterial;
        private Material guardBodyMaterial;
        private Material farmerBodyMaterial;
        private Material lumberjackBodyMaterial;
        private Material squireBodyMaterial;
        private Material alchemistBodyMaterial;
        private Material tavernBodyMaterial;
        private Material smithBodyMaterial;
        private Material healerBodyMaterial;
        private Material cartographerBodyMaterial;
        private Material acolyteBodyMaterial;
        private Material metalMaterial;
        private Material woodMaterial;
        private Material goldMaterial;
        private Material redMaterial;
        private Material potionMaterial;
        private Material parchmentMaterial;

        public void Initialize(MazeGenerationResult generationResult, MazeRenderer renderer)
        {
            Clear();
            result = generationResult;
            mazeRenderer = renderer;
            if (result == null || result.Grid == null || mazeRenderer == null)
            {
                return;
            }

            EnsureMaterials();
            root = new GameObject("CityWalkersRoot").transform;
            root.SetParent(transform, false);
            buildings.Add(new CityBuilding(BuildingType.Castle, result.BasePosition, BaseDevelopment.CastleFootprintRadiusCells));
        }

        public void Clear()
        {
            walkers.Clear();
            walkerAnchors.Clear();
            buildings.Clear();
            if (root != null)
            {
                Destroy(root.gameObject);
                root = null;
            }

            result = null;
            mazeRenderer = null;
        }

        public void RegisterBuilding(BuildingType type, Vector2Int position)
        {
            if (type == BuildingType.Castle || result == null || mazeRenderer == null)
            {
                return;
            }

            for (var i = 0; i < buildings.Count; i++)
            {
                if (buildings[i].Type == type && buildings[i].Position == position)
                {
                    return;
                }
            }

            var building = new CityBuilding(type, position, GetFootprintRadius(type));
            buildings.Add(building);
            var random = new System.Random(CreateBuildingAmbienceSeed(building));
            EnsureCastleAnchors(random);
            AddWalkerAnchors(building, random);
            SpawnWalkersForBuilding(building, random);
            GameDebugLog.Info(
                "Base",
                $"City walkers added for {type}: buildings={buildings.Count}, anchors={walkerAnchors.Count}, walkers={walkers.Count}.");
        }

        private void Update()
        {
            if (walkers.Count == 0 || mazeRenderer == null)
            {
                return;
            }

            var speed = mazeRenderer.CellSize * WalkerSpeedCellsPerSecond * Time.deltaTime;
            for (var i = 0; i < walkers.Count; i++)
            {
                if (walkers[i].Move(speed))
                {
                    AssignWalkerTarget(walkers[i], i);
                }
            }
        }

        private void RebuildWalkers()
        {
            if (root == null)
            {
                return;
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }

            walkers.Clear();
            walkerAnchors.Clear();
            var random = new System.Random(CreateAmbienceSeed());
            for (var i = 0; i < buildings.Count; i++)
            {
                AddWalkerAnchors(buildings[i], random);
            }

            SpawnWalkers(random);
            GameDebugLog.Info(
                "Base",
                $"City walkers rebuilt: buildings={buildings.Count}, anchors={walkerAnchors.Count}, walkers={walkers.Count}.");
        }

        private void EnsureCastleAnchors(System.Random random)
        {
            if (walkerAnchors.Count > 0 || buildings.Count == 0)
            {
                return;
            }

            AddWalkerAnchors(buildings[0], random);
        }

        private void AddWalkerAnchors(CityBuilding building, System.Random random)
        {
            for (var i = 0; i < AnchorsPerBuilding; i++)
            {
                for (var attempt = 0; attempt < 32; attempt++)
                {
                    var radius = building.FootprintRadius + random.Next(1, 4);
                    var cell = building.Position + new Vector2Int(
                        random.Next(-radius, radius + 1),
                        random.Next(-radius, radius + 1));
                    if (!IsSafeWalkerCell(cell))
                    {
                        continue;
                    }

                    walkerAnchors.Add(new WalkerAnchor(cell, building.Position, building.Type));
                    break;
                }
            }
        }

        private void SpawnWalkers(System.Random random)
        {
            if (walkerAnchors.Count < 2)
            {
                return;
            }

            var walkerIndex = 0;
            for (var i = 0; i < buildings.Count && walkerIndex < MaxCityWalkers; i++)
            {
                var building = buildings[i];
                var role = GetWalkerRole(building.Type);
                var count = GetWalkerCount(building.Type);
                for (var j = 0; j < count && walkerIndex < MaxCityWalkers; j++)
                {
                    if (!TryChooseBuildingAnchor(building, random, out var start))
                    {
                        continue;
                    }

                    var walkerObject = new GameObject($"City Walker {role}");
                    walkerObject.transform.SetParent(root, false);
                    walkerObject.transform.position = ToWorld(start);
                    BuildWalkerModel(walkerObject.transform, role);
                    var walker = new CityWalker(walkerObject.transform, start, ToWorld(start));
                    walkers.Add(walker);
                    AssignWalkerTarget(walker, walkerIndex);
                    walkerIndex++;
                }
            }
        }

        private void SpawnWalkersForBuilding(CityBuilding building, System.Random random)
        {
            if (walkerAnchors.Count < 2)
            {
                return;
            }

            var role = GetWalkerRole(building.Type);
            var count = GetWalkerCount(building.Type);
            for (var i = 0; i < count && walkers.Count < MaxCityWalkers; i++)
            {
                if (!TryChooseBuildingAnchor(building, random, out var start))
                {
                    continue;
                }

                var walkerObject = new GameObject($"City Walker {role}");
                walkerObject.transform.SetParent(root, false);
                walkerObject.transform.position = ToWorld(start);
                BuildWalkerModel(walkerObject.transform, role);
                var walker = new CityWalker(walkerObject.transform, start, ToWorld(start));
                walkers.Add(walker);
                AssignWalkerTarget(walker, walkers.Count - 1);
            }
        }

        private bool TryChooseBuildingAnchor(CityBuilding building, System.Random random, out Vector2Int anchor)
        {
            var candidates = new List<Vector2Int>();
            for (var i = 0; i < walkerAnchors.Count; i++)
            {
                if (walkerAnchors[i].SourceBuildingPosition == building.Position)
                {
                    candidates.Add(walkerAnchors[i].Position);
                }
            }

            if (candidates.Count > 0)
            {
                anchor = candidates[random.Next(candidates.Count)];
                return true;
            }

            if (walkerAnchors.Count > 0)
            {
                anchor = walkerAnchors[random.Next(walkerAnchors.Count)].Position;
                return true;
            }

            anchor = default;
            return false;
        }

        private void AssignWalkerTarget(CityWalker walker, int walkerIndex)
        {
            if (walkerAnchors.Count == 0)
            {
                return;
            }

            var seed = Hash(walker.CurrentCell + new Vector2Int(walkerIndex * 17, Mathf.RoundToInt(Time.time * 11f)));
            for (var offset = 0; offset < walkerAnchors.Count; offset++)
            {
                var target = walkerAnchors[(seed + offset) % walkerAnchors.Count].Position;
                if (target == walker.CurrentCell)
                {
                    continue;
                }

                if (TryBuildOutsidePath(walker.CurrentCell, target, out var path))
                {
                    walker.SetPath(path, BuildWorldPath(path));
                    return;
                }
            }
        }

        private void BuildWalkerModel(Transform parent, CityWalkerRole role)
        {
            var unit = mazeRenderer.ModelUnitSize * WalkerVisualScale * GetRoleScale(role);
            CreatePart(
                parent,
                "Walker Body",
                PrimitiveType.Capsule,
                new Vector3(0f, unit * 0.28f, 0f),
                new Vector3(unit * 0.18f, unit * 0.28f, unit * 0.18f),
                GetRoleBodyMaterial(role));
            CreatePart(
                parent,
                "Walker Head",
                PrimitiveType.Sphere,
                new Vector3(0f, unit * 0.65f, 0f),
                Vector3.one * unit * 0.16f,
                villagerHeadMaterial);
            CreatePart(
                parent,
                "Walker Pack",
                PrimitiveType.Cube,
                new Vector3(0f, unit * 0.34f, unit * -0.14f),
                new Vector3(unit * 0.16f, unit * 0.16f, unit * 0.08f),
                villagerPackMaterial);
            AddRoleDetails(parent, role, unit);
        }

        private void AddRoleDetails(Transform parent, CityWalkerRole role, float unit)
        {
            switch (role)
            {
                case CityWalkerRole.Guard:
                    CreatePart(parent, "Guard Shield", PrimitiveType.Cube, new Vector3(unit * 0.18f, unit * 0.4f, unit * 0.08f), new Vector3(unit * 0.08f, unit * 0.22f, unit * 0.2f), metalMaterial);
                    CreatePart(parent, "Guard Spear", PrimitiveType.Cube, new Vector3(unit * -0.18f, unit * 0.52f, unit * 0.03f), new Vector3(unit * 0.04f, unit * 0.54f, unit * 0.04f), woodMaterial);
                    return;
                case CityWalkerRole.Farmer:
                    CreatePart(parent, "Farmer Straw Hat", PrimitiveType.Cube, new Vector3(0f, unit * 0.82f, 0f), new Vector3(unit * 0.34f, unit * 0.045f, unit * 0.34f), goldMaterial);
                    CreatePart(parent, "Farmer Basket", PrimitiveType.Cube, new Vector3(unit * 0.22f, unit * 0.3f, unit * 0.04f), new Vector3(unit * 0.17f, unit * 0.15f, unit * 0.17f), woodMaterial);
                    return;
                case CityWalkerRole.Lumberjack:
                    CreatePart(parent, "Lumberjack Axe Handle", PrimitiveType.Cube, new Vector3(unit * 0.2f, unit * 0.42f, unit * -0.02f), new Vector3(unit * 0.04f, unit * 0.38f, unit * 0.04f), woodMaterial);
                    CreatePart(parent, "Lumberjack Axe Head", PrimitiveType.Cube, new Vector3(unit * 0.2f, unit * 0.62f, unit * -0.02f), new Vector3(unit * 0.16f, unit * 0.08f, unit * 0.08f), metalMaterial);
                    return;
                case CityWalkerRole.Squire:
                    CreatePart(parent, "Squire Buckler", PrimitiveType.Cube, new Vector3(unit * 0.2f, unit * 0.38f, unit * 0.08f), new Vector3(unit * 0.12f, unit * 0.18f, unit * 0.06f), metalMaterial);
                    return;
                case CityWalkerRole.Alchemist:
                    CreatePart(parent, "Alchemist Bottle", PrimitiveType.Sphere, new Vector3(unit * 0.2f, unit * 0.44f, unit * 0.05f), Vector3.one * unit * 0.1f, potionMaterial);
                    return;
                case CityWalkerRole.TavernWorker:
                    CreatePart(parent, "Tavern Mug", PrimitiveType.Cube, new Vector3(unit * 0.22f, unit * 0.42f, unit * 0.02f), new Vector3(unit * 0.12f, unit * 0.14f, unit * 0.12f), goldMaterial);
                    return;
                case CityWalkerRole.Smith:
                    CreatePart(parent, "Smith Hammer Handle", PrimitiveType.Cube, new Vector3(unit * 0.19f, unit * 0.4f, 0f), new Vector3(unit * 0.04f, unit * 0.3f, unit * 0.04f), woodMaterial);
                    CreatePart(parent, "Smith Hammer Head", PrimitiveType.Cube, new Vector3(unit * 0.19f, unit * 0.56f, 0f), new Vector3(unit * 0.18f, unit * 0.08f, unit * 0.08f), metalMaterial);
                    return;
                case CityWalkerRole.Healer:
                    CreatePart(parent, "Healer Satchel", PrimitiveType.Cube, new Vector3(unit * 0.22f, unit * 0.32f, 0f), new Vector3(unit * 0.14f, unit * 0.16f, unit * 0.1f), parchmentMaterial);
                    CreatePart(parent, "Healer Cross", PrimitiveType.Cube, new Vector3(unit * 0.26f, unit * 0.34f, 0f), new Vector3(unit * 0.04f, unit * 0.16f, unit * 0.04f), redMaterial);
                    return;
                case CityWalkerRole.Cartographer:
                    CreatePart(parent, "Cartographer Scroll", PrimitiveType.Cube, new Vector3(unit * 0.21f, unit * 0.42f, 0f), new Vector3(unit * 0.08f, unit * 0.22f, unit * 0.12f), parchmentMaterial);
                    return;
                case CityWalkerRole.Acolyte:
                    CreatePart(parent, "Acolyte Candle", PrimitiveType.Cube, new Vector3(unit * 0.18f, unit * 0.45f, 0f), new Vector3(unit * 0.07f, unit * 0.2f, unit * 0.07f), goldMaterial);
                    CreatePart(parent, "Acolyte Cross", PrimitiveType.Cube, new Vector3(unit * -0.18f, unit * 0.46f, 0f), new Vector3(unit * 0.04f, unit * 0.24f, unit * 0.04f), goldMaterial);
                    return;
                default:
                    return;
            }
        }

        private static void CreatePart(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(part);
        }

        private Vector3 ToWorld(Vector2Int cell)
        {
            return mazeRenderer.GridToWorld(cell) + new Vector3(0f, WalkerYOffset, 0f);
        }

        private bool IsSafeWalkerCell(Vector2Int cell)
        {
            return result != null
                && result.Grid != null
                && !result.Grid.InBounds(cell)
                && !IsInsideAnyBuildingFootprint(cell)
                && cell.x >= -MazeTerrain.PaddingCells + 1
                && cell.y >= -MazeTerrain.PaddingCells + 1
                && cell.x <= result.Grid.Width - 2 + MazeTerrain.PaddingCells
                && cell.y <= result.Grid.Height - 2 + MazeTerrain.PaddingCells;
        }

        private bool TryBuildOutsidePath(Vector2Int start, Vector2Int target, out List<Vector2Int> path)
        {
            path = new List<Vector2Int>();
            if (!IsSafeWalkerCell(start) || !IsSafeWalkerCell(target))
            {
                return false;
            }

            var frontier = new Queue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            frontier.Enqueue(start);
            cameFrom[start] = start;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (current == target)
                {
                    break;
                }

                for (var i = 0; i < CardinalDirections.Length; i++)
                {
                    var next = current + CardinalDirections[i];
                    if (cameFrom.ContainsKey(next) || !IsSafeWalkerCell(next))
                    {
                        continue;
                    }

                    cameFrom[next] = current;
                    frontier.Enqueue(next);
                }
            }

            if (!cameFrom.ContainsKey(target))
            {
                return false;
            }

            var step = target;
            while (step != start)
            {
                path.Add(step);
                step = cameFrom[step];
            }

            path.Add(start);
            path.Reverse();
            return path.Count > 1;
        }

        private List<Vector3> BuildWorldPath(IReadOnlyList<Vector2Int> path)
        {
            var worldPath = new List<Vector3>(path.Count);
            for (var i = 0; i < path.Count; i++)
            {
                worldPath.Add(ToWorld(path[i]));
            }

            return worldPath;
        }

        private bool IsInsideAnyBuildingFootprint(Vector2Int cell)
        {
            for (var i = 0; i < buildings.Count; i++)
            {
                if (ChebyshevDistance(cell, buildings[i].Position) <= buildings[i].FootprintRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private int CreateAmbienceSeed()
        {
            unchecked
            {
                var seed = result.Settings.Seed ^ (buildings.Count * 73856093);
                for (var i = 0; i < buildings.Count; i++)
                {
                    seed ^= Hash(buildings[i].Position + new Vector2Int((int)buildings[i].Type * 17, i * 31));
                }

                return seed;
            }
        }

        private int CreateBuildingAmbienceSeed(CityBuilding building)
        {
            unchecked
            {
                return result.Settings.Seed
                    ^ Hash(building.Position + new Vector2Int((int)building.Type * 17, buildings.Count * 31));
            }
        }

        private static int GetFootprintRadius(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Farm:
                    return BaseDevelopment.FarmFootprintRadiusCells;
                case BuildingType.LumberjackCamp:
                    return BaseDevelopment.LumberjackCampFootprintRadiusCells;
                case BuildingType.HeroHouse:
                    return BaseDevelopment.HeroHouseFootprintRadiusCells;
                case BuildingType.PeasantHut:
                    return BaseDevelopment.PeasantHutFootprintRadiusCells;
                case BuildingType.AlchemistShop:
                    return BaseDevelopment.AlchemistShopFootprintRadiusCells;
                case BuildingType.Tavern:
                    return BaseDevelopment.TavernFootprintRadiusCells;
                case BuildingType.Forge:
                    return BaseDevelopment.ForgeFootprintRadiusCells;
                case BuildingType.Infirmary:
                    return BaseDevelopment.InfirmaryFootprintRadiusCells;
                case BuildingType.CartographerHouse:
                    return BaseDevelopment.CartographerHouseFootprintRadiusCells;
                case BuildingType.Chapel:
                    return BaseDevelopment.ChapelFootprintRadiusCells;
                case BuildingType.MinersGuild:
                    return BaseDevelopment.MinersGuildFootprintRadiusCells;
                case BuildingType.Market:
                    return BaseDevelopment.MarketFootprintRadiusCells;
                case BuildingType.Castle:
                default:
                    return BaseDevelopment.CastleFootprintRadiusCells;
            }
        }

        private static CityWalkerRole GetWalkerRole(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Castle:
                    return CityWalkerRole.Guard;
                case BuildingType.Farm:
                    return CityWalkerRole.Farmer;
                case BuildingType.LumberjackCamp:
                    return CityWalkerRole.Lumberjack;
                case BuildingType.HeroHouse:
                    return CityWalkerRole.Squire;
                case BuildingType.PeasantHut:
                    return CityWalkerRole.Villager;
                case BuildingType.AlchemistShop:
                    return CityWalkerRole.Alchemist;
                case BuildingType.Tavern:
                    return CityWalkerRole.TavernWorker;
                case BuildingType.Forge:
                    return CityWalkerRole.Smith;
                case BuildingType.Infirmary:
                    return CityWalkerRole.Healer;
                case BuildingType.CartographerHouse:
                    return CityWalkerRole.Cartographer;
                case BuildingType.Chapel:
                    return CityWalkerRole.Acolyte;
                case BuildingType.MinersGuild:
                    return CityWalkerRole.Lumberjack;
                case BuildingType.Market:
                    return CityWalkerRole.Villager;
                default:
                    return CityWalkerRole.Villager;
            }
        }

        private static int GetWalkerCount(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Castle:
                    return 3;
                case BuildingType.Farm:
                case BuildingType.LumberjackCamp:
                case BuildingType.Tavern:
                case BuildingType.Market:
                    return 2;
                case BuildingType.PeasantHut:
                    return 1;
                default:
                    return 1;
            }
        }

        private static float GetRoleScale(CityWalkerRole role)
        {
            switch (role)
            {
                case CityWalkerRole.Guard:
                case CityWalkerRole.Smith:
                    return 1.08f;
                case CityWalkerRole.Acolyte:
                case CityWalkerRole.Cartographer:
                    return 0.96f;
                default:
                    return 1f;
            }
        }

        private Material GetRoleBodyMaterial(CityWalkerRole role)
        {
            switch (role)
            {
                case CityWalkerRole.Guard:
                    return guardBodyMaterial;
                case CityWalkerRole.Farmer:
                    return farmerBodyMaterial;
                case CityWalkerRole.Lumberjack:
                    return lumberjackBodyMaterial;
                case CityWalkerRole.Squire:
                    return squireBodyMaterial;
                case CityWalkerRole.Alchemist:
                    return alchemistBodyMaterial;
                case CityWalkerRole.TavernWorker:
                    return tavernBodyMaterial;
                case CityWalkerRole.Smith:
                    return smithBodyMaterial;
                case CityWalkerRole.Healer:
                    return healerBodyMaterial;
                case CityWalkerRole.Cartographer:
                    return cartographerBodyMaterial;
                case CityWalkerRole.Acolyte:
                    return acolyteBodyMaterial;
                default:
                    return villagerBodyMaterial;
            }
        }

        private static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private static int Hash(Vector2Int position)
        {
            unchecked
            {
                var hash = position.x * 73856093 ^ position.y * 19349663 ^ 0x28f7a9d;
                hash ^= hash >> 13;
                hash *= 1274126177;
                hash ^= hash >> 16;
                return hash & 0x7fffffff;
            }
        }

        private void EnsureMaterials()
        {
            if (villagerBodyMaterial != null)
            {
                return;
            }

            villagerBodyMaterial = CreateMaterial("City Walker Body", new Color(0.36f, 0.42f, 0.56f));
            villagerHeadMaterial = CreateMaterial("City Walker Head", new Color(0.78f, 0.68f, 0.48f));
            villagerPackMaterial = CreateMaterial("City Walker Pack", new Color(0.82f, 0.58f, 0.22f));
            guardBodyMaterial = CreateMaterial("City Guard Body", new Color(0.16f, 0.24f, 0.58f));
            farmerBodyMaterial = CreateMaterial("City Farmer Body", new Color(0.28f, 0.48f, 0.2f));
            lumberjackBodyMaterial = CreateMaterial("City Lumberjack Body", new Color(0.32f, 0.22f, 0.12f));
            squireBodyMaterial = CreateMaterial("City Squire Body", new Color(0.22f, 0.34f, 0.62f));
            alchemistBodyMaterial = CreateMaterial("City Alchemist Body", new Color(0.48f, 0.22f, 0.66f));
            tavernBodyMaterial = CreateMaterial("City Tavern Body", new Color(0.66f, 0.36f, 0.14f));
            smithBodyMaterial = CreateMaterial("City Smith Body", new Color(0.24f, 0.25f, 0.27f));
            healerBodyMaterial = CreateMaterial("City Healer Body", new Color(0.78f, 0.72f, 0.62f));
            cartographerBodyMaterial = CreateMaterial("City Cartographer Body", new Color(0.18f, 0.32f, 0.52f));
            acolyteBodyMaterial = CreateMaterial("City Acolyte Body", new Color(0.72f, 0.66f, 0.48f));
            metalMaterial = CreateMaterial("City Walker Metal", new Color(0.68f, 0.7f, 0.72f));
            woodMaterial = CreateMaterial("City Walker Wood", new Color(0.34f, 0.19f, 0.08f));
            goldMaterial = CreateMaterial("City Walker Gold", new Color(0.92f, 0.68f, 0.22f));
            redMaterial = CreateMaterial("City Walker Red", new Color(0.86f, 0.12f, 0.1f));
            potionMaterial = CreateMaterial("City Walker Potion", new Color(0.18f, 0.84f, 0.92f));
            parchmentMaterial = CreateMaterial("City Walker Parchment", new Color(0.82f, 0.72f, 0.5f));
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                name = name,
                color = color
            };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }

        private static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private readonly struct CityBuilding
        {
            public CityBuilding(BuildingType type, Vector2Int position, int footprintRadius)
            {
                Type = type;
                Position = position;
                FootprintRadius = footprintRadius;
            }

            public BuildingType Type { get; }

            public Vector2Int Position { get; }

            public int FootprintRadius { get; }
        }

        private readonly struct WalkerAnchor
        {
            public WalkerAnchor(Vector2Int position, Vector2Int sourceBuildingPosition, BuildingType sourceBuildingType)
            {
                Position = position;
                SourceBuildingPosition = sourceBuildingPosition;
                SourceBuildingType = sourceBuildingType;
            }

            public Vector2Int Position { get; }

            public Vector2Int SourceBuildingPosition { get; }

            public BuildingType SourceBuildingType { get; }
        }

        private sealed class CityWalker
        {
            private readonly Transform root;
            private readonly Queue<Vector2Int> targetCells = new Queue<Vector2Int>();
            private readonly Queue<Vector3> targetWorlds = new Queue<Vector3>();
            private Vector3 targetWorld;
            private Vector2Int targetCell;
            private bool hasTarget;

            public CityWalker(Transform root, Vector2Int currentCell, Vector3 targetWorld)
            {
                this.root = root;
                CurrentCell = currentCell;
                this.targetWorld = targetWorld;
                targetCell = currentCell;
            }

            public Vector2Int CurrentCell { get; private set; }

            public bool Move(float distance)
            {
                if (root == null || !hasTarget)
                {
                    return true;
                }

                var previous = root.position;
                root.position = Vector3.MoveTowards(root.position, targetWorld, distance);
                var delta = root.position - previous;
                if (delta.sqrMagnitude > 0.0001f)
                {
                    root.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
                }

                if (Vector3.Distance(root.position, targetWorld) > 0.03f)
                {
                    return false;
                }

                root.position = targetWorld;
                CurrentCell = targetCell;
                return !TryAdvanceTarget();
            }

            public void SetPath(IReadOnlyList<Vector2Int> path, IReadOnlyList<Vector3> worldPath)
            {
                targetCells.Clear();
                targetWorlds.Clear();
                if (path == null || worldPath == null)
                {
                    hasTarget = false;
                    return;
                }

                for (var i = 1; i < path.Count && i < worldPath.Count; i++)
                {
                    targetCells.Enqueue(path[i]);
                    targetWorlds.Enqueue(worldPath[i]);
                }

                hasTarget = TryAdvanceTarget();
            }

            private bool TryAdvanceTarget()
            {
                if (targetCells.Count == 0 || targetWorlds.Count == 0)
                {
                    hasTarget = false;
                    return false;
                }

                targetCell = targetCells.Dequeue();
                targetWorld = targetWorlds.Dequeue();
                hasTarget = true;
                return true;
            }
        }
    }
}
