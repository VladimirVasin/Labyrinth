using System.Collections.Generic;
using Labyrinth.Base;
using Labyrinth.Combat;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed class TaxCollectorController : MonoBehaviour
    {
        public const int HutTaxCapacity = 10;
        public const float TaxProductionIntervalSeconds = ResourceProductionController.FarmProductionIntervalSeconds;

        private const float CollectorSpeedCellsPerSecond = 2.1f;
        private const float CollectorYOffset = 0.07f;

        private readonly List<HutTaxRuntime> huts = new List<HutTaxRuntime>();
        private readonly Dictionary<Vector2Int, HutTaxRuntime> hutByPosition = new Dictionary<Vector2Int, HutTaxRuntime>();
        private readonly HashSet<Vector2Int> pathWarningPositions = new HashSet<Vector2Int>();

        private ResourceWallet resources;
        private BaseDevelopment baseDevelopment;
        private MazeRenderer mazeRenderer;
        private TerrainDecorationController terrainDecorations;
        private MazeGenerationResult result;
        private TaxCollectorRuntime collector;
        private Material bodyMaterial;
        private Material headMaterial;
        private Material bagMaterial;
        private float taxProgress;

        public void Configure(ResourceWallet wallet, BaseDevelopment development, MazeRenderer renderer, TerrainDecorationController decorations = null)
        {
            resources = wallet;
            baseDevelopment = development;
            mazeRenderer = renderer;
            terrainDecorations = decorations;
        }

        public void Initialize(MazeGenerationResult generationResult)
        {
            Clear();
            result = generationResult;
        }

        public void Clear()
        {
            huts.Clear();
            hutByPosition.Clear();
            pathWarningPositions.Clear();
            taxProgress = 0f;
            result = null;
            if (collector != null)
            {
                collector.Destroy();
                collector = null;
            }
        }

        public void RegisterHut(Vector2Int position, BuildingView view)
        {
            if (hutByPosition.TryGetValue(position, out var existing))
            {
                existing.View = view;
                existing.RefreshLabel();
                return;
            }

            var hut = new HutTaxRuntime(position, view);
            hut.RefreshLabel();
            huts.Add(hut);
            hutByPosition[position] = hut;
            GameDebugLog.Info("Base", $"Peasant hut tax registered at {GameDebugLog.Position(position)}. huts={huts.Count}.");
        }

        private void Update()
        {
            if (result == null || resources == null || baseDevelopment == null || mazeRenderer == null)
            {
                return;
            }

            ProduceTaxes();
            UpdateCollector();
            TryStartCollection();
        }

        private void ProduceTaxes()
        {
            taxProgress += Time.deltaTime;
            var wholeTicks = Mathf.FloorToInt(taxProgress / TaxProductionIntervalSeconds);
            if (wholeTicks <= 0)
            {
                return;
            }

            for (var tick = 0; tick < wholeTicks; tick++)
            {
                foreach (var hut in huts)
                {
                    if (hut.StoredGold >= HutTaxCapacity || hut.CollectionReserved)
                    {
                        continue;
                    }

                    hut.StoredGold++;
                    hut.RefreshLabel();
                    if (hut.StoredGold >= HutTaxCapacity)
                    {
                        GameDebugLog.Info("Base", $"Peasant hut tax ready at {GameDebugLog.Position(hut.Position)}: storedGold={hut.StoredGold}/{HutTaxCapacity}.");
                    }
                }
            }

            taxProgress -= wholeTicks * TaxProductionIntervalSeconds;
        }

        private void UpdateCollector()
        {
            if (collector == null)
            {
                return;
            }

            var speed = mazeRenderer.CellSize * CollectorSpeedCellsPerSecond * Time.deltaTime;
            if (!collector.Move(speed))
            {
                return;
            }

            if (!collector.ReturningToCastle)
            {
                CollectFromHut();
                return;
            }

            DeliverToCastle();
        }

        private void TryStartCollection()
        {
            if (collector != null)
            {
                return;
            }

            foreach (var hut in huts)
            {
                if (hut.StoredGold < HutTaxCapacity || hut.CollectionReserved)
                {
                    continue;
                }

                if (!TryBuildCollectorPath(hut, false, out var outbound))
                {
                    LogPathWarningOnce(hut);
                    continue;
                }

                EnsureMaterials();
                var root = new GameObject("Tax Collector");
                root.transform.SetParent(mazeRenderer.ContentRoot, false);
                root.transform.position = outbound[0];
                var bag = BuildCollectorModel(root.transform);
                bag.gameObject.SetActive(false);
                hut.CollectionReserved = true;
                collector = new TaxCollectorRuntime(root, outbound, hut, bag);
                GameDebugLog.Info("Base", $"Tax collector sent to hut={GameDebugLog.Position(hut.Position)}, storedGold={hut.StoredGold}, pathCells={outbound.Count}.");
                return;
            }
        }

        private void CollectFromHut()
        {
            if (collector == null || collector.TargetHut == null)
            {
                return;
            }

            var hut = collector.TargetHut;
            var collected = hut.StoredGold;
            hut.StoredGold = 0;
            hut.RefreshLabel();

            if (!TryBuildCollectorPath(hut, true, out var returnPath))
            {
                resources.AddGold(collected);
                GameDebugLog.Warning("Base", $"Tax collector return path missing for hut={GameDebugLog.Position(hut.Position)}. Deposited instantly to avoid losing {collected} gold.");
                FinishCollector();
                return;
            }

            collector.StartReturn(returnPath, collected);
            GameAudioController.Play(GameSfx.TaxCollect, mazeRenderer.GridToWorld(hut.Position));
            GameDebugLog.Info("Base", $"Tax collector picked up {collected} gold from hut={GameDebugLog.Position(hut.Position)}, returnPathCells={returnPath.Count}.");
        }

        private void DeliverToCastle()
        {
            if (collector == null)
            {
                return;
            }

            var delivered = collector.CarriedGold;
            resources.AddGold(delivered);
            DamageNumberView.CreateText(
                mazeRenderer,
                result.BasePosition,
                $"+{delivered} зол.",
                new Color(1f, 0.84f, 0.26f),
                3.8f);
            GameAudioController.Play(GameSfx.TaxDeposit, mazeRenderer.GridToWorld(result.BasePosition));
            GameDebugLog.Info("Base", $"Tax collector delivered {delivered} gold to castle. treasuryGold={resources.Gold}.");
            FinishCollector();
        }

        private void FinishCollector()
        {
            if (collector != null && collector.TargetHut != null)
            {
                collector.TargetHut.CollectionReserved = false;
            }

            collector?.Destroy();
            collector = null;
        }

        private bool TryBuildCollectorPath(HutTaxRuntime hut, bool returning, out List<Vector3> worldPath)
        {
            var start = returning ? hut.Position : result.BasePosition;
            var end = returning ? result.BasePosition : hut.Position;
            if (!TryBuildOutsideCellPath(start, end, hut.Position, out var cellPath))
            {
                worldPath = null;
                return false;
            }

            worldPath = SubCellPathBuilder.Build(
                mazeRenderer,
                cellPath,
                CollectorYOffset,
                SubCellPathBuilder.BuildSeed(cellPath, returning ? 0x4d91 : 0x1b37),
                SubCellPathProfile.Civilian);

            return worldPath.Count > 1;
        }

        private bool TryBuildOutsideCellPath(Vector2Int start, Vector2Int end, Vector2Int targetHut, out List<Vector2Int> path)
        {
            path = new List<Vector2Int>();
            if (!IsAllowedPathCell(start, targetHut, true) || !IsAllowedPathCell(end, targetHut, true))
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
                if (current == end)
                {
                    break;
                }

                foreach (var direction in MazeDirections.Cardinal)
                {
                    var next = current + direction;
                    if (cameFrom.ContainsKey(next) || !IsAllowedPathCell(next, targetHut, next == end))
                    {
                        continue;
                    }

                    cameFrom[next] = current;
                    frontier.Enqueue(next);
                }
            }

            if (!cameFrom.ContainsKey(end))
            {
                return false;
            }

            var step = end;
            path.Add(step);
            while (step != start)
            {
                step = cameFrom[step];
                path.Add(step);
            }

            path.Reverse();
            return true;
        }

        private bool IsAllowedPathCell(Vector2Int position, Vector2Int targetHut, bool endpoint)
        {
            if (result.Grid.InBounds(position) || !IsInsideTerrain(position))
            {
                return false;
            }

            if (terrainDecorations != null && terrainDecorations.BlocksCityWalker(position))
            {
                return false;
            }

            if (IsInsideFootprint(result.BasePosition, BaseDevelopment.CastleFootprintRadiusCells, position)
                || IsInsideFootprint(targetHut, BaseDevelopment.PeasantHutFootprintRadiusCells, position))
            {
                return true;
            }

            return !IsInsideAnyBuildingFootprint(position, targetHut);
        }

        private bool IsInsideAnyBuildingFootprint(Vector2Int position, Vector2Int targetHut)
        {
            if (IsInsideFootprint(result.BasePosition, BaseDevelopment.CastleFootprintRadiusCells, position))
            {
                return true;
            }

            foreach (var farm in baseDevelopment.FarmPositions)
            {
                if (IsInsideFootprint(farm, BaseDevelopment.FarmFootprintRadiusCells, position))
                {
                    return true;
                }
            }

            foreach (var camp in baseDevelopment.LumberjackCampPositions)
            {
                if (IsInsideFootprint(camp, BaseDevelopment.LumberjackCampFootprintRadiusCells, position))
                {
                    return true;
                }
            }

            foreach (var house in baseDevelopment.HeroHousePositions)
            {
                if (IsInsideFootprint(house, BaseDevelopment.HeroHouseFootprintRadiusCells, position))
                {
                    return true;
                }
            }

            foreach (var hut in baseDevelopment.PeasantHutPositions)
            {
                if (hut != targetHut && IsInsideFootprint(hut, BaseDevelopment.PeasantHutFootprintRadiusCells, position))
                {
                    return true;
                }
            }

            return IsServiceBuildingFootprint(position);
        }

        private bool IsServiceBuildingFootprint(Vector2Int position)
        {
            return (baseDevelopment.HasAlchemistShop && IsInsideFootprint(baseDevelopment.AlchemistShopPosition, BaseDevelopment.AlchemistShopFootprintRadiusCells, position))
                || (baseDevelopment.HasTavern && IsInsideFootprint(baseDevelopment.TavernPosition, BaseDevelopment.TavernFootprintRadiusCells, position))
                || (baseDevelopment.HasForge && IsInsideFootprint(baseDevelopment.ForgePosition, BaseDevelopment.ForgeFootprintRadiusCells, position))
                || (baseDevelopment.HasInfirmary && IsInsideFootprint(baseDevelopment.InfirmaryPosition, BaseDevelopment.InfirmaryFootprintRadiusCells, position))
                || (baseDevelopment.HasCartographerHouse && IsInsideFootprint(baseDevelopment.CartographerHousePosition, BaseDevelopment.CartographerHouseFootprintRadiusCells, position))
                || (baseDevelopment.HasChapel && IsInsideFootprint(baseDevelopment.ChapelPosition, BaseDevelopment.ChapelFootprintRadiusCells, position))
                || (baseDevelopment.HasMinersGuild && IsInsideFootprint(baseDevelopment.MinersGuildPosition, BaseDevelopment.MinersGuildFootprintRadiusCells, position))
                || (baseDevelopment.HasMarket && IsInsideFootprint(baseDevelopment.MarketPosition, BaseDevelopment.MarketFootprintRadiusCells, position))
                || (baseDevelopment.HasAntiquary && IsInsideFootprint(baseDevelopment.AntiquaryPosition, BaseDevelopment.AntiquaryFootprintRadiusCells, position))
                || (baseDevelopment.HasHeroesGuild && IsInsideFootprint(baseDevelopment.HeroesGuildPosition, BaseDevelopment.HeroesGuildFootprintRadiusCells, position));
        }

        private bool IsInsideTerrain(Vector2Int position)
        {
            return position.x >= -MazeTerrain.PaddingCells
                && position.y >= -MazeTerrain.PaddingCells
                && position.x <= result.Grid.Width - 1 + MazeTerrain.PaddingCells
                && position.y <= result.Grid.Height - 1 + MazeTerrain.PaddingCells;
        }

        private void LogPathWarningOnce(HutTaxRuntime hut)
        {
            if (pathWarningPositions.Add(hut.Position))
            {
                GameDebugLog.Warning(
                    "Base",
                    $"Tax collector path not found for hut={GameDebugLog.Position(hut.Position)}, storedGold={hut.StoredGold}, base={GameDebugLog.Position(result.BasePosition)}, terrainPadding={MazeTerrain.PaddingCells}.");
            }
        }

        private Transform BuildCollectorModel(Transform parent)
        {
            var unit = mazeRenderer.ModelUnitSize;
            VoxelVisuals.CreateContactShadow(
                "Tax Collector Contact Shadow",
                parent,
                new Vector3(0f, 0.006f, 0f),
                new Vector3(unit * 0.34f, 0.004f, unit * 0.27f),
                0.32f);
            CreatePart(parent, "Tax Collector Body", PrimitiveType.Capsule, new Vector3(0f, unit * 0.28f, 0f), new Vector3(unit * 0.17f, unit * 0.3f, unit * 0.17f), bodyMaterial);
            CreatePart(parent, "Tax Collector Left Foot", PrimitiveType.Cube, new Vector3(unit * -0.08f, unit * 0.08f, unit * 0.05f), new Vector3(unit * 0.09f, unit * 0.08f, unit * 0.16f), bodyMaterial);
            CreatePart(parent, "Tax Collector Right Foot", PrimitiveType.Cube, new Vector3(unit * 0.08f, unit * 0.08f, unit * 0.05f), new Vector3(unit * 0.09f, unit * 0.08f, unit * 0.16f), bodyMaterial);
            CreatePart(parent, "Tax Collector Head", PrimitiveType.Sphere, new Vector3(0f, unit * 0.66f, 0f), Vector3.one * unit * 0.15f, headMaterial);
            var bag = CreatePart(parent, "Tax Collector Coin Bag", PrimitiveType.Cube, new Vector3(unit * 0.18f, unit * 0.34f, unit * 0.08f), new Vector3(unit * 0.16f, unit * 0.18f, unit * 0.12f), bagMaterial);
            AmbientWalkerMoveAnimator.Attach(parent, unit, BuildCollectorAnimationSeed(parent));
            return bag;
        }

        private Transform CreatePart(Transform parent, string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var part = GameObject.CreatePrimitive(VoxelVisuals.ResolvePrimitive(primitive, name));
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            VoxelVisuals.ApplyBlockStyle(part, primitive, material, false);
            return part.transform;
        }

        private void EnsureMaterials()
        {
            if (bodyMaterial != null)
            {
                return;
            }

            bodyMaterial = CreateMaterial("Tax Collector Body", new Color(0.18f, 0.22f, 0.36f));
            headMaterial = CreateMaterial("Tax Collector Head", new Color(0.78f, 0.67f, 0.48f));
            bagMaterial = CreateMaterial("Tax Collector Bag", new Color(0.95f, 0.68f, 0.16f));
        }

        private static bool IsInsideFootprint(Vector2Int center, int radius, Vector2Int position)
        {
            return Mathf.Abs(position.x - center.x) <= radius && Mathf.Abs(position.y - center.y) <= radius;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            return VoxelVisuals.CreateLitMaterial(name, color);
        }

        private static int BuildCollectorAnimationSeed(Transform parent)
        {
            var position = parent != null ? parent.position : Vector3.zero;
            return Mathf.RoundToInt(position.x * 89f)
                ^ Mathf.RoundToInt(position.y * 47f)
                ^ Mathf.RoundToInt(position.z * 173f)
                ^ 0x2d71;
        }

        private sealed class HutTaxRuntime
        {
            public HutTaxRuntime(Vector2Int position, BuildingView view)
            {
                Position = position;
                View = view;
            }

            public Vector2Int Position { get; }

            public BuildingView View { get; set; }

            public int StoredGold { get; set; }

            public bool CollectionReserved { get; set; }

            public void RefreshLabel()
            {
                View?.SetEffectText($"Налоги: {StoredGold} / {HutTaxCapacity} зол.");
            }
        }

        private sealed class TaxCollectorRuntime
        {
            private const float ArrivalSqrDistance = 0.0025f;

            private readonly GameObject root;
            private readonly Transform bag;
            private List<Vector3> waypoints;
            private int nextWaypoint = 1;

            public TaxCollectorRuntime(GameObject root, List<Vector3> waypoints, HutTaxRuntime targetHut, Transform bag)
            {
                this.root = root;
                this.waypoints = waypoints;
                this.bag = bag;
                TargetHut = targetHut;
                FaceNextWaypoint();
            }

            public HutTaxRuntime TargetHut { get; }

            public bool ReturningToCastle { get; private set; }

            public int CarriedGold { get; private set; }

            public void StartReturn(List<Vector3> returnPath, int carriedGold)
            {
                waypoints = returnPath;
                nextWaypoint = 1;
                ReturningToCastle = true;
                CarriedGold = carriedGold;
                if (root != null && waypoints.Count > 0)
                {
                    root.transform.position = waypoints[0];
                }

                if (bag != null)
                {
                    bag.gameObject.SetActive(true);
                }

                FaceNextWaypoint();
            }

            public bool Move(float distance)
            {
                if (root == null || waypoints == null || nextWaypoint >= waypoints.Count)
                {
                    return true;
                }

                var target = waypoints[nextWaypoint];
                root.transform.position = Vector3.MoveTowards(root.transform.position, target, distance);
                var direction = target - root.transform.position;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    root.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }

                if ((target - root.transform.position).sqrMagnitude > ArrivalSqrDistance)
                {
                    return false;
                }

                root.transform.position = target;
                nextWaypoint++;
                FaceNextWaypoint();
                return nextWaypoint >= waypoints.Count;
            }

            public void Destroy()
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }
            }

            private void FaceNextWaypoint()
            {
                if (root == null || waypoints == null || nextWaypoint >= waypoints.Count)
                {
                    return;
                }

                var direction = waypoints[nextWaypoint] - root.transform.position;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    root.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }
        }
    }
}
