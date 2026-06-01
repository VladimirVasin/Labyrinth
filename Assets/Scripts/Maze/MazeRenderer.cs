using System.Collections.Generic;
using Labyrinth.Base;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public sealed partial class MazeRenderer : MonoBehaviour
    {
        private const float ModelUnit = 1.35f;
        private const float BuildingScale = 2f;
        private const float BuildingUnit = ModelUnit * BuildingScale;
        private const float VisualWallHeightMultiplier = 0.64f;
        private const float VisualWallWidthRatio = 1f;
        private const float VisualFloorWidthRatio = 1f;

        [SerializeField]
        private float cellSize = 1.65f;

        [SerializeField]
        private float wallHeight = 1.35f;

        private Transform root;
        private Material wallMaterial;
        private Material pathMaterial;
        private Material entranceMaterial;
        private Material castleStoneMaterial;
        private Material castleRoofMaterial;
        private Material castleDoorMaterial;
        private Material farmGroundMaterial;
        private Material farmWallMaterial;
        private Material farmRoofMaterial;
        private Material farmCropMaterial;
        private Material farmFenceMaterial;
        private Material heroHouseWallMaterial;
        private Material heroHouseRoofMaterial;
        private Material heroHouseDoorMaterial;
        private Material alchemistWallMaterial;
        private Material alchemistRoofMaterial;
        private Material alchemistBottleMaterial;
        private Material alchemistAccentMaterial;
        private Material tavernWallMaterial;
        private Material tavernRoofMaterial;
        private Material tavernSignMaterial;
        private Material rationMaterial;
        private Material centralDoorMaterial;
        private Material centralDoorMetalMaterial;
        private Material keyGoldMaterial;
        private Material chestWoodMaterial;
        private Material chestDarkWoodMaterial;
        private Material chestMetalMaterial;
        private Material lightingFogMaterial;
        private Material dungeonSeamMaterial;
        private GameObject lightingFogCover;
        private GameObject dungeonSeamUnderlay;
        private readonly Dictionary<Vector2Int, List<Renderer>> cellRenderers = new Dictionary<Vector2Int, List<Renderer>>();
        private readonly Dictionary<Vector2Int, List<Renderer>> externalCellRenderers = new Dictionary<Vector2Int, List<Renderer>>();
        private readonly Dictionary<Vector2Int, bool> cellVisibilityStates = new Dictionary<Vector2Int, bool>();
        private HashSet<Vector2Int> currentExternalVisibleCells;
        private MazeGrid currentExternalVisibilityGrid;
        private bool externalVisibilityMaskActive;

        public float CellSize => cellSize;

        public float ModelUnitSize => ModelUnit;

        public float WallHeight => wallHeight * cellSize * VisualWallHeightMultiplier;

        public Transform ContentRoot => root;

        partial void EnsureVoxelMaterials();

        public BaseView Render(MazeGenerationResult result)
        {
            EnsureMaterials();
            Clear();

            root = new GameObject("MazeRoot").transform;
            root.SetParent(transform, false);

            CreateDungeonSeamUnderlay(result.Grid);
            CreateLightingFogCover(result.Grid);
            foreach (var cell in result.Grid.Cells())
            {
                RenderCell(cell, result.Grid);
            }

            RenderCentralDoors(result);
            RenderKeyPickups(result);
            RenderChests(result);
            OreDepositRenderer.Render(this, result);
            DungeonStairsRenderer.Render(this, result);
            if (result.LevelNumber <= 1 || result.UpStairs == null)
            {
                RenderEntranceMarker(result.EntrancePosition);
            }

            var baseView = RenderBase(result);
            ApplyStaticVoxelLightGrid(result);
            return baseView;
        }

        public Vector3 GridToWorld(Vector2Int gridPosition)
        {
            return new Vector3(gridPosition.x * cellSize, 0f, gridPosition.y * cellSize);
        }

        private float Scale(float value)
        {
            return value * cellSize;
        }

        private static float ScaleModel(float value)
        {
            return value * ModelUnit;
        }

        private static float ScaleBuilding(float value)
        {
            return value * BuildingUnit;
        }

        public BuildingView RenderFarm(Vector2Int farmPosition)
        {
            EnsureMaterials();
            if (root == null)
            {
                return null;
            }

            var farmCenter = GridToWorld(farmPosition);
            var farmRoot = new GameObject($"Farm {farmPosition.x},{farmPosition.y}");
            farmRoot.transform.SetParent(root, false);
            farmRoot.transform.position = farmCenter;
            var buildingView = farmRoot.AddComponent<BuildingView>();
            buildingView.Configure(
                BuildingType.Farm,
                "Ферма",
                "пищевое производство",
                "+1 пища/сек",
                farmPosition,
                BaseDevelopment.FarmFootprintRadiusCells);

            CreateCube(
                "Farm Plot",
                farmCenter + new Vector3(0f, ScaleBuilding(-0.025f), 0f),
                new Vector3(BuildingUnit * 1.36f, ScaleBuilding(0.06f), BuildingUnit * 1.36f),
                farmGroundMaterial,
                farmRoot.transform,
                false);

            CreateCube(
                "Farm House",
                farmCenter + new Vector3(BuildingUnit * -0.28f, ScaleBuilding(0.29f), BuildingUnit * -0.24f),
                new Vector3(BuildingUnit * 0.48f, ScaleBuilding(0.58f), BuildingUnit * 0.46f),
                farmWallMaterial,
                farmRoot.transform,
                true);

            CreateCube(
                "Farm Roof",
                farmCenter + new Vector3(BuildingUnit * -0.28f, ScaleBuilding(0.69f), BuildingUnit * -0.24f),
                new Vector3(BuildingUnit * 0.66f, ScaleBuilding(0.24f), BuildingUnit * 0.62f),
                farmRoofMaterial,
                farmRoot.transform,
                true);

            for (var i = -1; i <= 1; i++)
            {
                CreateCube(
                    "Farm Crop Row",
                    farmCenter + new Vector3(BuildingUnit * 0.32f, ScaleBuilding(0.055f), BuildingUnit * i * 0.24f),
                    new Vector3(BuildingUnit * 0.58f, ScaleBuilding(0.11f), BuildingUnit * 0.07f),
                    farmCropMaterial,
                    farmRoot.transform,
                    false);
            }

            CreateFarmFence(farmRoot.transform, farmCenter);
            return buildingView;
        }

        public BuildingView RenderHeroHouse(Vector2Int housePosition, int heroNumber)
        {
            EnsureMaterials();
            if (root == null)
            {
                return null;
            }

            var houseCenter = GridToWorld(housePosition);
            var houseRoot = new GameObject($"Hero House {heroNumber}");
            houseRoot.transform.SetParent(root, false);
            houseRoot.transform.position = houseCenter;
            var buildingView = houseRoot.AddComponent<BuildingView>();
            buildingView.Configure(
                BuildingType.HeroHouse,
                $"Дом героя {heroNumber}",
                "жилье рыцаря",
                $"Рыцарь {heroNumber}",
                housePosition,
                BaseDevelopment.HeroHouseFootprintRadiusCells);

            CreateCube(
                "Hero House Walls",
                houseCenter + new Vector3(0f, ScaleBuilding(0.42f), 0f),
                new Vector3(BuildingUnit * 0.9f, ScaleBuilding(0.84f), BuildingUnit * 0.78f),
                heroHouseWallMaterial,
                houseRoot.transform,
                true);

            CreateCube(
                "Hero House Roof",
                houseCenter + new Vector3(0f, ScaleBuilding(0.96f), 0f),
                new Vector3(BuildingUnit * 1.08f, ScaleBuilding(0.28f), BuildingUnit * 0.96f),
                heroHouseRoofMaterial,
                houseRoot.transform,
                true);

            CreateCube(
                "Hero House Door",
                houseCenter + new Vector3(BuildingUnit * 0.46f, ScaleBuilding(0.28f), 0f),
                new Vector3(BuildingUnit * 0.08f, ScaleBuilding(0.56f), BuildingUnit * 0.28f),
                heroHouseDoorMaterial,
                houseRoot.transform,
                false);

            CreateCube(
                "Hero House Chimney",
                houseCenter + new Vector3(BuildingUnit * -0.28f, ScaleBuilding(1.2f), BuildingUnit * 0.22f),
                new Vector3(BuildingUnit * 0.16f, ScaleBuilding(0.44f), BuildingUnit * 0.16f),
                castleStoneMaterial,
                houseRoot.transform,
                false);

            CreateCube(
                "Hero House Banner",
                houseCenter + new Vector3(BuildingUnit * 0.52f, ScaleBuilding(0.78f), BuildingUnit * -0.34f),
                new Vector3(BuildingUnit * 0.06f, ScaleBuilding(0.38f), BuildingUnit * 0.28f),
                castleRoofMaterial,
                houseRoot.transform,
                false);
            BuildingDetailRenderer.AddHeroHouseDetails(houseRoot.transform, houseCenter, BuildingUnit);

            return buildingView;
        }

        public BuildingView RenderAlchemistShop(Vector2Int shopPosition)
        {
            EnsureMaterials();
            if (root == null)
            {
                return null;
            }

            var shopCenter = GridToWorld(shopPosition);
            var shopRoot = new GameObject($"Alchemist Shop {shopPosition.x},{shopPosition.y}");
            shopRoot.transform.SetParent(root, false);
            shopRoot.transform.position = shopCenter;
            var buildingView = shopRoot.AddComponent<BuildingView>();
            buildingView.Configure(
                BuildingType.AlchemistShop,
                "Лавка алхимика",
                "зелья и реагенты",
                $"Зелье здоровья: {BaseDevelopment.HealthPotionGoldCost} зол.",
                shopPosition,
                BaseDevelopment.AlchemistShopFootprintRadiusCells);

            CreateCube(
                "Alchemist Shop Walls",
                shopCenter + new Vector3(0f, ScaleBuilding(0.5f), 0f),
                new Vector3(BuildingUnit * 1.05f, ScaleBuilding(1f), BuildingUnit * 0.9f),
                alchemistWallMaterial,
                shopRoot.transform,
                true);

            CreateCube(
                "Alchemist Shop Roof",
                shopCenter + new Vector3(0f, ScaleBuilding(1.1f), 0f),
                new Vector3(BuildingUnit * 1.28f, ScaleBuilding(0.32f), BuildingUnit * 1.08f),
                alchemistRoofMaterial,
                shopRoot.transform,
                true);

            CreateCube(
                "Alchemist Shop Door",
                shopCenter + new Vector3(BuildingUnit * 0.54f, ScaleBuilding(0.33f), 0f),
                new Vector3(BuildingUnit * 0.08f, ScaleBuilding(0.66f), BuildingUnit * 0.3f),
                heroHouseDoorMaterial,
                shopRoot.transform,
                false);

            CreateCube(
                "Alchemist Shop Sign",
                shopCenter + new Vector3(BuildingUnit * 0.58f, ScaleBuilding(0.82f), BuildingUnit * -0.28f),
                new Vector3(BuildingUnit * 0.08f, ScaleBuilding(0.38f), BuildingUnit * 0.32f),
                alchemistAccentMaterial,
                shopRoot.transform,
                false);

            var bottle = GameObject.CreatePrimitive(VoxelVisuals.ResolvePrimitive(PrimitiveType.Sphere, "Potion Bottle"));
            bottle.name = "Potion Bottle";
            bottle.transform.SetParent(shopRoot.transform, false);
            bottle.transform.position = shopCenter + new Vector3(BuildingUnit * -0.28f, ScaleBuilding(1.38f), BuildingUnit * 0.18f);
            bottle.transform.localScale = new Vector3(BuildingUnit * 0.2f, ScaleBuilding(0.28f), BuildingUnit * 0.2f);
            bottle.GetComponent<Renderer>().sharedMaterial = alchemistBottleMaterial;
            RemoveCollider(bottle);
            VoxelVisuals.ApplyBlockStyle(bottle, PrimitiveType.Sphere, alchemistBottleMaterial, false);

            CreateCube(
                "Potion Bottle Cork",
                shopCenter + new Vector3(BuildingUnit * -0.28f, ScaleBuilding(1.56f), BuildingUnit * 0.18f),
                new Vector3(BuildingUnit * 0.08f, ScaleBuilding(0.12f), BuildingUnit * 0.08f),
                heroHouseDoorMaterial,
                shopRoot.transform,
                false);
            BuildingDetailRenderer.AddAlchemistDetails(shopRoot.transform, shopCenter, BuildingUnit);

            return buildingView;
        }

        public BuildingView RenderTavern(Vector2Int tavernPosition)
        {
            EnsureMaterials();
            if (root == null)
            {
                return null;
            }

            var tavernCenter = GridToWorld(tavernPosition);
            var tavernRoot = new GameObject($"Tavern {tavernPosition.x},{tavernPosition.y}");
            tavernRoot.transform.SetParent(root, false);
            tavernRoot.transform.position = tavernCenter;
            var buildingView = tavernRoot.AddComponent<BuildingView>();
            buildingView.Configure(
                BuildingType.Tavern,
                "Харчевня",
                "еда для походов",
                $"Паёк: {BaseDevelopment.RationFoodCost} пищи -> {BaseDevelopment.RationGoldCost} зол.",
                tavernPosition,
                BaseDevelopment.TavernFootprintRadiusCells);

            CreateCube(
                "Tavern Walls",
                tavernCenter + new Vector3(0f, ScaleBuilding(0.5f), 0f),
                new Vector3(BuildingUnit * 1.08f, ScaleBuilding(1f), BuildingUnit * 0.86f),
                tavernWallMaterial,
                tavernRoot.transform,
                true);
            CreateCube(
                "Tavern Roof",
                tavernCenter + new Vector3(0f, ScaleBuilding(1.12f), 0f),
                new Vector3(BuildingUnit * 1.3f, ScaleBuilding(0.34f), BuildingUnit * 1.04f),
                tavernRoofMaterial,
                tavernRoot.transform,
                true);
            CreateCube(
                "Tavern Door",
                tavernCenter + new Vector3(BuildingUnit * 0.56f, ScaleBuilding(0.32f), 0f),
                new Vector3(BuildingUnit * 0.08f, ScaleBuilding(0.64f), BuildingUnit * 0.3f),
                castleDoorMaterial,
                tavernRoot.transform,
                false);
            CreateCube(
                "Tavern Sign",
                tavernCenter + new Vector3(BuildingUnit * 0.6f, ScaleBuilding(0.86f), BuildingUnit * -0.28f),
                new Vector3(BuildingUnit * 0.08f, ScaleBuilding(0.34f), BuildingUnit * 0.36f),
                tavernSignMaterial,
                tavernRoot.transform,
                false);
            CreateCube(
                "Tavern Ration",
                tavernCenter + new Vector3(BuildingUnit * -0.28f, ScaleBuilding(1.35f), BuildingUnit * 0.14f),
                new Vector3(BuildingUnit * 0.38f, ScaleBuilding(0.16f), BuildingUnit * 0.24f),
                rationMaterial,
                tavernRoot.transform,
                false);
            BuildingDetailRenderer.AddTavernDetails(tavernRoot.transform, tavernCenter, BuildingUnit);

            return buildingView;
        }

        public void Clear()
        {
            cellRenderers.Clear();
            externalCellRenderers.Clear();
            cellVisibilityStates.Clear();
            currentExternalVisibleCells = null;
            currentExternalVisibilityGrid = null;
            externalVisibilityMaskActive = false;
            lightingFogCover = null;
            dungeonSeamUnderlay = null;
            if (root == null)
            {
                return;
            }

            Destroy(root.gameObject);
            root = null;
        }

        private void RenderCell(MazeCell cell, MazeGrid grid)
        {
            if (RenderVoxelCell(cell, grid))
            {
                return;
            }

            var cellPosition = new Vector2Int(cell.X, cell.Y);
            var position = GridToWorld(cellPosition);

            if (cell.Type == MazeCellType.Wall)
            {
                var currentWallHeight = WallHeight;
                var wall = CreateCube(
                    "Wall",
                    position + new Vector3(0f, currentWallHeight * 0.5f, 0f),
                    new Vector3(cellSize * VisualWallWidthRatio, currentWallHeight, cellSize * VisualWallWidthRatio),
                    wallMaterial,
                    root,
                    false);
                TrackCellRenderer(cellPosition, wall);
                return;
            }

            var material = pathMaterial;
            if (cell.Type == MazeCellType.Entrance)
            {
                material = entranceMaterial;
            }
            var floor = CreateCube(
                cell.Type.ToString(),
                position + new Vector3(0f, Scale(-0.03f), 0f),
                new Vector3(cellSize * VisualFloorWidthRatio, Scale(0.05f), cellSize * VisualFloorWidthRatio),
                material,
                root,
                false);
            TrackCellRenderer(cellPosition, floor);
        }

        private void RenderCentralDoors(MazeGenerationResult result)
        {
            if (result.CentralDoors == null)
            {
                return;
            }

            foreach (var door in result.CentralDoors)
            {
                if (door == null)
                {
                    continue;
                }

                var doorRoot = new GameObject(door.Name);
                doorRoot.transform.SetParent(root, false);
                var position = GridToWorld(door.Position);
                doorRoot.transform.position = position;

                var slab = CreateCube(
                    "Door Slab",
                    position + new Vector3(0f, Scale(0.48f), 0f),
                    new Vector3(cellSize * 0.18f, Scale(0.96f), cellSize * 0.86f),
                    centralDoorMaterial,
                    doorRoot.transform,
                    true);
                TrackExternalCellRenderer(door.Position, slab);

                for (var i = -1; i <= 1; i++)
                {
                    var bar = CreateCube(
                        "Door Metal Bar",
                        position + new Vector3(0f, Scale(0.48f), cellSize * i * 0.24f),
                        new Vector3(cellSize * 0.22f, Scale(1.08f), cellSize * 0.045f),
                        centralDoorMetalMaterial,
                        doorRoot.transform,
                        false);
                    TrackExternalCellRenderer(door.Position, bar);
                }

                door.AttachVisual(doorRoot);
            }
        }

        private void RenderChests(MazeGenerationResult result)
        {
            if (result.Chests == null)
            {
                return;
            }

            foreach (var chest in result.Chests)
            {
                RenderChest(chest);
            }
        }

        private void RenderChest(ChestModel chest)
        {
            if (chest == null)
            {
                return;
            }

            var chestRoot = new GameObject($"Chest {chest.Position.x},{chest.Position.y}");
            chestRoot.transform.SetParent(root, false);
            var position = GridToWorld(chest.Position);
            chestRoot.transform.position = position;

            var body = CreateCube(
                "Chest Body",
                position + new Vector3(0f, Scale(0.14f), 0f),
                new Vector3(cellSize * 0.52f, Scale(0.28f), cellSize * 0.42f),
                chestWoodMaterial,
                chestRoot.transform,
                false);
            TrackExternalCellRenderer(chest.Position, body);

            var lidPivot = new GameObject("Chest Lid Pivot").transform;
            lidPivot.SetParent(chestRoot.transform, false);
            lidPivot.position = position + new Vector3(0f, Scale(0.31f), cellSize * -0.2f);

            var lid = CreateCube(
                "Chest Lid",
                position + new Vector3(0f, Scale(0.34f), cellSize * 0.02f),
                new Vector3(cellSize * 0.56f, Scale(0.16f), cellSize * 0.46f),
                chestDarkWoodMaterial,
                lidPivot,
                false);
            TrackExternalCellRenderer(chest.Position, lid);

            CreateChestBand(chestRoot.transform, chest.Position, position, -0.22f);
            CreateChestBand(chestRoot.transform, chest.Position, position, 0.22f);

            var lockPlate = CreateCube(
                "Chest Lock",
                position + new Vector3(0f, Scale(0.22f), cellSize * 0.23f),
                new Vector3(cellSize * 0.12f, Scale(0.13f), cellSize * 0.035f),
                keyGoldMaterial,
                chestRoot.transform,
                false);
            TrackExternalCellRenderer(chest.Position, lockPlate);

            var view = chestRoot.AddComponent<ChestView>();
            view.Initialize(lidPivot, ModelUnit);
            chest.AttachView(view);
            var hudTarget = chestRoot.AddComponent<ObjectMicroHudTarget>();
            hudTarget.Configure(
                "Сундук",
                "награда пещеры",
                "Сундук",
                chest.Position,
                new Color(0.94f, 0.68f, 0.24f),
                () => chest.IsOpened ? "открыт" : "закрыт",
                () => chest.IsOpened ? $"Найдено: {BuildChestRewardText(chest)}" : "Рыцарь откроет его, когда зайдет в эту пещеру.");
            var collider = chestRoot.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, Scale(0.24f), 0f);
            collider.size = new Vector3(cellSize * 0.68f, Scale(0.5f), cellSize * 0.58f);
        }

        private static string BuildChestRewardText(ChestModel chest)
        {
            if (chest == null)
            {
                return "-";
            }

            return chest.RewardType == ChestRewardType.Gold
                ? $"+{chest.RewardGold} зол."
                : chest.RewardItemName;
        }

        private void CreateChestBand(Transform parent, Vector2Int cellPosition, Vector3 position, float xOffset)
        {
            var band = CreateCube(
                "Chest Metal Band",
                position + new Vector3(cellSize * xOffset, Scale(0.27f), 0f),
                new Vector3(cellSize * 0.045f, Scale(0.34f), cellSize * 0.48f),
                chestMetalMaterial,
                parent,
                false);
            TrackExternalCellRenderer(cellPosition, band);
        }

        private void RenderEntranceMarker(Vector2Int entrancePosition)
        {
            var position = GridToWorld(entrancePosition);
            var left = CreateCube(
                "Entrance Gate Left",
                position + new Vector3(ModelUnit * -0.28f, ScaleModel(0.42f), ModelUnit * -0.35f),
                new Vector3(ModelUnit * 0.14f, ScaleModel(0.84f), ModelUnit * 0.16f),
                entranceMaterial,
                root,
                false);
            TrackCellRenderer(entrancePosition, left);

            var right = CreateCube(
                "Entrance Gate Right",
                position + new Vector3(ModelUnit * -0.28f, ScaleModel(0.42f), ModelUnit * 0.35f),
                new Vector3(ModelUnit * 0.14f, ScaleModel(0.84f), ModelUnit * 0.16f),
                entranceMaterial,
                root,
                false);
            TrackCellRenderer(entrancePosition, right);
        }

        private BaseView RenderBase(MazeGenerationResult result)
        {
            var baseCenter = GridToWorld(result.BasePosition);
            var castleRoot = new GameObject("Base Castle");
            castleRoot.transform.SetParent(root, false);
            castleRoot.transform.position = baseCenter;
            var baseView = castleRoot.AddComponent<BaseView>();
            baseView.Configure(result);
            var buildingView = castleRoot.AddComponent<BuildingView>();
            buildingView.Configure(
                BuildingType.Castle,
                "Замок",
                "центр базы",
                "управление базой",
                result.BasePosition,
                BaseDevelopment.CastleFootprintRadiusCells);

            CreateCube(
                "Castle Keep",
                baseCenter + new Vector3(BuildingUnit * -0.16f, ScaleBuilding(1.15f), 0f),
                new Vector3(BuildingUnit * 1.35f, ScaleBuilding(2.3f), BuildingUnit * 1.22f),
                castleStoneMaterial,
                castleRoot.transform,
                true);

            CreateCube(
                "Castle Keep Roof",
                baseCenter + new Vector3(BuildingUnit * -0.16f, ScaleBuilding(2.48f), 0f),
                new Vector3(BuildingUnit * 1.55f, ScaleBuilding(0.42f), BuildingUnit * 1.42f),
                castleRoofMaterial,
                castleRoot.transform,
                false);

            CreateCube(
                "Castle Gatehouse",
                baseCenter + new Vector3(BuildingUnit * 0.92f, ScaleBuilding(0.74f), 0f),
                new Vector3(BuildingUnit * 0.94f, ScaleBuilding(1.48f), BuildingUnit * 1.58f),
                castleStoneMaterial,
                castleRoot.transform,
                true);

            CreateCube(
                "Castle Door",
                baseCenter + new Vector3(BuildingUnit * 1.45f, ScaleBuilding(0.48f), 0f),
                new Vector3(BuildingUnit * 0.14f, ScaleBuilding(0.96f), BuildingUnit * 0.66f),
                castleDoorMaterial,
                castleRoot.transform,
                false);

            CreateCastleTower(castleRoot.transform, baseCenter, -0.95f, -0.78f);
            CreateCastleTower(castleRoot.transform, baseCenter, -0.95f, 0.78f);
            CreateCastleTower(castleRoot.transform, baseCenter, 0.92f, -0.78f);
            CreateCastleTower(castleRoot.transform, baseCenter, 0.92f, 0.78f);
            CreateCastleBattlements(castleRoot.transform, baseCenter);

            return baseView;
        }

        private void CreateCastleTower(Transform parent, Vector3 baseCenter, float offsetX, float offsetZ)
        {
            CreateCube(
                "Castle Tower",
                baseCenter + new Vector3(BuildingUnit * offsetX, ScaleBuilding(1.18f), BuildingUnit * offsetZ),
                new Vector3(BuildingUnit * 0.62f, ScaleBuilding(2.36f), BuildingUnit * 0.62f),
                castleStoneMaterial,
                parent,
                true);

            CreateCube(
                "Castle Tower Roof",
                baseCenter + new Vector3(BuildingUnit * offsetX, ScaleBuilding(2.54f), BuildingUnit * offsetZ),
                new Vector3(BuildingUnit * 0.82f, ScaleBuilding(0.42f), BuildingUnit * 0.82f),
                castleRoofMaterial,
                parent,
                false);
        }

        private void CreateCastleBattlements(Transform parent, Vector3 baseCenter)
        {
            for (var i = -2; i <= 2; i++)
            {
                CreateCube(
                    "Castle Battlement",
                    baseCenter + new Vector3(BuildingUnit * (i * 0.28f - 0.16f), ScaleBuilding(2.44f), BuildingUnit * -0.58f),
                    new Vector3(BuildingUnit * 0.2f, ScaleBuilding(0.24f), BuildingUnit * 0.18f),
                    castleStoneMaterial,
                    parent,
                    false);
                CreateCube(
                    "Castle Battlement",
                    baseCenter + new Vector3(BuildingUnit * (i * 0.28f - 0.16f), ScaleBuilding(2.44f), BuildingUnit * 0.58f),
                    new Vector3(BuildingUnit * 0.2f, ScaleBuilding(0.24f), BuildingUnit * 0.18f),
                    castleStoneMaterial,
                    parent,
                    false);
            }
        }

        private void CreateFarmFence(Transform parent, Vector3 farmCenter)
        {
            CreateCube(
                "Farm Fence North",
                farmCenter + new Vector3(0f, ScaleBuilding(0.16f), BuildingUnit * 0.7f),
                new Vector3(BuildingUnit * 1.36f, ScaleBuilding(0.22f), BuildingUnit * 0.06f),
                farmFenceMaterial,
                parent,
                false);
            CreateCube(
                "Farm Fence South",
                farmCenter + new Vector3(0f, ScaleBuilding(0.16f), BuildingUnit * -0.7f),
                new Vector3(BuildingUnit * 1.36f, ScaleBuilding(0.22f), BuildingUnit * 0.06f),
                farmFenceMaterial,
                parent,
                false);
            CreateCube(
                "Farm Fence West",
                farmCenter + new Vector3(BuildingUnit * -0.7f, ScaleBuilding(0.16f), 0f),
                new Vector3(BuildingUnit * 0.06f, ScaleBuilding(0.22f), BuildingUnit * 1.36f),
                farmFenceMaterial,
                parent,
                false);
            CreateCube(
                "Farm Fence East",
                farmCenter + new Vector3(BuildingUnit * 0.7f, ScaleBuilding(0.16f), 0f),
                new Vector3(BuildingUnit * 0.06f, ScaleBuilding(0.22f), BuildingUnit * 1.36f),
                farmFenceMaterial,
                parent,
                false);
        }

        private GameObject CreateCube(
            string objectName,
            Vector3 position,
            Vector3 scale,
            Material material,
            Transform parent,
            bool keepCollider)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;

            if (!keepCollider)
            {
                RemoveCollider(cube);
            }

            VoxelVisuals.ApplyBlockStyle(cube, PrimitiveType.Cube, material, keepCollider);
            return cube;
        }

        private void EnsureMaterials()
        {
            if (wallMaterial != null)
            {
                return;
            }

            wallMaterial = CreateMaterial("Maze Wall", new Color(0.2f, 0.215f, 0.245f));
            pathMaterial = CreateMaterial("Maze Path", new Color(0.68f, 0.66f, 0.58f));
            entranceMaterial = CreateMaterial("Maze Entrance", new Color(0.15f, 0.72f, 0.78f));
            castleStoneMaterial = CreateMaterial("Castle Stone", new Color(0.44f, 0.45f, 0.47f));
            castleRoofMaterial = CreateMaterial("Castle Roof", new Color(0.34f, 0.09f, 0.08f));
            castleDoorMaterial = CreateMaterial("Castle Door", new Color(0.22f, 0.13f, 0.06f));
            farmGroundMaterial = CreateMaterial("Farm Ground", new Color(0.3f, 0.2f, 0.1f));
            farmWallMaterial = CreateMaterial("Farm Wall", new Color(0.68f, 0.48f, 0.26f));
            farmRoofMaterial = CreateMaterial("Farm Roof", new Color(0.48f, 0.12f, 0.08f));
            farmCropMaterial = CreateMaterial("Farm Crop", new Color(0.28f, 0.62f, 0.18f));
            farmFenceMaterial = CreateMaterial("Farm Fence", new Color(0.5f, 0.34f, 0.16f));
            heroHouseWallMaterial = CreateMaterial("Hero House Wall", new Color(0.58f, 0.45f, 0.31f));
            heroHouseRoofMaterial = CreateMaterial("Hero House Roof", new Color(0.26f, 0.12f, 0.08f));
            heroHouseDoorMaterial = CreateMaterial("Hero House Door", new Color(0.18f, 0.1f, 0.05f));
            alchemistWallMaterial = CreateMaterial("Alchemist Shop Wall", new Color(0.44f, 0.38f, 0.56f));
            alchemistRoofMaterial = CreateMaterial("Alchemist Shop Roof", new Color(0.16f, 0.1f, 0.24f));
            alchemistBottleMaterial = VoxelVisuals.CreateEmissiveMaterial("Alchemist Potion Bottle", new Color(0.12f, 0.88f, 0.68f), 1.55f);
            alchemistAccentMaterial = CreateMaterial("Alchemist Accent", new Color(0.72f, 0.9f, 0.36f));
            tavernWallMaterial = CreateMaterial("Tavern Wall", new Color(0.5f, 0.32f, 0.18f));
            tavernRoofMaterial = CreateMaterial("Tavern Roof", new Color(0.3f, 0.12f, 0.07f));
            tavernSignMaterial = CreateMaterial("Tavern Sign", new Color(0.95f, 0.72f, 0.28f));
            rationMaterial = CreateMaterial("Ration Bread", new Color(0.86f, 0.58f, 0.25f));
            centralDoorMaterial = CreateMaterial("Central Door Wood", new Color(0.3f, 0.16f, 0.07f));
            centralDoorMetalMaterial = CreateMaterial("Central Door Metal", new Color(0.09f, 0.1f, 0.11f));
            keyGoldMaterial = VoxelVisuals.CreateEmissiveMaterial("Central Key Gold", new Color(1f, 0.74f, 0.16f), 1.45f);
            chestWoodMaterial = CreateMaterial("Chest Wood", new Color(0.42f, 0.22f, 0.08f));
            chestDarkWoodMaterial = CreateMaterial("Chest Dark Wood", new Color(0.24f, 0.11f, 0.04f));
            chestMetalMaterial = CreateMaterial("Chest Metal", new Color(0.12f, 0.12f, 0.12f));
            lightingFogMaterial = CreateUnlitMaterial("Maze Lighting Fog", new Color(0.005f, 0.006f, 0.008f));
            dungeonSeamMaterial = CreateUnlitMaterial("Dungeon Seam Underlay", new Color(0f, 0f, 0f, 1f));
            EnsureVoxelMaterials();
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            return VoxelVisuals.CreateLitMaterial(materialName, color);
        }

        private static Material CreateUnlitMaterial(string materialName, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                return CreateMaterial(materialName, color);
            }

            var material = new Material(shader)
            {
                name = materialName,
                color = color
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
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

    }
}
